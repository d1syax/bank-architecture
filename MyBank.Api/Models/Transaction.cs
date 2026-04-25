namespace MyBank.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Account FromAccount { get; set; } = null!;
    public Account ToAccount { get; set; } = null!;
}