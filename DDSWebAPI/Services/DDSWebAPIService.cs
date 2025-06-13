using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DDSWebAPI.Models;
using DDSWebAPI.Events;

namespace DDSWebAPI.Services
{
    /// <summary>
    /// DDS Web API 主要服務類別
    /// 提供配針機與 MES/IoT 系統的完整通訊功能
    /// </summary>
    public class DDSWebAPIService : INotifyPropertyChanged, IDisposable
    {
        #region 私有欄位

        private HttpServerService _httpServerService;
        private ApiClientService _apiClientService;
        private SecurityMiddleware _securityMiddleware;
        private PerformanceController _performanceController;
        private ApiCommandProcessor _commandProcessor;
        private ObservableCollection<ClientConnection> _clientConnections;
        private bool _isServerRunning;
        private string _serverUrl;
        private string _remoteApiUrl;
        private string _deviceCode;
        private string _operatorName;

        #endregion

        #region 事件定義

        /// <summary>
        /// 屬性變更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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
        /// API 呼叫成功事件
        /// </summary>
        public event EventHandler<ApiCallSuccessEventArgs> ApiCallSuccess;

        /// <summary>
        /// API 呼叫失敗事件
        /// </summary>
        public event EventHandler<ApiCallFailureEventArgs> ApiCallFailure;

        /// <summary>
        /// 日誌訊息事件
        /// </summary>
        public event EventHandler<LogMessageEventArgs> LogMessage;

        #endregion

        #region 屬性

        /// <summary>
        /// 伺服器是否正在執行
        /// </summary>
        public bool IsServerRunning
        {
            get => _isServerRunning;
            private set
            {
                if (_isServerRunning != value)
                {
                    _isServerRunning = value;
                    OnPropertyChanged(nameof(IsServerRunning));
                }
            }
        }

