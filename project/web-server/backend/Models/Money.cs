namespace WebServer.Models;

/// <summary>
/// Simple money value object mapped to numeric(18,2) in the database.
/// </summary>
public readonly record struct Money(decimal Amount)
{
    public static implicit operator decimal(Money money) => money.Amount;
    public static implicit operator Money(decimal amount) => new Money(amount);
    public override string ToString() => Amount.ToString("0.00");
}
