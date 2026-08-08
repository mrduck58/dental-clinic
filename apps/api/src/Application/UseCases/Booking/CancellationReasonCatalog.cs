using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

/// <param name="Code">Giá trị gửi lại khi hủy lịch.</param>
/// <param name="LabelVi">Nhãn hiển thị tiếng Việt.</param>
/// <param name="LabelEn">Nhãn hiển thị tiếng Anh (mobile có song ngữ).</param>
/// <param name="RequiresNote">Client phải bắt nhập ghi chú trước khi cho gửi.</param>
/// <param name="StaffOnly">Chỉ nhân viên phòng khám thấy lựa chọn này.</param>
public record CancellationReasonOption(
    string Code,
    string LabelVi,
    string LabelEn,
    bool RequiresNote,
    bool StaffOnly);

public record GetCancellationReasonsQuery : IRequest<IReadOnlyList<CancellationReasonOption>>;

/// <summary>
/// Danh sách lý do hủy dùng chung cho mobile và admin. Trước đây mỗi client tự chép cứng danh sách
/// của mình (mobile hardcode 4 lý do tiếng Anh rồi tự dịch sang tiếng Việt), nên thêm một lý do
/// đồng nghĩa với phải phát hành lại app — và hai client dễ lệch nhau lúc nào không hay.
///
/// Bệnh nhân và nhân viên nhìn thấy HAI danh sách khác nhau, không phải một danh sách chung bị lọc
/// bớt: lý do của bệnh nhân là lý do cá nhân ("tôi đổi kế hoạch"), còn lễ tân từ chối một yêu cầu
/// đặt lịch thì lý do luôn thuộc phía phòng khám ("bác sĩ nghỉ", "khung giờ đã kín"). Cho lễ tân
/// chọn "tôi đổi kế hoạch" là bắt họ đoán thay bệnh nhân, và làm dữ liệu báo cáo sai lệch.
/// </summary>
public static class CancellationReasonCatalog
{
    private enum Audience { Patient, Staff, Both }

    private record Entry(CancellationReason Reason, string Vi, string En, Audience For, bool RequiresNote = false);

    private static readonly IReadOnlyList<Entry> Entries =
    [
        // Bệnh nhân tự hủy trên app — lý do cá nhân.
        new(CancellationReason.ChangeOfPlans,      "Thay đổi kế hoạch cá nhân",        "Change of plans",              Audience.Patient),
        new(CancellationReason.ScheduleConflict,   "Trùng lịch công việc",             "Schedule conflict",            Audience.Patient),
        new(CancellationReason.HealthIssue,        "Sức khỏe không cho phép đến khám", "Health issue",                 Audience.Patient),
        new(CancellationReason.FoundAnotherClinic, "Chọn phòng khám khác",             "Found another clinic",         Audience.Patient),

        // Lễ tân hủy / từ chối — lý do vận hành, đủ cụ thể để không cần bắt ghi chú thêm.
        new(CancellationReason.PatientRequested,   "Bệnh nhân gọi điện xin hủy",       "Patient called to cancel",     Audience.Staff),
        new(CancellationReason.PatientUnreachable, "Không liên lạc được với bệnh nhân","Patient unreachable",          Audience.Staff),
        new(CancellationReason.DentistUnavailable, "Bác sĩ không thể tiếp nhận",       "Dentist unavailable",          Audience.Staff),
        new(CancellationReason.SlotUnavailable,    "Khung giờ đã kín",                 "Time slot full",               Audience.Staff),
        new(CancellationReason.ClinicClosed,       "Phòng khám nghỉ (lễ / sự cố)",     "Clinic closed",                Audience.Staff),
        new(CancellationReason.DuplicateBooking,   "Lịch đặt trùng",                   "Duplicate booking",            Audience.Staff),

        // Chỉ mục này bắt ghi chú: chọn "khác" mà không nói khác thế nào thì không ghi nhận được gì.
        new(CancellationReason.Other,              "Lý do khác",                       "Other",                        Audience.Both, RequiresNote: true),
    ];

    public static IReadOnlyList<CancellationReasonOption> ForStaff() => Build(Audience.Staff);

    public static IReadOnlyList<CancellationReasonOption> ForPatient() => Build(Audience.Patient);

    private static IReadOnlyList<CancellationReasonOption> Build(Audience audience) =>
        Entries
            .Where(e => e.For == audience || e.For == Audience.Both)
            .Select(e => new CancellationReasonOption(
                e.Reason.ToString(), e.Vi, e.En, e.RequiresNote, StaffOnly: e.For == Audience.Staff))
            .ToList();

    /// <summary>
    /// Nhãn tiếng Việt của một lý do, dùng cho nhật ký và nội dung thông báo. Giá trị cũ đã bỏ khỏi
    /// danh sách chọn (ClinicUnavailable) vẫn còn trong dữ liệu nên phải có đường lùi về tên enum.
    /// </summary>
    public static string LabelOf(CancellationReason reason) =>
        Entries.FirstOrDefault(e => e.Reason == reason)?.Vi ?? reason.ToString();

    /// <summary>
    /// Đổi mã lý do do client gửi lên thành enum.
    ///
    /// Phải parse tay chứ không khai báo thẳng enum trong DTO: dự án không cấu hình
    /// <c>JsonStringEnumConverter</c>, nên System.Text.Json chỉ nhận enum dưới dạng SỐ. Khai báo
    /// enum trong request body sẽ làm mọi chuỗi hợp lệ như "PatientRequested" bị từ chối ở bước bind,
    /// trả 400 trước khi vào tới handler. Cùng cách làm với PaymentsController.ParseGateway và
    /// CreateAccountHandler.
    /// </summary>
    public static CancellationReason Parse(string? code)
    {
        if (!Enum.TryParse<CancellationReason>(code, ignoreCase: true, out var reason))
            throw new ValidationException($"Lý do hủy '{code}' không hợp lệ.");

        return reason;
    }
}

public class GetCancellationReasonsHandler(ICurrentUserService currentUser)
    : IRequestHandler<GetCancellationReasonsQuery, IReadOnlyList<CancellationReasonOption>>
{
    public Task<IReadOnlyList<CancellationReasonOption>> Handle(
        GetCancellationReasonsQuery query, CancellationToken ct) =>
        Task.FromResult(currentUser.UserRole == "Patient"
            ? CancellationReasonCatalog.ForPatient()
            : CancellationReasonCatalog.ForStaff());
}
