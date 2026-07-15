namespace OrderGenerator.Domain.ValueObjects;

public sealed record ResultFailure<T>(string Error) : Result<T>;
