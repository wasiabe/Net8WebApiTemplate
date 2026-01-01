using Microsoft.AspNetCore.Mvc;

[ApiController]
public abstract class AppControllerBase : ControllerBase
{
    protected IActionResult OkQuery<T>(T data) => Ok(ApiResponse<T>.Success(data));
    protected IActionResult OkCommand() => Ok(ApiResponse.Success());

    protected IActionResult ProblemFrom(Result result)
    {
        var pd = CustomizedProblemDetailsFactory.FromResult(HttpContext, result);
        return StatusCode(pd.Status ?? StatusCodes.Status500InternalServerError, pd);
    }
}
