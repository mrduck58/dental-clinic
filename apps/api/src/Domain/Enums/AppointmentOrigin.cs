namespace DentalClinic.API.Domain.Enums;

/// <summary>
/// Lịch hẹn được sinh ra từ đâu. Cần lưu lại vì hai nguồn có LỊCH SỬ TRẠNG THÁI khác hẳn nhau:
/// lịch đặt từ xa đi qua Pending → Confirmed rồi mới CheckedIn, còn lịch lập tại quầy vào thẳng
/// CheckedIn. Khi hoàn tác một lần check-in bấm nhầm, chỉ nguồn mới trả lời được câu hỏi
/// "quay về trạng thái nào" — lịch tại quầy không có trạng thái nào trước đó để quay về.
/// </summary>
public enum AppointmentOrigin
{
    /// <summary>Bệnh nhân tự đặt trên app/website (hoặc qua chatbot) — có bước chờ xác nhận.</summary>
    Online,

    /// <summary>Lễ tân lập tại quầy khi bệnh nhân đã có mặt: đặt lịch vãng lai, check-in tái khám.</summary>
    WalkIn,
}
