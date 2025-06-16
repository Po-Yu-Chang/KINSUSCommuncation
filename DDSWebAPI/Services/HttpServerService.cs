///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: HttpServerService.cs
// 檔案描述: 實現設備端 HTTP 伺服器服務，處理來自 MES/IoT 系統的請求以及客製化 API
// 功能概述:
//   1. 提供標準 MES API 接口服務 (遠程資訊下發、時間同步、設備控制等)
//   2. 支援客製化倉庫管理 API (入料、出料、位置查詢等)
//   3. WebSocket 即時通訊支援
//   4. 靜態檔案服務
//   5. 相依性注入架構，支援單元測試與擴充
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

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
using DDSWebAPI.Interfaces;
using DDSWebAPI.Models;
using DDSWebAPI.Events;
using DDSWebAPI.Enums;
using DDSWebAPI.Services.Handlers;

namespace DDSWebAPI.Services
{
    /// <summary>
    /// HTTP 伺服器服務主要類別
    /// 
    /// 功能說明:
    /// 1. 實現完整的 HTTP 伺服器功能，支援多執行緒併發處理
    /// 2. 提供標準 MES API 接口，符合工業 4.0 通訊規範
    /// 3. 支援客製化倉庫管理 API，滿足特定業務需求
    /// 4. 整合 WebSocket 即時通訊功能
    /// 5. 提供靜態檔案服務，支援 Web 介面
    /// 6. 採用相依性注入架構，提升程式碼可測試性和延展性
    /// 7. 完整的錯誤處理和日誌記錄機制
    /// 8. 支援優雅關閉和資源清理
    /// 
    /// 使用方式:
    /// ```csharp
    /// var server = new HttpServerService("http://localhost:8085/", "./wwwroot", 
    ///     databaseService, warehouseService, workflowService, configService, utilityService);
    /// await server.StartAsync();
    /// ```
    /// </summary>   
    public class HttpServerService : IDisposable
    {
        #region 私有欄位

        /// <summary>
        /// HTTP 監聽器，負責接收和處理 HTTP 請求
        /// </summary>
        private HttpListener _httpListener;

        /// <summary>
        /// 伺服器監聽狀態旗標
        /// 使用執行緒安全的方式控制伺服器啟停狀態
        /// </summary>
        private bool _isListening;

        /// <summary>
        /// HTTP 伺服器監聽的 URL 前綴
        /// 格式範例: "http://localhost:8085/"
        /// </summary>
        private readonly string _urlPrefix;

        /// <summary>
        /// 靜態檔案根目錄路徑
        /// 用於提供 HTML、CSS、JavaScript 等靜態檔案服務
        /// </summary>
        private readonly string _staticFilesPath;

        /// <summary>
        /// 執行緒同步鎖定物件
        /// 用於保護共享資源的執行緒安全存取
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>        /// MIME 類型對應表
        /// 用於根據檔案副檔名設定正確的 Content-Type
        /// </summary>
        private Dictionary<string, string> _mimeTypes;

        /// <summary>        /// 取消標記來源，用於優雅關閉伺服器
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region 處理器實例

        /// <summary>
        /// API 請求處理器
        /// </summary>
        private readonly ApiRequestHandler _apiRequestHandler;

        /// <summary>
        /// WebSocket 處理器
        /// </summary>
        private readonly WebSocketHandler _webSocketHandler;

        /// <summary>
        /// 靜態檔案處理器
        /// </summary>
        private readonly StaticFileHandler _staticFileHandler;

        /// <summary>
        /// 安全性中介軟體
        /// </summary>
        private readonly SecurityMiddleware _securityMiddleware;

        /// <summary>
        /// 效能控制器
        /// </summary>
        private readonly PerformanceController _performanceController;

        #endregion

        #region 連線管理

        /// <summary>
        /// 已連接的用戶端字典
        /// </summary>
        private readonly Dictionary<string, ClientConnection> _connectedClients;

        /// <summary>
        /// 用戶端連接鎖定物件
        /// </summary>
        private readonly object _clientsLock = new object();

        #endregion

        #region 相依性注入欄位

        /// <summary>
        /// 資料庫服務實例
        /// 透過相依性注入提供資料庫操作功能
        /// </summary>
        private readonly IDatabaseService _databaseService;

        /// <summary>
        /// 倉庫查詢服務實例
        /// 透過相依性注入提供倉庫管理業務邏輯
        /// </summary>
        private readonly IWarehouseQueryService _warehouseQueryService;

        /// <summary>
        /// 工作流程任務服務實例
        /// 透過相依性注入提供設備操作和工作流程控制
        /// </summary>
        private readonly IWorkflowTaskService _workflowTaskService;

        /// <summary>
        /// 全域配置服務實例
        /// 透過相依性注入管理系統全域設定
        /// </summary>
        private readonly IGlobalConfigService _globalConfigService;

        /// <summary>
        /// 公用程式服務實例
        /// 透過相依性注入提供各種輔助功能
        /// </summary>
        private readonly IUtilityService _utilityService;

        #endregion

        #region 事件定義

        /// <summary>
        /// 訊息接收事件
        /// 當伺服器接收到 HTTP 請求訊息時觸發
        /// 用於日誌記錄和訊息監控
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageReceived;

        /// <summary>
        /// 用戶端連接事件
        /// 當有新的用戶端建立連接時觸發
        /// 用於連接管理和統計
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 用戶端斷線事件
        /// 當用戶端連接中斷時觸發
        /// 用於資源清理和連接統計更新
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// 伺服器狀態變更事件
        /// 當伺服器狀態發生變化時觸發 (啟動、停止、錯誤等)
        /// 用於狀態監控和管理介面更新
        /// </summary>
        public event EventHandler<ServerStatusChangedEventArgs> ServerStatusChanged;

