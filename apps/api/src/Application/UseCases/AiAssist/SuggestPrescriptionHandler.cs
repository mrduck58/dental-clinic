using System.Text;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.AiAssist;

public record PrescriptionSuggestionResult(string Suggestion, string Disclaimer);

public record SuggestPrescriptionQuery(Guid AppointmentId) : IRequest<PrescriptionSuggestionResult>;

/// <summary>
/// Gợi ý đơn thuốc bằng AI, dựa trên phiếu khám (chẩn đoán) VÀ liệu trình điều trị đã ghi nhận cho
/// buổi khám hiện tại — hỗ trợ bác sĩ tham khảo tên thuốc/liều dùng phù hợp, KHÔNG tự động thêm vào
/// đơn thuốc (bác sĩ vẫn phải tự nhập ở form "Kê thuốc" sau khi xem gợi ý).
/// Chỉ dùng dữ liệu của ĐÚNG buổi khám này (không kéo lịch sử các buổi trước như
/// <see cref="SuggestTreatmentHandler"/>) vì yêu cầu gốc chỉ nói "dựa vào chẩn đoán và liệu trình
/// điều trị". Không cache vì chẩn đoán/liệu trình có thể thay đổi trong buổi khám.
/// </summary>
public class SuggestPrescriptionHandler(IAiChatService aiChatService, AppDbContext dbContext)
    : IRequestHandler<SuggestPrescriptionQuery, PrescriptionSuggestionResult>
{
    private const string DisclaimerText =
        "⚠️ Đây là gợi ý tham khảo do AI tạo tự động — bác sĩ PHẢI tự kiểm tra tiền sử dị ứng, chống chỉ định và tương tác thuốc thực tế của bệnh nhân trước khi kê đơn chính thức.";

    public async Task<PrescriptionSuggestionResult> Handle(SuggestPrescriptionQuery request, CancellationToken ct)
    {
        var appointmentId = request.AppointmentId;

        var appointment = await dbContext.Appointments
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var diagnosis = appointment.Diagnoses.FirstOrDefault()
            ?? throw new ValidationException("Cần lưu phiếu khám trước khi tạo gợi ý đơn thuốc.");

        var activeMedicines = await dbContext.Medicines
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

        var prompt = BuildPrompt(appointment, diagnosis, activeMedicines);
        var suggestion = await aiChatService.SummarizeAsync(
            BuildSystemInstruction(), prompt, feature: "PrescriptionSuggestion", ct: ct);

        return new PrescriptionSuggestionResult(suggestion.Trim(), DisclaimerText);
    }

    private static string BuildSystemInstruction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý hỗ trợ bác sĩ nha khoa, trả lời bằng tiếng Việt.");
        sb.AppendLine("Nhiệm vụ: dựa vào chẩn đoán và liệu trình điều trị của buổi khám HIỆN TẠI bên dưới, gợi ý đơn thuốc phù hợp (tên thuốc, liều dùng, cách dùng, số ngày) để HỖ TRỢ bác sĩ tham khảo — bác sĩ vẫn là người quyết định và tự nhập đơn thuốc chính thức.");
        sb.AppendLine("BẮT BUỘC kiểm tra trường \"Tiền sử dị ứng\" trước khi gợi ý: nếu có ghi dị ứng với loại thuốc nào, TUYỆT ĐỐI không gợi ý thuốc cùng nhóm đó, và nêu rõ lý do loại trừ. Nếu tiền sử dị ứng để trống/không rõ, PHẢI nhắc bác sĩ tự hỏi lại bệnh nhân trước khi kê — không được mặc định là không dị ứng.");
        sb.AppendLine("Ưu tiên chọn tên thuốc có trong \"Danh mục thuốc hiện có tại phòng khám\" được cung cấp nếu phù hợp; chỉ gợi ý thuốc ngoài danh mục khi thực sự cần thiết và phải nêu rõ đây là thuốc ngoài danh mục hiện có.");
        sb.AppendLine("Không lặp lại các thuốc đã có sẵn trong đơn (nếu được cung cấp) trừ khi cần điều chỉnh liều.");
        sb.AppendLine("Trình bày ngắn gọn theo gạch đầu dòng, mỗi gạch đầu dòng là 1 thuốc kèm liều dùng/cách dùng/số ngày; có thể thêm 1-2 dòng lưu ý chăm sóc sau điều trị nếu phù hợp.");
        return sb.ToString();
    }

    private static string BuildPrompt(Appointment appointment, Diagnosis diagnosis, List<Medicine> activeMedicines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("== Chẩn đoán buổi khám hiện tại ==");
        if (appointment.Service is not null) sb.AppendLine($"Dịch vụ đăng ký: {appointment.Service.Name}");
        sb.AppendLine($"Chẩn đoán: {diagnosis.Description}");
        AppendIfPresent(sb, "Tình trạng lợi", diagnosis.GumCondition);
        AppendIfPresent(sb, "Chảy máu lợi", diagnosis.GumBleeding);
        AppendIfPresent(sb, "Đau khi chạm / ăn nhai", diagnosis.PainOnChewing);
        AppendIfPresent(sb, "Răng sâu", diagnosis.DecayedTeeth);
        AppendIfPresent(sb, "Răng mòn / nứt / vỡ", diagnosis.WornOrBrokenTeeth);
        AppendIfPresent(sb, "Răng lung lay", diagnosis.LooseTeeth);
        AppendIfPresent(sb, "Cao răng", diagnosis.Tartar);
        AppendIfPresent(sb, "Mảng bám", diagnosis.Plaque);
        AppendIfPresent(sb, "Triệu chứng khớp thái dương hàm", diagnosis.TmjSymptoms);
        AppendIfPresent(sb, "Tiền sử bệnh lý", diagnosis.MedicalHistory);
        sb.AppendLine($"Tiền sử dị ứng: {(string.IsNullOrWhiteSpace(diagnosis.AllergyHistory) ? "(không ghi nhận — cần hỏi lại bệnh nhân)" : diagnosis.AllergyHistory)}");
        AppendIfPresent(sb, "Kết quả & kế hoạch điều trị đã ghi", diagnosis.Conclusion);
        sb.AppendLine();

        if (appointment.TreatmentPlans.Count == 0)
        {
            sb.AppendLine("Chưa có liệu trình điều trị nào được ghi nhận cho buổi khám này.");
        }
        else
        {
            sb.AppendLine("== Liệu trình điều trị buổi khám hiện tại ==");
            foreach (var t in appointment.TreatmentPlans)
            {
                var name = string.IsNullOrWhiteSpace(t.Teeth) ? t.Service.Name : $"{t.Service.Name} - Răng {t.Teeth}";
                sb.AppendLine($"- {name} — trạng thái: {t.Status}");
                if (!string.IsNullOrWhiteSpace(t.Notes)) sb.AppendLine($"  Ghi chú: {t.Notes}");
            }
        }
        sb.AppendLine();

        var existingItems = appointment.Prescriptions.SelectMany(p => p.Items).ToList();
        if (existingItems.Count > 0)
        {
            sb.AppendLine("== Thuốc đã có sẵn trong đơn (tránh lặp lại) ==");
            foreach (var item in existingItems)
            {
                sb.AppendLine($"- {item.MedicineName} ({item.Dosage}, {item.Usage})");
            }
            sb.AppendLine();
        }

        if (activeMedicines.Count > 0)
        {
            sb.AppendLine("== Danh mục thuốc hiện có tại phòng khám ==");
            foreach (var m in activeMedicines)
            {
                var desc = string.IsNullOrWhiteSpace(m.Description) ? "" : $" — {m.Description}";
                sb.AppendLine($"- {m.Name} ({m.Unit}){desc}");
            }
        }

        return sb.ToString();
    }

    private static void AppendIfPresent(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) sb.AppendLine($"{label}: {value}");
    }
}
