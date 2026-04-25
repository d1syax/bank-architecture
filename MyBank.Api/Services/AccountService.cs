using Microsoft.EntityFrameworkCore;
using MyBank.Api.Data;
using MyBank.Api.Models;

namespace MyBank.Api.Services;

public class AccountService
{
    private readonly BankDbContext _db;

    public AccountService(BankDbContext db)
    {
        _db = db;
    }

    private static readonly string[] AllowedCurrencies = ["USD", "UAH", "EUR"];

    public async Task<(bool Success, Account? Account, string? Error)> CreateAccountAsync(
        int userId, string currency)
    {
        if (!AllowedCurrencies.Contains(currency.ToUpper()))
            return (false, null, $"Unknown currency. Allowed: {string.Join(", ", AllowedCurrencies)}");

        var account = new Account
        {
            AccountNumber = Guid.NewGuid().ToString("N")[..16].ToUpper(),
            Balance = 0,
            Currency = currency.ToUpper(),
            UserId = userId
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        return (true, account, null);
    }

    public async Task<List<Account>> GetUserAccountsAsync(int userId)
    {
        return await _db.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> TransferAsync(
        int userId, int fromAccountId, int toAccountId, decimal amount)
    {
        if (amount <= 0)
            return (false, "Transfer amount must be greater than zero");

        var fromAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == fromAccountId);
        if (fromAccount == null)
            return (false, "Source account not found");

        if (fromAccount.UserId != userId)
            return (false, "Source account not found");

        var toAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == toAccountId);
        if (toAccount == null)
            return (false, "Destination account not found");

        if (fromAccount.Balance < amount)
            return (false, "Insufficient funds");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            fromAccount.Balance -= amount;
            toAccount.Balance += amount;

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                Amount = amount,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            return (false, "Transfer failed");
        }
    }

    public async Task<(bool Success, string? Error)> DepositAsync(
        int userId, int accountId, decimal amount)
    {
        if (amount <= 0)
            return (false, "Amount must be greater than zero");

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        if (account == null)
            return (false, "Account not found");

        if (account.UserId != userId)
            return (false, "Account not found");

        account.Balance += amount;
        await _db.SaveChangesAsync();

        return (true, null);
    }
}