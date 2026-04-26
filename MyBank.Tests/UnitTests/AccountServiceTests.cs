using Microsoft.EntityFrameworkCore;
using MyBank.Api.Data;
using MyBank.Api.Models;
using MyBank.Api.Services;

namespace MyBank.Tests.UnitTests;

public class AccountServiceTests
{
    private BankDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new BankDbContext(options);
    }

    [Fact]
    public async Task CreateAccount_ValidCurrency_ReturnsAccount()
    {
        var db = CreateDb();
        var service = new AccountService(db);

        var (success, account, error) = await service.CreateAccountAsync(1, "USD");

        Assert.True(success);
        Assert.NotNull(account);
        Assert.Equal("USD", account.Currency);
        Assert.Equal(0, account.Balance);
    }

    [Fact]
    public async Task CreateAccount_InvalidCurrency_ReturnsError()
    {
        var db = CreateDb();
        var service = new AccountService(db);

        var (success, account, error) = await service.CreateAccountAsync(1, "XYZ");

        Assert.False(success);
        Assert.Null(account);
        Assert.Contains("Unknown currency", error);
    }

    [Fact]
    public async Task Transfer_SufficientFunds_Succeeds()
    {
        var db = CreateDb();
        db.Accounts.AddRange(
            new Account { Id = 1, Balance = 1000, Currency = "USD", UserId = 1, AccountNumber = "AAA" },
            new Account { Id = 2, Balance = 0, Currency = "USD", UserId = 2, AccountNumber = "BBB" }
        );
        await db.SaveChangesAsync();

        var service = new AccountService(db);
        var (success, error) = await service.TransferAsync(1, 1, 2, 300);

        Assert.True(success);
        Assert.Equal(700, db.Accounts.Find(1)!.Balance);
        Assert.Equal(300, db.Accounts.Find(2)!.Balance);
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_ReturnsError()
    {
        var db = CreateDb();
        db.Accounts.AddRange(
            new Account { Id = 1, Balance = 100, Currency = "USD", UserId = 1, AccountNumber = "AAA" },
            new Account { Id = 2, Balance = 0, Currency = "USD", UserId = 2, AccountNumber = "BBB" }
        );
        await db.SaveChangesAsync();

        var service = new AccountService(db);
        var (success, error) = await service.TransferAsync(1, 1, 2, 500);

        Assert.False(success);
        Assert.Equal("Insufficient funds", error);
    }

    [Fact]
    public async Task Transfer_NegativeAmount_ReturnsError()
    {
        var db = CreateDb();
        var service = new AccountService(db);

        var (success, error) = await service.TransferAsync(1, 1, 2, -100);

        Assert.False(success);
        Assert.Contains("greater than zero", error);
    }
}