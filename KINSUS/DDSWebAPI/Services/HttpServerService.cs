using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DDSWebAPI.Models;

namespace DDSWebAPI.Services
{
    /// <summary>
    /// HTTP 伺服器服務類別
    /// </summary>
    public class HttpServerService
    {
        #region 私有欄位

        private HttpListener _httpListener;
        private bool _isRunning;
        private int _port;
        private string _staticFilesPath;
        private readonly Dictionary<string, string> _mimeTypes;

        #endregion

        #region 公用屬性

        /// <summary>
        /// 伺服器是否執行中
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 伺服器埠號
        /// </summary>
        public int Port => _port;

        /// <summary>
        /// 連接的用戶端數量
        /// </summary>
        public int ConnectedClientCount { get; private set; }

        #endregion

        #region 事件

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
        /// 自訂 API 請求事件
        /// </summary>
        public event EventHandler<CustomApiRequestEventArgs> CustomApiRequest;

        #endregion

        #region 建構函式

        /// <summary>
        /// 初始化 HttpServerService
        /// </summary>
        public HttpServerService()
        {
            _mimeTypes = new Dictionary<string, string>
            {
                { ".html", "text/html" },
                { ".css", "text/css" },
                { ".js", "application/javascript" },
                { ".json", "application/json" },
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".gif", "image/gif" },
                { ".ico", "image/x-icon" }
            };
        }

        #endregion

        #region 公用方法

        /// <summary>
        /// 啟動 HTTP 伺服器
        /// </summary>
        /// <param name="port">埠號</param>
        /// <param name="staticFilesPath">靜態檔案路徑</param>
        public async Task<bool> StartAsync(int port, string staticFilesPath = null)
        {
            try
            {
                if (_isRunning)
                {
                    return false;
                }

                _port = port;
                _staticFilesPath = staticFilesPath;

                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{port}/");

                _httpListener.Start();
                _isRunning = true;

                OnServerStatusChanged(ServerStatus.Running, $"伺服器已啟動於埠號 {port}");

                // 開始接受請求
                _ = Task.Run(ProcessRequestsAsync);

                return true;
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"啟動伺服器失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止 HTTP 伺服器
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                if (!_isRunning)
                {
                    return;
                }

                _isRunning = false;
                _httpListener?.Stop();
                _httpListener?.Close();

                OnServerStatusChanged(ServerStatus.Stopped, "伺服器已停止");

                await Task.Delay(100); // 給予時間讓所有連接關閉
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"停止伺服器時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 處理 HTTP 請求
        /// </summary>
        private async Task ProcessRequestsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
                catch (HttpListenerException)
                {
                    // 伺服器已停止時會拋出此例外，這是正常的
                    break;
                }
                catch (Exception ex)
                {
                    OnServerStatusChanged(ServerStatus.Error, $"處理請求時發生錯誤: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 處理單一 HTTP 請求
        /// </summary>
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string clientEndpoint = request.RemoteEndPoint?.ToString() ?? "Unknown";
                string clientId = Guid.NewGuid().ToString();

                OnClientConnected(clientId, clientEndpoint);

                // 處理不同類型的請求
                if (request.Url.AbsolutePath.StartsWith("/api/"))
                {
                    await HandleApiRequestAsync(context, clientId);
                }
                else
                {
                    await HandleStaticFileRequestAsync(context);
                }

                OnClientDisconnected(clientId, clientEndpoint);
            }
            catch (Exception ex)
            {
                try
                {
                    response.StatusCode = 500;
                    byte[] buffer = Encoding.UTF8.GetBytes($"Internal Server Error: {ex.Message}");
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                catch
                {
                    // 忽略回應寫入錯誤
                }
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch
                {
                    // 忽略關閉錯誤
                }
            }
        }

        /// <summary>
        /// 處理 API 請求
        /// </summary>
        private async Task HandleApiRequestAsync(HttpListenerContext context, string clientId)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string requestBody = "";
                if (request.HasEntityBody)
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }
                }

                string serviceName = request.Url.AbsolutePath.Substring(5); // 移除 "/api/" 前綴

                // 觸發自訂 API 請求事件
                CustomApiRequest?.Invoke(this, new CustomApiRequestEventArgs(serviceName, requestBody, clientId));

                // 預設回應
                response.ContentType = "application/json";
                response.StatusCode = 200;

                string responseJson = "{\"status\":\"success\",\"message\":\"API request received\"}";
                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                byte[] buffer = Encoding.UTF8.GetBytes($"{{\"status\":\"error\",\"message\":\"{ex.Message}\"}}");
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// 處理靜態檔案請求
        /// </summary>
        private async Task HandleStaticFileRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string filePath = request.Url.AbsolutePath;
                if (filePath == "/")
                {
                    filePath = "/index.html";
                }

                string fullPath = Path.Combine(_staticFilesPath ?? "", filePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    string extension = Path.GetExtension(fullPath).ToLowerInvariant();
                    if (_mimeTypes.TryGetValue(extension, out string mimeType))
                    {
                        response.ContentType = mimeType;
                    }

                    byte[] fileBytes = File.ReadAllBytes(fullPath);
                    response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
                else
                {
                    response.StatusCode = 404;
                    byte[] buffer = Encoding.UTF8.GetBytes("File not found");
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                byte[] buffer = Encoding.UTF8.GetBytes($"Server Error: {ex.Message}");
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        #endregion

        #region 事件觸發方法

        /// <summary>
        /// 觸發訊息接收事件
        /// </summary>
        private void OnMessageReceived(string message, string clientId, string clientIp)
        {
            WebSocketMessageReceived?.Invoke(this, new WebSocketMessageEventArgs(clientId, message));
        }

        /// <summary>
        /// 觸發用戶端連接事件
        /// </summary>
        private void OnClientConnected(string clientId, string clientIp)
        {
            ConnectedClientCount++;
            ClientConnected?.Invoke(this, new ClientConnectedEventArgs(clientId, clientIp));
        }

        /// <summary>
        /// 觸發用戶端斷線事件
        /// </summary>
        private void OnClientDisconnected(string clientId, string clientIp, string reason = null)
        {
            ConnectedClientCount = Math.Max(0, ConnectedClientCount - 1);
            var args = new ClientDisconnectedEventArgs(clientId, clientIp);
            args.Reason = reason;
            ClientDisconnected?.Invoke(this, args);
        }

        /// <summary>
        /// 觸發伺服器狀態變更事件
        /// </summary>
        private void OnServerStatusChanged(ServerStatus status, string description)
        {
            bool isRunning = status == ServerStatus.Running;
            var args = new ServerStatusChangedEventArgs(isRunning, $"http://localhost:{Port}");
            args.Description = description;
            ServerStatusChanged?.Invoke(this, args);
        }

        #endregion

        #region IDisposable 實作

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            _httpListener?.Close();
        }

        #endregion
    }
}
