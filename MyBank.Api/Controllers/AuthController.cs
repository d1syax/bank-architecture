using Microsoft.AspNetCore.Mvc;
using MyBank.Api.Services;

namespace MyBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, token, error) = await _authService.RegisterAsync(
            request.Email, request.Password, request.FullName);

        if (!success)
        {
            if (error == "Email already taken")
                return Conflict(new { error });
            return BadRequest(new { error });
        }

        return StatusCode(201, new { token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, token, error) = await _authService.LoginAsync(
            request.Email, request.Password);

        if (!success)
            return Unauthorized(new { error });

        return Ok(new { token });
    }
}

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);