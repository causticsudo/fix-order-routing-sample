
namespace OrderGenerator.Domain.ValueObjects;

public sealed record Quantity
{
    public long Value { get; }

    private Quantity(long value) => Value = value;

    public static Quantity Create(long value) => new(value);

    public override string ToString() => Value.ToString();
}
