using FixOrderRouting.SharedKernel.Constants;

namespace OrderAccumulator.Domain.ValueObjects;

public sealed record OrderSide
{
    public string Value { get; }

    private OrderSide(string value)
    {
        Value = value;
    }

    public static OrderSide Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Side cannot be empty");

        if (value != BusinessConstants.Sides.Buy && value != BusinessConstants.Sides.Sell)
            throw new ArgumentException($"Invalid side: {value}. Must be {BusinessConstants.Sides.Buy} or {BusinessConstants.Sides.Sell}");

        return new OrderSide(value);
    }

    public bool IsBuy() => Value == BusinessConstants.Sides.Buy;
    public bool IsSell() => Value == BusinessConstants.Sides.Sell;
}
