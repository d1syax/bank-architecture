using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
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
                var descriptors = services
                    .Where(d =>
                        d.ServiceType.FullName != null && (
                        d.ServiceType.FullName.Contains("DbContext") ||
                        d.ServiceType.FullName.Contains("Npgsql") ||
                        d.ServiceType.FullName.Contains("EntityFramework")))
                    .ToList();
                foreach (var d in descriptors) services.Remove(d);

                services.AddDbContext<BankDbContext>(options =>
                    options.UseInMemoryDatabase("DbName")
                           .ConfigureWarnings(w => w.Ignore(
                               Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Register_ValidData_Returns201WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "test@example.com",
            password = "password123",
            fullName = "Test User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body!["token"]);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dup_{Guid.NewGuid()}@example.com";
        var payload = new { email, password = "pass123", fullName = "User" };

        var first = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        
        var second = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
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