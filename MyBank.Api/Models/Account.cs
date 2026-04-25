namespace MyBank.Api.Models;

public class Account
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}