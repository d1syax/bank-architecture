using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyBank.Api.Data;
using MyBank.Api.Models;

namespace MyBank.Api.Services;

public class AuthService
{
    private readonly BankDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(BankDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(bool Success, string? Token, string? Error)> RegisterAsync(string email, string password, string fullName)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return (false, null, "Invalid email");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, null, "Password must be at least 6 characters");

        if (string.IsNullOrWhiteSpace(fullName))
            return (false, null, "Full name is required");

        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists)
            return (false, null, "Email already taken");

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return (true, GenerateToken(user), null);
    }

    public async Task<(bool Success, string? Token, string? Error)> LoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return (false, null, "Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, null, "Invalid credentials");

        return (true, GenerateToken(user), null);
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}