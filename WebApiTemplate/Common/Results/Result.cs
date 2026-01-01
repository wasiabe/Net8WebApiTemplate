public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }


    public Error? Error { get; }

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, Error? error) : base(error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(Error error) => new(false, default, error);
}