## 安全控管:
- Global的來源IP卡控機制:可設定卡控Proxy IP及X-Forwarded-For或是單純卡控來源IP
- Global的限流(Rate Limiting)機制
## 錯誤處理:
- 使用全域錯誤處理(IExceptionHandler)
- 對於已知如何處理及可控的錯誤類型使用Result Pattern及Problem Details(標準化錯誤回應格式 RFC 9457)
  Use Result Pattern and Problem Details(RFC 9457) for errors you know how to handle
- 除了標準的Problem Detail, 尚需加上
### (1) 「當前這一次 HTTP 請求」的唯一標識(Request Id): 
- 獲取方式：context.HttpContext.TraceIdentifier
- 用途：
-- 單機追蹤：當伺服器收到請求時，會自動生成一個 ID（例如 0HNFB9...）。
-- 日誌過濾：如果你在伺服器日誌（如 Serilog, NLog）中搜尋這個 ID，你可以看到這台主機處理該請求的所有步驟。
-- 客戶端反饋：當 API 報錯時，客戶端把這個 ID 給後端人員，開發者可以直接在 Log 中找到該次報錯的上下文。
### (2)「整個分散式事務」的唯一標識(遵循 W3C Trace Context 標準)(Trace Id):
- 獲取方式：程式碼中透過 IHttpActivityFeature 獲取當前運行的 Activity.Id。
- 用途：
-- 跨服務追蹤 (Distributed Tracing)：在微服務架構中，若 A 服務調用 B 服務，B 再調用 C，它們會共用同一個 Trace ID。
-- 全鏈路監控：你可以使用像 Jaeger、Zipkin 或 Azure Application Insights 這樣的工具，用 Trace ID 畫出整個請求在多個服務間流轉的時序圖，找出哪一個環節最慢或出錯。
-- 標準化：它的格式通常是 00-<TraceId>-<SpanId>-00。
## 回傳內文(Response Body):
### Http Status 200以外的情況:採用Result Pattern and Problem Details(RFC 9457)
### Http Status 200的查詢類型請求:回傳 
{
	"isSuccess":true,		//API 執行成功或失敗
	"code":"0",				//執行預設成功回傳0或其他狀態代碼 
	"message":"success",	//執行預設成功回傳success或其他說明(ex.查無資料)
	"data":"...."			//回傳所要求的資料
}
### Http Status 200的交易類型請求:回傳 
{
	"isSuccess":true,
	"code":"0",
	"message":"success"
}
## Logging
- 使用Serilog
- 記錄 Request 及 Response
- 資料庫連線字串不使用明碼
