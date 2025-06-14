using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DDSWebAPI.Models;

namespace DDSWebAPI.Services
{
    /// <summary>
    /// HTTP 伺服器服務，處理來自 MES/IoT 系統的請求，支援靜態檔案和 WebSocket
    /// </summary>
    public class HttpServerService : IDisposable
    {
        private HttpListener _httpListener;
        private bool _isListening;
        private string _urlPrefix;
        private string _staticFilesPath;
        private readonly object _lockObject = new object();
        private readonly Dictionary<string, string> _mimeTypes;

        #region 事件定義

        /// <summary>
        /// 訊息接收事件
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageReceived;

        /// <summary>
        /// 用戶端連接事件
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 用戶端斷線事件
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// 伺服器狀態變更事件
        /// </summary>
        public event EventHandler<ServerStatusChangedEventArgs> ServerStatusChanged;

        /// <summary>
        /// WebSocket 訊息接收事件
        /// </summary>
        public event EventHandler<WebSocketMessageEventArgs> WebSocketMessageReceived;

        /// <summary>
        /// 自訂 API 處理事件
        /// </summary>
        public event EventHandler<CustomApiRequestEventArgs> CustomApiRequest;

        #endregion

        #region 屬性

        /// <summary>
        /// 是否正在監聽
        /// </summary>
        public bool IsListening
        {
            get
            {
                lock (_lockObject)
                {
                    return _isListening;
                }
            }
            private set
            {
                lock (_lockObject)
                {
                    _isListening = value;
                }
            }
        }

        /// <summary>
        /// 監聽位址
        /// </summary>
        public string UrlPrefix => _urlPrefix;

        /// <summary>
        /// 靜態檔案路徑
        /// </summary>
        public string StaticFilesPath
        {
            get => _staticFilesPath;
            set => _staticFilesPath = value;
        }

        /// <summary>
        /// 目前連接的用戶端數量
        /// </summary>
        public int ConnectedClientCount { get; private set; }

        #endregion

        #region 建構函式

