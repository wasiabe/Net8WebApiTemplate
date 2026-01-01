
public sealed record ApiResponse<T>(T? Data, string? Message = "SUCCESS", string? Code = "0")
{
    public static ApiResponse<T> Success(T data) => new(data);
    public static ApiResponse<T> Success() => new(default(T?));
    public static ApiResponse<T> Failure(string code, string message) => new(default, message, code);

}

public sealed record ApiResponse(string? Message = "SUCCESS", string? Code = "0")
{
    public static ApiResponse Success() => new();

    public static ApiResponse Failure(string code, string message) => new(message, code);
}
