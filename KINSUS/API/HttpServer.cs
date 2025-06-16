///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: HttpServer.cs
// 檔案描述: 實現設備端 HTTP 伺服器，處理來自 MES/IoT 系統的請求
// 包含功能:
//   - 實現設備端接收 MES 系統發送的指令的接口
//   - 支援 MES 接口規範中定義的服務端角色功能
//   - 處理接口錯誤、重試和響應格式化
// 接口規範參考: Client.prompt.md
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OthinCloud.Model; // 導入資料模型類別

namespace OthinCloud.API
{
    /// <summary>
    /// 訊息接收事件參數類別
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string ClientId { get; set; }
        public string ClientIp { get; set; }
    }

    /// <summary>
    /// 用戶端連接事件參數類別
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string ClientIp { get; set; }
    }

    /// <summary>
    /// 實現 HTTP 伺服器功能，用於接收和處理來自 MES/IoT 系統的請求
    /// 根據 MES 與設備接口規範提供 Server 端接口服務
    /// 支援以下功能：
    /// 1. 遠程資訊下發指令 (SEND_MESSAGE_COMMAND)
    /// 2. 設備時間同步指令 (DATE_MESSAGE_COMMAND)
    /// 3. 盒子碼下發命令 (SWITCH_RECIPE_COMMAND)
    /// 4. 設備啟停控制指令 (DEVICE_CONTROL_COMMAND)
    /// </summary>
    public class HttpServer
    {
        /// <summary>
        /// 當收到 HTTP 請求訊息時觸發
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageReceived;

        /// <summary>
        /// 當有新的用戶端連接時觸發
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// HTTP 監聽器，用於接收並處理 HTTP 請求
        /// </summary>
        private HttpListener listener;
        
        /// <summary>
        /// 伺服器 URL 位址，依照 MES 接口配置設定
        /// 可在配置檔案中設定該值以適應不同環境
        /// </summary>
        private string url = "http://localhost:8080/"; // 預設 URL，應設定為可配置選項
        
        /// <summary>
        /// 伺服器當前執行狀態
        /// </summary>
        private bool isRunning = false;

        /// <summary>
        /// 初始化 HTTP 伺服器
        /// </summary>
        /// <param name="prefix">伺服器監聽的 URL 前綴，格式為 "http://hostname:port/"</param>
        /// <remarks>
        /// 根據 MES 與設備接口規範，接口呼叫地址需設為可配置選項
        /// </remarks>
        public HttpServer(string prefix = "http://localhost:8080/")
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // URI prefixes are required,
            // for example "http://+:8080/index/".
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be empty");

            url = prefix;
            listener = new HttpListener();
            listener.Prefixes.Add(url);
        }

        /// <summary>
        /// 啟動 HTTP 伺服器，開始監聽指定的 URL
        /// </summary>
        /// <remarks>
        /// 啟動後伺服器將等待 MES/IoT 系統的請求，並根據接口規範處理
        /// </remarks>
        public void Start()
        {
            if (isRunning)
            {
                Console.WriteLine("Server is already running.");
                return;
            }

            try
            {
                listener.Start();
                isRunning = true;
                Console.WriteLine($"Listening on {url}...");

                // Start listening for requests in a new thread
                Task.Run(() => HandleIncomingConnections());
            }
            catch (HttpListenerException hlex)
            {
                Console.WriteLine($"Failed to start listener: {hlex.Message}");
                // Consider specific error handling, e.g., port already in use (ErrorCode 183)
                isRunning = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                isRunning = false;
            }
        }

        /// <summary>
        /// 停止 HTTP 伺服器，結束所有監聽活動
        /// </summary>
        public void Stop()
        {
            if (!isRunning)
            {
                return;
            }
            listener.Stop();
            listener.Close();
            isRunning = false;
            Console.WriteLine("Server stopped.");
        }

        /// <summary>
        /// 處理所有進入的 HTTP 連接和請求
        /// </summary>
        /// <remarks>
        /// 此方法負責接收請求、解析 JSON 內容並根據 serviceName 路由到相應的處理方法
        /// 根據 MES 接口規範，所有請求都必須是 POST 方法，並包含 JSON 格式的請求內容
        /// 處理程序同時包含錯誤捕捉及處理機制，符合接口規範的錯誤資訊報警顯示需求
        /// </remarks>
        private async Task HandleIncomingConnections()
        {
            while (isRunning)
            {                try
                {
                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await listener.GetContextAsync();

                    // Peel out the requests and response objects
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;
                    
                    // 檢查是否為 Keep-Alive 連線
                    string connectionHeader = req.Headers["Connection"];
                    bool keepAlive = string.Equals(connectionHeader, "keep-alive", StringComparison.OrdinalIgnoreCase);
                    
                    // 生成請求的唯一ID
                    string clientId = Guid.NewGuid().ToString();
                    string clientIp = req.RemoteEndPoint.ToString();

                    // 觸發用戶端連接事件
                    ClientConnected?.Invoke(this, new ClientConnectedEventArgs
                    {
                        ClientId = clientId,
                        ClientIp = clientIp
                    });

                    // Print out some info about the request
                    Console.WriteLine($"Received request for: {req.Url}");
                    Console.WriteLine($"Method: {req.HttpMethod}");
                    Console.WriteLine($"UserAgent: {req.UserAgent}");
                    Console.WriteLine($"Client IP: {clientIp}");
                    Console.WriteLine($"Keep-Alive: {keepAlive}");
                    Console.WriteLine();

                    // Basic routing based on URL path and ServiceName in body
                    if (req.HttpMethod == "POST" && req.HasEntityBody)
                    {
                        using (Stream body = req.InputStream) // here we have data
                        {
                            using (var reader = new StreamReader(body, req.ContentEncoding))
                            {
                                string requestBody = await reader.ReadToEndAsync();                                Console.WriteLine($"Request Body: {requestBody}");

                                // 觸發訊息接收事件
                                MessageReceived?.Invoke(this, new MessageEventArgs
                                {
                                    Message = requestBody,
                                    ClientId = clientId,
                                    ClientIp = clientIp
                                });

                                // Attempt to deserialize the base request to get ServiceName
                                BaseRequest<object> baseRequest = null;
                                try
                                {
                                    baseRequest = JsonConvert.DeserializeObject<BaseRequest<object>>(requestBody);
                                }
                                catch (JsonException jsonEx)
                                {
                                    Console.WriteLine($"JSON Deserialization Error: {jsonEx.Message}");
                                    await SendErrorResponse(resp, HttpStatusCode.BadRequest, baseRequest?.RequestID, "Invalid JSON format", keepAlive: keepAlive);
                                    continue; // Skip further processing for this request
                                }

                                if (baseRequest != null)
                                {
                                    // Route based on ServiceName
                                    switch (baseRequest.ServiceName)
                                    {
                                        case "SEND_MESSAGE_COMMAND":
                                            await HandleSendMessageCommand(requestBody, resp, baseRequest.RequestID, keepAlive);
                                            break;
                                        case "DATE_MESSAGE_COMMAND":
                                            await HandleDateMessageCommand(requestBody, resp, baseRequest.RequestID, keepAlive);
                                            break;
                                        case "SWITCH_RECIPE_COMMAND":
                                            await HandleSwitchRecipeCommand(requestBody, resp, baseRequest.RequestID, keepAlive);
                                            break;
                                        case "DEVICE_CONTROL_COMMAND": // Reserved, but add handler
                                            await HandleDeviceControlCommand(requestBody, resp, baseRequest.RequestID, keepAlive);
                                            break;
                                        // Add cases for other service names if this server needs to handle them
                                        default:
                                            Console.WriteLine($"Unknown ServiceName: {baseRequest.ServiceName}");
                                            await SendErrorResponse(resp, HttpStatusCode.NotFound, baseRequest.RequestID, $"Unknown service: {baseRequest.ServiceName}", keepAlive: keepAlive);
                                            break;
                                    }
                                }
                                else
                                {
                                    await SendErrorResponse(resp, HttpStatusCode.BadRequest, null, "Could not parse request body", keepAlive: keepAlive);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Handle non-POST requests or requests without body if necessary
                        await SendErrorResponse(resp, HttpStatusCode.MethodNotAllowed, null, "Only POST requests are allowed", keepAlive: keepAlive);
                    }
                }
                catch (HttpListenerException hlex)
                {
                    // This exception can occur when Stop() is called, so check isRunning
                    if (isRunning)
                    {
                        Console.WriteLine($"HttpListenerException in listener loop: {hlex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in listener loop: {ex.Message}");
                    // Consider logging the full exception details
                }
            }
        }

        // --- Request Handlers (Server Role) ---

        /// <summary>
        /// 處理「1. 遠程資訊下發指令」接口請求
        /// </summary>
        /// <param name="requestBody">請求內容字串</param>
        /// <param name="resp">HTTP 響應物件</param>
        /// <param name="requestID">請求 ID</param>
        /// <remarks>
        /// 接口標識：SEND_MESSAGE_COMMAND
        /// 接口概述：IoT 下發資訊至設備，設備接收後顯示於觸摸屏，
        ///          觸發蜂鳴器和三色燈。根據 actionType 決定持續或間隔報警。
        /// 呼叫順序：IoT -> 設備
        /// 請求主要參數：
        ///     - message: 下發消息
        ///     - actionType: 主動類別 (0: 間隔消息, 1: 持續消息)
        ///     - intervalSecondTime: 間隔秒數設定 (0-255)
        /// </remarks>
        private async Task HandleSendMessageCommand(string requestBody, HttpListenerResponse resp, string requestID, bool keepAlive = false)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<SendMessageCommandData>>(requestBody);
                // TODO: Implement logic to handle the command (display message, trigger alarms)
                Console.WriteLine($"Handling SEND_MESSAGE_COMMAND for RequestID: {request.RequestID}");
                if (request.Data != null && request.Data.Any())
                {
                    var commandData = request.Data.First();
                    Console.WriteLine($"  Message: {commandData.Message}");
                    Console.WriteLine($"  ActionType: {commandData.ActionType}");
                    Console.WriteLine($"  Interval: {commandData.IntervalSecondTime}");
                    // --- Add actual device interaction logic here ---
                }                // Send success response
                await SendSuccessResponse<object>(resp, request.RequestID, request.ServiceName, request.DevCode, null, "執行成功", keepAlive: keepAlive);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"SEND_MESSAGE_COMMAND Deserialization Error: {jsonEx.Message}");
                await SendErrorResponse(resp, HttpStatusCode.BadRequest, requestID, "Invalid data format for SEND_MESSAGE_COMMAND", keepAlive: keepAlive);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling SEND_MESSAGE_COMMAND: {ex.Message}");
                await SendErrorResponse(resp, HttpStatusCode.InternalServerError, requestID, "Internal server error handling command", keepAlive: keepAlive);
            }
        }

        /// <summary>
        /// 處理「6. 設備時間同步指令」接口請求
        /// </summary>
        /// <param name="requestBody">請求內容字串</param>
        /// <param name="resp">HTTP 響應物件</param>
        /// <param name="requestID">請求 ID</param>
        /// <remarks>
        /// 接口標識：DATE_MESSAGE_COMMAND
        /// 接口概述：IoT 同步設備的日期和時間（PC 設備可自行同步時間，無需此接口）。
        /// 呼叫順序：IoT -> 設備
        /// 請求主要參數：
        ///     - time: 時間
        ///     - week: 星期 (0-6，0 表示週日)
        /// </remarks>
        private async Task HandleDateMessageCommand(string requestBody, HttpListenerResponse resp, string requestID, bool keepAlive = false)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<DateMessageCommandData>>(requestBody);
                // TODO: Implement logic to sync device time
                Console.WriteLine($"Handling DATE_MESSAGE_COMMAND for RequestID: {request.RequestID}");
                 if (request.Data != null && request.Data.Any())
                {
                    var commandData = request.Data.First();
                    Console.WriteLine($"  Time to sync: {commandData.Time}");
                    Console.WriteLine($"  Week: {commandData.Week}");
                    // --- Add actual device time sync logic here ---
                }                // Send success response
                await SendSuccessResponse<object>(resp, request.RequestID, request.ServiceName, request.DevCode, null, "執行成功", keepAlive: keepAlive);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"DATE_MESSAGE_COMMAND Deserialization Error: {jsonEx.Message}");
                await SendErrorResponse(resp, HttpStatusCode.BadRequest, requestID, "Invalid data format for DATE_MESSAGE_COMMAND", keepAlive: keepAlive);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling DATE_MESSAGE_COMMAND: {ex.Message}");
                await SendErrorResponse(resp, HttpStatusCode.InternalServerError, requestID, "Internal server error handling command", keepAlive: keepAlive);
            }
        }

        /// <summary>
        /// 處理「10. 盒子碼下發命令」接口請求
        /// </summary>
        /// <param name="requestBody">請求內容字串</param>
        /// <param name="resp">HTTP 響應物件</param>
        /// <param name="requestID">請求 ID</param>
        /// <remarks>
        /// 接口標識：SWITCH_RECIPE_COMMAND
        /// 接口概述：設備上報盒子碼後，IoT 通過此接口下發盒子資訊至設備。
        /// 呼叫順序：IoT -> 設備
        /// 請求主要參數：
        ///     - start: 盒子狀態 (OK/NG 合格/不合格)
        ///     - gfNum: 研磨次數
        ///     - pnum: 鑽針數量
        ///     - vehicleCode: 盒子碼
        ///     - recipeParams: 配方參數 (由供應商協定)
        /// </remarks>
        private async Task HandleSwitchRecipeCommand(string requestBody, HttpListenerResponse resp, string requestID, bool keepAlive = false)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<SwitchRecipeCommandData>>(requestBody);
                // TODO: Implement logic to handle recipe switch
                Console.WriteLine($"Handling SWITCH_RECIPE_COMMAND for RequestID: {request.RequestID}");
                 if (request.Data != null && request.Data.Any())
                {
                    var commandData = request.Data.First();
                    Console.WriteLine($"  VehicleCode: {commandData.VehicleCode}");
                    Console.WriteLine($"  Start: {commandData.Start}");
                    Console.WriteLine($"  GfNum: {commandData.GfNum}");
                    Console.WriteLine($"  Pnum: {commandData.Pnum}");
                    // --- Add actual device recipe handling logic here ---
                }                // Send success response
                await SendSuccessResponse<object>(resp, request.RequestID, request.ServiceName, request.DevCode, null, "盒子資訊接收成功", keepAlive: keepAlive);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"SWITCH_RECIPE_COMMAND Deserialization Error: {jsonEx.Message}");
                await SendErrorResponse(resp, HttpStatusCode.BadRequest, requestID, "Invalid data format for SWITCH_RECIPE_COMMAND", keepAlive: keepAlive);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling SWITCH_RECIPE_COMMAND: {ex.Message}");
                await SendErrorResponse(resp, HttpStatusCode.InternalServerError, requestID, "Internal server error handling command", keepAlive: keepAlive);
            }
        }

        /// <summary>
        /// 處理「13. 設備啟停控制指令」接口請求
        /// </summary>
        /// <param name="requestBody">請求內容字串</param>
        /// <param name="resp">HTTP 響應物件</param>
        /// <param name="requestID">請求 ID</param>
        /// <remarks>
        /// 接口標識：DEVICE_CONTROL_COMMAND
        /// 接口概述：IoT 根據業務邏輯下發啟停指令，控制設備執行或停止（完成當前作業後執行）。
        ///          此接口為預留，可不實現。
        /// 呼叫順序：IoT -> 設備
        /// 請求主要參數：
        ///     - command: 指令類型 (1: 設備啟動, 2: 設備停止)
        /// </remarks>
        private async Task HandleDeviceControlCommand(string requestBody, HttpListenerResponse resp, string requestID, bool keepAlive = false)
        {
             try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<DeviceControlCommandData>>(requestBody);
                // TODO: Implement logic to start/stop device
                Console.WriteLine($"Handling DEVICE_CONTROL_COMMAND for RequestID: {request.RequestID}");
                 if (request.Data != null && request.Data.Any())
                {
                    var commandData = request.Data.First();
                    Console.WriteLine($"  Command: {commandData.Command}"); // 1: Start, 2: Stop
                    // --- Add actual device control logic here ---
                }

                // Send success response                await SendSuccessResponse<object>(resp, request.RequestID, request.ServiceName, request.DevCode, null, "執行成功", keepAlive: keepAlive);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"DEVICE_CONTROL_COMMAND Deserialization Error: {jsonEx.Message}");
                await SendErrorResponse(resp, HttpStatusCode.BadRequest, requestID, "Invalid data format for DEVICE_CONTROL_COMMAND", keepAlive: keepAlive);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling DEVICE_CONTROL_COMMAND: {ex.Message}");
                await SendErrorResponse(resp, HttpStatusCode.InternalServerError, requestID, "Internal server error handling command", keepAlive: keepAlive);
            }
        }

        // --- Helper Methods for Responses ---

        /// <summary>
        /// 發送成功響應給客戶端
        /// </summary>
        /// <typeparam name="T">響應資料的類型</typeparam>
        /// <param name="resp">HTTP 響應物件</param>
        /// <param name="requestID">請求 ID，用於追蹤請求與響應的對應關係</param>
        /// <param name="serviceName">服務名稱，標識請求的接口類型</param>
        /// <param name="devCode">設備編碼</param>
        /// <param name="data">響應業務資料</param>        /// <param name="message">響應消息</param>
        /// <remarks>
        /// 根據 MES 接口規範，成功響應狀態碼為 "0000"
        /// </remarks>
        private async Task SendSuccessResponse<T>(HttpListenerResponse resp, string requestID, string serviceName, string devCode, List<T> data, string message = "執行成功", bool keepAlive = false)
        {
            var responseObj = new BaseResponse<T>
            {
                RequestID = requestID,
                ServiceName = serviceName,
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DevCode = devCode, // Should ideally get this from config or the request if appropriate
                Operator = null, // Operator is usually provided by the device in requests
                Status = "0000",
                Message = message,
                Data = data,
                ExtendData = null
            };
            string responseString = JsonConvert.SerializeObject(responseObj, Formatting.Indented);
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = buffer.Length;
            resp.StatusCode = (int)HttpStatusCode.OK;

            // 設置 Connection 標頭
            if (keepAlive)
            {
                resp.Headers.Add("Connection", "keep-alive");
                resp.KeepAlive = true;
            }
            else
            {
                resp.Headers.Add("Connection", "close");
                resp.KeepAlive = false;
            }

            await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);

            // 只有在非 keep-alive 的情況下才關閉連線
            if (!keepAlive)
            {
                resp.Close();
            }

            Console.WriteLine($"Success response sent. Keep-alive: {keepAlive}");
        }

        private async Task SendErrorResponse(HttpListenerResponse resp, HttpStatusCode statusCode, string requestID, string message, string serviceName = null, string devCode = null, bool keepAlive = false)
        {
            var responseObj = new BaseResponse<object>
            {
                RequestID = requestID ?? Guid.NewGuid().ToString(),
                ServiceName = serviceName,
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DevCode = devCode,
                Operator = null,
                Status = "9999",
                Message = message,
                Data = null,
                ExtendData = null
            };
            string responseString = JsonConvert.SerializeObject(responseObj, Formatting.Indented);
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = buffer.Length;
            resp.StatusCode = (int)statusCode;

            // 設置 Connection 標頭
            if (keepAlive)
            {
                resp.Headers.Add("Connection", "keep-alive");
                resp.KeepAlive = true;
            }
            else
            {
                resp.Headers.Add("Connection", "close");
                resp.KeepAlive = false;
            }

            await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);

            // 只有在非 keep-alive 的情況下才關閉連線
            if (!keepAlive)
            {
                resp.Close();
            }

            Console.WriteLine($"Error response sent: {statusCode} - {message}. Keep-alive: {keepAlive}");
        }

        // --- Client Role Methods  ---
        // 實現設備主動向 IoT 系統發送請求的方法

        /// <summary>
        /// 2. 設備狀態資訊
        /// 設備上報當前的執行狀態（執行、待機、故障、停止、保養等）
        /// </summary>
        /// <param name="statusData">設備狀態資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <param name="operator">操作員</param>
        /// <returns>API響應結果</returns>
        public async Task<BaseResponse<object>> SendDeviceStatus(DeviceStatusData statusData, string iotEndpoint, string devCode, string @operator = null)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/deviceStatusMessage");
                var request = new BaseRequest<DeviceStatusData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "DEVICE_STATUS_MESSAGE",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Operator = @operator,
                    Data = new List<DeviceStatusData> { statusData },
                    ExtendData = null
                };
                return await SendHttpRequest<DeviceStatusData, object>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending device status: {ex.Message}");
                return CreateErrorResponse<object>(ex.Message);
            }
        }

        /// <summary>
        /// 3. 設備報警資訊
        /// 上報設備的報警資訊（報警編碼、描述、等級、發生狀態等）
        /// </summary>
        /// <param name="warningData">報警資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <param name="operator">操作員</param>
        /// <returns>API響應結果</returns>
        public async Task<BaseResponse<object>> SendDeviceWarning(DeviceWarningData warningData, string iotEndpoint, string devCode, string @operator = null)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/deviceWarningMessage");
                var request = new BaseRequest<DeviceWarningData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "DEVICE_WARNING_MESSAGE",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Operator = @operator,
                    Data = new List<DeviceWarningData> { warningData },
                    ExtendData = null
                };
                return await SendHttpRequest<DeviceWarningData, object>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending device warning: {ex.Message}");
                return CreateErrorResponse<object>(ex.Message);
            }
        }

        /// <summary>
        /// 4. 設備生產實時數據監控
        /// 上報設備的生產參數數據（如稼動率、能源、壓力值等）
        /// </summary>
        /// <param name="paramData">生產參數資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <returns>API響應結果</returns>
        public async Task<BaseResponse<object>> SendDeviceParameters(DeviceParamData paramData, string iotEndpoint, string devCode)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/deviceParamMessage");
                var request = new BaseRequest<DeviceParamData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "DEVICE_PARAM_REQUEST",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Data = new List<DeviceParamData> { paramData },
                    ExtendData = null
                };
                return await SendHttpRequest<DeviceParamData, object>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending device parameters: {ex.Message}");
                return CreateErrorResponse<object>(ex.Message);
            }
        }

        /// <summary>
        /// 5. 設備事件起止時間上傳
        /// 上報設備的事件起止時間數據（如校正、檢測、保養、維修、開關機、執行等）
        /// </summary>
        /// <param name="eventTimeData">事件時間資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <param name="operator">操作員</param>
        /// <returns>API響應結果，包含校驗結果</returns>
        public async Task<BaseResponse<EventTimeResponseData>> SendEventTime(EventTimeData eventTimeData, string iotEndpoint, string devCode, string @operator = null)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/eventTimeMessage");
                var request = new BaseRequest<EventTimeData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "EVENT_TIME_MESSAGE",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Operator = @operator,
                    Data = new List<EventTimeData> { eventTimeData },
                    ExtendData = null
                };
                return await SendHttpRequest<EventTimeData, EventTimeResponseData>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending event time: {ex.Message}");
                return CreateErrorResponse<EventTimeResponseData>(ex.Message);
            }
        }

        /// <summary>
        /// 7. 設備聯網狀態監控功能
        /// 定期發送心跳，維持與IoT系統的連接狀態
        /// </summary>
        /// <param name="heartBeatData">心跳資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <param name="operator">操作員</param>
        /// <returns>API響應結果，包含心跳值</returns>
        public async Task<BaseResponse<DeviceHeartbeatResponseData>> SendHeartbeat(DeviceHeartbeatData heartBeatData, string iotEndpoint, string devCode, string @operator = null)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/deviceHeartbeatMessage");
                var request = new BaseRequest<DeviceHeartbeatData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "DEVICE_HEARTBEAT_MESSAGE",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Operator = @operator,
                    Data = new List<DeviceHeartbeatData> { heartBeatData },
                    ExtendData = null
                };
                return await SendHttpRequest<DeviceHeartbeatData, DeviceHeartbeatResponseData>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat: {ex.Message}");
                return CreateErrorResponse<DeviceHeartbeatResponseData>(ex.Message);
            }
        }

        /// <summary>
        /// 8. 設備點檢參數報告
        /// 上報設備點檢參數資訊（如備件、輔料、治具的使用次數與壽命等）
        /// </summary>
        /// <param name="checkData">點檢資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <returns>API響應結果</returns>
        public async Task<BaseResponse<object>> SendDeviceKeyChecking(DeviceKeycheckingData checkData, string iotEndpoint, string devCode)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/deviceKeycheckingRequest");
                var request = new BaseRequest<DeviceKeycheckingData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "DEVICE_KEYCHECKING_REQUEST",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Data = new List<DeviceKeycheckingData> { checkData },
                    ExtendData = null
                };
                return await SendHttpRequest<DeviceKeycheckingData, object>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending device key checking: {ex.Message}");
                return CreateErrorResponse<object>(ex.Message);
            }
        }

        /// <summary>
        /// 9. 設備採集到盒子碼上報
        /// 當設備採集到新的盒子碼時，上報至IoT系統
        /// </summary>
        /// <param name="vehicleData">盒子碼資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <param name="operator">操作員</param>
        /// <returns>API響應結果</returns>
        public async Task<BaseResponse<object>> UploadVehicleCode(DeviceVehicleUploadData vehicleData, string iotEndpoint, string devCode, string @operator = null)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/DeviceVehicleUpload");
                var request = new BaseRequest<DeviceVehicleUploadData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "DEVICE_VEHICLE_UPLOAD",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Operator = @operator,
                    Data = new List<DeviceVehicleUploadData> { vehicleData },
                    ExtendData = null
                };
                return await SendHttpRequest<DeviceVehicleUploadData, object>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading vehicle code: {ex.Message}");
                return CreateErrorResponse<object>(ex.Message);
            }
        }

        /// <summary>
        /// 11. 盒子做完上報資訊接口
        /// 設備完成一盒生產後，上報盒子碼、良品數量及不良品數量
        /// </summary>
        /// <param name="completeData">盒子完成資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <returns>API響應結果</returns>
        public async Task<BaseResponse<object>> SendBatchCompleteInfo(BatchCompleteData completeData, string iotEndpoint, string devCode)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/batchCompleteMessage");
                var request = new BaseRequest<BatchCompleteData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "BATCH_COMPLETE_MESSAGE",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Data = new List<BatchCompleteData> { completeData },
                    ExtendData = null
                };
                return await SendHttpRequest<BatchCompleteData, object>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending batch complete info: {ex.Message}");
                return CreateErrorResponse<object>(ex.Message);
            }
        }

        /// <summary>
        /// 12. 良品盒生成完成上報
        /// 設備完成良品盒生產後，上報MES盒子碼、鑽針數量及研磨次數
        /// </summary>
        /// <param name="reportData">良品盒資料</param>
        /// <param name="iotEndpoint">IoT系統API端點</param>
        /// <param name="devCode">設備編碼</param>
        /// <returns>API響應結果</returns>
        public async Task<BaseResponse<object>> SendBatchReportedInfo(BatchReportedData reportData, string iotEndpoint, string devCode)
        {
            try
            {
                string targetUrl = BuildApiUrl(iotEndpoint, "/api/batchReportedMessage");
                var request = new BaseRequest<BatchReportedData>
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ServiceName = "BATCH_REPORTED_MESSAGE",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DevCode = devCode,
                    Data = new List<BatchReportedData> { reportData },
                    ExtendData = null
                };

                return await SendHttpRequest<BatchReportedData, object>(targetUrl, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending batch reported info: {ex.Message}");
                return CreateErrorResponse<object>(ex.Message);
            }
        }

        // 通用的 HTTP 請求發送方法
        private async Task<BaseResponse<TResponse>> SendHttpRequest<TRequest, TResponse>(string url, BaseRequest<TRequest> request)
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                // 序列化請求
                string jsonRequest = JsonConvert.SerializeObject(request);
                int retryCount = 0;
                const int maxRetries = 3; // 最多重試3次
                const int retryDelayMs = 5000; // 等待5秒再重試
                while (retryCount < maxRetries)
                {
                    try
                    {
                        using (var content = new System.Net.Http.StringContent(jsonRequest, Encoding.UTF8, "application/json"))
                        {
                            var response = await httpClient.PostAsync(url, content);
                            // 檢查HTTP狀態碼
                            if (response.IsSuccessStatusCode)
                            {
                                string jsonResponse = await response.Content.ReadAsStringAsync();
                                var apiResponse = JsonConvert.DeserializeObject<BaseResponse<TResponse>>(jsonResponse);
                                // 檢查業務狀態碼
                                if (apiResponse.Status == "0000")
                                {
                                    return apiResponse;
                                }
                                else
                                {
                                    Console.WriteLine($"API 業務錯誤: {apiResponse.Status} - {apiResponse.Message}");
                                    return apiResponse; // 返回包含錯誤資訊的響應
                                }
                            }
                            else
                            {
                                Console.WriteLine($"HTTP 錯誤: {(int)response.StatusCode} - {response.ReasonPhrase}");
                                // 重試邏輯
                                retryCount++;
                                if (retryCount < maxRetries)
                                {
                                    Console.WriteLine($"重試第 {retryCount} 次，等待 {retryDelayMs/1000} 秒...");
                                    await Task.Delay(retryDelayMs);
                                    continue;
                                }
                                return CreateErrorResponse<TResponse>($"HTTP錯誤: {(int)response.StatusCode} - {response.ReasonPhrase}");
                            }
                        }
                    }
                    catch (System.Net.Http.HttpRequestException ex)
                    {
                        Console.WriteLine($"HTTP 請求異常: {ex.Message}");
                        // 網路錯誤重試
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            Console.WriteLine($"重試第 {retryCount} 次，等待 {retryDelayMs/1000} 秒...");
                            await Task.Delay(retryDelayMs);
                            continue;
                        }
                        return CreateErrorResponse<TResponse>($"HTTP請求異常: {ex.Message}");
                    }
                }
                // 如果所有重試都失敗
                return CreateErrorResponse<TResponse>("重試請求後仍然失敗");
            }
        }

        // 建立錯誤響應
        private BaseResponse<T> CreateErrorResponse<T>(string errorMessage)
        {
            return new BaseResponse<T>
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceName = null,
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DevCode = null,
                Operator = null,
                Status = "9999", // 使用通用錯誤代碼
                Message = errorMessage,
                Data = null,
                ExtendData = null
            };
        }

        // 彈性組合 API 路徑的輔助函式
        private string BuildApiUrl(string iotEndpoint, string apiPath)
        {
            if (string.IsNullOrWhiteSpace(iotEndpoint)) return apiPath;
            string endpoint = iotEndpoint.TrimEnd('/');
            string path = apiPath.StartsWith("/") ? apiPath : "/" + apiPath;
            // 如果 endpoint 已經包含 apiPath 的結尾，不再補
            if (endpoint.EndsWith(path, StringComparison.OrdinalIgnoreCase))
                return endpoint;
            return endpoint + path;
        }
    }
}
