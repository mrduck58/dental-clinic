using System.Text;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.AiAssist;

/// <summary>FromCache = true khi trả lại tóm tắt đã tạo trước đó (không gọi lại Gemini) vì chưa có
/// lịch khám mới nào kể từ lần tóm tắt trước — giúp UI có thể hiện chú thích và nút "Làm mới".</summary>
public record PatientHistorySummaryResult(string Summary, string Disclaimer, bool FromCache = false);

public record SummarizePatientHistoryQuery(Guid AppointmentId, bool ForceRegenerate = false)
    : IRequest<PatientHistorySummaryResult>;

/// <summary>
/// Tóm tắt lịch sử khám trước đây của một bệnh nhân bằng AI, hỗ trợ bác sĩ nắm nhanh trước khi khám.
/// Chỉ tổng hợp lại dữ liệu đã có trong hồ sơ (chẩn đoán/liệu trình/đơn thuốc các lần khám cũ) —
/// không tự chẩn đoán mới, không đề xuất phác đồ điều trị. <see cref="PatientHistorySummaryResult.Disclaimer"/>
/// luôn là một hằng số cố định do backend gán, không phụ thuộc vào việc AI có tự nhắc hay không.
/// Kết quả được cache trực tiếp trên <see cref="Appointment"/> (AiSummary/AiSummaryGeneratedAt/
/// AiSummaryBasedOnCount) — chỉ gọi lại Gemini khi có thêm lịch hẹn mới trong lịch sử hoặc khi
/// <paramref name="forceRegenerate"/> = true, tránh tốn chi phí AI cho mỗi lần bác sĩ mở lại trang.
/// </summary>
public class SummarizePatientHistoryHandler(
    IAiChatService aiChatService,
    IAppointmentRepository appointmentRepository,
    ITreatmentPlanRepository treatmentPlanRepository)
    : IRequestHandler<SummarizePatientHistoryQuery, PatientHistorySummaryResult>
{
    private const string DisclaimerText =
        "⚠️ Đây là tóm tắt do AI tạo tự động — vui lòng đối chiếu hồ sơ gốc trước khi đưa ra quyết định điều trị.";

    public async Task<PatientHistorySummaryResult> Handle(
        SummarizePatientHistoryQuery request, CancellationToken ct)
    {
        var appointmentId = request.AppointmentId;
        var forceRegenerate = request.ForceRegenerate;

        var currentAppointment = await appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var pastAppointments = (await appointmentRepository.GetPatientHistoryExcludingAsync(
            currentAppointment.PatientId, appointmentId, ct)).ToList();

        if (pastAppointments.Count == 0)
        {
            return new PatientHistorySummaryResult(
                "Bệnh nhân chưa có lịch sử khám nào trước đây để tóm tắt.",
                DisclaimerText);
        }

        if (!forceRegenerate
            && !string.IsNullOrWhiteSpace(currentAppointment.AiSummary)
            && currentAppointment.AiSummaryBasedOnCount == pastAppointments.Count)
        {
            return new PatientHistorySummaryResult(
                currentAppointment.AiSummary,
                DisclaimerText,
                FromCache: true);
        }

        var plans = (await treatmentPlanRepository.GetByPatientIdAsync(currentAppointment.PatientId, ct)).ToList();
        var historyText = BuildHistoryText(pastAppointments, plans);
        var summary = await aiChatService.SummarizeAsync(
            BuildSystemInstruction(), historyText, feature: "PatientSummary", ct: ct);

        currentAppointment.SetAiSummary(summary, pastAppointments.Count);
        await appointmentRepository.UpdateAsync(currentAppointment, ct);

        return new PatientHistorySummaryResult(summary, DisclaimerText);
    }

    private static string BuildSystemInstruction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý hỗ trợ bác sĩ nha khoa, trả lời bằng tiếng Việt.");
        sb.AppendLine("Nhiệm vụ: tóm tắt lại lịch sử khám bệnh TRƯỚC ĐÂY của một bệnh nhân, dựa HOÀN TOÀN vào dữ liệu được cung cấp bên dưới, để bác sĩ nắm nhanh trước khi khám lần này.");
        sb.AppendLine("TUYỆT ĐỐI KHÔNG tự đưa ra chẩn đoán mới, KHÔNG đề xuất phác đồ điều trị — chỉ tổng hợp và sắp xếp lại những gì đã có trong dữ liệu.");
        sb.AppendLine("Nếu có thông tin quan trọng bác sĩ cần đặc biệt lưu ý (ví dụ dấu hiệu dị ứng thuốc, liệu trình điều trị còn dang dở, triệu chứng lặp lại nhiều lần), hãy nêu bật riêng ở đầu bản tóm tắt.");
        sb.AppendLine("Trình bày ngắn gọn, có cấu trúc rõ ràng (có thể dùng gạch đầu dòng theo mốc thời gian), không lặp lại nguyên văn toàn bộ dữ liệu thô.");
        return sb.ToString();
    }

    private static string BuildHistoryText(List<Appointment> appointments, List<TreatmentPlan> plans)
    {
        var sb = new StringBuilder();

        if (plans.Count > 0)
        {
            sb.AppendLine("== Toàn bộ liệu trình điều trị của bệnh nhân ==");
            foreach (var t in plans)
            {
                var name = string.IsNullOrWhiteSpace(t.Teeth) ? (t.Service?.Name ?? t.Title) : $"{t.Service?.Name ?? t.Title} - Răng {t.Teeth}";
                var cost = t.TotalCost > 0 ? $", chi phí: {t.TotalCost:N0}đ" : "";
                sb.AppendLine($"Liệu trình: {name} — trạng thái: {t.Status}{cost}");
            }
            sb.AppendLine();
        }

        foreach (var a in appointments)
        {
            sb.AppendLine($"== Lần khám ngày {a.AppointmentDate:dd/MM/yyyy} ==");
            if (a.Service is not null)
            {
                sb.AppendLine($"Dịch vụ: {a.Service.Name}");
            }
            if (!string.IsNullOrWhiteSpace(a.Symptoms))
            {
                sb.AppendLine($"Triệu chứng: {a.Symptoms}");
            }

            foreach (var d in a.Diagnoses)
            {
                sb.AppendLine($"Chẩn đoán: {d.Description}");
                if (!string.IsNullOrWhiteSpace(d.Conclusion))
                    sb.AppendLine($"Kết quả & kế hoạch điều trị: {d.Conclusion}");
                if (!string.IsNullOrWhiteSpace(d.MedicalHistory))
                    sb.AppendLine($"Tiền sử bệnh lý: {d.MedicalHistory}");
                if (!string.IsNullOrWhiteSpace(d.AllergyHistory))
                    sb.AppendLine($"Tiền sử dị ứng: {d.AllergyHistory}");
            }

            foreach (var p in a.Prescriptions)
            {
                foreach (var item in p.Items)
                {
                    var note = string.IsNullOrWhiteSpace(item.Notes) ? "" : $" (Ghi chú: {item.Notes})";
                    sb.AppendLine($"Đơn thuốc: {item.MedicineName}, liều {item.Dosage}, cách dùng: {item.Usage}{note}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
