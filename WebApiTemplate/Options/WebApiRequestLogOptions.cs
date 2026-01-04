
public class WebApiRequestLogOptions
{
    public bool Enabled { get; set; } = true;

    public bool EnableBatchWrite { get; set; } = true;

    // (原本) 排除路徑（前綴）
    public string[] ExcludedPathPrefixes { get; set; } = ["/swagger", "/health"];

    // ✅ 新增：只記錄這些路徑（前綴）。空陣列=不限制（全都可記錄）
    public string[] IncludedPathPrefixes { get; set; } = [];

    // ✅ 新增：不記錄這些路徑（前綴），優先於 Included
    public string[] ExcludedPathPrefixesHard { get; set; } =
    [
        "/auth",            // 登入/SSO 常含敏感資訊
        "/oauth",
        "/token",
        "/signin",
        "/callback"
    ];

    // Body 記錄開關
    public bool LogRequestHeaders { get; set; } = true;
    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseBody { get; set; } = true;

    // ✅ 新增：只有這些 Request Content-Type 才會讀 request body
    public string[] RequestBodyContentTypesAllowList { get; set; } =
    [
        "application/json",
        "text/plain",
        "application/x-www-form-urlencoded"
    ];

    // ✅ 新增：只有這些 Response Content-Type 才會讀 response body
    public string[] ResponseBodyContentTypesAllowList { get; set; } =
    [
        "application/json",
        "text/plain",
        "text/json",
        "application/problem+json"
    ];

    // 最大字元數（避免 DB 撐爆）
    public int MaxBodyChars { get; set; } = 50_000;

    // Header 遮罩
    public string[] SensitiveHeaders { get; set; } =
    [
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key"
    ];

    // JSON key 遮罩（request/response）
    public string[] SensitiveJsonKeys { get; set; } =
    [
        "password", "pwd", "pass",
        "token", "access_token", "refresh_token", "id_token",
        "secret"
    ];
}
