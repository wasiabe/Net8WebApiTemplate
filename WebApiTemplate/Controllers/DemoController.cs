using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebApiTemplate.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoController : ControllerBase
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
            //已知且可控錯誤:查詢參數檢核錯誤
            if (days < 0) return Ok(Result.Failure("400", "只可查詢未來日期的天氣預測"));

            var data = new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(days)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            };
            //通過查詢參數檢核並回傳資料
            return Ok(Result<WeatherForecast>.Success(data));
        }

        // 處理可控的異常狀況
        [HttpGet("GetKnownException")]
        public IActionResult GetException()
        {
            // 建立錯誤回應物件（符合專案 Result 型別）
            var errorResult = Result.Failure("500", "無法連接後端服務");

            // 記錄錯誤以利診斷（包含錯誤代碼與訊息）
            _logger.LogError("GetKnownException 回傳 HTTP 500 - Code: {Code}, Message: {Message}", "500", "無法連接後端服務");

            // 回傳 HTTP 500 與錯誤 payload
            return StatusCode(500, errorResult);

            // 若要以例外方式模擬，可改為 throw new Exception("無法連接後端服務");
        }

        // 模擬發生非預期的異常狀況
        [HttpGet("GetUnknownException")]
        public IActionResult GetUnknownException(int zero = 0)
        {
            int dividedByZero = 100 / zero;

            return Ok(Result.Success());
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
