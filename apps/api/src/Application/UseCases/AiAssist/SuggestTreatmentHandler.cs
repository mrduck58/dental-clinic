using System.Text;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.AiAssist;

public record TreatmentSuggestionResult(string Suggestion, string Disclaimer);

public record SuggestTreatmentQuery(Guid AppointmentId) : IRequest<TreatmentSuggestionResult>;

/// <summary>
/// Gợi ý hướng điều trị bằng AI, dựa trên phiếu khám (chẩn đoán) VỪA được lưu cho buổi khám hiện tại
/// và tóm tắt lịch sử khám trước đây của bệnh nhân — hỗ trợ bác sĩ tham khảo thêm trong lúc khám,
/// KHÔNG thay thế chỉ định chuyên môn. Bắt buộc phải có phiếu khám đã lưu cho buổi hẹn này (dùng
/// dữ liệu đã lưu ở DB, không nhận trực tiếp dữ liệu form chưa lưu từ client).
/// Không cache như <see cref="SummarizePatientHistoryHandler"/> vì chẩn đoán có thể được bác sĩ
/// chỉnh sửa nhiều lần trong buổi khám — mỗi lần bấm sẽ tạo gợi ý mới theo dữ liệu mới nhất.
/// </summary>
public class SuggestTreatmentHandler(IAiChatService aiChatService, IAppointmentRepository appointmentRepository)
    : IRequestHandler<SuggestTreatmentQuery, TreatmentSuggestionResult>
{
    private const string DisclaimerText =
        "⚠️ Đây là gợi ý tham khảo do AI tạo tự động, không thay thế chỉ định chuyên môn — bác sĩ cần đối chiếu tình trạng thực tế của bệnh nhân trước khi quyết định điều trị.";

    public async Task<TreatmentSuggestionResult> Handle(SuggestTreatmentQuery request, CancellationToken ct)
    {
        var appointmentId = request.AppointmentId;

        var currentAppointment = await appointmentRepository.GetForTreatmentSuggestionAsync(appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var diagnosis = currentAppointment.Diagnoses.FirstOrDefault()
            ?? throw new ValidationException("Cần lưu phiếu khám trước khi tạo gợi ý điều trị.");

        var pastAppointments = (await appointmentRepository.GetPatientHistoryExcludingAsync(
            currentAppointment.PatientId, appointmentId, ct)).ToList();

        var prompt = BuildPrompt(currentAppointment, diagnosis, pastAppointments);
        var suggestion = await aiChatService.SummarizeAsync(
            BuildSystemInstruction(), prompt, feature: "TreatmentSuggestion", ct: ct);

        return new TreatmentSuggestionResult(suggestion.Trim(), DisclaimerText);
    }

    private static string BuildSystemInstruction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý hỗ trợ bác sĩ nha khoa, trả lời bằng tiếng Việt.");
        sb.AppendLine("Nhiệm vụ: dựa vào phiếu khám/chẩn đoán của buổi khám HIỆN TẠI và tóm tắt lịch sử khám trước đây (nếu có) được cung cấp bên dưới, đề xuất các hướng điều trị / lời khuyên phù hợp để HỖ TRỢ bác sĩ tham khảo thêm.");
        sb.AppendLine("Đây là công cụ HỖ TRỢ tham khảo cho bác sĩ đã có chuyên môn — KHÔNG khẳng định chắc chắn thay bác sĩ, KHÔNG kê tên thuốc/liều lượng cụ thể, KHÔNG thay thế quyết định lâm sàng cuối cùng của bác sĩ.");
        sb.AppendLine("Nếu dữ liệu chưa đủ để đề xuất rõ ràng, hãy nêu cần thăm khám/cận lâm sàng thêm gì thay vì đoán bừa.");
        sb.AppendLine("Nêu bật các điểm bác sĩ cần đặc biệt lưu ý trước (vd: tiền sử dị ứng, bệnh lý nền ảnh hưởng điều trị/gây tê), sau đó mới đến các hướng điều trị đề xuất — trình bày ngắn gọn theo gạch đầu dòng.");
        return sb.ToString();
    }

    private static string BuildPrompt(Appointment current, Diagnosis diagnosis, List<Appointment> pastAppointments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("== Phiếu khám buổi hiện tại ==");
        if (current.Service is not null) sb.AppendLine($"Dịch vụ đăng ký: {current.Service.Name}");
        if (!string.IsNullOrWhiteSpace(current.Symptoms)) sb.AppendLine($"Lý do đến khám: {current.Symptoms}");
        sb.AppendLine($"Chẩn đoán: {diagnosis.Description}");
        AppendIfPresent(sb, "Tình trạng lợi", diagnosis.GumCondition);
        AppendIfPresent(sb, "Tình trạng niêm mạc miệng", diagnosis.OralMucosaCondition);
        AppendIfPresent(sb, "Chảy máu lợi", diagnosis.GumBleeding);
        AppendIfPresent(sb, "Đau khi chạm / ăn nhai", diagnosis.PainOnChewing);
        AppendIfPresent(sb, "Số răng hiện có", diagnosis.TeethCount);
        AppendIfPresent(sb, "Răng sâu", diagnosis.DecayedTeeth);
        AppendIfPresent(sb, "Răng mòn / nứt / vỡ", diagnosis.WornOrBrokenTeeth);
        AppendIfPresent(sb, "Răng lung lay", diagnosis.LooseTeeth);
        AppendIfPresent(sb, "Cao răng", diagnosis.Tartar);
        AppendIfPresent(sb, "Mảng bám", diagnosis.Plaque);
        AppendIfPresent(sb, "Mùi hôi miệng", diagnosis.BadBreath);
        AppendIfPresent(sb, "Triệu chứng khớp thái dương hàm", diagnosis.TmjSymptoms);
        AppendIfPresent(sb, "Khớp cắn", diagnosis.Occlusion);
        AppendIfPresent(sb, "Sai lệch khớp cắn", diagnosis.OcclusionDeviation);
        AppendIfPresent(sb, "Tiền sử bệnh lý", diagnosis.MedicalHistory);
        AppendIfPresent(sb, "Tiền sử dị ứng", diagnosis.AllergyHistory);
        AppendIfPresent(sb, "Kết quả & kế hoạch điều trị đã ghi", diagnosis.Conclusion);
        sb.AppendLine();

        if (pastAppointments.Count == 0)
        {
            sb.AppendLine("Bệnh nhân chưa có lịch sử khám nào trước đây.");
            return sb.ToString();
        }

        sb.AppendLine("== Tóm tắt lịch sử khám trước đây ==");
        foreach (var a in pastAppointments)
        {
            sb.AppendLine($"- Ngày {a.AppointmentDate:dd/MM/yyyy}: {a.Service?.Name ?? ""}");
            foreach (var d in a.Diagnoses)
            {
                if (!string.IsNullOrWhiteSpace(d.Description)) sb.AppendLine($"  Chẩn đoán: {d.Description}");
                if (!string.IsNullOrWhiteSpace(d.Conclusion)) sb.AppendLine($"  Kết luận: {d.Conclusion}");
                if (!string.IsNullOrWhiteSpace(d.AllergyHistory)) sb.AppendLine($"  Dị ứng: {d.AllergyHistory}");
            }
            foreach (var t in a.TreatmentPlans)
            {
                sb.AppendLine($"  Liệu trình: {t.Service.Name} — trạng thái: {t.Status}");
            }
            foreach (var p in a.Prescriptions)
            {
                foreach (var item in p.Items)
                {
                    sb.AppendLine($"  Đơn thuốc: {item.MedicineName}, liều {item.Dosage}");
                }
            }
        }

        return sb.ToString();
    }

    private static void AppendIfPresent(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) sb.AppendLine($"{label}: {value}");
    }
}
