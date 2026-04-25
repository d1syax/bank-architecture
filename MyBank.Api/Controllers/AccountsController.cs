using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBank.Api.Services;

namespace MyBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountsController(AccountService accountService)
    {
        _accountService = accountService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var (success, account, error) = await _accountService.CreateAccountAsync(
            GetUserId(), request.Currency);

        if (!success)
            return BadRequest(new { error });

        return Created($"/api/accounts/{account!.Id}", new
        {
            account.Id,
            account.AccountNumber,
            account.Balance,
            account.Currency
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAccounts()
    {
        var accounts = await _accountService.GetUserAccountsAsync(GetUserId());
        return Ok(accounts.Select(a => new
        {
            a.Id,
            a.AccountNumber,
            a.Balance,
            a.Currency
        }));
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var (success, error) = await _accountService.TransferAsync(
            GetUserId(), request.FromAccountId, request.ToAccountId, request.Amount);

        if (!success)
        {
            if (error!.Contains("not found"))
                return NotFound(new { error });
            return BadRequest(new { error });
        }

        return Ok(new { message = "Transfer successful" });
    }

    [HttpPost("{accountId}/deposit")]
    public async Task<IActionResult> Deposit(int accountId, [FromBody] DepositRequest request)
    {
        var (success, error) = await _accountService.DepositAsync(
            GetUserId(), accountId, request.Amount);

        if (!success)
        {
            if (error!.Contains("not found"))
                return NotFound(new { error });
            return BadRequest(new { error });
        }

        return Ok(new { message = "Deposit successful" });
    }
}

public record CreateAccountRequest(string Currency);
public record TransferRequest(int FromAccountId, int ToAccountId, decimal Amount);
public record DepositRequest(decimal Amount);