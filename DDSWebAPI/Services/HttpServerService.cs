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
    /// HTTP 伺服器服務，處理來自 MES/IoT 系統的請求
    /// </summary>    public class HttpServerService : IDisposable
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
        /// 目前連接的用戶端數量
        /// </summary>
        public int ConnectedClientCount { get; private set; }

        #endregion

        #region 建構函式

        /// <summary>
        /// 初始化 HTTP 伺服器服務
        /// </summary>
        /// <param name="urlPrefix">監聽位址前綴</param>
        public HttpServerService(string urlPrefix = "http://localhost:8085/")
        {
            _urlPrefix = urlPrefix?.TrimEnd('/') + "/";
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

                // 讀取請求內容
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // 觸發訊息接收事件
                OnMessageReceived(requestBody, clientId, clientIp);

                // 處理請求並生成回應
                var response = await ProcessApiRequestAsync(requestBody, context.Request);

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
        /// 處理 API 請求
        /// </summary>
        private async Task<string> ProcessApiRequestAsync(string requestBody, HttpListenerRequest request)
        {
            try
            {
                // 解析 JSON 請求
                var jsonRequest = JsonConvert.DeserializeObject<dynamic>(requestBody);
                string serviceName = jsonRequest?.serviceName;

                if (string.IsNullOrEmpty(serviceName))
                {
                    return CreateErrorResponse("缺少 serviceName 參數");
                }

                // 根據服務名稱處理不同的 API
                switch (serviceName)
                {
                    case ApiServiceNames.SendMessageCommand:
                        return await ProcessSendMessageCommandAsync(requestBody);

                    case ApiServiceNames.CreateNeedleWorkorderCommand:
                        return await ProcessCreateNeedleWorkorderCommandAsync(requestBody);

                    case ApiServiceNames.DateMessageCommand:
                        return await ProcessDateMessageCommandAsync(requestBody);

                    case ApiServiceNames.SwitchRecipeCommand:
                        return await ProcessSwitchRecipeCommandAsync(requestBody);

                    case ApiServiceNames.DeviceControlCommand:
                        return await ProcessDeviceControlCommandAsync(requestBody);

                    case ApiServiceNames.WarehouseResourceQueryCommand:
                        return await ProcessWarehouseResourceQueryCommandAsync(requestBody);

                    case ApiServiceNames.ToolTraceHistoryQueryCommand:
                        return await ProcessToolTraceHistoryQueryCommandAsync(requestBody);

                    default:
                        return CreateErrorResponse($"不支援的服務類型: {serviceName}");
                }
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"處理 API 請求時發生錯誤: {ex.Message}");
            }
        }

        #region API 處理方法

        private async Task<string> ProcessSendMessageCommandAsync(string requestBody)
        {
            // 實作遠程資訊下發指令處理邏輯
            await Task.Delay(10); // 模擬處理時間
            return CreateSuccessResponse("SEND_MESSAGE_RESPONSE", "遠程資訊下發指令處理成功");
        }

        private async Task<string> ProcessCreateNeedleWorkorderCommandAsync(string requestBody)
        {
            // 實作派針工單建立指令處理邏輯
            await Task.Delay(10); // 模擬處理時間
            return CreateSuccessResponse("CREATE_NEEDLE_WORKORDER_RESPONSE", "派針工單建立指令處理成功");
        }

        private async Task<string> ProcessDateMessageCommandAsync(string requestBody)
        {
            // 實作設備時間同步指令處理邏輯
            await Task.Delay(10); // 模擬處理時間
            return CreateSuccessResponse("DATE_MESSAGE_RESPONSE", "設備時間同步指令處理成功");
        }

        private async Task<string> ProcessSwitchRecipeCommandAsync(string requestBody)
        {
            // 實作刀具工鑽袋檔發送指令處理邏輯
            await Task.Delay(10); // 模擬處理時間
            return CreateSuccessResponse("SWITCH_RECIPE_RESPONSE", "刀具工鑽袋檔發送指令處理成功");
        }

        private async Task<string> ProcessDeviceControlCommandAsync(string requestBody)
        {
            // 實作設備啟停控制指令處理邏輯
            await Task.Delay(10); // 模擬處理時間
            return CreateSuccessResponse("DEVICE_CONTROL_RESPONSE", "設備啟停控制指令處理成功");
        }

        private async Task<string> ProcessWarehouseResourceQueryCommandAsync(string requestBody)
        {
            // 實作倉庫資源查詢指令處理邏輯
            await Task.Delay(10); // 模擬處理時間
            return CreateSuccessResponse("WAREHOUSE_RESOURCE_QUERY_RESPONSE", "倉庫資源查詢指令處理成功");
        }

        private async Task<string> ProcessToolTraceHistoryQueryCommandAsync(string requestBody)
        {
            // 實作鑽針履歷查詢指令處理邏輯
            await Task.Delay(10); // 模擬處理時間
            return CreateSuccessResponse("TOOL_TRACE_HISTORY_QUERY_RESPONSE", "鑽針履歷查詢指令處理成功");
        }

        #endregion

        /// <summary>
        /// 發送 HTTP 回應
        /// </summary>
        private async Task SendResponseAsync(HttpListenerResponse response, string responseBody)
        {
            try
            {
                response.ContentType = "application/json; charset=utf-8";
                response.StatusCode = 200;

                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                // 記錄發送回應時的錯誤
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
