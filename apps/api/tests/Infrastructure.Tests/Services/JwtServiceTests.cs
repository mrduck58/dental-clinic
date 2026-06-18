using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Services;
using DentalClinic.API.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class JwtServiceTests
{
    private JwtSettings _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new JwtSettings
        {
            Secret = "super-secret-test-key-at-least-32-chars!!",
            Issuer = "DentalClinic",
            Audience = "DentalClinicApp",
            ExpiryMinutes = 60
        };
    }

    private JwtService CreateSut() => new(Options.Create(_settings));

    /// <summary>
    /// Token hợp lệ phải là chuỗi không rỗng, đúng định dạng JWT 3 phần cách nhau bởi dấu chấm.
    /// </summary>
    [Test]
    public void GenerateToken_ValidUser_ReturnsWellFormedJwt()
    {
        var user = User.Create("testuser", "test@clinic.com", "hash", "Staff");

        var token = CreateSut().GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3, because: "JWT has header.payload.signature format");
    }

    /// <summary>
    /// Claim email phải khớp với email của user.
    /// </summary>
    [Test]
    public void GenerateToken_ContainsCorrectEmailClaim()
    {
        var user = User.Create("testuser", "test@clinic.com", "hash", "Staff");

        var claims = GetClaims(CreateSut().GenerateToken(user));

        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@clinic.com");
    }

    /// <summary>
    /// Claim role phải khớp với role của user.
    /// </summary>
    [Test]
    public void GenerateToken_ContainsCorrectRoleClaim()
    {
        var user = User.Create("dentist01", "dentist@clinic.com", "hash", "Dentist");

        var claims = GetClaims(CreateSut().GenerateToken(user));

        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Dentist");
    }

    /// <summary>
    /// Claim sub phải là Id của user dưới dạng chuỗi.
    /// </summary>
    [Test]
    public void GenerateToken_ContainsSubClaimWithUserId()
    {
        var user = User.Create("testuser", "test@clinic.com", "hash", "Staff");

        var claims = GetClaims(CreateSut().GenerateToken(user));

        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
    }

    /// <summary>
    /// Khi username được thiết lập, claim "username" phải chứa username đó.
    /// </summary>
    [Test]
    public void GenerateToken_UserHasUsername_UsernameClaimContainsUsername()
    {
        var user = User.Create("bsnguyenvan", "nguyenvan@clinic.com", "hash", "Staff");

        var claims = GetClaims(CreateSut().GenerateToken(user));

        claims.Should().Contain(c => c.Type == "username" && c.Value == "bsnguyenvan");
    }

    /// <summary>
    /// Khi username là null (nhân viên chưa có tài khoản), claim "username" phải fallback về email,
    /// đảm bảo token luôn có thông tin định danh hợp lệ.
    /// </summary>
    [Test]
    public void GenerateToken_NullUsername_UsernameClaimFallsBackToEmail()
    {
        var user = User.CreateEmployee("noname@clinic.com", "Staff");

        var claims = GetClaims(CreateSut().GenerateToken(user));

        claims.Should().Contain(c => c.Type == "username" && c.Value == "noname@clinic.com");
    }

    /// <summary>
    /// Token phải hết hạn sau đúng số phút được cấu hình trong JwtSettings.
    /// </summary>
    [Test]
    public void GenerateToken_TokenExpiresAfterConfiguredMinutes()
    {
        _settings = _settings with { ExpiryMinutes = 90 };
        var user = User.Create("testuser", "test@clinic.com", "hash", "Staff");

        var jwt = ReadJwt(CreateSut().GenerateToken(user));

        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(90), precision: TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Issuer và Audience phải khớp với cấu hình JwtSettings.
    /// </summary>
    [Test]
    public void GenerateToken_IssuerAndAudienceMatchSettings()
    {
        var user = User.Create("testuser", "test@clinic.com", "hash", "Staff");

        var jwt = ReadJwt(CreateSut().GenerateToken(user));

        jwt.Issuer.Should().Be("DentalClinic");
        jwt.Audiences.Should().Contain("DentalClinicApp");
    }

    private static IEnumerable<Claim> GetClaims(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;

    private static JwtSecurityToken ReadJwt(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token);
}