        /// <summary>
        /// 伺服器 URL
        /// </summary>
        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                if (_serverUrl != value)
                {
                    _serverUrl = value;
                    OnPropertyChanged(nameof(ServerUrl));
                }
            }
        }

        /// <summary>
        /// 遠端 API URL
        /// </summary>
        public string RemoteApiUrl
        {
            get => _remoteApiUrl;
            set
            {
                if (_remoteApiUrl != value)
                {
                    _remoteApiUrl = value;
                    OnPropertyChanged(nameof(RemoteApiUrl));
                    _apiClientService?.SetBaseUrl(value);
                }
            }
        }

        /// <summary>
        /// 設備代碼
        /// </summary>
        public string DeviceCode
        {
            get => _deviceCode;
            set
            {
                if (_deviceCode != value)
                {
                    _deviceCode = value;
                    OnPropertyChanged(nameof(DeviceCode));
                }
            }
        }

        /// <summary>
        /// 操作人員名稱
        /// </summary>
        public string OperatorName
        {
            get => _operatorName;
            set
            {
                if (_operatorName != value)
                {
                    _operatorName = value;
                    OnPropertyChanged(nameof(OperatorName));
                }
            }
        }

        /// <summary>
        /// 用戶端連接集合
        /// </summary>
        public ObservableCollection<ClientConnection> ClientConnections => _clientConnections;

        /// <summary>
        /// 目前連接的用戶端數量
        /// </summary>
        public int ConnectedClientCount => _httpServerService?.ConnectedClientCount ?? 0;

        #endregion

        #region 建構函式

        /// <summary>
        /// 初始化 DDS Web API 服務（使用預設設定）
        /// </summary>
        public DDSWebAPIService()
        {
            _clientConnections = new ObservableCollection<ClientConnection>();
            
            // 設定預設值
            ServerUrl = "http://localhost:8085/";
            RemoteApiUrl = "http://localhost:8086/";
            DeviceCode = "KINSUS001";
            OperatorName = "SYSTEM";

            InitializeServices();
        }

        /// <summary>
        /// 初始化 DDS Web API 服務（使用參數）
        /// </summary>
        /// <param name="serverUrl">伺服器監聽 URL</param>
        /// <param name="remoteApiUrl">遠端 API URL</param>
        /// <param name="deviceCode">設備代碼</param>
        /// <param name="operatorName">操作人員名稱</param>
        public DDSWebAPIService(string serverUrl, string remoteApiUrl, string deviceCode, string operatorName)
        {
            _clientConnections = new ObservableCollection<ClientConnection>();
            
            ServerUrl = serverUrl;
            RemoteApiUrl = remoteApiUrl;
            DeviceCode = deviceCode;
            OperatorName = operatorName;

            InitializeServices();
        }

        /// <summary>
        /// 初始化 DDS Web API 服務（使用設定檔）
        /// </summary>
        /// <param name="configFilePath">設定檔路徑</param>
        public DDSWebAPIService(string configFilePath)
        {
            _clientConnections = new ObservableCollection<ClientConnection>();
            
            try
            {
                var configManager = new ConfigurationManager(configFilePath);
                var appConfig = AppConfiguration.LoadFromConfigManager(configManager);
                
                ServerUrl = appConfig.Server.ServerUrl;
                RemoteApiUrl = appConfig.Server.RemoteApiUrl;
                DeviceCode = appConfig.Server.DeviceCode;
                OperatorName = appConfig.Server.OperatorName;

                InitializeServices(appConfig);
            }
            catch (Exception ex)
            {
                OnLogMessage(LogLevel.Error, $"載入設定檔失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 初始化 DDS Web API 服務（使用應用程式設定物件）
        /// </summary>
        /// <param name="appConfig">應用程式設定</param>
        public DDSWebAPIService(AppConfiguration appConfig)
        {
            _clientConnections = new ObservableCollection<ClientConnection>();
            
            ServerUrl = appConfig.Server.ServerUrl;
            RemoteApiUrl = appConfig.Server.RemoteApiUrl;
            DeviceCode = appConfig.Server.DeviceCode;
            OperatorName = appConfig.Server.OperatorName;

            InitializeServices(appConfig);
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 啟動 HTTP 伺服器
        /// </summary>
        /// <returns>是否啟動成功</returns>
        public async Task<bool> StartServerAsync()
        {
            try
            {
                OnLogMessage(LogLevel.Info, "正在啟動 HTTP 伺服器...");
                
                if (_httpServerService == null)
                {
                    InitializeHttpServer();
                }

                bool success = await _httpServerService.StartAsync();
                IsServerRunning = success;

                if (success)
                {
                    OnLogMessage(LogLevel.Info, $"HTTP 伺服器啟動成功，監聽位址: {ServerUrl}");
                }
                else
                {
                    OnLogMessage(LogLevel.Error, "HTTP 伺服器啟動失敗");
                }

                return success;
            }
            catch (Exception ex)
            {
                OnLogMessage(LogLevel.Error, $"啟動 HTTP 伺服器時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止 HTTP 伺服器
        /// </summary>
        public void StopServer()
        {
            try
            {
                OnLogMessage(LogLevel.Info, "正在停止 HTTP 伺服器...");
                
                _httpServerService?.Stop();
                IsServerRunning = false;
                
                // 清空用戶端連接列表
                _clientConnections.Clear();
                
                OnLogMessage(LogLevel.Info, "HTTP 伺服器已停止");
            }
            catch (Exception ex)
            {
                OnLogMessage(LogLevel.Error, $"停止 HTTP 伺服器時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 發送配針回報
        /// </summary>
        /// <param name="reportData">配針回報資料</param>
        /// <returns>API 呼叫結果</returns>
        public async Task<ApiCallResult> SendToolOutputReportAsync(ToolOutputReportData reportData)
        {
            var request = CreateBaseRequest(new List<ToolOutputReportData> { reportData });
            return await _apiClientService.SendToolOutputReportAsync(request);
        }

        /// <summary>
        /// 發送錯誤回報
        /// </summary>
        /// <param name="errorData">錯誤回報資料</param>
        /// <returns>API 呼叫結果</returns>
        public async Task<ApiCallResult> SendErrorReportAsync(ErrorReportData errorData)
        {
            var request = CreateBaseRequest(new List<ErrorReportData> { errorData });
            return await _apiClientService.SendErrorReportAsync(request);
        }

        /// <summary>
        /// 發送機臺狀態回報
        /// </summary>
        /// <param name="statusData">機臺狀態資料</param>
        /// <returns>API 呼叫結果</returns>
        public async Task<ApiCallResult> SendMachineStatusReportAsync(MachineStatusReportData statusData)
        {
            var request = CreateBaseRequest(new List<MachineStatusReportData> { statusData });
            return await _apiClientService.SendMachineStatusReportAsync(request);
        }

        /// <summary>
        /// 重新載入服務設定
        /// </summary>
        public void ReloadConfiguration()
        {
            OnLogMessage(LogLevel.Info, "重新載入服務設定");
            
            // 如果伺服器正在執行，需要重新啟動
            bool wasRunning = IsServerRunning;
            if (wasRunning)
            {
                StopServer();
            }

            // 重新初始化服務
            InitializeServices();

            // 如果之前在執行，重新啟動
            if (wasRunning)
            {
                _ = StartServerAsync();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化服務（使用預設設定）
        /// </summary>
        private void InitializeServices()
        {
            InitializeSecurityMiddleware();
            InitializePerformanceController();
            InitializeCommandProcessor();
            InitializeHttpServer();
            InitializeApiClient();
        }

        /// <summary>
        /// 初始化服務（使用應用程式設定）
        /// </summary>
        /// <param name="appConfig">應用程式設定</param>
        private void InitializeServices(AppConfiguration appConfig)
        {
            InitializeSecurityMiddleware(appConfig.Security);
            InitializePerformanceController(appConfig.Performance);
            InitializeCommandProcessor();
            InitializeHttpServer();
            InitializeApiClient();
        }

        /// <summary>
        /// 初始化 HTTP 伺服器
        /// </summary>
        private void InitializeHttpServer()
        {
            // 釋放舊的服務
            if (_httpServerService != null)
            {
                _httpServerService.MessageReceived -= OnHttpServerMessageReceived;
                _httpServerService.ClientConnected -= OnHttpServerClientConnected;
                _httpServerService.ClientDisconnected -= OnHttpServerClientDisconnected;
                _httpServerService.ServerStatusChanged -= OnHttpServerStatusChanged;
                _httpServerService.Dispose();
            }

            // 建立新的服務
            _httpServerService = new HttpServerService(ServerUrl);
            
            // 註冊事件
            _httpServerService.MessageReceived += OnHttpServerMessageReceived;
            _httpServerService.ClientConnected += OnHttpServerClientConnected;
            _httpServerService.ClientDisconnected += OnHttpServerClientDisconnected;
            _httpServerService.ServerStatusChanged += OnHttpServerStatusChanged;
        }

        /// <summary>
        /// 初始化 API 用戶端
        /// </summary>
        private void InitializeApiClient()
        {
            // 釋放舊的服務
            if (_apiClientService != null)
            {
                _apiClientService.ApiCallSuccess -= OnApiClientCallSuccess;
                _apiClientService.ApiCallFailure -= OnApiClientCallFailure;
                _apiClientService.Dispose();
            }

            // 建立新的服務
            _apiClientService = new ApiClientService(RemoteApiUrl);
            
            // 註冊事件
            _apiClientService.ApiCallSuccess += OnApiClientCallSuccess;
            _apiClientService.ApiCallFailure += OnApiClientCallFailure;
        }        /// <summary>
        /// 初始化安全性中介軟體（使用預設設定）
        /// </summary>
        private void InitializeSecurityMiddleware()
        {
            // 設定有效的 API 金鑰列表
            var validApiKeys = new[]
            {
                "KINSUS_API_KEY_001",
                "KINSUS_API_KEY_002"
            };

            // 設定 IP 白名單（空白表示允許所有 IP）
            var ipWhitelist = new string[0];

            // 設定簽章密鑰
            var secretKey = "KINSUS_SECRET_KEY_2025";

            _securityMiddleware = new SecurityMiddleware(validApiKeys, ipWhitelist, secretKey);
        }

        /// <summary>
        /// 初始化安全性中介軟體（使用設定檔）
        /// </summary>
        /// <param name="securityConfig">安全性設定</param>
        private void InitializeSecurityMiddleware(SecurityConfiguration securityConfig)
        {
            // 設定有效的 API 金鑰列表
            var validApiKeys = string.IsNullOrEmpty(securityConfig.ApiKey) 
                ? new[] { "KINSUS_API_KEY_001", "KINSUS_API_KEY_002" }
                : new[] { securityConfig.ApiKey };

            // 設定 IP 白名單
            var ipWhitelist = securityConfig.AllowedIpAddresses ?? new string[0];

            // 設定簽章密鑰
            var secretKey = string.IsNullOrEmpty(securityConfig.SignatureSecret) 
                ? "KINSUS_SECRET_KEY_2025" 
                : securityConfig.SignatureSecret;

            _securityMiddleware = new SecurityMiddleware(validApiKeys, ipWhitelist, secretKey)
            {
                EnableApiKeyValidation = securityConfig.EnableApiKeyValidation,
                EnableSignatureValidation = securityConfig.EnableSignatureValidation,
                EnableIpWhitelist = securityConfig.EnableIpWhitelist
            };
        }        /// <summary>
        /// 初始化效能控制器（使用預設設定）
        /// </summary>
        private void InitializePerformanceController()
        {
            _performanceController = new PerformanceController(
                maxRequestsPerMinute: 100,
                maxConcurrentConnections: 10,
                maxDataSizeMB: 10,
                requestTimeoutSeconds: 30
            );
        }

        /// <summary>
        /// 初始化效能控制器（使用設定檔）
        /// </summary>
        /// <param name="performanceConfig">效能設定</param>
        private void InitializePerformanceController(PerformanceConfiguration performanceConfig)
        {
            _performanceController = new PerformanceController(
                maxRequestsPerMinute: performanceConfig.MaxRequestsPerMinute,
                maxConcurrentConnections: performanceConfig.MaxConcurrentConnections,
                maxDataSizeMB: (int)(performanceConfig.MaxRequestSizeBytes / (1024 * 1024)), // 轉換為 MB
                requestTimeoutSeconds: performanceConfig.RequestTimeoutSeconds
            );
        }

        /// <summary>
        /// 初始化指令處理器
        /// </summary>
        private void InitializeCommandProcessor()
        {
            _commandProcessor = new ApiCommandProcessor();
            
            // 註冊事件
            _commandProcessor.CommandProcessed += OnCommandProcessed;
            _commandProcessor.LogMessage += OnCommandProcessorLogMessage;
        }

        /// <summary>
        /// 建立基礎請求物件
        /// </summary>
        private BaseRequest<T> CreateBaseRequest<T>(List<T> data)
        {
            return new BaseRequest<T>
            {
                RequestID = Guid.NewGuid().ToString(),
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DevCode = DeviceCode,
                Operator = OperatorName,
                Data = data,
                ExtendData = null
            };
        }

        /// <summary>
        /// 更新用戶端連接資訊
        /// </summary>
        private void UpdateClientConnection(string clientId, string clientIp, string requestType = null)
        {
            var client = _clientConnections.FirstOrDefault(c => c.Id == clientId);
            if (client == null)
            {
                // 新增用戶端連接
                _clientConnections.Add(new ClientConnection
                {
                    Id = clientId,
                    IpAddress = clientIp,
                    ConnectTime = DateTime.Now,
                    LastActivityTime = DateTime.Now,
                    RequestType = requestType ?? "未知"
                });
            }
            else
            {
                // 更新現有連接
                client.LastActivityTime = DateTime.Now;
                if (!string.IsNullOrEmpty(requestType))
                {
                    client.RequestType = requestType;
                }
            }        }

        #endregion

        #region 事件處理方法

        /// <summary>
        /// HTTP 伺服器訊息接收事件處理
        /// </summary>
        private async void OnHttpServerMessageReceived(object sender, MessageEventArgs e)
        {
            string connectionToken = null;
            try
            {
                // 1. 效能檢查
                var performanceResult = await _performanceController?.ValidateRequestAsync(e.ClientId, e.Message);
                if (performanceResult != null && !performanceResult.IsAllowed)
                {
                    OnLogMessage(LogLevel.Warning, $"效能檢查失敗: {performanceResult.ErrorMessage} (來自 {e.ClientIp})");
                    return;
                }
                connectionToken = performanceResult?.ConnectionToken;

                // 2. 安全性檢查（從 HTTP 標頭中提取，這裡先跳過詳細實現）
                // TODO: 從 HttpContext 中提取 Authorization, Signature, Timestamp 等標頭
                
                // 3. 嘗試解析請求內容
                BaseRequest request = null;
                string serviceName = "未知";
                string commandType = "UNKNOWN";
                
                try
                {
                    dynamic jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(e.Message);
                    serviceName = jsonData?.serviceName ?? "未知";
                    commandType = jsonData?.command ?? "UNKNOWN";
                    
                    // 嘗試解析為 BaseRequest
                    request = Newtonsoft.Json.JsonConvert.DeserializeObject<BaseRequest>(e.Message);
                }
                catch (Exception parseEx)
                {
                    OnLogMessage(LogLevel.Warning, $"無法解析請求內容: {parseEx.Message}");
                }

                // 4. 更新用戶端連接資訊
                UpdateClientConnection(e.ClientId, e.ClientIp, serviceName);

                // 5. 記錄訊息
                OnLogMessage(LogLevel.Info, $"收到來自 {e.ClientIp} 的 {serviceName} 請求 (指令: {commandType})");

                // 6. 處理具體的 API 指令
                if (request != null && commandType != "UNKNOWN")
                {
                    var response = await ProcessApiCommand(commandType, request);
                    OnLogMessage(LogLevel.Info, $"指令 {commandType} 處理完成，結果: {(response.Success ? "成功" : "失敗")}");
                }

                // 7. 轉發事件
                MessageReceived?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                OnLogMessage(LogLevel.Error, $"處理 HTTP 伺服器訊息時發生錯誤: {ex.Message}");
            }
            finally
            {
                // 釋放平行處理能力
                if (!string.IsNullOrEmpty(connectionToken))
                {
                    _performanceController?.ReleaseConcurrency(connectionToken);
                }
            }
        }

        /// <summary>
        /// HTTP 伺服器用戶端連接事件處理
        /// </summary>
        private void OnHttpServerClientConnected(object sender, ClientConnectedEventArgs e)
        {
            OnLogMessage(LogLevel.Info, $"新用戶端連接: {e.ClientIp} (ID: {e.ClientId})");
            ClientConnected?.Invoke(this, e);
        }

        /// <summary>
        /// HTTP 伺服器用戶端斷線事件處理
        /// </summary>
        private void OnHttpServerClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            // 從列表中移除用戶端
            var client = _clientConnections.FirstOrDefault(c => c.Id == e.ClientId);
            if (client != null)
            {
                _clientConnections.Remove(client);
            }

            OnLogMessage(LogLevel.Info, $"用戶端斷線: {e.ClientIp} (ID: {e.ClientId})");
            ClientDisconnected?.Invoke(this, e);
        }

        /// <summary>
        /// HTTP 伺服器狀態變更事件處理
        /// </summary>
        private void OnHttpServerStatusChanged(object sender, ServerStatusChangedEventArgs e)
        {
            OnLogMessage(LogLevel.Info, $"伺服器狀態變更: {e.Status} - {e.Description}");
            ServerStatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// API 用戶端呼叫成功事件處理
        /// </summary>
        private void OnApiClientCallSuccess(object sender, ApiCallSuccessEventArgs e)
        {
            OnLogMessage(LogLevel.Info, $"API 呼叫成功: {e.Result.RequestUrl} (耗時: {e.Result.ProcessingTimeMs:F2}ms)");
            ApiCallSuccess?.Invoke(this, e);
        }

        /// <summary>
        /// API 用戶端呼叫失敗事件處理
        /// </summary>
        private void OnApiClientCallFailure(object sender, ApiCallFailureEventArgs e)
        {
            OnLogMessage(LogLevel.Error, $"API 呼叫失敗: {e.Result.RequestUrl} - {e.Result.ErrorMessage}");
            ApiCallFailure?.Invoke(this, e);
        }

        /// <summary>
        /// 指令處理完成事件處理
        /// </summary>
        private void OnCommandProcessed(object sender, CommandProcessedEventArgs e)
        {
            OnLogMessage(LogLevel.Info, $"指令處理完成: {e.CommandType}, 請求ID: {e.RequestId}, 成功: {e.Success}");
        }        /// <summary>
        /// 指令處理器日誌事件處理
        /// </summary>
        private void OnCommandProcessorLogMessage(object sender, LogEventArgs e)
        {
            LogLevel logLevel;
            switch (e.Level)
            {
                case "DEBUG":
                    logLevel = LogLevel.Debug;
                    break;
                case "INFO":
                    logLevel = LogLevel.Info;
                    break;
                case "WARNING":
                    logLevel = LogLevel.Warning;
                    break;
                case "ERROR":
                    logLevel = LogLevel.Error;
                    break;
                default:
                    logLevel = LogLevel.Info;
                    break;
            }
            
            OnLogMessage(logLevel, e.Message);
        }

        #endregion

        #region 事件觸發方法

        /// <summary>
        /// 觸發屬性變更事件
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 觸發日誌訊息事件
        /// </summary>
        private void OnLogMessage(LogLevel level, string message)
        {
            LogMessage?.Invoke(this, new LogMessageEventArgs
            {
                Level = level,
                Message = message,
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
                    StopServer();
                    _httpServerService?.Dispose();
                    _apiClientService?.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        /// <summary>
        /// 處理具體的 API 指令
        /// </summary>
        public async Task<BaseResponse> ProcessApiCommand(string commandType, BaseRequest request)
        {
            if (_commandProcessor == null)
            {
                return new BaseResponse
                {
                    RequestId = request?.RequestId ?? Guid.NewGuid().ToString(),
                    Success = false,
                    Message = "指令處理器未初始化",
                    Timestamp = DateTime.Now
                };
            }

            switch (commandType)
            {
                case "SEND_MESSAGE_COMMAND":
                    return await _commandProcessor.ProcessSendMessageCommand(request);
                case "CREATE_NEEDLE_WORKORDER_COMMAND":
                    return await _commandProcessor.ProcessCreateWorkorderCommand(request);
                case "DATE_MESSAGE_COMMAND":
                    return await _commandProcessor.ProcessDateSyncCommand(request);
                case "SWITCH_RECIPE_COMMAND":
                    return await _commandProcessor.ProcessSwitchRecipeCommand(request);
                case "DEVICE_CONTROL_COMMAND":
                    return await _commandProcessor.ProcessDeviceControlCommand(request);
                case "WAREHOUSE_RESOURCE_QUERY_COMMAND":
                    return await _commandProcessor.ProcessWarehouseResourceQueryCommand(request);
                case "TOOL_TRACE_HISTORY_QUERY_COMMAND":
                    return await _commandProcessor.ProcessToolTraceHistoryQueryCommand(request);
                default:
                    return new BaseResponse
                    {
                        RequestId = request?.RequestId ?? Guid.NewGuid().ToString(),
                        Success = false,
                        Message = $"不支援的指令類型: {commandType}",
                        Timestamp = DateTime.Now
                    };
            }
        }
    }

    #region 輔助類別

    /// <summary>
    /// 日誌訊息事件參數
    /// </summary>
    public class LogMessageEventArgs : EventArgs
    {
        /// <summary>
        /// 日誌等級
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// 訊息內容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 時間戳記
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 日誌等級列舉
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 除錯
        /// </summary>
        Debug,

        /// <summary>
        /// 資訊
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 錯誤
        /// </summary>
        Error,

        /// <summary>
        /// 嚴重錯誤
        /// </summary>
        Critical
    }

    #endregion
}
