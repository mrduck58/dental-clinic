using System.Text;
using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Chat;

public class SendChatMessageHandler(
    IPatientRepository patientRepository,
    IClinicInfoRepository clinicInfoRepository,
    GetDentistsHandler getDentistsHandler,
    GetDentistSlotsHandler getDentistSlotsHandler,
    CreateAppointmentHandler createAppointmentHandler,
    UpdateAppointmentStatusHandler updateAppointmentStatusHandler,
    IAiChatService aiChatService,
    AppDbContext dbContext,
    ILogger<SendChatMessageHandler> logger)
{
    private const int MaxMessageLength = 2000;
    private const int MaxMessagesPerMinute = 10;
    private const int HistoryMessageCount = 10;

    /// <summary>Số ngày (kể từ hôm nay) đưa vào dữ liệu lịch trống cho bot xem và đặt lịch.</summary>
    private const int AvailabilityDays = 7;

    /// <summary>Số lịch hẹn sắp tới tối đa (của chính chủ + người thân) đưa vào ngữ cảnh cho bot —
    /// đủ dùng để trả lời "tôi có lịch nào sắp tới" và xác định đúng lịch cần hủy.</summary>
    private const int MaxUpcomingAppointments = 20;

    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<SendChatMessageResult> HandleAsync(
        Guid userId, Guid conversationId, string message, string? language = null, CancellationToken ct = default)
    {
        message = message?.Trim() ?? string.Empty;
        if (message.Length == 0)
        {
            throw new ValidationException("Tin nhắn không được để trống.");
        }
        if (message.Length > MaxMessageLength)
        {
            throw new ValidationException($"Tin nhắn quá dài (tối đa {MaxMessageLength} ký tự).");
        }

        var patient = await patientRepository.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy hồ sơ bệnh nhân.");

        var conversation = await dbContext.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null || conversation.PatientId != patient.Id)
        {
            throw new NotFoundException("Không tìm thấy cuộc trò chuyện.");
        }

        // Chatbot là endpoint tốn phí theo từng request AI — chặn spam ở mức bệnh nhân,
        // đếm trên DB để hoạt động đúng cả khi API chạy nhiều instance.
        var oneMinuteAgo = DateTimeOffset.UtcNow.AddMinutes(-1);
        var recentUserMessageCount = await dbContext.ChatMessages.CountAsync(
            m => m.Role == "user" && m.CreatedAt >= oneMinuteAgo &&
                 dbContext.ChatConversations.Any(c => c.Id == m.ConversationId && c.PatientId == patient.Id),
            ct);
        if (recentUserMessageCount >= MaxMessagesPerMinute)
        {
            throw new ValidationException("Bạn đang gửi tin nhắn quá nhanh. Vui lòng chờ một chút rồi thử lại.");
        }

        var recentHistory = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(HistoryMessageCount)
            .ToListAsync(ct);
        recentHistory.Reverse();

        var snapshot = await BuildSnapshotAsync(patient, ct);
        var systemInstruction = BuildSystemInstruction(snapshot, recentHistory, language);

        // Lưu tin nhắn của bệnh nhân TRƯỚC khi gọi AI — nếu AI lỗi thì hội thoại không bị mất tin nhắn.
        dbContext.ChatMessages.Add(ChatMessage.Create(conversation.Id, "user", message));
        conversation.Touch();
        await dbContext.SaveChangesAsync(ct);

        AiChatReply reply;
        try
        {
            reply = await aiChatService.AskAsync(systemInstruction, message, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // AI lỗi (timeout, 429, 5xx, chưa cấu hình...) không được đổ 500 ra client —
            // trả câu xin lỗi thân thiện và giữ nguyên hội thoại.
            logger.LogError(ex, "Gọi AI chat thất bại cho hội thoại {ConversationId}", conversation.Id);
            reply = new AiChatReply(BuildFallbackReply(language), false);
        }

        var bookingHint = ResolveBookingHint(reply, snapshot);

        // Bệnh nhân đã xác nhận đặt/hủy/dời lịch trong hội thoại → thực sự thao tác NGAY TẠI ĐÂY,
        // rồi mới lưu tin nhắn trả lời — nội dung trả lời phản ánh kết quả thật (thành công/hết
        // chỗ/không tìm thấy) thay vì lời hứa của AI. Ba hành động loại trừ nhau trong 1 tin nhắn.
        var bookingCreated = false;
        var bookingCancelled = false;
        var bookingRescheduled = false;
        string? appointmentCode = null;
        if (reply.ConfirmBooking)
        {
            var outcome = await TryCreateBookingAsync(userId, reply, bookingHint, snapshot, language, ct);
            reply = reply with { Reply = outcome.Reply, SuggestBooking = outcome.SuggestBooking };
            bookingCreated = outcome.Created;
            appointmentCode = outcome.AppointmentCode;
        }
        else if (reply.ConfirmCancel)
        {
            var outcome = await TryCancelBookingAsync(reply, snapshot, language, ct);
            reply = reply with { Reply = outcome.Reply, SuggestBooking = outcome.SuggestBooking };
            bookingCancelled = outcome.Cancelled;
            appointmentCode = outcome.AppointmentCode;
        }
        else if (reply.ConfirmReschedule)
        {
            var outcome = await TryRescheduleBookingAsync(userId, reply, snapshot, language, ct);
            reply = reply with { Reply = outcome.Reply, SuggestBooking = outcome.SuggestBooking };
            bookingRescheduled = outcome.Rescheduled;
            appointmentCode = outcome.AppointmentCode;
        }

        dbContext.ChatMessages.Add(ChatMessage.Create(
            conversation.Id, "assistant", reply.Reply,
            suggestBooking: reply.SuggestBooking,
            bookingActionTaken: bookingCreated || bookingCancelled || bookingRescheduled));
        conversation.Touch();
        await dbContext.SaveChangesAsync(ct);

        return new SendChatMessageResult(
            reply.Reply, reply.SuggestBooking, bookingHint, bookingCreated, appointmentCode,
            bookingCancelled, bookingRescheduled);
    }

    private static bool IsEnglish(string? language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

    private static string BuildFallbackReply(string? language) => IsEnglish(language)
        ? "Sorry, I'm unable to answer right now. Please try again in a few minutes."
        : "Xin lỗi, hiện tại tôi chưa thể trả lời. Vui lòng thử lại sau ít phút.";

    private sealed record DentistDayAvailability(Guid DentistId, string FullName, List<string> FreeStartTimes);

    private sealed record DayAvailability(DateOnly Date, List<DentistDayAvailability> Dentists);

    /// <summary>Một lịch hẹn sắp tới (của chính chủ hoặc người thân) — Code dùng để bot tham chiếu
    /// chính xác khi hủy/dời lịch, tránh phải đoán/khớp mờ theo ngày giờ tự nhiên. PatientId/DentistId/
    /// ServiceId dùng để tạo lại lịch hẹn mới khi dời lịch (giữ nguyên bác sĩ/dịch vụ/người khám).</summary>
    private sealed record UpcomingAppointmentInfo(
        Guid AppointmentId, string Code, Guid PatientId, string PatientName, bool IsSelf,
        Guid DentistId, string DentistName, Guid? ServiceId, DateTimeOffset AppointmentDate, string Status);

    private sealed record ClinicSnapshot(
        Domain.Entities.ClinicInfo? ClinicInfo,
        List<Service> Services,
        List<Promotion> Promotions,
        IEnumerable<DentistSummaryDto> Dentists,
        List<Dentist> DentistEntities,
        List<Post> Posts,
        DateOnly Today,
        TimeOnly NowTime,
        List<DayAvailability> Availability,
        List<Patient> FamilyMembers,
        List<UpcomingAppointmentInfo> UpcomingAppointments);

    private async Task<ClinicSnapshot> BuildSnapshotAsync(Patient patient, CancellationToken ct)
    {
        // "Hôm nay" phải theo giờ Việt Nam — dùng UTC thì từ 0h đến 7h sáng bot sẽ quy đổi
        // "ngày mai", "thứ 7 này" lệch một ngày.
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var today = DateOnly.FromDateTime(nowVn);
        var nowTime = TimeOnly.FromDateTime(nowVn);

        var clinicInfo = await clinicInfoRepository.GetAsync(ct);

        var services = await dbContext.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        // GetPromotionsHandler hiện không lọc theo IsActive/khoảng ngày nên không dùng lại được —
        // tự lọc trực tiếp ở đây để chatbot không nhắc tới ưu đãi đã hết hạn hoặc bị tắt.
        var promotions = await dbContext.Promotions
            .Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today)
            .OrderBy(p => p.EndDate)
            .ToListAsync(ct);

        var dentists = await getDentistsHandler.HandleAsync(ct);

        // Bảng Dentists là nguồn Id thật cho lịch hẹn/slot (DentistSummaryDto.Id là User.Id,
        // không dùng được cho CreateAppointment) — cần cho việc đối chiếu tên → DentistId.
        var dentistEntities = await dbContext.Dentists.ToListAsync(ct);

        var posts = await dbContext.Posts
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .Take(5)
            .ToListAsync(ct);

        var availability = await BuildAvailabilityAsync(today, nowTime, ct);

        var familyMembers = (await patientRepository.GetFamilyMembersAsync(patient.Id, ct)).ToList();
        var upcomingAppointments = await BuildUpcomingAppointmentsAsync(patient, familyMembers, ct);

        return new ClinicSnapshot(
            clinicInfo, services, promotions, dentists, dentistEntities, posts, today, nowTime, availability,
            familyMembers, upcomingAppointments);
    }

    /// <summary>Lịch hẹn sắp tới (chưa diễn ra, chưa hủy/hoàn thành) của chính chủ và người thân —
    /// để bot trả lời "tôi có lịch nào sắp tới" và xác định đúng lịch cần hủy qua mã lịch hẹn.</summary>
    private async Task<List<UpcomingAppointmentInfo>> BuildUpcomingAppointmentsAsync(
        Patient patient, List<Patient> familyMembers, CancellationToken ct)
    {
        var relevantPatientIds = new List<Guid> { patient.Id };
        relevantPatientIds.AddRange(familyMembers.Select(f => f.Id));

        var nowUtc = DateTimeOffset.UtcNow;
        var upcoming = await dbContext.Appointments
            .Include(a => a.Dentist)
            .Include(a => a.Patient)
            .Where(a => relevantPatientIds.Contains(a.PatientId) &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                a.AppointmentDate >= nowUtc)
            .OrderBy(a => a.AppointmentDate)
            .Take(MaxUpcomingAppointments)
            .ToListAsync(ct);

        return upcoming.Select(a => new UpcomingAppointmentInfo(
            a.Id,
            BuildAppointmentCode(a.Id, a.AppointmentDate),
            a.PatientId,
            a.Patient.FullName,
            a.PatientId == patient.Id,
            a.DentistId,
            a.Dentist.FullName,
            a.ServiceId,
            a.AppointmentDate,
            a.Status.ToString())).ToList();
    }

    /// <summary>Cùng công thức tạo mã lịch hẹn với <see cref="CreateAppointmentHandler"/> — để bot
    /// đọc lại đúng mã hiển thị trên app khi cần tham chiếu một lịch hẹn cụ thể.</summary>
    private static string BuildAppointmentCode(Guid appointmentId, DateTimeOffset appointmentDate) =>
        $"DK{appointmentDate:yyyyMMdd}{appointmentId.ToString("N")[..6].ToUpper()}";

    /// <summary>
    /// Lịch trống thật của các bác sĩ trong <see cref="AvailabilityDays"/> ngày tới, tính từ cùng
    /// nguồn dữ liệu với màn hình đặt lịch (WorkSchedule + lịch hẹn đã đặt) — để bot trả lời câu hỏi
    /// "còn giờ nào trống" và chỉ đề xuất/đặt các khung giờ đặt được thật. Slot đã qua giờ trong
    /// hôm nay bị loại để bot không mời bệnh nhân đặt vào quá khứ.
    /// </summary>
    private async Task<List<DayAvailability>> BuildAvailabilityAsync(
        DateOnly today, TimeOnly nowTime, CancellationToken ct)
    {
        var days = new List<DayAvailability>(AvailabilityDays);
        for (var offset = 0; offset < AvailabilityDays; offset++)
        {
            var date = today.AddDays(offset);
            var dentistsOfDay = await getDentistSlotsHandler.HandleAsync(date, ct);

            var entries = dentistsOfDay
                .Select(d => new DentistDayAvailability(
                    d.DentistId,
                    d.FullName,
                    d.Slots
                        .Where(s => !s.IsBooked)
                        .Select(s => s.Range.Split('-')[0].Trim())
                        .Where(start => offset > 0 ||
                            (TimeOnly.TryParse(start, out var t) && t > nowTime))
                        .ToList()))
                .Where(d => d.FreeStartTimes.Count > 0)
                .ToList();

            days.Add(new DayAvailability(date, entries));
        }

        return days;
    }

    private static string VietnameseDayOfWeek(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Thứ Hai",
        DayOfWeek.Tuesday => "Thứ Ba",
        DayOfWeek.Wednesday => "Thứ Tư",
        DayOfWeek.Thursday => "Thứ Năm",
        DayOfWeek.Friday => "Thứ Sáu",
        DayOfWeek.Saturday => "Thứ Bảy",
        _ => "Chủ Nhật",
    };

    /// <summary>
    /// Xây dựng system instruction cho Gemini từ snapshot dữ liệu phòng khám hiện tại
    /// (ClinicInfo + dịch vụ/ưu đãi đang active + bác sĩ + tin tức gần đây) cộng thêm guardrail.
    /// </summary>
    private static string BuildSystemInstruction(
        ClinicSnapshot snapshot, IReadOnlyList<ChatMessage> recentHistory, string? language)
    {
        var clinicInfo = snapshot.ClinicInfo;
        var services = snapshot.Services;
        var promotions = snapshot.Promotions;
        var dentists = snapshot.Dentists;
        var posts = snapshot.Posts;
        var today = snapshot.Today;

        var replyLanguage = IsEnglish(language) ? "tiếng Anh" : "tiếng Việt";

        var sb = new StringBuilder();
        sb.AppendLine($"Bạn là trợ lý AI của phòng khám nha khoa, trả lời bằng {replyLanguage}, thân thiện, ngắn gọn, dễ hiểu.");
        sb.AppendLine($"Hôm nay là {VietnameseDayOfWeek(today.DayOfWeek)}, ngày {today:dd/MM/yyyy}.");
        sb.AppendLine("CHỈ trả lời các câu hỏi về thông tin phòng khám: dịch vụ, giá, giờ làm việc, ưu đãi, bác sĩ, lịch trống, đặt lịch hẹn, tin tức, thông tin liên hệ.");
        sb.AppendLine("TUYỆT ĐỐI KHÔNG chẩn đoán bệnh, KHÔNG tư vấn điều trị y khoa cụ thể.");
        sb.AppendLine();
        sb.AppendLine("== Quy tắc trình bày câu trả lời ==");
        sb.AppendLine("- Giữ câu trả lời NGẮN GỌN, tối đa vài câu.");
        sb.AppendLine("- Khi liệt kê từ 2 mục trở lên (dịch vụ, giá, bác sĩ, khung giờ, ưu đãi...), PHẢI xuống dòng và trình bày mỗi mục trên một dòng bắt đầu bằng '- '. KHÔNG viết dồn danh sách thành một đoạn văn dài.");
        sb.AppendLine("- Khi liệt kê khung giờ trống, chỉ nêu tối đa 6 khung giờ gần nhất rồi hỏi bệnh nhân muốn chọn giờ nào.");
        sb.AppendLine();
        sb.AppendLine("== Quy tắc gợi ý đặt lịch (suggestBooking) ==");
        sb.AppendLine("suggestBooking = true CHỈ KHI: bệnh nhân mô tả triệu chứng cần thăm khám (đau, sưng, ê buốt, chảy máu...), hoặc việc đặt lịch qua chat không thực hiện được (ví dụ muốn đặt cho người thân).");
        sb.AppendLine("Khi bệnh nhân chỉ hỏi thông tin chung (giá, dịch vụ, giờ làm việc, bác sĩ, ưu đãi), suggestBooking PHẢI là false — KHÔNG chèo kéo đặt lịch sau mỗi câu trả lời.");
        sb.AppendLine("Trong lúc đang gom thông tin đặt lịch qua chat (các bước bên dưới), suggestBooking cũng để false vì bạn đang tự xử lý việc đặt lịch rồi.");
        sb.AppendLine();
        sb.AppendLine("== Quy trình đặt lịch trực tiếp qua chat ==");
        sb.AppendLine("Bạn CÓ THỂ đặt lịch hẹn trực tiếp cho bệnh nhân, cho CHÍNH CHỦ tài khoản HOẶC cho một người thân đã đăng ký (xem danh sách 'Người thân đã đăng ký' bên dưới nếu có).");
        sb.AppendLine("1. Khi bệnh nhân muốn đặt lịch: nếu tài khoản CÓ người thân đã đăng ký, hỏi rõ đặt lịch này cho ai (chính họ hay tên người thân cụ thể) — đừng tự suy đoán. Nếu KHÔNG có người thân nào, mặc định đặt cho chính chủ, không cần hỏi thêm.");
        sb.AppendLine("2. Gom đủ 3 thông tin bắt buộc: bác sĩ, ngày, giờ. Dịch vụ và ghi chú triệu chứng là tùy chọn. Quy đổi cách nói tương đối ('ngày mai', 'thứ 7 này') sang ngày cụ thể dựa trên hôm nay.");
        sb.AppendLine("3. Chỉ đề xuất và chấp nhận khung giờ CÓ TRONG danh sách 'Lịch trống' bên dưới. Nếu ngày/giờ bệnh nhân muốn không còn trống, nói rõ và đề xuất vài khung giờ gần nhất còn trống.");
        sb.AppendLine("4. Khi đã đủ thông tin, hỏi lại bệnh nhân MỘT câu xác nhận, tóm tắt: đặt cho ai, bác sĩ, ngày, giờ, dịch vụ (nếu có). Lúc này confirmBooking vẫn là false.");
        sb.AppendLine("5. CHỈ sau khi bệnh nhân trả lời đồng ý rõ ràng ('đồng ý', 'ok', 'xác nhận', 'đặt đi'...), đặt confirmBooking = true và điền đầy đủ bookingHint: dentistName, preferredDate (yyyy-MM-dd), preferredTime (HH:mm), patientName (tên người thân nếu đặt hộ, để null nếu đặt cho chính chủ). Khi đó reply chỉ cần một câu ngắn — hệ thống sẽ tự thông báo kết quả đặt lịch thật cho bệnh nhân.");
        sb.AppendLine("6. Khi chưa được xác nhận, TUYỆT ĐỐI không đặt confirmBooking = true và không tự bịa thông tin còn thiếu.");
        sb.AppendLine();
        sb.AppendLine("== Quy trình hủy lịch hẹn qua chat ==");
        sb.AppendLine("Bạn CÓ THỂ hủy lịch hẹn trực tiếp, CHỈ được chọn trong danh sách 'Lịch hẹn sắp tới' bên dưới (của chính chủ hoặc người thân) — KHÔNG được tự bịa mã lịch hẹn.");
        sb.AppendLine("1. Nếu bệnh nhân chỉ nói mơ hồ ('hủy lịch giúp tôi') mà có nhiều hơn 1 lịch sắp tới, hỏi rõ họ muốn hủy lịch nào (đọc lại ngày giờ + bác sĩ + tên người của từng lịch để họ chọn). Nếu chỉ có đúng 1 lịch sắp tới, xác nhận luôn lịch đó.");
        sb.AppendLine("2. Hỏi lại MỘT câu xác nhận trước khi hủy thật (nêu rõ ngày giờ + bác sĩ + tên người). Lúc này confirmCancel vẫn là false.");
        sb.AppendLine("3. CHỈ sau khi bệnh nhân xác nhận đồng ý, đặt confirmCancel = true và cancelAppointmentCode = ĐÚNG mã lịch hẹn (cột 'Mã') lấy từ danh sách bên dưới. Reply chỉ cần một câu ngắn — hệ thống sẽ tự thông báo kết quả hủy thật.");
        sb.AppendLine("4. Nếu không có lịch hẹn nào sắp tới phù hợp, báo cho bệnh nhân biết, KHÔNG đặt confirmCancel = true.");
        sb.AppendLine();
        sb.AppendLine("== Quy trình dời lịch hẹn qua chat ==");
        sb.AppendLine("Khi bệnh nhân muốn ĐỔI NGÀY/GIỜ một lịch hẹn đã có (không phải hủy hẳn), đây là dời lịch — GIỮ NGUYÊN bác sĩ và dịch vụ của lịch hẹn gốc, chỉ đổi ngày/giờ.");
        sb.AppendLine("1. Xác định đúng lịch hẹn cần dời trong danh sách 'Lịch hẹn sắp tới' bên dưới (như quy trình hủy lịch — hỏi rõ nếu có nhiều hơn 1 lịch).");
        sb.AppendLine("2. Hỏi ngày/giờ mới bệnh nhân muốn đổi sang. Chỉ chấp nhận khung giờ CÓ TRONG danh sách 'Lịch trống' của ĐÚNG bác sĩ đang phụ trách lịch hẹn đó.");
        sb.AppendLine("3. Hỏi lại MỘT câu xác nhận (nêu rõ dời từ ngày giờ nào sang ngày giờ nào, với bác sĩ nào). Lúc này confirmReschedule vẫn là false.");
        sb.AppendLine("4. CHỈ sau khi bệnh nhân xác nhận đồng ý, đặt confirmReschedule = true, rescheduleAppointmentCode = ĐÚNG mã lịch hẹn cần dời, và điền bookingHint.preferredDate/preferredTime là ngày/giờ MỚI. Reply chỉ cần một câu ngắn — hệ thống sẽ tự thông báo kết quả dời lịch thật.");
        sb.AppendLine("5. Nếu bệnh nhân muốn đổi CẢ bác sĩ khác (không chỉ đổi giờ), đó không phải dời lịch — hãy xử lý như hủy lịch cũ rồi đặt lịch mới (2 bước riêng biệt qua 2 quy trình ở trên).");
        sb.AppendLine();
        sb.AppendLine("Với bookingHint: chỉ trích xuất những gì bệnh nhân đã nói rõ (dịch vụ, tên bác sĩ, ngày, giờ, tên người thân, mô tả triệu chứng/ghi chú). Trường nào bệnh nhân không nói tới thì để null, KHÔNG tự suy đoán hoặc bịa ra.");
        sb.AppendLine("Nếu câu hỏi không liên quan đến phòng khám, hãy lịch sự từ chối và hướng người dùng quay lại các chủ đề trên.");
        sb.AppendLine("Chỉ trả lời dựa trên dữ liệu được cung cấp bên dưới — không bịa thêm thông tin không có trong dữ liệu.");
        sb.AppendLine();
        sb.AppendLine("Bạn PHẢI trả lời bằng đúng một object JSON hợp lệ, không kèm markdown hay giải thích gì thêm, theo đúng cấu trúc sau (xuống dòng trong reply dùng \\n):");
        sb.AppendLine($$$"""{"reply": "<câu trả lời bằng {{{replyLanguage}}}>", "suggestBooking": <true hoặc false>, "confirmBooking": <true hoặc false>, "confirmCancel": <true hoặc false>, "cancelAppointmentCode": <string hoặc null>, "confirmReschedule": <true hoặc false>, "rescheduleAppointmentCode": <string hoặc null>, "bookingHint": {"serviceName": <string hoặc null>, "dentistName": <string hoặc null>, "preferredDate": <"yyyy-MM-dd" hoặc null>, "preferredTime": <"HH:mm" hoặc null>, "notes": <string hoặc null>, "patientName": <string hoặc null>}}""");
        sb.AppendLine();

        if (clinicInfo is not null)
        {
            sb.AppendLine("== Thông tin phòng khám ==");
            sb.AppendLine($"Địa chỉ: {clinicInfo.Address}");
            sb.AppendLine($"Điện thoại: {clinicInfo.Phone}");
            sb.AppendLine($"Email: {clinicInfo.Email}");
            if (!string.IsNullOrWhiteSpace(clinicInfo.WorkingHours))
            {
                sb.AppendLine($"Giờ làm việc: {clinicInfo.WorkingHours}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("== Dịch vụ đang cung cấp ==");
        foreach (var s in services)
        {
            sb.AppendLine($"- {s.Name}: {s.Price:N0}đ, thời gian khoảng {s.DurationMinutes} phút. {s.Description}");
        }
        sb.AppendLine();

        sb.AppendLine("== Ưu đãi đang áp dụng ==");
        if (promotions.Count == 0)
        {
            sb.AppendLine("(Hiện không có ưu đãi nào đang áp dụng.)");
        }
        foreach (var p in promotions)
        {
            var discount = p.DiscountType == "Percentage" ? $"{p.DiscountValue}%" : $"{p.DiscountValue:N0}đ";
            sb.AppendLine($"- {p.Name} ({p.Code}): giảm {discount}, áp dụng đến {p.EndDate:dd/MM/yyyy}. {p.Description}");
        }
        sb.AppendLine();

        sb.AppendLine("== Đội ngũ bác sĩ ==");
        foreach (var d in dentists)
        {
            sb.AppendLine($"- {d.FullName}, chuyên khoa: {d.Specialty}, {d.YearsOfExperience} năm kinh nghiệm.");
        }
        sb.AppendLine();

        if (posts.Count > 0)
        {
            sb.AppendLine("== Tin tức / bài viết gần đây ==");
            foreach (var post in posts)
            {
                sb.AppendLine($"- {post.Title} ({post.Category})");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"== Lịch trống {AvailabilityDays} ngày tới (giờ bắt đầu các khung 30 phút còn đặt được) ==");
        foreach (var day in snapshot.Availability)
        {
            var dayLabel = $"{VietnameseDayOfWeek(day.Date.DayOfWeek)} {day.Date:dd/MM/yyyy}";
            if (day.Dentists.Count == 0)
            {
                sb.AppendLine($"- {dayLabel}: phòng khám nghỉ hoặc đã kín lịch, không nhận đặt.");
                continue;
            }

            sb.AppendLine($"- {dayLabel}:");
            foreach (var dentist in day.Dentists)
            {
                sb.AppendLine($"  + {dentist.FullName}: {string.Join(", ", dentist.FreeStartTimes)}");
            }
        }
        sb.AppendLine();

        if (snapshot.FamilyMembers.Count > 0)
        {
            sb.AppendLine("== Người thân đã đăng ký (có thể đặt/hủy lịch hộ) ==");
            foreach (var member in snapshot.FamilyMembers)
            {
                var relationship = string.IsNullOrWhiteSpace(member.Relationship) ? "Người thân" : member.Relationship;
                sb.AppendLine($"- {member.FullName} ({relationship})");
            }
            sb.AppendLine();
        }

        sb.AppendLine("== Lịch hẹn sắp tới (chưa diễn ra) của bạn và người thân ==");
        if (snapshot.UpcomingAppointments.Count == 0)
        {
            sb.AppendLine("(Không có lịch hẹn nào sắp tới.)");
        }
        foreach (var appt in snapshot.UpcomingAppointments)
        {
            var who = appt.IsSelf ? "bạn" : appt.PatientName;
            sb.AppendLine($"- Mã {appt.Code}: {who}, BS {appt.DentistName}, {appt.AppointmentDate.UtcDateTime.AddHours(7):HH:mm dd/MM/yyyy}, trạng thái {appt.Status}");
        }
        sb.AppendLine();

        if (recentHistory.Count > 0)
        {
            sb.AppendLine("== Lịch sử hội thoại gần đây ==");
            foreach (var m in recentHistory)
            {
                sb.AppendLine($"{(m.Role == "user" ? "Bệnh nhân" : "Bot")}: {m.Content}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Đối chiếu tên dịch vụ/bác sĩ AI trích xuất (dạng tự nhiên, có thể không khớp tuyệt đối) với dữ liệu
    /// thật đã dùng để build snapshot — chỉ trả về Id khi tìm được một kết quả khớp rõ ràng, tránh mobile
    /// điền sai dữ liệu vào form đặt lịch từ một cái tên AI đoán/viết sai.
    /// </summary>
    private static BookingHintDto ResolveBookingHint(AiChatReply reply, ClinicSnapshot snapshot)
    {
        Guid? serviceId = null;
        string? serviceName = null;
        if (!string.IsNullOrWhiteSpace(reply.ServiceNameHint))
        {
            var match = snapshot.Services.FirstOrDefault(s =>
                s.Name.Contains(reply.ServiceNameHint, StringComparison.OrdinalIgnoreCase) ||
                reply.ServiceNameHint.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                serviceId = match.Id;
                serviceName = match.Name;
            }
        }

        // Đối chiếu với bảng Dentists (không phải danh sách staff user) — Id ở đây mới là
        // DentistId mà slot khả dụng và CreateAppointment sử dụng.
        Guid? dentistId = null;
        string? dentistName = null;
        if (!string.IsNullOrWhiteSpace(reply.DentistNameHint))
        {
            var match = snapshot.DentistEntities.FirstOrDefault(d =>
                !string.IsNullOrWhiteSpace(d.FullName) &&
                (d.FullName.Contains(reply.DentistNameHint, StringComparison.OrdinalIgnoreCase) ||
                 reply.DentistNameHint.Contains(d.FullName, StringComparison.OrdinalIgnoreCase)));
            if (match is not null)
            {
                dentistId = match.Id;
                dentistName = match.FullName;
            }
        }

        // Bỏ qua nếu AI lỡ tính ra một ngày đã qua — mobile không thể chọn ngày quá khứ trong lịch.
        var preferredDate = reply.PreferredDate is { } d2 && d2 >= snapshot.Today ? (DateOnly?)d2 : null;

        // Chỉ đối chiếu với DANH SÁCH NGƯỜI THÂN ĐÃ ĐĂNG KÝ — nếu không khớp được ai (kể cả khi AI
        // đoán bừa một cái tên), coi như đặt cho chính chủ (PatientId = null, hành vi mặc định cũ).
        Guid? targetPatientId = null;
        string? targetPatientName = null;
        if (!string.IsNullOrWhiteSpace(reply.PatientNameHint))
        {
            var match = snapshot.FamilyMembers.FirstOrDefault(m =>
                m.FullName.Contains(reply.PatientNameHint, StringComparison.OrdinalIgnoreCase) ||
                reply.PatientNameHint.Contains(m.FullName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                targetPatientId = match.Id;
                targetPatientName = match.FullName;
            }
        }

        return new BookingHintDto(
            serviceId, serviceName, dentistId, dentistName, preferredDate, reply.NotesHint,
            targetPatientId, targetPatientName);
    }

    private sealed record BookingOutcome(bool Created, string Reply, bool SuggestBooking, string? AppointmentCode);

    /// <summary>
    /// Thực sự tạo lịch hẹn khi AI báo bệnh nhân đã xác nhận (confirmBooking = true). Đối chiếu lại
    /// với lịch trống thật tại thời điểm đặt (không tin dữ liệu AI trích xuất) rồi đi qua đúng
    /// CreateAppointmentHandler như luồng đặt lịch thường — thông báo bác sĩ/lễ tân vẫn được gửi.
    /// Mọi lỗi đều trả về câu trả lời thân thiện thay vì ném exception làm hỏng hội thoại.
    /// </summary>
    private async Task<BookingOutcome> TryCreateBookingAsync(
        Guid userId,
        AiChatReply reply,
        BookingHintDto hint,
        ClinicSnapshot snapshot,
        string? language,
        CancellationToken ct)
    {
        var en = IsEnglish(language);

        if (hint.DentistId is null || reply.PreferredDate is not { } date || reply.PreferredTime is not { } time)
        {
            return new BookingOutcome(false,
                en
                    ? "Sorry, I couldn't complete the booking because the dentist, date or time is still missing or unclear. Please tell me again, or use the Booking screen."
                    : "Xin lỗi, tôi chưa thể đặt lịch vì thông tin bác sĩ, ngày hoặc giờ còn thiếu hoặc chưa rõ. Bạn nhắn lại giúp tôi, hoặc dùng màn hình Đặt lịch nhé.",
                SuggestBooking: true, null);
        }

        // Đối chiếu với lịch trống thật ngay tại thời điểm đặt (không dùng snapshot đã build trước đó) —
        // chặn được cả ngày nghỉ, bác sĩ không có ca, slot vừa bị người khác đặt và giờ đã trôi qua.
        var dentistsOfDay = await getDentistSlotsHandler.HandleAsync(date, ct);
        var dentist = dentistsOfDay.FirstOrDefault(d => d.DentistId == hint.DentistId.Value);
        var timeText = time.ToString("HH:mm");
        var slotFree = dentist is not null && dentist.Slots.Any(
            s => !s.IsBooked && s.Range.StartsWith(timeText, StringComparison.Ordinal));
        var inPast = date < snapshot.Today || (date == snapshot.Today && time <= snapshot.NowTime);

        if (dentist is null || !slotFree || inPast)
        {
            return new BookingOutcome(false,
                en
                    ? $"Sorry, the {timeText} slot on {date:dd/MM/yyyy} is no longer available. Please pick another time from the free slots and I'll book it for you."
                    : $"Xin lỗi, khung giờ {timeText} ngày {date:dd/MM/yyyy} hiện không còn đặt được (có thể vừa có người đặt hoặc bác sĩ không có ca). Bạn chọn giúp tôi một khung giờ khác còn trống nhé, tôi sẽ đặt ngay.",
                SuggestBooking: false, null);
        }

        var appointmentDate = new DateTimeOffset(date.ToDateTime(time), TimeSpan.FromHours(7)).ToUniversalTime();

        try
        {
            var result = await createAppointmentHandler.HandleAsync(new CreateAppointmentCommand(
                userId,
                dentist.DentistId,
                appointmentDate,
                Symptoms: reply.NotesHint,
                ServiceId: hint.ServiceId,
                PatientId: hint.PatientId), ct);

            var confirmation = new StringBuilder();
            if (en)
            {
                confirmation.AppendLine("✅ Your appointment has been booked!");
                confirmation.AppendLine($"- Code: {result.AppointmentCode}");
                if (hint.PatientName is not null)
                {
                    confirmation.AppendLine($"- For: {hint.PatientName}");
                }
                confirmation.AppendLine($"- Dentist: {dentist.FullName}");
                confirmation.AppendLine($"- Time: {timeText}, {date:dd/MM/yyyy}");
                if (hint.ServiceName is not null)
                {
                    confirmation.AppendLine($"- Service: {hint.ServiceName}");
                }
                confirmation.Append("The clinic will confirm it shortly. You can view it in My Appointments.");
            }
            else
            {
                confirmation.AppendLine("✅ Đặt lịch thành công!");
                confirmation.AppendLine($"- Mã lịch hẹn: {result.AppointmentCode}");
                if (hint.PatientName is not null)
                {
                    confirmation.AppendLine($"- Đặt cho: {hint.PatientName}");
                }
                confirmation.AppendLine($"- Bác sĩ: {dentist.FullName}");
                confirmation.AppendLine($"- Thời gian: {timeText}, {VietnameseDayOfWeek(date.DayOfWeek)} {date:dd/MM/yyyy}");
                if (hint.ServiceName is not null)
                {
                    confirmation.AppendLine($"- Dịch vụ: {hint.ServiceName}");
                }
                confirmation.Append("Phòng khám sẽ sớm xác nhận lịch hẹn. Bạn có thể xem chi tiết trong mục Lịch hẹn.");
            }

            return new BookingOutcome(true, confirmation.ToString(), SuggestBooking: false, result.AppointmentCode);
        }
        catch (ConflictException)
        {
            return new BookingOutcome(false,
                en
                    ? $"Sorry, the {timeText} slot on {date:dd/MM/yyyy} was just taken by someone else. Please pick another free slot and I'll book it for you."
                    : $"Xin lỗi, khung giờ {timeText} ngày {date:dd/MM/yyyy} vừa có người khác đặt mất. Bạn chọn giúp tôi một khung giờ khác còn trống nhé, tôi sẽ đặt ngay.",
                SuggestBooking: false, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bot đặt lịch thất bại cho user {UserId} ({Date} {Time})", userId, date, timeText);
            return new BookingOutcome(false,
                en
                    ? "Sorry, something went wrong while booking. Please try again or use the Booking screen."
                    : "Xin lỗi, đã có lỗi khi đặt lịch. Bạn thử lại sau ít phút hoặc dùng màn hình Đặt lịch nhé.",
                SuggestBooking: true, null);
        }
    }

    private sealed record CancelOutcome(bool Cancelled, string Reply, bool SuggestBooking, string? AppointmentCode);

    /// <summary>
    /// Thực sự hủy lịch hẹn khi AI báo bệnh nhân đã xác nhận (confirmCancel = true). Mã lịch hẹn AI đọc
    /// lại chỉ được chấp nhận nếu khớp CHÍNH XÁC với một lịch hẹn sắp tới trong snapshot (của chính chủ
    /// hoặc người thân) — tránh AI bịa mã hoặc hủy nhầm lịch không thuộc về bệnh nhân đang chat.
    /// Việc kiểm tra quyền sở hữu cuối cùng vẫn do <see cref="UpdateAppointmentStatusHandler.CancelAsync"/>
    /// đảm nhiệm (dựa trên JWT của request hiện tại), đây chỉ là lớp đối chiếu dữ liệu ở tầng chatbot.
    /// </summary>
    private async Task<CancelOutcome> TryCancelBookingAsync(
        AiChatReply reply, ClinicSnapshot snapshot, string? language, CancellationToken ct)
    {
        var en = IsEnglish(language);

        var target = string.IsNullOrWhiteSpace(reply.CancelAppointmentCodeHint)
            ? null
            : snapshot.UpcomingAppointments.FirstOrDefault(a =>
                string.Equals(a.Code, reply.CancelAppointmentCodeHint, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            return new CancelOutcome(false,
                en
                    ? "Sorry, I couldn't find that appointment among your upcoming ones. Please check My Appointments or tell me again which one to cancel."
                    : "Xin lỗi, tôi không tìm thấy đúng lịch hẹn đó trong danh sách lịch sắp tới của bạn. Bạn xem lại trong mục Lịch hẹn hoặc nói lại giúp tôi lịch cần hủy nhé.",
                SuggestBooking: false, null);
        }

        try
        {
            await updateAppointmentStatusHandler.CancelAsync(target.AppointmentId, reason: reply.NotesHint, ct);

            var who = target.IsSelf ? (en ? "you" : "bạn") : target.PatientName;
            var confirmation = en
                ? $"✅ Appointment {target.Code} for {who} with {target.DentistName} on " +
                  $"{target.AppointmentDate.UtcDateTime.AddHours(7):dd/MM/yyyy HH:mm} has been cancelled."
                : $"✅ Đã hủy lịch hẹn {target.Code} của {who} với {target.DentistName} lúc " +
                  $"{target.AppointmentDate.UtcDateTime.AddHours(7):HH:mm dd/MM/yyyy}.";

            return new CancelOutcome(true, confirmation, SuggestBooking: false, target.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bot hủy lịch thất bại cho appointment {AppointmentId}", target.AppointmentId);
            return new CancelOutcome(false,
                en
                    ? "Sorry, something went wrong while cancelling. Please try again or use My Appointments."
                    : "Xin lỗi, đã có lỗi khi hủy lịch. Bạn thử lại sau ít phút hoặc dùng mục Lịch hẹn nhé.",
                SuggestBooking: true, null);
        }
    }

    private sealed record RescheduleOutcome(bool Rescheduled, string Reply, bool SuggestBooking, string? AppointmentCode);

    /// <summary>
    /// Thực sự dời lịch hẹn khi AI báo bệnh nhân đã xác nhận (confirmReschedule = true): tạo một lịch
    /// hẹn MỚI ở ngày/giờ mới (giữ nguyên bác sĩ/dịch vụ/người khám của lịch gốc) TRƯỚC, rồi mới hủy
    /// lịch gốc — tạo trước để nếu có lỗi thì bệnh nhân vẫn còn lịch gốc thay vì mất cả hai.
    /// </summary>
    private async Task<RescheduleOutcome> TryRescheduleBookingAsync(
        Guid userId, AiChatReply reply, ClinicSnapshot snapshot, string? language, CancellationToken ct)
    {
        var en = IsEnglish(language);

        var target = string.IsNullOrWhiteSpace(reply.RescheduleAppointmentCodeHint)
            ? null
            : snapshot.UpcomingAppointments.FirstOrDefault(a =>
                string.Equals(a.Code, reply.RescheduleAppointmentCodeHint, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            return new RescheduleOutcome(false,
                en
                    ? "Sorry, I couldn't find that appointment among your upcoming ones. Please check My Appointments or tell me again which one to reschedule."
                    : "Xin lỗi, tôi không tìm thấy đúng lịch hẹn đó trong danh sách lịch sắp tới của bạn. Bạn xem lại trong mục Lịch hẹn hoặc nói lại giúp tôi lịch cần dời nhé.",
                SuggestBooking: false, null);
        }

        if (reply.PreferredDate is not { } date || reply.PreferredTime is not { } time)
        {
            return new RescheduleOutcome(false,
                en
                    ? "Sorry, I still need the new date and time to reschedule this appointment. Please tell me again."
                    : "Xin lỗi, tôi cần bạn cho biết ngày và giờ mới để dời lịch hẹn này. Bạn nhắn lại giúp tôi nhé.",
                SuggestBooking: true, null);
        }

        // Đối chiếu với lịch trống thật của ĐÚNG bác sĩ đang phụ trách lịch hẹn gốc — không đổi bác sĩ.
        var dentistsOfDay = await getDentistSlotsHandler.HandleAsync(date, ct);
        var dentist = dentistsOfDay.FirstOrDefault(d => d.DentistId == target.DentistId);
        var timeText = time.ToString("HH:mm");
        var slotFree = dentist is not null && dentist.Slots.Any(
            s => !s.IsBooked && s.Range.StartsWith(timeText, StringComparison.Ordinal));
        var inPast = date < snapshot.Today || (date == snapshot.Today && time <= snapshot.NowTime);

        if (dentist is null || !slotFree || inPast)
        {
            return new RescheduleOutcome(false,
                en
                    ? $"Sorry, the {timeText} slot on {date:dd/MM/yyyy} is no longer available for {target.DentistName}. Please pick another free slot and I'll reschedule for you."
                    : $"Xin lỗi, khung giờ {timeText} ngày {date:dd/MM/yyyy} của {target.DentistName} hiện không còn trống. Bạn chọn giúp tôi một khung giờ khác còn trống nhé, tôi sẽ dời lịch ngay.",
                SuggestBooking: false, null);
        }

        var newAppointmentDate = new DateTimeOffset(date.ToDateTime(time), TimeSpan.FromHours(7)).ToUniversalTime();

        CreateAppointmentResult created;
        try
        {
            created = await createAppointmentHandler.HandleAsync(new CreateAppointmentCommand(
                userId,
                target.DentistId,
                newAppointmentDate,
                Symptoms: reply.NotesHint,
                ServiceId: target.ServiceId,
                PatientId: target.IsSelf ? null : target.PatientId), ct);
        }
        catch (ConflictException)
        {
            return new RescheduleOutcome(false,
                en
                    ? $"Sorry, the {timeText} slot on {date:dd/MM/yyyy} was just taken by someone else. Please pick another free slot and I'll reschedule for you."
                    : $"Xin lỗi, khung giờ {timeText} ngày {date:dd/MM/yyyy} vừa có người khác đặt mất. Bạn chọn giúp tôi một khung giờ khác còn trống nhé, tôi sẽ dời lịch ngay.",
                SuggestBooking: false, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bot dời lịch thất bại khi tạo lịch mới cho appointment gốc {AppointmentId}", target.AppointmentId);
            return new RescheduleOutcome(false,
                en
                    ? "Sorry, something went wrong while rescheduling. Please try again or use My Appointments."
                    : "Xin lỗi, đã có lỗi khi dời lịch. Bạn thử lại sau ít phút hoặc dùng mục Lịch hẹn nhé.",
                SuggestBooking: true, null);
        }

        var who = target.IsSelf ? (en ? "you" : "bạn") : target.PatientName;
        try
        {
            await updateAppointmentStatusHandler.CancelAsync(
                target.AppointmentId, reason: "Dời sang lịch hẹn mới", ct);
        }
        catch (Exception ex)
        {
            // Lịch mới đã tạo thành công — chỉ log để staff xử lý thủ công lịch cũ, KHÔNG báo lỗi cho
            // bệnh nhân (với họ, việc dời lịch coi như đã xong).
            logger.LogError(ex,
                "Dời lịch: tạo lịch mới {NewId} thành công nhưng hủy lịch cũ {OldId} thất bại",
                created.AppointmentId, target.AppointmentId);
        }

        var confirmation = en
            ? $"✅ Appointment for {who} with {target.DentistName} has been rescheduled to {timeText}, " +
              $"{date:dd/MM/yyyy}.\n- New code: {created.AppointmentCode}"
            : $"✅ Đã dời lịch hẹn của {who} với {target.DentistName} sang {timeText}, " +
              $"{VietnameseDayOfWeek(date.DayOfWeek)} {date:dd/MM/yyyy}.\n- Mã lịch hẹn mới: {created.AppointmentCode}";

        return new RescheduleOutcome(true, confirmation, SuggestBooking: false, created.AppointmentCode);
    }
}