        /// <summary>
        /// WebSocket 訊息接收事件
        /// 當 WebSocket 連接接收到訊息時觸發
        /// 用於即時通訊功能和訊息廣播
        /// </summary>
        public event EventHandler<WebSocketMessageEventArgs> WebSocketMessageReceived;

        /// <summary>
        /// 自訂 API 處理事件
        /// 當收到無法識別的 API 請求時觸發
        /// 允許外部模組處理自訂的 API 端點
        /// </summary>
        public event EventHandler<CustomApiRequestEventArgs> CustomApiRequest;

        #endregion

        #region 公開屬性

        /// <summary>
        /// 取得伺服器目前是否正在監聽狀態
        /// 執行緒安全的屬性存取
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
        /// 取得伺服器監聽的 URL 前綴
        /// 唯讀屬性，在建構函式中設定
        /// </summary>
        public string UrlPrefix => _urlPrefix;

        /// <summary>
        /// 取得目前連接的用戶端數量
        /// 用於監控和管理用戶端連接
        /// </summary>
        public int ConnectedClientCount { get; private set; }

        /// <summary>
        /// 取得靜態檔案根目錄路徑
        /// 唯讀屬性，用於檔案服務配置
        /// </summary>
        public string StaticFilesPath => _staticFilesPath;

        /// <summary>
        /// 取得伺服器啟動時間
        /// 用於運行時間統計和監控
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>        /// 取得處理的請求總數
        /// 用於效能監控和統計
        /// </summary>
        private long _totalRequestsProcessed;
        
        /// <summary>
        /// 已處理的請求總數（公開屬性）
        /// </summary>
        public long TotalRequestsProcessed => _totalRequestsProcessed;

        #endregion

        #region 建構函式

