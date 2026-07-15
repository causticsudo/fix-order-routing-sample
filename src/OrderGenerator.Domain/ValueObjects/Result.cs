
namespace OrderGenerator.Domain.ValueObjects;

public abstract record Result<T>
{
    public static implicit operator Result<T>(T value) => new ResultSuccess<T>(value);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure) =>
        this switch
        {
            ResultSuccess<T> s => onSuccess(s.Value),
            ResultFailure<T> f => onFailure(f.Error),
            _ => throw new InvalidOperationException()
        };

    public void Match(
        Action<T> onSuccess,
        Action<string> onFailure)
    {
        switch (this)
        {
            case ResultSuccess<T> s:
                onSuccess(s.Value);
                break;
            case ResultFailure<T> f:
                onFailure(f.Error);
                break;
        }
    }
}
