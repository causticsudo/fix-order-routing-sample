using FixOrderRouting.SharedKernel.Constants;

namespace OrderAccumulator.Domain.ValueObjects;

public sealed record Quantity
{
    public long Value { get; }

    private Quantity(long value)
    {
        Value = value;
    }

    public static Quantity Create(long value)
    {
        if (value < BusinessConstants.Orders.MinQuantity || value > BusinessConstants.Orders.MaxQuantity)
            throw new ArgumentException($"Quantity must be >= {BusinessConstants.Orders.MinQuantity} and <= {BusinessConstants.Orders.MaxQuantity}, got {value}");

        return new Quantity(value);
    }
}