        /// <summary>
        /// 初始化 HTTP 伺服器服務實例
        /// 
        /// 建構函式負責:
        /// 1. 驗證和設定基本參數
        /// 2. 注入相依性服務
        /// 3. 初始化 MIME 類型對應表
        /// 4. 設定預設值和初始狀態
        /// 
        /// </summary>
        /// <param name="urlPrefix">
        /// HTTP 伺服器監聽位址前綴
        /// 格式: "http://localhost:8085/" 或 "https://0.0.0.0:8085/"
        /// 預設值: "http://localhost:8085/"
        /// </param>
        /// <param name="staticFilesPath">
        /// 靜態檔案根目錄路徑，用於提供 HTML、CSS、JS 等檔案
        /// 可以是相對路徑或絕對路徑
        /// 預設值: 目前執行目錄
        /// </param>
        /// <param name="databaseService">
        /// 資料庫服務實例，為 null 時將無法使用資料庫相關功能
        /// 建議在生產環境中提供實際實現
        /// </param>
        /// <param name="warehouseQueryService">
        /// 倉庫查詢服務實例，為 null 時將無法使用倉庫管理功能
        /// 客製化 API 需要此服務才能正常運作
        /// </param>
        /// <param name="workflowTaskService">
        /// 工作流程任務服務實例，為 null 時將無法使用設備控制功能
        /// 包含機器人控制、速度調整等功能
        /// </param>
        /// <param name="globalConfigService">
        /// 全域配置服務實例，為 null 時將使用預設配置
        /// 管理系統全域設定和狀態
        /// </param>
        /// <param name="utilityService">
        /// 公用程式服務實例，為 null 時將無法使用工具方法
        /// 提供格式轉換、驗證等輔助功能
        /// </param>
        /// <exception cref="ArgumentException">當 urlPrefix 格式不正確時拋出</exception>
        /// <exception cref="DirectoryNotFoundException">當 staticFilesPath 目錄不存在且無法建立時拋出</exception>
        public HttpServerService(
            string urlPrefix = "http://localhost:8085/",
            string staticFilesPath = null,
            IDatabaseService databaseService = null,
            IWarehouseQueryService warehouseQueryService = null,
            IWorkflowTaskService workflowTaskService = null,
            IGlobalConfigService globalConfigService = null,
            IUtilityService utilityService = null)
        {
            // 驗證和設定 URL 前綴
            if (string.IsNullOrWhiteSpace(urlPrefix))
            {
                throw new ArgumentException("URL 前綴不能為空", nameof(urlPrefix));
            }

            // 確保 URL 前綴以斜線結尾
            _urlPrefix = urlPrefix.TrimEnd('/') + "/";

            // 驗證 URL 前綴格式
            if (!Uri.TryCreate(_urlPrefix, UriKind.Absolute, out Uri uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException($"無效的 URL 前綴格式: {urlPrefix}", nameof(urlPrefix));
            }

            // 設定靜態檔案路徑
            _staticFilesPath = staticFilesPath ?? Environment.CurrentDirectory;

            // 驗證靜態檔案目錄是否存在，不存在則嘗試建立
            if (!Directory.Exists(_staticFilesPath))
            {
                try
                {
                    Directory.CreateDirectory(_staticFilesPath);
                }
                catch (Exception ex)
                {
                    throw new DirectoryNotFoundException($"無法建立靜態檔案目錄: {_staticFilesPath}. 錯誤: {ex.Message}");
                }
            }

            // 注入相依性服務
            _databaseService = databaseService;
            _warehouseQueryService = warehouseQueryService;
            _workflowTaskService = workflowTaskService;
            _globalConfigService = globalConfigService;
            _utilityService = utilityService;            // 初始化 MIME 類型對應表
            InitializeMimeTypes();

            // 初始化連線管理
            _connectedClients = new Dictionary<string, ClientConnection>();

            // 初始化安全性中介軟體
            var validApiKeys = new[] { "default-api-key", "kinsus-api-key", "KINSUS-API-KEY-2024" };
            var ipWhitelist = new[] { "127.0.0.1", "localhost", "::1" };
            var secretKey = "kinsus-secret-key-2025";
            _securityMiddleware = new SecurityMiddleware(validApiKeys, ipWhitelist, secretKey);

            // 初始化效能控制器
            _performanceController = new PerformanceController(
                maxRequestsPerMinute: 100,
                maxConcurrentConnections: 20,
                maxDataSizeMB: 10,
                requestTimeoutSeconds: 30);

            // 初始化處理器
            _apiRequestHandler = new ApiRequestHandler(
                _databaseService, _warehouseQueryService, _workflowTaskService, 
                _globalConfigService, _utilityService);

            _webSocketHandler = new WebSocketHandler(_utilityService);
            _webSocketHandler.MessageReceived += OnWebSocketMessageReceived;
            _webSocketHandler.StatusChanged += (sender, args) => OnServerStatusChanged(args.Status, args.Message);

            _staticFileHandler = new StaticFileHandler(_staticFilesPath, _mimeTypes);

            // 初始化取消標記
            _cancellationTokenSource = new CancellationTokenSource();

            // 初始化統計資料
            ConnectedClientCount = 0;
            _totalRequestsProcessed = 0;
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 非同步啟動 HTTP 伺服器
        /// 
        /// 啟動流程:
        /// 1. 檢查伺服器是否已在執行中
        /// 2. 建立和配置 HttpListener
        /// 3. 開始監聽指定的 URL 前綴
        /// 4. 啟動請求處理迴圈
        /// 5. 觸發狀態變更事件
        /// 
        /// </summary>
        /// <returns>
        /// 布林值表示啟動結果
        /// true: 啟動成功或已在執行中
        /// false: 啟動失敗
        /// </returns>
        /// <exception cref="HttpListenerException">當 HTTP 監聽器無法啟動時拋出</exception>
        /// <exception cref="InvalidOperationException">當系統不支援 HttpListener 時拋出</exception>
        public async Task<bool> StartAsync()
        {
            try
            {
                // 檢查是否已在執行中
                if (IsListening)
                {
                    OnServerStatusChanged(ServerStatus.Warning, "伺服器已在執行中，無需重複啟動");
                    return true;
                }

                // 檢查系統是否支援 HttpListener
                if (!HttpListener.IsSupported)
                {
                    throw new InvalidOperationException("此系統不支援 HttpListener，需要 Windows XP SP2 或 Server 2003 以上版本");
                }

                // 觸發啟動中事件
                OnServerStatusChanged(ServerStatus.Starting, "正在啟動 HTTP 伺服器...");

                // 建立新的 HttpListener 實例
                _httpListener = new HttpListener();
                
                // 設定監聽前綴
                _httpListener.Prefixes.Add(_urlPrefix);

                // 設定 HttpListener 屬性 (可選)
                _httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                _httpListener.IgnoreWriteExceptions = true;

                // 啟動監聽
                _httpListener.Start();
                IsListening = true;
                StartTime = DateTime.Now;

                // 觸發成功啟動事件
                OnServerStatusChanged(ServerStatus.Running, $"HTTP 伺服器已啟動，監聽位址: {_urlPrefix}");                // 在背景啟動請求處理迴圈
                _ = Task.Run(ProcessRequestsAsync, _cancellationTokenSource.Token);

                // 啟動定期清理過期連接的任務
                _ = Task.Run(async () =>
                {
                    while (IsListening && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                            CleanupExpiredConnections();
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            OnServerStatusChanged(ServerStatus.Warning, $"清理過期連接時發生錯誤: {ex.Message}");
                        }
                    }
                }, _cancellationTokenSource.Token);

                return true;
            }
            catch (HttpListenerException hlex)
            {
                string errorMessage = $"啟動 HTTP 監聽器失敗: {hlex.Message} (錯誤程式碼: {hlex.ErrorCode})";
                
                // 提供常見錯誤的解決建議
                switch (hlex.ErrorCode)
                {
                    case 5: // 拒絕存取
                        errorMessage += "\n建議: 請以系統管理員身分執行，或使用 netsh 指令配置 URL 權限";
                        break;
                    case 183: // 位址已在使用中
                        errorMessage += "\n建議: 請檢查是否有其他程式正在使用相同的連接埠";
                        break;
                }

                OnServerStatusChanged(ServerStatus.Error, errorMessage);
                return false;
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"啟動 HTTP 伺服器時發生未預期的錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步停止 HTTP 伺服器
        /// 
        /// 停止流程:
        /// 1. 檢查伺服器是否正在執行
        /// 2. 設定停止旗標，阻止新請求
        /// 3. 等待目前處理中的請求完成
        /// 4. 關閉 HttpListener
        /// 5. 清理資源和重設狀態
        /// 6. 觸發狀態變更事件
        /// 
        /// </summary>
        public void Stop()
        {
            try
            {
                // 檢查是否正在執行
                if (!IsListening)
                {
                    OnServerStatusChanged(ServerStatus.Warning, "伺服器未在執行中，無需停止");
                    return;
                }

                OnServerStatusChanged(ServerStatus.Stopping, "正在停止 HTTP 伺服器...");

                // 設定停止旗標
                IsListening = false;

                // 取消所有進行中的非同步操作
                _cancellationTokenSource?.Cancel();                // 停止 HttpListener
                try
                {
                    _httpListener?.Stop();
                    _httpListener?.Close();
                }
                catch (Exception ex)
                {
                    OnServerStatusChanged(ServerStatus.Warning, $"關閉 HTTP 監聽器時發生警告: {ex.Message}");
                }

                // 清理所有連接
                lock (_clientsLock)
                {
                    var allClients = _connectedClients.Values.ToList();
                    foreach (var client in allClients)
                    {
                        OnClientDisconnected(client.Id, client.IpAddress, "伺服器停止");
                    }
                    _connectedClients.Clear();
                }

                // 重設狀態
                ConnectedClientCount = 0;
                StartTime = null;

                OnServerStatusChanged(ServerStatus.Stopped, "HTTP 伺服器已停止");
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"停止 HTTP 伺服器時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 非同步重新啟動 HTTP 伺服器
        /// 相當於依序執行 Stop() 和 StartAsync()
        /// </summary>
        /// <returns>重新啟動是否成功</returns>
        public async Task<bool> RestartAsync()
        {
            OnServerStatusChanged(ServerStatus.Restarting, "正在重新啟動 HTTP 伺服器...");
            
            Stop();
            
            // 等待一小段時間確保資源完全釋放
            await Task.Delay(1000);
            
            return await StartAsync();
        }        /// <summary>
        /// 取得伺服器統計資訊
        /// </summary>
        /// <returns>包含伺服器統計資訊的物件</returns>
        public object GetServerStatistics()
        {
            return new
            {
                IsListening = IsListening,
                UrlPrefix = _urlPrefix,
                ConnectedClientCount = ConnectedClientCount,
                TotalRequestsProcessed = TotalRequestsProcessed,
                StartTime = StartTime,
                Uptime = StartTime.HasValue ? DateTime.Now - StartTime.Value : TimeSpan.Zero,
                StaticFilesPath = _staticFilesPath
            };
        }

        /// <summary>
        /// 處理 IoT 連線測試請求
        /// </summary>
        /// <param name="requestBody">請求內容</param>
        /// <returns>連線測試回應</returns>
        private async Task<BaseResponse> ProcessConnectionTestAsync(string requestBody)
        {
            try
            {
                // 解析請求內容
                BaseRequest<object> request = null;
                if (!string.IsNullOrEmpty(requestBody))
                {
                    request = JsonConvert.DeserializeObject<BaseRequest<object>>(requestBody);
                }

                // 建立回應資料
                var responseData = new
                {
                    ConnectionStatus = "Success",
                    ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ServerStatus = "Running",
                    Message = "IoT 連線測試成功",
                    DeviceCode = request?.DevCode ?? "Unknown",
                    TestResult = new
                    {
                        Latency = $"{new Random().Next(10, 100)}ms",
                        NetworkStatus = "Connected",
                        ApiVersion = "1.0.0",
                        RequestReceived = DateTime.Now
                    }
                };

                return CreateSuccessResponse("IoT 連線測試成功", responseData);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"IoT 連線測試失敗: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法 - 請求處理核心

        /// <summary>
        /// 非同步處理 HTTP 請求的主要迴圈
        /// 
        /// 此方法在背景持續執行，負責:
        /// 1. 等待接收新的 HTTP 請求
        /// 2. 為每個請求建立獨立的處理任務
        /// 3. 處理監聽器異常和優雅關閉
        /// 4. 維護請求統計資訊
        /// 
        /// </summary>
        private async Task ProcessRequestsAsync()
        {
            while (IsListening && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // 等待接收 HTTP 請求
                    var context = await _httpListener.GetContextAsync();
                    
                    // 增加請求計數
                    Interlocked.Increment(ref _totalRequestsProcessed);

                    // 在背景處理請求，避免阻塞主迴圈
                    // 這樣可以同時處理多個請求，提高併發效能
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleRequestAsync(context);
                        }
                        catch (Exception ex)
                        {
                            OnServerStatusChanged(ServerStatus.Warning, $"處理個別請求時發生錯誤: {ex.Message}");
                        }
                    }, _cancellationTokenSource.Token);
                }
                catch (ObjectDisposedException)
                {
                    // HttpListener 已經被釋放，這是正常的關閉流程
                    break;
                }
                catch (HttpListenerException hlex) when (hlex.ErrorCode == 995)
                {
                    // 錯誤程式碼 995 表示操作因為取消而中止，這是正常的關閉流程
                    break;
                }
                catch (OperationCanceledException)
                {
                    // 操作被取消，這是正常的關閉流程
                    break;
                }
                catch (Exception ex)
                {
                    // 記錄非預期的錯誤，但繼續執行迴圈
                    OnServerStatusChanged(ServerStatus.Error, $"處理 HTTP 請求迴圈時發生錯誤: {ex.Message}");
                    
                    // 短暫延遲後繼續，避免錯誤迴圈
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }        /// <summary>
        /// 處理單一 HTTP 請求的完整流程
        /// 
        /// 處理流程:
        /// 1. 產生用戶端識別碼和取得 IP 位址
        /// 2. 效能和安全性檢查
        /// 3. 觸發用戶端連接事件
        /// 4. 判斷請求類型 (WebSocket/HTTP)
        /// 5. 讀取請求內容
        /// 6. 路由到對應的處理方法
        /// 7. 處理異常和清理資源
        /// 
        /// </summary>
        /// <param name="context">HTTP 請求上下文，包含請求和回應物件</param>
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            // 產生唯一的用戶端識別碼，用於追蹤和日誌記錄
            string clientId = _utilityService?.GenerateUniqueId() ?? Guid.NewGuid().ToString();           
            string clientIp = GetClientIpAddress(context.Request);
            string requestBody = string.Empty;
            string connectionToken = null;

            try
            {                // 1. 效能控制 - 檢查連線限制
                var concurrencyCheck = await _performanceController.CheckConcurrencyLimitAsync();
                if (!concurrencyCheck.IsAllowed)
                {
                    var errorResponse = CreateErrorResponse($"伺服器忙碌中：{concurrencyCheck.ErrorMessage}");
                    await SendResponseAsync(context.Response, errorResponse, HttpStatusCode.ServiceUnavailable);
                    return;
                }
                connectionToken = concurrencyCheck.ConnectionToken;// 2. 安全性檢查 - IP 白名單驗證
                var ipValidationResult = _securityMiddleware.ValidateIPWhitelist(clientIp);
                if (!ipValidationResult.IsValid)
                {
                    var errorResponse = CreateErrorResponse($"存取被拒絕：{ipValidationResult.ErrorMessage}");
                    await SendResponseAsync(context.Response, errorResponse, HttpStatusCode.Forbidden);
                    return;
                }

                // 3. 觸發用戶端連接事件，讓外部模組可以進行連接管理
                OnClientConnected(clientId, clientIp);
                RegisterClientConnection(clientId, clientIp);

                // 4. 檢查是否為 WebSocket 升級請求
                if (context.Request.IsWebSocketRequest)
                {
                    await HandleWebSocketRequestAsync(context, clientId);
                    return;
                }

                // 5. 讀取 HTTP 請求主體內容
                // 使用 UTF-8 編碼確保中文字元正確處理
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                }               
                // 6. 效能控制 - 檢查資料大小限制
                var dataSizeCheck = _performanceController.CheckDataSizeLimit(requestBody);
                if (!dataSizeCheck.IsAllowed)
                {
                    var errorResponse = CreateErrorResponse($"請求資料過大：{dataSizeCheck.ErrorMessage}");
                    await SendResponseAsync(context.Response, errorResponse, HttpStatusCode.RequestEntityTooLarge);
                    return;
                }                
                // 7. 效能控制 - 檢查請求頻率限制
                var rateLimitCheck = _performanceController.CheckRateLimit(clientIp);
                if (!rateLimitCheck.IsAllowed)
                {
                    var errorResponse = CreateErrorResponse($"請求頻率過高：{rateLimitCheck.ErrorMessage}");
                    // 使用 429 Too Many Requests - 在舊版 .NET 中可能不存在，改用 ServiceUnavailable
                    await SendResponseAsync(context.Response, errorResponse, HttpStatusCode.ServiceUnavailable);
                    return;
                }

                // 8. 安全性檢查 - API 金鑰驗證 (如果需要)
                if (context.Request.Url.AbsolutePath.StartsWith("/api/"))
                {
                    var authHeader = context.Request.Headers["Authorization"];
                    var authResult = _securityMiddleware.ValidateApiKey(authHeader);
                    if (!authResult.IsValid)
                    {
                        var errorResponse = CreateErrorResponse($"認證失敗：{authResult.ErrorMessage}");
                        await SendResponseAsync(context.Response, errorResponse, HttpStatusCode.Unauthorized);
                        return;
                    }
                }

                // 9. 觸發訊息接收事件，讓外部模組可以進行日誌記錄或監控
                if (!string.IsNullOrEmpty(requestBody))
                {
                    OnMessageReceived(requestBody, clientId, clientIp);
                }

                // 10. 路由請求到對應的處理方法
                await RouteRequestAsync(context, requestBody, clientId, clientIp);
            }
            catch (Exception ex)
            {
                // 處理請求過程中發生的任何異常
                try
                {
                    // 嘗試發送錯誤回應給用戶端
                    var errorResponse = CreateErrorResponse($"處理請求時發生錯誤: {ex.Message}");
                    await SendResponseAsync(context.Response, errorResponse, HttpStatusCode.InternalServerError);
                }
                catch
                {
                    // 如果連錯誤回應都無法發送，則忽略
                    // 這種情況通常發生在用戶端已經斷線的時候
                }

                // 記錄錯誤到日誌
                OnServerStatusChanged(ServerStatus.Error, $"處理來自 {clientIp} 的請求時發生錯誤: {ex.Message}");            }
            finally
            {
                try
                {
                    // 確保回應串流被正確關閉，釋放資源
                    context.Response.Close();
                }
                catch
                {
                    // 忽略關閉回應時的錯誤
                    // 這種情況通常發生在連接已經中斷的時候
                }                // 釋放效能控制器的連線資源
                if (!string.IsNullOrEmpty(connectionToken))
                {
                    _performanceController.ReleaseConcurrency(connectionToken);
                }

                // 移除用戶端連接記錄
                UnregisterClientConnection(clientId);

                // 觸發用戶端斷線事件
                OnClientDisconnected(clientId, clientIp, "請求處理完成");
            }
        }

        /// <summary>
        /// 路由 HTTP 請求到對應的處理方法
        /// 
        /// 路由規則:
        /// 1. POST /api/mes - 標準 MES API 端點
        /// 2. POST /api/* - 客製化 API 端點
        /// 3. GET /api/* - 查詢類 API 端點
        /// 4. GET /* - 靜態檔案服務
        /// 
        /// </summary>
        /// <param name="context">HTTP 請求上下文</param>
        /// <param name="requestBody">請求主體內容</param>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="clientIp">用戶端 IP 位址</param>
        private async Task RouteRequestAsync(HttpListenerContext context, string requestBody, string clientId, string clientIp)
        {
            var request = context.Request;
            var response = context.Response;
            string path = request.Url.AbsolutePath.ToLower(); // 轉換為小寫以便不區分大小寫比對
            string method = request.HttpMethod.ToUpper();

            try
            {
                // 記錄請求資訊 (可選，用於除錯)
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {method} {path} from {clientIp}");

                // POST 請求路由
                if (method == "POST")
                {
                    BaseResponse apiResponse = null;
                    
                    switch (path)
                    {
                        // 標準 MES API 統一端點
                        case "/api/mes":
                            apiResponse = await _apiRequestHandler.ProcessMesApiRequestAsync(requestBody, request);
                            break;

                        // === 標準 MES API v1 端點 ===
                        case "/api/v1/send_message":
                            apiResponse = await _apiRequestHandler.ProcessSendMessageCommandAsync(requestBody);
                            break;

                        case "/api/v1/create_workorder":
                            apiResponse = await _apiRequestHandler.ProcessCreateNeedleWorkorderCommandAsync(requestBody);
                            break;

                        case "/api/v1/sync_time":
                            apiResponse = await _apiRequestHandler.ProcessDateMessageCommandAsync(requestBody);
                            break;

                        case "/api/v1/switch_recipe":
                            apiResponse = await _apiRequestHandler.ProcessSwitchRecipeCommandAsync(requestBody);
                            break;

                        case "/api/v1/device_control":
                            apiResponse = await _apiRequestHandler.ProcessDeviceControlCommandAsync(requestBody);
                            break;

                        case "/api/v1/warehouse_query":
                            apiResponse = await _apiRequestHandler.ProcessWarehouseResourceQueryCommandAsync(requestBody);
                            break;

                        case "/api/v1/tool_history_query":
                            apiResponse = await _apiRequestHandler.ProcessToolTraceHistoryQueryCommandAsync(requestBody);
                            break;

                        case "/api/v1/tool_history_report":
                            apiResponse = await _apiRequestHandler.HandleToolTraceHistoryReportAsync(requestBody);
                            break;

                        // 客製化倉庫管理 API
                        case "/api/in-material":
                            apiResponse = await _apiRequestHandler.HandleInMaterialRequestAsync(requestBody);
                            break;

                        case "/api/out-material":
                            apiResponse = await _apiRequestHandler.HandleOutMaterialRequestAsync(requestBody);
                            break;

                        case "/api/getlocationbystorage":
                            apiResponse = await _apiRequestHandler.GetLocationByStorageAsync(requestBody);
                            break;

                        case "/api/getlocationbypin":
                            apiResponse = await _apiRequestHandler.GetLocationByPinAsync(requestBody);
                            break;

                        case "/api/operationclamp":
                            apiResponse = await _apiRequestHandler.HandleClampRequestAsync(requestBody);
                            break;

                        case "/api/changespeed":
                            apiResponse = await _apiRequestHandler.HandleSpeedRequestAsync(requestBody);
                            break;                        // IoT 連線測試 API
                        case "/api/connection":
                            apiResponse = await ProcessConnectionTestAsync(requestBody);
                            break;

                        // 系統管理 API
                        case "/api/server/status":
                            apiResponse = CreateSuccessResponse("伺服器運行正常", GetServerStatistics());
                            break;

                        case "/api/server/restart":
                            var restartSuccess = await RestartAsync();
                            apiResponse = CreateSuccessResponse($"伺服器重新啟動{(restartSuccess ? "成功" : "失敗")}", new { success = restartSuccess });
                            break;

                        default:
                            // 觸發自訂 API 處理事件
                            await HandleCustomApiRequest(context, path, method, requestBody);
                            return;
                    }

                    if (apiResponse != null)
                    {
                        await SendResponseAsync(response, apiResponse);
                    }
                    return;
                }
                // GET 請求路由
                else if (method == "GET")
                {
                    switch (path)
                    {
                        // 查詢類 API
                        case "/api/out-getpins":
                            var pinsResponse = await _apiRequestHandler.GetOutPinsDataAsync();
                            await SendResponseAsync(response, pinsResponse);
                            return;

                        case "/api/server/statistics":
                            var statsResponse = CreateSuccessResponse("統計資料查詢成功", GetServerStatistics());
                            await SendResponseAsync(response, statsResponse);
                            return;

                        case "/api/health":
                            var healthResponse = CreateSuccessResponse("健康檢查通過", new
                            {
                                status = "healthy",
                                timestamp = DateTime.Now,
                                uptime = StartTime.HasValue ? DateTime.Now - StartTime.Value : TimeSpan.Zero
                            });
                            await SendResponseAsync(response, healthResponse);
                            return;

                        default:
                            // 靜態檔案服務
                            await _staticFileHandler.HandleStaticFileRequestAsync(context, path);
                            return;
                    }
                }
                // OPTIONS 請求處理 (CORS 預檢)
                else if (method == "OPTIONS")
                {
                    await HandleOptionsRequestAsync(context);
                    return;
                }

                // 如果沒有任何處理器處理請求，回傳 404 Not Found
                response.StatusCode = 404;
                var notFoundResponse = CreateErrorResponse($"找不到請求的端點: {method} {path}");
                await SendResponseAsync(response, notFoundResponse, HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                // 路由過程中發生錯誤，回傳 500 Internal Server Error
                response.StatusCode = 500;
                var errorResponse = CreateErrorResponse($"路由請求時發生錯誤: {ex.Message}");
                await SendResponseAsync(response, errorResponse, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// 處理 WebSocket 升級請求
        /// </summary>
        /// <param name="context">HTTP 請求上下文</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleWebSocketRequestAsync(HttpListenerContext context, string clientId)
        {
            try
            {
                // 接受 WebSocket 升級請求
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                
                // 使用 WebSocketHandler 處理 WebSocket 通訊
                await _webSocketHandler.HandleWebSocketClientAsync(webSocketContext.WebSocket, clientId, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, $"處理 WebSocket 升級請求時發生錯誤: {ex.Message}");
                
                // 嘗試發送 HTTP 錯誤回應
                try
                {
                    context.Response.StatusCode = 400;
                    var errorBytes = Encoding.UTF8.GetBytes("WebSocket upgrade failed");
                    await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                }
                catch
                {
                    // 忽略發送錯誤回應時的異常
                }
            }
        }

        /// <summary>
        /// 處理自訂 API 請求
        /// </summary>
        private async Task HandleCustomApiRequest(HttpListenerContext context, string path, string method, string requestBody)
        {
            var customApiArgs = new CustomApiRequestEventArgs
            {
                Context = context,
                Path = path,
                Method = method,
                RequestBody = requestBody,
                Headers = new Dictionary<string, string>(),
                Timestamp = DateTime.Now
            };

            // 複製請求標頭到事件參數
            foreach (string headerName in context.Request.Headers.AllKeys)
            {
                customApiArgs.Headers[headerName] = context.Request.Headers[headerName];
            }

            CustomApiRequest?.Invoke(this, customApiArgs);

            // 檢查自訂處理是否已經處理了請求
            if (!customApiArgs.IsHandled)
            {
                context.Response.StatusCode = 404;
                var notFoundResponse = CreateErrorResponse($"找不到請求的端點: {method} {path}");
                await SendResponseAsync(context.Response, notFoundResponse, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// 處理 OPTIONS 請求 (CORS 預檢)
        /// </summary>
        private async Task HandleOptionsRequestAsync(HttpListenerContext context)
        {
            var response = context.Response;
            
            // 設定 CORS 標頭
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
            response.Headers.Add("Access-Control-Max-Age", "86400"); // 24 小時
            
            response.StatusCode = 200;
            response.ContentLength64 = 0;
            
            await Task.CompletedTask;
        }

        #endregion

        #region 私有方法 - 工具方法

        /// <summary>
        /// 初始化 MIME 類型對應表
        /// </summary>
        private void InitializeMimeTypes()
        {
            _mimeTypes = new Dictionary<string, string>
            {
                // 文件類型
                { ".html", "text/html; charset=utf-8" },
                { ".htm", "text/html; charset=utf-8" },
                { ".txt", "text/plain; charset=utf-8" },
                { ".xml", "application/xml; charset=utf-8" },
                { ".json", "application/json; charset=utf-8" },
                
                // 樣式和腳本
                { ".css", "text/css; charset=utf-8" },
                { ".js", "application/javascript; charset=utf-8" },
                { ".jsx", "text/jsx; charset=utf-8" },
                { ".ts", "application/typescript; charset=utf-8" },
                
                // 圖片類型
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".gif", "image/gif" },
                { ".bmp", "image/bmp" },
                { ".ico", "image/x-icon" },
                { ".svg", "image/svg+xml" },
                { ".webp", "image/webp" },
                
                // 影片和音訊
                { ".mp4", "video/mp4" },
                { ".avi", "video/x-msvideo" },
                { ".mov", "video/quicktime" },
                { ".mp3", "audio/mpeg" },
                { ".wav", "audio/wav" },
                
                // 文件格式
                { ".pdf", "application/pdf" },
                { ".doc", "application/msword" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { ".xls", "application/vnd.ms-excel" },
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                
                // 壓縮檔案
                { ".zip", "application/zip" },
                { ".rar", "application/x-rar-compressed" },
                { ".7z", "application/x-7z-compressed" },
                
                // 字型檔案
                { ".woff", "application/font-woff" },
                { ".woff2", "application/font-woff2" },
                { ".ttf", "application/font-ttf" },
                { ".eot", "application/vnd.ms-fontobject" }
            };
        }

        /// <summary>
        /// 取得用戶端 IP 位址
        /// </summary>
        /// <param name="request">HTTP 請求物件</param>
        /// <returns>用戶端 IP 位址字串</returns>
        private string GetClientIpAddress(HttpListenerRequest request)
        {
            // 檢查是否有 X-Forwarded-For 標頭 (Proxy 環境)
            string xForwardedFor = request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                // 取得第一個 IP (可能有多個 Proxy)
                string firstIp = xForwardedFor.Split(',')[0].Trim();
                return firstIp;
            }

            // 檢查是否有 X-Real-IP 標頭
            string xRealIp = request.Headers["X-Real-IP"];
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp.Trim();
            }

            // 使用 RemoteEndPoint 的 IP
            return request.RemoteEndPoint?.Address?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// 發送 API 回應
        /// </summary>
        /// <param name="response">HTTP 回應物件</param>
        /// <param name="apiResponse">API 回應資料</param>
        /// <param name="statusCode">HTTP 狀態碼</param>
        private async Task SendResponseAsync(HttpListenerResponse response, BaseResponse apiResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                // 設定 CORS 標頭
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
                
                // 序列化回應資料
                var jsonResponse = JsonConvert.SerializeObject(apiResponse, Formatting.Indented);
                var bytes = Encoding.UTF8.GetBytes(jsonResponse);

                // 設定回應屬性
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = bytes.Length;
                response.StatusCode = (int)statusCode;

                // 寫入回應資料
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Warning, $"發送 API 回應時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 建立成功回應
        /// </summary>
        /// <param name="message">回應訊息</param>
        /// <param name="data">回應資料</param>
        /// <returns>成功回應物件</returns>
        private BaseResponse CreateSuccessResponse(string message, object data = null)
        {
            return new BaseResponse
            {
                IsSuccess = true,
                Message = message,
                Data = data,
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// 建立錯誤回應
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        /// <returns>錯誤回應物件</returns>
        private BaseResponse CreateErrorResponse(string message)
        {
            return new BaseResponse
            {
                IsSuccess = false,
                Message = message,
                Data = null,
                Timestamp = DateTime.Now
            };
        }

        #endregion

        #region 事件觸發方法

        /// <summary>
        /// 觸發伺服器狀態變更事件
        /// </summary>
        /// <param name="status">伺服器狀態</param>
        /// <param name="message">狀態訊息</param>
        protected virtual void OnServerStatusChanged(ServerStatus status, string message)
        {
            ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs(status, message));
        }

        /// <summary>
        /// 觸發用戶端連接事件
        /// </summary>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="clientIp">用戶端 IP 位址</param>
        /// <param name="connectionType">連接類型</param>
        protected virtual void OnClientConnected(string clientId, string clientIp, string connectionType = "HTTP")
        {
            ConnectedClientCount++;
            ClientConnected?.Invoke(this, new ClientConnectedEventArgs(clientId, clientIp, connectionType));
        }

        /// <summary>
        /// 觸發用戶端斷線事件
        /// </summary>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="clientIp">用戶端 IP 位址</param>
        /// <param name="reason">斷線原因</param>
        protected virtual void OnClientDisconnected(string clientId, string clientIp, string reason)
        {
            ConnectedClientCount = Math.Max(0, ConnectedClientCount - 1);
            ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(clientId, clientIp, reason));
        }

        /// <summary>
        /// 觸發訊息接收事件
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="clientIp">用戶端 IP 位址</param>
        protected virtual void OnMessageReceived(string message, string clientId, string clientIp)
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
        /// 觸發 WebSocket 訊息接收事件
        /// </summary>
        /// <param name="sender">事件發送者</param>
        /// <param name="e">WebSocket 訊息事件參數</param>
        private void OnWebSocketMessageReceived(object sender, WebSocketMessageEventArgs e)
        {
            WebSocketMessageReceived?.Invoke(this, e);
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
                    _cancellationTokenSource?.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        #region 連線管理方法

        /// <summary>
        /// 註冊用戶端連接
        /// </summary>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="clientIp">用戶端 IP 位址</param>
        private void RegisterClientConnection(string clientId, string clientIp)
        {
            lock (_clientsLock)
            {
                if (!_connectedClients.ContainsKey(clientId))
                {
                    var connection = new ClientConnection
                    {
                        Id = clientId,
                        IpAddress = clientIp,
                        ConnectTime = DateTime.Now,
                        LastActivityTime = DateTime.Now,
                        RequestType = "HTTP"
                    };
                    _connectedClients[clientId] = connection;
                    
                    // 更新連接統計
                    ConnectedClientCount = _connectedClients.Count;
                }
                else
                {
                    // 更新最後活動時間
                    _connectedClients[clientId].LastActivityTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// 取消註冊用戶端連接
        /// </summary>
        /// <param name="clientId">用戶端識別碼</param>
        private void UnregisterClientConnection(string clientId)
        {
            lock (_clientsLock)
            {
                if (_connectedClients.ContainsKey(clientId))
                {
                    _connectedClients.Remove(clientId);
                    
                    // 更新連接統計
                    ConnectedClientCount = _connectedClients.Count;
                }
            }
        }

        /// <summary>
        /// 取得所有已連接的用戶端
        /// </summary>
        /// <returns>用戶端連接列表</returns>
        public List<ClientConnection> GetConnectedClients()
        {
            lock (_clientsLock)
            {
                return _connectedClients.Values.ToList();
            }
        }

        /// <summary>
        /// 清理過期的連接記錄
        /// </summary>
        /// <param name="timeoutMinutes">逾時分鐘數，預設5分鐘</param>
        public void CleanupExpiredConnections(int timeoutMinutes = 5)
        {
            var expiredTime = DateTime.Now.AddMinutes(-timeoutMinutes);
            var expiredClients = new List<string>();

            lock (_clientsLock)
            {
                foreach (var kvp in _connectedClients)
                {
                    if (kvp.Value.LastActivityTime < expiredTime)
                    {
                        expiredClients.Add(kvp.Key);
                    }
                }

                foreach (var clientId in expiredClients)
                {
                    var connection = _connectedClients[clientId];
                    _connectedClients.Remove(clientId);
                    
                    // 觸發斷線事件
                    OnClientDisconnected(clientId, connection.IpAddress, "連接逾時");
                }

                if (expiredClients.Count > 0)
                {
                    ConnectedClientCount = _connectedClients.Count;
                    OnServerStatusChanged(ServerStatus.Info, $"清理了 {expiredClients.Count} 個過期連接");
                }
            }
        }

        #endregion
    }
}
