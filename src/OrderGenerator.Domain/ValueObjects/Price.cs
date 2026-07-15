
namespace OrderGenerator.Domain.ValueObjects;

public sealed record Price
{
    public decimal Value { get; }

    private Price(decimal value) => Value = value;

    public static Price Create(decimal value) => new(value);

    public override string ToString() => Value.ToString("F2");
}
