namespace OrderGenerator.Domain.ValueObjects;

public sealed record Symbol
{
    public string Value { get; }

    private Symbol(string value) => Value = value;

    public static Symbol Create(string value) => new(value.ToUpperInvariant());

    public override string ToString() => Value;
}
