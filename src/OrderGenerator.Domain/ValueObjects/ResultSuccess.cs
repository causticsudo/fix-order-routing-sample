namespace OrderGenerator.Domain.ValueObjects;

public sealed record ResultSuccess<T>(T Value) : Result<T>;
