using DentalClinic.API.Domain.Schedules;
using FluentAssertions;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Schedules;

/// <summary>
/// Đối chiếu tên giữa bảng xếp lịch và hồ sơ người dùng. Các cặp tên dưới đây lấy từ dữ liệu THẬT:
/// hồ sơ ghi "BS.Đỗ Văn Phong" còn bảng xếp lịch ghi "Đỗ Văn Phong", và phép so chuỗi chính xác
/// trước đây khiến 3/4 bác sĩ có ca làm việc không hiện lên lưới đặt lịch tại quầy.
/// </summary>
[TestFixture]
public class StaffNameMatcherTests
{
    [TestCase("BS.Đỗ Văn Phong",      "Đỗ Văn Phong")]
    [TestCase("BS. Nguyễn Thu Thảo",  "Nguyễn Thu Thảo")]
    [TestCase("BS.Nguyên Đức Vương",  "Nguyên Đức Vương")]
    [TestCase("BS.Đào Tuấn Anh",      "Đào Tuấn Anh")]
    public void RealWorldPrefixMismatch_StillMatches(string profileName, string scheduleName)
    {
        StaffNameMatcher.IsSamePerson(profileName, scheduleName).Should().BeTrue();
    }

    /// <summary>Chức danh viết kiểu gì cũng phải quy về một mối — người dùng gõ tay mỗi lúc một khác.</summary>
    [TestCase("BS. Trần A")]
    [TestCase("BS.Trần A")]
    [TestCase("BS Trần A")]
    [TestCase("B.S. Trần A")]
    [TestCase("Bác sĩ Trần A")]
    [TestCase("bs. trần a")]
    [TestCase("Dr. Trần A")]
    [TestCase("  Trần   A  ")]
    public void TitleAndSpacingVariants_AllMapToSameKey(string written)
    {
        StaffNameMatcher.IsSamePerson(written, "Trần A").Should().BeTrue();
    }

    /// <summary>Hai người khác nhau thì không được gộp — nới lỏng quá tay còn nguy hơn so chính xác.</summary>
    [TestCase("Trần A", "Trần B")]
    [TestCase("Nguyễn Văn Hùng", "Nguyễn Thu Thảo")]
    public void DifferentPeople_DoNotMatch(string a, string b)
    {
        StaffNameMatcher.IsSamePerson(a, b).Should().BeFalse();
    }

    /// <summary>
    /// Tên rỗng không khớp với bất cứ gì, kể cả một tên rỗng khác. Nếu không, mọi dòng lịch thiếu
    /// tên sẽ dính vào nhau và gán bừa cho một bác sĩ nào đó.
    /// </summary>
    [TestCase(null, "Trần A")]
    [TestCase("",   "Trần A")]
    [TestCase("   ", "   ")]
    [TestCase(null, null)]
    public void BlankNames_NeverMatch(string? a, string? b)
    {
        StaffNameMatcher.IsSamePerson(a, b).Should().BeFalse();
    }

    /// <summary>
    /// Tên chỉ có mỗi chức danh thì giữ nguyên bản gốc, không rút thành rỗng — rỗng sẽ khớp bừa
    /// với các dòng lịch không có tên.
    /// </summary>
    [Test]
    public void TitleOnlyName_DoesNotCollapseToEmpty()
    {
        StaffNameMatcher.Key("BS.").Should().NotBeEmpty();
        StaffNameMatcher.IsSamePerson("BS.", "PT.").Should().BeFalse();
    }
}