        /// <summary>
        /// 初始化 HTTP 伺服器服務
        /// </summary>
        /// <param name="urlPrefix">監聽位址前綴</param>
        /// <param name="staticFilesPath">靜態檔案路徑</param>
        public HttpServerService(string urlPrefix = "http://localhost:8085/", string staticFilesPath = null)
        {
            _urlPrefix = urlPrefix?.TrimEnd('/') + "/";
            _staticFilesPath = staticFilesPath;
            
            // 初始化 MIME 類型對應
            _mimeTypes = new Dictionary<string, string>
            {
                { ".html", "text/html; charset=utf-8" },
                { ".css", "text/css; charset=utf-8" },
                { ".js", "application/javascript; charset=utf-8" },
                { ".json", "application/json; charset=utf-8" },
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".gif", "image/gif" },
                { ".svg", "image/svg+xml" },
                { ".ico", "image/x-icon" },
                { ".txt", "text/plain; charset=utf-8" },
                { ".xml", "application/xml; charset=utf-8" }
            };
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 啟動 HTTP 伺服器
        /// </summary>
        /// <returns>是否啟動成功</returns>
        public async Task<bool> StartAsync()
        {
            try
            {
                if (IsListening)
                {
                    return true;
                }

                OnServerStatusChanged(ServerStatus.Starting, "正在啟動 HTTP 伺服器...");

                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(_urlPrefix);
                
                _httpListener.Start();
                IsListening = true;

                OnServerStatusChanged(ServerStatus.Running, $"HTTP 伺服器已啟動，監聽位址: {_urlPrefix}");

                // 開始非同步處理請求
                _ = Task.Run(ProcessRequestsAsync);

                return true;
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"啟動 HTTP 伺服器失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止 HTTP 伺服器
        /// </summary>
        public void Stop()
        {
            try
            {
                if (!IsListening)
                {
                    return;
                }

                IsListening = false;
                _httpListener?.Stop();
                _httpListener?.Close();
                
                ConnectedClientCount = 0;
                OnServerStatusChanged(ServerStatus.Stopped, "HTTP 伺服器已停止");
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"停止 HTTP 伺服器時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 非同步處理 HTTP 請求
        /// </summary>
        private async Task ProcessRequestsAsync()
        {
            while (IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    
                    // 在背景處理請求，避免阻塞
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
                catch (ObjectDisposedException)
                {
                    // HttpListener 已經被釋放，正常結束
                    break;
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                {
                    // 操作因為取消而中止，正常結束
                    break;
                }
                catch (Exception ex)
                {
                    OnServerStatusChanged(ServerStatus.Error, $"處理 HTTP 請求時發生錯誤: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 處理單一 HTTP 請求
        /// </summary>
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            string clientId = Guid.NewGuid().ToString();
            string clientIp = GetClientIpAddress(context.Request);
            string requestBody = string.Empty;

            try
            {
                // 觸發用戶端連接事件
                OnClientConnected(clientId, clientIp);

                // WebSocket 請求處理
                if (context.Request.IsWebSocketRequest)
                {
                    await HandleWebSocketRequestAsync(context, clientId, clientIp);
                    return;
                }

                // 讀取請求內容（僅對 POST 請求）
                if (context.Request.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }

                    // 觸發訊息接收事件
                    OnMessageReceived(requestBody, clientId, clientIp);
                }

                // 處理請求並生成回應
                string response = await ProcessHttpRequestAsync(context, requestBody);

                // 發送回應
                await SendResponseAsync(context.Response, response);
            }
            catch (Exception ex)
            {
                // 發送錯誤回應
                var errorResponse = CreateErrorResponse($"處理請求時發生錯誤: {ex.Message}");
                await SendResponseAsync(context.Response, errorResponse);
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch
                {
                    // 忽略關閉回應時的錯誤
                }
            }
        }

        /// <summary>
        /// 處理 WebSocket 請求
        /// </summary>
        private async Task HandleWebSocketRequestAsync(HttpListenerContext context, string clientId, string clientIp)
        {
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                _ = Task.Run(() => HandleWebSocketClientAsync(webSocketContext.WebSocket, clientId, clientIp));
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"處理 WebSocket 請求時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理 WebSocket 用戶端
        /// </summary>
        private async Task HandleWebSocketClientAsync(WebSocket webSocket, string clientId, string clientIp)
        {
            var buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        
                        // 觸發 WebSocket 訊息事件
                        OnWebSocketMessageReceived(clientId, clientIp, message, webSocket);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "由伺服器關閉", CancellationToken.None);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"WebSocket 處理錯誤: {ex.Message}");
            }
            finally
            {
                OnClientDisconnected(clientId, clientIp, "WebSocket 連接關閉");
            }
        }

        /// <summary>
        /// 處理 HTTP 請求
        /// </summary>
        private async Task<string> ProcessHttpRequestAsync(HttpListenerContext context, string requestBody)
        {
            try
            {
                string method = context.Request.HttpMethod;
                string path = context.Request.Url.AbsolutePath;

                // 處理 API 請求
                if (path.StartsWith("/api/"))
                {
                    return await ProcessApiRequestAsync(context, requestBody);
                }

                // 處理靜態檔案請求
                if (method == "GET" && !string.IsNullOrEmpty(_staticFilesPath))
                {
                    return await ProcessStaticFileRequestAsync(context);
                }

                // 未知請求類型
                context.Response.StatusCode = 404;
                return CreateErrorResponse("找不到請求的資源");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"處理請求時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理 API 請求
        /// </summary>
        private async Task<string> ProcessApiRequestAsync(HttpListenerContext context, string requestBody)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;
                
                // 處理標準 DDS API
                if (context.Request.HttpMethod == "POST")
                {
                    // 解析 JSON 請求
                    var jsonRequest = JsonConvert.DeserializeObject<dynamic>(requestBody);
                    string serviceName = jsonRequest?.serviceName;

                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        return await ProcessStandardApiRequestAsync(serviceName, requestBody);
                    }
                }

                // 處理自訂 API
                var customApiArgs = new CustomApiRequestEventArgs
                {
                    Method = context.Request.HttpMethod,
                    Path = path,
                    RequestBody = requestBody,
                    QueryString = context.Request.Url.Query,
                    Headers = context.Request.Headers
                };

                OnCustomApiRequest(customApiArgs);

                if (customApiArgs.IsHandled)
                {
                    return customApiArgs.ResponseBody;
                }

                return CreateErrorResponse($"不支援的 API 端點: {path}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"處理 API 請求時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理標準 DDS API 請求
        /// </summary>
        private async Task<string> ProcessStandardApiRequestAsync(string serviceName, string requestBody)
        {
            await Task.Delay(10); // 模擬處理時間

            // 根據服務名稱處理不同的 API
            switch (serviceName)
            {
                case ApiServiceNames.SendMessageCommand:
                    return CreateSuccessResponse("SEND_MESSAGE_RESPONSE", "遠程資訊下發指令處理成功");

                case ApiServiceNames.CreateNeedleWorkorderCommand:
                    return CreateSuccessResponse("CREATE_NEEDLE_WORKORDER_RESPONSE", "派針工單建立指令處理成功");

                case ApiServiceNames.DateMessageCommand:
                    return CreateSuccessResponse("DATE_MESSAGE_RESPONSE", "設備時間同步指令處理成功");

                case ApiServiceNames.SwitchRecipeCommand:
                    return CreateSuccessResponse("SWITCH_RECIPE_RESPONSE", "刀具工鑽袋檔發送指令處理成功");

                case ApiServiceNames.DeviceControlCommand:
                    return CreateSuccessResponse("DEVICE_CONTROL_RESPONSE", "設備啟停控制指令處理成功");

                case ApiServiceNames.WarehouseResourceQueryCommand:
                    return CreateSuccessResponse("WAREHOUSE_RESOURCE_QUERY_RESPONSE", "倉庫資源查詢指令處理成功");

                case ApiServiceNames.ToolTraceHistoryQueryCommand:
                    return CreateSuccessResponse("TOOL_TRACE_HISTORY_QUERY_RESPONSE", "鑽針履歷查詢指令處理成功");

                default:
                    return CreateErrorResponse($"不支援的服務類型: {serviceName}");
            }
        }

        /// <summary>
        /// 處理靜態檔案請求
        /// </summary>
        private async Task<string> ProcessStaticFileRequestAsync(HttpListenerContext context)
        {
            try
            {
                string relativePath = context.Request.Url.AbsolutePath.Trim('/');
                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = "index.html"; // 預設為 index.html
                }

                string filePath = Path.Combine(_staticFilesPath, relativePath);

                if (File.Exists(filePath))
                {
                    await ServeStaticFileAsync(context, filePath);
                    return null; // 已直接處理回應
                }
                else
                {
                    context.Response.StatusCode = 404;
                    return CreateErrorResponse("找不到檔案");
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                return CreateErrorResponse($"處理靜態檔案時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 提供靜態檔案服務
        /// </summary>
        private async Task ServeStaticFileAsync(HttpListenerContext context, string filePath)
        {
            try
            {
                // 獲取檔案擴展名
                string extension = Path.GetExtension(filePath).ToLower();

                // 檢查是否支援該擴展名
                if (!_mimeTypes.TryGetValue(extension, out string mimeType))
                {
                    mimeType = "application/octet-stream";
                }

                // 讀取檔案內容
                byte[] buffer = await ReadAllBytesAsync(filePath);

                // 設定回應標頭
                context.Response.ContentType = mimeType;
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = 200;

                // 發送檔案內容
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (FileNotFoundException)
            {
                context.Response.StatusCode = 404;
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
            }
        }

        /// <summary>
        /// 非同步讀取檔案所有位元組
        /// </summary>
        private async Task<byte[]> ReadAllBytesAsync(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                var buffer = new byte[fs.Length];
                await fs.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        /// <summary>
        /// 發送 HTTP 回應
        /// </summary>
        private async Task SendResponseAsync(HttpListenerResponse response, string responseBody)
        {
            if (string.IsNullOrEmpty(responseBody))
                return; // 已經在其他地方處理回應

            try
            {
                response.ContentType = "application/json; charset=utf-8";
                response.StatusCode = response.StatusCode == 0 ? 200 : response.StatusCode;

                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"發送 HTTP 回應時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 建立成功回應
        /// </summary>
        private string CreateSuccessResponse(string serviceName, string message)
        {
            var response = new
            {
                responseID = Guid.NewGuid().ToString(),
                serviceName = serviceName,
                timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                statusCode = ResponseStatusCode.Success,
                statusMessage = message,
                data = new object[0],
                extendData = (object)null
            };

            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }

        /// <summary>
        /// 建立錯誤回應
        /// </summary>
        private string CreateErrorResponse(string errorMessage)
        {
            var response = new
            {
                responseID = Guid.NewGuid().ToString(),
                serviceName = "ERROR_RESPONSE",
                timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                statusCode = ResponseStatusCode.InternalServerError,
                statusMessage = errorMessage,
                data = new object[0],
                extendData = (object)null
            };

            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }

        /// <summary>
        /// 取得用戶端 IP 位址
        /// </summary>
        private string GetClientIpAddress(HttpListenerRequest request)
        {
            // 嘗試從 X-Forwarded-For 標頭取得真實 IP
            string forwardedFor = request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // 嘗試從 X-Real-IP 標頭取得真實 IP
            string realIp = request.Headers["X-Real-IP"];
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // 使用遠端端點 IP
            return request.RemoteEndPoint?.Address?.ToString() ?? "Unknown";
        }

        #endregion

        #region 事件觸發方法

        /// <summary>
        /// 觸發訊息接收事件
        /// </summary>
        private void OnMessageReceived(string message, string clientId, string clientIp)
        {
            MessageReceived?.Invoke(this, new MessageEventArgs
            {
                Message = message,
                ClientId = clientId,
                ClientIp = clientIp,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 觸發用戶端連接事件
        /// </summary>
        private void OnClientConnected(string clientId, string clientIp)
        {
            ConnectedClientCount++;
            ClientConnected?.Invoke(this, new ClientConnectedEventArgs
            {
                ClientId = clientId,
                ClientIp = clientIp,
                ConnectTime = DateTime.Now
            });
        }

        /// <summary>
        /// 觸發用戶端斷線事件
        /// </summary>
        private void OnClientDisconnected(string clientId, string clientIp, string reason = null)
        {
            ConnectedClientCount = Math.Max(0, ConnectedClientCount - 1);
            ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs
            {
                ClientId = clientId,
                ClientIp = clientIp,
                DisconnectTime = DateTime.Now,
                Reason = reason
            });
        }

        /// <summary>
        /// 觸發伺服器狀態變更事件
        /// </summary>
        private void OnServerStatusChanged(ServerStatus status, string description)
        {
            ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
            {
                Status = status,
                Description = description,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 觸發 WebSocket 訊息接收事件
        /// </summary>
        private void OnWebSocketMessageReceived(string clientId, string clientIp, string message, WebSocket webSocket)
        {
            WebSocketMessageReceived?.Invoke(this, new WebSocketMessageEventArgs
            {
                ClientId = clientId,
                ClientIp = clientIp,
                Message = message,
                WebSocket = webSocket,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 觸發自訂 API 請求事件
        /// </summary>
        private void OnCustomApiRequest(CustomApiRequestEventArgs args)
        {
            CustomApiRequest?.Invoke(this, args);
        }

        #endregion

        #region IDisposable 實作

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                    _httpListener?.Close();
                }
                _disposed = true;
            }
        }

        #endregion
    }
}
