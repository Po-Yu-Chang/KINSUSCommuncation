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
        /// 停止 HTTP 伺服器 (非同步版本)
        /// </summary>
        public async Task StopServerAsync()
        {
            await Task.Run(() => StopServer());
        }        
        /// <summary>
        /// 發送 API 請求
        /// </summary>
        /// <param name="endpoint">API 端點</param>
        /// <param name="data">請求資料</param>
        /// <returns>API 呼叫結果</returns>
        public async Task<string> SendApiRequestAsync(string endpoint, object data)
        {
            try
            {
                var result = await _apiClientService.SendCustomRequestAsync(endpoint, data);
                
                if (result.IsSuccess)
                {
                    OnApiCallSuccess(endpoint, result.ResponseData);
                    return result.ResponseData;
                }
                else
                {
                    OnApiCallFailure(endpoint, result.ErrorMessage);
                    throw new Exception(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                OnApiCallFailure(endpoint, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 發送工具輸出報告
        /// </summary>
        /// <param name="report">工具輸出報告</param>
        /// <returns>發送結果</returns>
        public async Task<string> SendToolOutputReportAsync(object report)
        {
            return await SendApiRequestAsync("/api/tool-output-report", report);
        }

        /// <summary>
        /// 發送錯誤報告
        /// </summary>
        /// <param name="report">錯誤報告</param>
        /// <returns>發送結果</returns>
        public async Task<string> SendErrorReportAsync(object report)
        {
            return await SendApiRequestAsync("/api/error-report", report);
        }

        /// <summary>
        /// 發送機台狀態報告
        /// </summary>
        /// <param name="report">機台狀態報告</param>
        /// <returns>發送結果</returns>
        public async Task<string> SendMachineStatusReportAsync(object report)
        {
            return await SendApiRequestAsync("/api/machine-status-report", report);
        }

        /// <summary>
        /// 取得伺服器統計資訊
        /// </summary>
        /// <returns>伺服器統計資訊</returns>
        public ServerStatistics GetServerStatistics()
        {
            return new ServerStatistics
            {
                IsRunning = IsServerRunning,
                ServerUrl = ServerUrl,
                ConnectedClients = _clientConnections?.Count ?? 0,
                Uptime = IsServerRunning && _httpServerService?.StartTime.HasValue == true ? 
                    DateTime.Now - _httpServerService.StartTime.Value : TimeSpan.Zero,
                TotalRequests = _httpServerService?.TotalRequestsProcessed ?? 0,
                ActiveConnections = _clientConnections?.Count ?? 0
            };
        }

        /// <summary>
        /// 取得預設 API 金鑰（用於測試和內部呼叫）
        /// </summary>
        /// <returns>預設 API 金鑰</returns>
        public string GetDefaultApiKey()
        {
            return "KINSUS-API-KEY-2024";
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化服務（使用預設設定）
        /// </summary>
        private void InitializeServices()
        {
            InitializeHttpServer();
            InitializeApiClient();
        }

        /// <summary>
        /// 初始化服務（使用應用程式設定）
        /// </summary>
        /// <param name="appConfig">應用程式設定</param>
        private void InitializeServices(AppConfiguration appConfig)
        {
            InitializeHttpServer();
            InitializeApiClient();
        }        /// <summary>
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

            // 初始化安全性中介軟體
            InitializeSecurityMiddleware();
            
            // 初始化效能控制器
            InitializePerformanceController();

            // 建立新的服務
            _httpServerService = new HttpServerService(ServerUrl);
            
            // 註冊事件
            _httpServerService.MessageReceived += OnHttpServerMessageReceived;
            _httpServerService.ClientConnected += OnHttpServerClientConnected;
            _httpServerService.ClientDisconnected += OnHttpServerClientDisconnected;
            _httpServerService.ServerStatusChanged += OnHttpServerStatusChanged;
        }

        /// <summary>
        /// 初始化安全性中介軟體
        /// </summary>
        private void InitializeSecurityMiddleware()
        {
            // 設定有效的 API 金鑰
            var validApiKeys = new List<string>
            {
                "KINSUS-API-KEY-2024",
                "KINSUS-DEV-KEY-2024",
                "KINSUS-TEST-KEY-2024"
            };

            // 設定 IP 白名單
            var ipWhitelist = new List<string>
            {
                "127.0.0.1",
                "::1",
                "localhost",
                "192.168.1.0/24"  // 可根據需要調整
            };

            //// 初始化安全性中介軟體
            //_securityMiddleware = new SecurityMiddleware(validApiKeys, ipWhitelist, "KINSUS-SECRET-KEY-2024");
            
            //// 在開發環境中可以暫時停用某些驗證
            //#if DEBUG
            //_securityMiddleware.EnableApiKeyValidation = false; // 開發時暫時停用
            //#endif
        }

        /// <summary>
        /// 初始化效能控制器
        /// </summary>
        private void InitializePerformanceController()
        {
        }        /// <summary>
        /// 初始化 API 用戶端
        /// </summary>
        private void InitializeApiClient()
        {
            // 釋放舊的服務
            if (_apiClientService != null)
            {
                _apiClientService.Dispose();
            }

            // 建立新的服務
            _apiClientService = new ApiClientService(RemoteApiUrl);
            
            // 設定 API 金鑰
            _apiClientService.SetApiKey(GetDefaultApiKey());
        }

        #endregion

        #region 事件處理方法

        /// <summary>
        /// HTTP 伺服器訊息接收事件處理
        /// </summary>
        private async void OnHttpServerMessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                OnLogMessage(LogLevel.Info, $"收到來自 {e.ClientIp} 的請求");
                MessageReceived?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                OnLogMessage(LogLevel.Error, $"處理 HTTP 伺服器訊息時發生錯誤: {ex.Message}");
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
            OnLogMessage(LogLevel.Info, $"伺服器狀態變更: {e.Status} - {e.Message}");
            ServerStatusChanged?.Invoke(this, e);
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
            LogMessage?.Invoke(this, new LogMessageEventArgs(level, message));
        }        /// <summary>
        /// 觸發 API 呼叫成功事件
        /// </summary>
        private void OnApiCallSuccess(string endpoint, string response)
        {
            ApiCallSuccess?.Invoke(this, new ApiCallSuccessEventArgs(endpoint, response));
        }

        /// <summary>
        /// 觸發 API 呼叫失敗事件
        /// </summary>
        private void OnApiCallFailure(string endpoint, string error)
        {
            ApiCallFailure?.Invoke(this, new ApiCallFailureEventArgs(endpoint, error));
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
    }
}
