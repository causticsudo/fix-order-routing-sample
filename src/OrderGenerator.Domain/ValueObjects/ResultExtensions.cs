namespace OrderGenerator.Domain.ValueObjects;

public static class ResultExtensions
{
    public static Result<T> Success<T>(T value) => new ResultSuccess<T>(value);
    public static Result<T> Failure<T>(string error) => new ResultFailure<T>(error);
}
