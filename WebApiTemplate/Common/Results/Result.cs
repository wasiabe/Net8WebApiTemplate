public record Result<T>(T? Data, string? Message = "SUCCESS", string? Code = "0")
{
    public static Result<T> Success(T data) => new(data);
    public static Result<T> Success() => new(default(T?));
    public static Result<T> Failure(string code, string message) => new(default, message, code);
}

public record Result(string? Message = "SUCCESS", string? Code = "0")
{
    public static Result Success() => new();

    public static Result Failure(string code, string message) => new(message, code);
}