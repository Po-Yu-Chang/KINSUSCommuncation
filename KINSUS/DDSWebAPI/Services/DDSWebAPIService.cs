using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DDSWebAPI.Models;

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
        /// 初始化 DDS Web API 服務
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
        /// 初始化 DDS Web API 服務
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

        #endregion

        #region 公開方法        /// <summary>
        /// 啟動 HTTP 伺服器
        /// </summary>
        /// <param name="serverUrl">伺服器 URL</param>
        /// <returns>是否啟動成功</returns>
        public async Task<bool> StartServerAsync(string serverUrl)
        {
            try
            {
                ServerUrl = serverUrl;
                return await StartServerAsync();
            }
            catch (Exception ex)
            {
                OnLogMessage(LogLevel.Error, $"啟動 HTTP 伺服器時發生錯誤: {ex.Message}");
                return false;
            }
        }

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

                // 從 ServerUrl 提取埠號
                var uri = new Uri(ServerUrl);
                int port = uri.Port;
                
                bool success = await _httpServerService.StartAsync(port);
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
        }        /// <summary>
        /// 停止 HTTP 伺服器
        /// </summary>
        public async Task StopServerAsync()
        {
            try
            {
                OnLogMessage(LogLevel.Info, "正在停止 HTTP 伺服器...");
                
                if (_httpServerService != null)
                {
                    await _httpServerService.StopAsync();
                }
                IsServerRunning = false;
                
                // 清空用戶端連接列表
                _clientConnections.Clear();
                
                OnLogMessage(LogLevel.Info, "HTTP 伺服器已停止");
            }
            catch (Exception ex)
            {
                OnLogMessage(LogLevel.Error, $"停止 HTTP 伺服器時發生錯誤: {ex.Message}");
            }
        }        /// <summary>
        /// 發送 API 請求
        /// </summary>
        /// <param name="endpoint">API 端點</param>
        /// <param name="requestBody">請求內容</param>
        /// <returns>是否發送成功</returns>
        public async Task<bool> SendApiRequestAsync(string endpoint, string requestBody)
        {
            try
            {
                OnLogMessage(LogLevel.Info, $"正在發送 API 請求到: {endpoint}");
                
                var result = await _apiClientService.SendApiRequestAsync(endpoint, requestBody);
                  if (result.IsSuccess)
                {
                    ApiCallSuccess?.Invoke(this, new ApiCallSuccessEventArgs(endpoint, requestBody, result.ResponseData));
                    OnLogMessage(LogLevel.Info, $"API 請求成功: {endpoint}");
                    return true;
                }
                else
                {
                    ApiCallFailure?.Invoke(this, new ApiCallFailureEventArgs(endpoint, requestBody, result.ErrorMessage, result.Exception));
                    OnLogMessage(LogLevel.Error, $"API 請求失敗: {result.ErrorMessage}");
                    return false;
                }
            }            catch (Exception ex)
            {
                ApiCallFailure?.Invoke(this, new ApiCallFailureEventArgs(endpoint, requestBody, ex.Message, ex));
                OnLogMessage(LogLevel.Error, $"發送 API 請求時發生錯誤: {ex.Message}");
                return false;
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
        }        /// <summary>
        /// 重新載入服務設定
        /// </summary>
        public async Task ReloadConfiguration()
        {
            OnLogMessage(LogLevel.Info, "重新載入服務設定");
            
            // 如果伺服器正在執行，需要重新啟動
            bool wasRunning = IsServerRunning;
            if (wasRunning)
            {
                await StopServerAsync();
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
        /// 初始化服務
        /// </summary>
        private void InitializeServices()
        {
            InitializeHttpServer();
            InitializeApiClient();
        }

        /// <summary>
        /// 初始化 HTTP 伺服器
        /// </summary>
        private void InitializeHttpServer()
        {            // 釋放舊的服務
            if (_httpServerService != null)
            {
                _httpServerService.WebSocketMessageReceived -= OnHttpServerMessageReceived;
                _httpServerService.ClientConnected -= OnHttpServerClientConnected;
                _httpServerService.ClientDisconnected -= OnHttpServerClientDisconnected;
                _httpServerService.ServerStatusChanged -= OnHttpServerStatusChanged;
                _httpServerService.Dispose();
            }

            // 建立新的服務
            _httpServerService = new HttpServerService();
            
            // 註冊事件
            _httpServerService.WebSocketMessageReceived += OnHttpServerMessageReceived;
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
            }
        }

        #endregion

        #region 事件處理方法        /// <summary>
        /// HTTP 伺服器訊息接收事件處理
        /// </summary>
        private void OnHttpServerMessageReceived(object sender, WebSocketMessageEventArgs e)
        {
            try
            {
                // 嘗試解析服務名稱
                string serviceName = "未知";
                try
                {
                    dynamic jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(e.Message);
                    serviceName = jsonData?.serviceName ?? "未知";
                }
                catch
                {
                    // 無法解析 JSON，使用預設值
                }

                // 更新用戶端連接資訊
                UpdateClientConnection(e.ClientId, "Unknown", serviceName);                // 記錄訊息
                OnLogMessage(LogLevel.Info, $"收到來自用戶端 {e.ClientId} 的 {serviceName} 請求");

                // 轉發事件
                MessageReceived?.Invoke(this, new MessageEventArgs 
                { 
                    Message = e.Message, 
                    ClientId = e.ClientId, 
                    ClientIp = "Unknown",
                    Timestamp = e.Timestamp 
                });
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
            OnLogMessage(LogLevel.Info, $"伺服器狀態變更: {e.Status} - {e.Description}");
            ServerStatusChanged?.Invoke(this, e);
        }        /// <summary>
        /// API 用戶端呼叫成功事件處理
        /// </summary>
        private void OnApiClientCallSuccess(object sender, ApiCallSuccessEventArgs e)
        {
            OnLogMessage(LogLevel.Info, $"API 呼叫成功: {e.Endpoint}");
            ApiCallSuccess?.Invoke(this, e);
        }

        /// <summary>
        /// API 用戶端呼叫失敗事件處理
        /// </summary>
        private void OnApiClientCallFailure(object sender, ApiCallFailureEventArgs e)
        {
            OnLogMessage(LogLevel.Error, $"API 呼叫失敗: {e.Endpoint} - {e.Error}");
            ApiCallFailure?.Invoke(this, e);
        }

        #endregion

        #region 事件觸發方法

        /// <summary>
        /// 觸發屬性變更事件
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }        /// <summary>
        /// 觸發日誌訊息事件
        /// </summary>
        private void OnLogMessage(LogLevel level, string message)
        {
            LogMessage?.Invoke(this, new LogMessageEventArgs(message, level));
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
            {                if (disposing)
                {
                    StopServerAsync().GetAwaiter().GetResult();
                    _httpServerService?.Dispose();
                    _apiClientService?.Dispose();
                }
                _disposed = true;
            }        }

        #endregion
    }
}
