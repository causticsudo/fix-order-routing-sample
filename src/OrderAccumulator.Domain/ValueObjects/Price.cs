using FixOrderRouting.SharedKernel.Constants;

namespace OrderAccumulator.Domain.ValueObjects;

public sealed record Price
{
    public decimal Value { get; }

    private Price(decimal value)
    {
        Value = value;
    }

    public static Price Create(decimal value)
    {
        if (value <= BusinessConstants.Orders.MinPrice || value > BusinessConstants.Orders.MaxPrice)
            throw new ArgumentException($"Price must be > {BusinessConstants.Orders.MinPrice} and <= {BusinessConstants.Orders.MaxPrice}, got {value}");

        if (!IsMultipleOf001(value))
            throw new ArgumentException($"Price must be a multiple of 0.01, got {value}");

        return new Price(value);
    }

    private static bool IsMultipleOf001(decimal value)
    {
        var scaled = value * 100;
        return scaled == Math.Floor(scaled);
    }
}
