using System.Text.RegularExpressions;

namespace DentalClinic.API.Domain.Schedules;

/// <summary>
/// Đối chiếu tên nhân sự giữa <c>WorkSchedules.StaffName</c> (chuỗi tự do, do người dùng gõ hoặc
/// import từ Excel) và <c>Users.FullName</c>.
///
/// <para>
/// Tồn tại vì hai nguồn này viết tên KHÔNG giống nhau trong thực tế: hồ sơ người dùng ghi
/// "BS.Đỗ Văn Phong" còn bảng xếp lịch ghi "Đỗ Văn Phong", thậm chí ngay trong cùng bảng Users cũng
/// có chỗ "BS." liền, chỗ "BS. " có dấu cách. Trước đây việc so khớp dùng phép so chuỗi chính xác,
/// nên mọi khác biệt kiểu này khiến bác sĩ BIẾN MẤT khỏi lưới đặt lịch mà không có lỗi nào được
/// báo ra — lịch làm việc hiện 4 bác sĩ, quầy lễ tân chỉ chọn được 1.
/// </para>
///
/// <para>
/// Đây là lưới an toàn cho dữ liệu cũ, KHÔNG phải cách nối quan hệ chính thức: từ nay
/// <c>WorkSchedule.EmployeeId</c> mới là khóa thật. Chỉ những dòng chưa gán được nhân sự mới rơi
/// xuống đối chiếu theo tên.
/// </para>
/// </summary>
public static class StaffNameMatcher
{
    /// <summary>
    /// Chức danh người dùng hay gõ kèm tên. Bắt buộc phải có dấu chấm hoặc khoảng trắng ngăn cách
    /// sau chức danh — nếu không, một cái tên bắt đầu bằng đúng những chữ cái đó (vd "Tsering")
    /// sẽ bị cắt cụt và gộp nhầm với người khác.
    ///
    /// Dấu tiếng Việt được GIỮ NGUYÊN ở bước chuẩn hoá: bỏ dấu sẽ gộp "Nguyên" với "Nguyễn",
    /// hai cái tên thực sự khác nhau.
    /// </summary>
    private static readonly Regex TitlePrefix = new(
        @"^(bs|b\.s|bác\s*sĩ|bac\s*si|pt|phụ\s*tá|ths|ts|đd|điều\s*dưỡng|dr)\s*(\.|\s)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Khóa đối chiếu của một cái tên: bỏ chức danh đứng đầu, gom dấu cách thừa, bỏ phân biệt hoa
    /// thường. Tên rỗng trả về chuỗi rỗng — đừng dùng nó để so khớp, vì mọi tên rỗng sẽ khớp nhau.
    /// </summary>
    public static string Key(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var stripped = TitlePrefix.Replace(name.Trim(), "");

        // Tên chỉ gồm mỗi chức danh ("BS.") thì giữ nguyên bản gốc còn hơn trả về rỗng — rỗng sẽ
        // khớp bừa với mọi dòng lịch không có tên.
        if (string.IsNullOrWhiteSpace(stripped)) stripped = name.Trim();

        return WhitespaceRun.Replace(stripped, " ").ToLowerInvariant();
    }

    /// <summary>Hai cái tên có cùng chỉ một người không? Tên rỗng không khớp với bất cứ gì.</summary>
    public static bool IsSamePerson(string? a, string? b)
    {
        var keyA = Key(a);
        return keyA.Length > 0 && keyA == Key(b);
    }
}
