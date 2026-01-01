using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebApiTemplate.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoController : AppControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<DemoController> _logger;

        public DemoController(ILogger<DemoController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetWeatherAfterDays")]
        public ActionResult<Result<WeatherForecast>> GetDateAfterDays(int days)
        {
            //北岿粇:琩高把计浪岿粇
            if (days < 0) return Ok(ApiResponse.Failure("400", "琩高ゼㄓら戳ぱ箇代"));

            var data = new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(days)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            };
            //硄筁琩高把计浪肚戈
            return Ok(ApiResponse<WeatherForecast>.Success(data));
        }

        // 矪瞶北钵盽猵
        [HttpGet("HandleKnownProblem")]
        public IActionResult HandleKnownProblem()
        {
            return ProblemFrom(Result.Failure(Error.Unauthorized("礚舦磅︽API")));
        }

        // 家览祇ネ獶箇戳钵盽猵
        [HttpGet("GetUnknownException")]
        public IActionResult GetUnknownException(int zero = 0)
        {
            int dividedByZero = 100 / zero;

            return Ok(ApiResponse.Success());
        }

        public class WeatherForecast 
        {
            public DateOnly Date { get; set; }

            public int TemperatureC { get; set; }

            public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

            public string? Summary { get; set; }
        }
    }
}
