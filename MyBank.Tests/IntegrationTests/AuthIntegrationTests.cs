using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyBank.Api.Data;

namespace MyBank.Tests.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<BankDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<BankDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Register_ValidData_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "test@example.com",
            password = "password123",
            fullName = "Test User"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body!["token"]);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var payload = new { email = "dup@example.com", password = "pass123", fullName = "User" };
        await _client.PostAsJsonAsync("/api/auth/register", payload);
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "password123",
            fullName = "Test User"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "login@example.com",
            password = "correctpass",
            fullName = "User"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login@example.com",
            password = "wrongpass"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}