using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(LoginHandler loginHandler, CreateAccountHandler createAccountHandler, GetAccountsHandler getAccountsHandler) : ControllerBase
{
    /// <summary>POST api/auth/login — Đăng nhập, nhận JWT</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await loginHandler.HandleAsync(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/auth/accounts — Admin tạo tài khoản cho nhân viên</summary>
    [HttpPost("accounts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await createAccountHandler.HandleAsync(
            new CreateAccountCommand(request.FullName, request.Email, request.PhoneNumber, request.Role),
            cancellationToken);

        return CreatedAtAction(nameof(CreateAccount), new { id = result.Id }, result);
    }

    /// <summary>GET api/auth/accounts — Admin lấy danh sách tài khoản</summary>
    [HttpGet("accounts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var result = await getAccountsHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}
