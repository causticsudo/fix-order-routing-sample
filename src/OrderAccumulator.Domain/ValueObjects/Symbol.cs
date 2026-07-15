using FixOrderRouting.SharedKernel.Constants;

namespace OrderAccumulator.Domain.ValueObjects;

public sealed record Symbol
{
    public string Value { get; }

    private Symbol(string value)
    {
        Value = value;
    }

    public static Symbol Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Symbol cannot be empty");

        if (!BusinessConstants.Symbols.Valid.Contains(value))
            throw new ArgumentException($"Invalid symbol: {value}");

        return new Symbol(value);
    }
}
