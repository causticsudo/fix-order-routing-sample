namespace OrderGenerator.Domain.ValueObjects;

public sealed record OrderSide
{
    public string Value { get; }

    private OrderSide(string value) => Value = value;

    public static readonly OrderSide Buy = new("BUY");
    public static readonly OrderSide Sell = new("SELL");

    public static OrderSide Create(string value)
    {
        var normalized = value.ToUpperInvariant();
        return normalized switch
        {
            "BUY" => Buy,
            "SELL" => Sell,
            _ => throw new ArgumentException($"Invalid side: {value}")
        };
    }

    public override string ToString() => Value;
}
