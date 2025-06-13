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
using Newtonsoft.Json.Linq;
using DDSWebAPI.Models;

namespace DDSWebAPI.Services
{
    #region 相依性注入介面定義

    /// <summary>
    /// 資料庫查詢服務介面
    /// 提供基本的資料庫操作功能，支援泛型查詢
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// 非同步執行 SQL 查詢並返回指定類型的物件清單
        /// </summary>
        /// <typeparam name="T">返回的物件類型</typeparam>
        /// <param name="sql">SQL 查詢語句</param>
        /// <returns>查詢結果物件清單</returns>
        /// <exception cref="InvalidOperationException">當資料庫連線失敗時拋出</exception>
        /// <exception cref="ArgumentException">當 SQL 語句為空或無效時拋出</exception>
        Task<List<T>> GetAllAsync<T>(string sql);

        /// <summary>
        /// 非同步執行 SQL 查詢並返回單一物件
        /// </summary>
        /// <typeparam name="T">返回的物件類型</typeparam>
        /// <param name="sql">SQL 查詢語句</param>
        /// <returns>查詢結果物件，如果沒有找到則返回 default(T)</returns>
        Task<T> GetSingleAsync<T>(string sql);

        /// <summary>
        /// 非同步執行 SQL 指令 (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="sql">SQL 指令語句</param>
        /// <returns>受影響的資料列數</returns>
        Task<int> ExecuteAsync(string sql);
    }

    /// <summary>
    /// 倉庫查詢服務介面
    /// 提供倉庫管理相關的業務邏輯功能
    /// </summary>
    public interface IWarehouseQueryService
    {
        /// <summary>
        /// 非同步執行出庫對話操作
        /// </summary>
        /// <param name="pin">針具物件資訊</param>
        /// <param name="boxQty">出庫盒數數量</param>
        /// <returns>操作是否成功，true 表示成功，false 表示失敗</returns>
        /// <exception cref="ArgumentNullException">當 pin 參數為 null 時拋出</exception>
        /// <exception cref="ArgumentException">當 boxQty 小於或等於 0 時拋出</exception>
        Task<bool> OutWarehouseDialogAsync(object pin, int boxQty);

        /// <summary>
        /// 非同步取得軌道位置資訊
        /// </summary>
        /// <param name="pin">針具物件資訊</param>
        /// <param name="boxStatus">盒子狀態篩選條件</param>
        /// <param name="includeEmpty">是否包含空的位置</param>
        /// <returns>軌道位置資訊清單</returns>
        Task<List<object>> GetTrackLocationAsync(object pin, object boxStatus, bool includeEmpty);

        /// <summary>
        /// 非同步取得倉庫完整資訊
        /// </summary>
        /// <returns>倉庫資訊物件清單</returns>
        Task<List<object>> GetWarehouseInfoAsync();

        /// <summary>
        /// 非同步取得指定區域的庫存統計
        /// </summary>
        /// <param name="area">倉庫區域識別碼</param>
        /// <returns>庫存統計資訊</returns>
        Task<object> GetInventoryStatisticsAsync(string area);
    }

    /// <summary>
    /// 工作流程任務服務介面
    /// 提供設備操作和工作流程控制功能
    /// </summary>
    public interface IWorkflowTaskService
    {
        /// <summary>
        /// 非同步執行機器人夾具操作
        /// </summary>
        /// <param name="requestData">夾具操作請求資料物件</param>
        /// <returns>完成操作的任務</returns>
        /// <exception cref="ArgumentNullException">當 requestData 為 null 時拋出</exception>
        /// <exception cref="InvalidOperationException">當機器人不在可操作狀態時拋出</exception>
        Task OperationRobotClampAsync(object requestData);

        /// <summary>
        /// 非同步執行倉庫入庫作業流程
        /// </summary>
        /// <returns>完成入庫作業的任務</returns>
        /// <exception cref="InvalidOperationException">當倉庫系統忙碌時拋出</exception>
        Task WarehouseInputAsync();

        /// <summary>
        /// 非同步變更設備執行速度
        /// </summary>
        /// <param name="speed">目標速度值 (字串格式，如 "HIGH", "MEDIUM", "LOW" 或數值)</param>
        /// <returns>完成速度變更的任務</returns>
        /// <exception cref="ArgumentException">當速度值格式不正確時拋出</exception>
        Task ChangeSpeedAsync(string speed);

        /// <summary>
        /// 非同步停止所有進行中的工作流程
        /// </summary>
        /// <returns>完成停止操作的任務</returns>
        Task StopAllWorkflowsAsync();

        /// <summary>
        /// 非同步取得目前工作流程狀態
        /// </summary>
        /// <returns>工作流程狀態物件</returns>
        Task<object> GetWorkflowStatusAsync();
    }

    /// <summary>
    /// 全域配置服務介面
    /// 管理系統全域設定和狀態
    /// </summary>
    public interface IGlobalConfigService
    {
        /// <summary>
        /// 取得或設定是否為連續入庫模式
        /// true: 連續入庫，false: 指定數量入庫
        /// </summary>
        bool IsContinueIntoWarehouse { get; set; }

        /// <summary>
        /// 取得或設定入庫盒數數量 (當非連續模式時使用)
        /// </summary>
        int IntoWarehouseBoxQty { get; set; }

        /// <summary>
        /// 取得或設定系統運行模式
        /// </summary>
        string SystemMode { get; set; }

        /// <summary>
        /// 取得或設定設備狀態
        /// </summary>
        string DeviceStatus { get; set; }

        /// <summary>
        /// 非同步載入系統配置
        /// </summary>
        /// <returns>載入操作的任務</returns>
        Task LoadConfigAsync();

        /// <summary>
        /// 非同步儲存系統配置
        /// </summary>
        /// <returns>儲存操作的任務</returns>
        Task SaveConfigAsync();
    }

    /// <summary>
    /// 公用程式服務介面
    /// 提供各種輔助功能和工具方法
    /// </summary>
    public interface IUtilityService
    {
        /// <summary>
        /// 將儲存位置編號轉換為儲存位置物件
        /// </summary>
        /// <param name="storageNo">儲存位置編號字串</param>
        /// <returns>儲存位置物件，包含區域、層級、軌道等資訊</returns>
        /// <exception cref="ArgumentException">當 storageNo 格式不正確時拋出</exception>
        object StorageNoConverter(string storageNo);

        /// <summary>
        /// 產生唯一識別碼
        /// </summary>
        /// <returns>唯一識別碼字串</returns>
        string GenerateUniqueId();

        /// <summary>
        /// 驗證資料格式
        /// </summary>
        /// <param name="data">要驗證的資料物件</param>
        /// <param name="validationType">驗證類型</param>
        /// <returns>驗證結果，true 表示通過驗證</returns>
        bool ValidateData(object data, string validationType);

        /// <summary>
        /// 格式化時間戳記
        /// </summary>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="format">格式字串，預設為 "yyyy-MM-dd HH:mm:ss"</param>
        /// <returns>格式化後的時間字串</returns>
        string FormatTimestamp(DateTime timestamp, string format = "yyyy-MM-dd HH:mm:ss");
    }

    #endregion

    #region 客製化資料模型

    /// <summary>
    /// 速度變更請求資料模型
    /// 用於封裝速度變更 API 的請求參數
    /// </summary>
    public class SpeedRequest
    {
        /// <summary>
        /// 目標速度值
        /// 支援格式: "HIGH", "MEDIUM", "LOW" 或數值字串 "1-100"
        /// </summary>
        [JsonProperty("speed")]
        public string Speed { get; set; }

        /// <summary>
        /// 速度變更生效時間 (可選)
        /// ISO 8601 格式: "2025-06-13T10:30:00Z"
        /// </summary>
        [JsonProperty("effectiveTime")]
        public string EffectiveTime { get; set; }

        /// <summary>
        /// 速度變更原因說明 (可選)
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    /// <summary>
    /// 夾具操作請求資料模型
    /// 用於封裝機器人夾具操作 API 的請求參數
    /// </summary>
    public class ClampRequest
    {
        /// <summary>
        /// 夾具操作狀態
        /// true: 夾取，false: 釋放
        /// </summary>
        [JsonProperty("checked")]
        public bool Checked { get; set; }

        /// <summary>
        /// 夾具識別碼或名稱
        /// 例如: "CLAMP_001", "LEFT_CLAMP", "RIGHT_CLAMP"
        /// </summary>
        [JsonProperty("clip")]
        public string Clip { get; set; }

        /// <summary>
        /// 夾取力度 (可選，範圍 1-100)
        /// </summary>
        [JsonProperty("force")]
        public int? Force { get; set; }

        /// <summary>
        /// 操作超時時間，單位秒 (可選，預設 30 秒)
        /// </summary>
        [JsonProperty("timeout")]
        public int? Timeout { get; set; }
    }

    /// <summary>
    /// 儲存位置資訊模型
    /// 用於表示倉庫中的儲存位置詳細資訊
    /// </summary>
    public class StorageInfo
    {
        /// <summary>
        /// 儲存位置編號
        /// 格式範例: "A01-L02-T03" (區域A01-層級L02-軌道T03)
        /// </summary>
        [JsonProperty("storageNo")]
        public string StorageNo { get; set; }

        /// <summary>
        /// 儲存區域識別碼
        /// 例如: "A01", "B02", "C03"
        /// </summary>
        [JsonProperty("area")]
        public string Area { get; set; }

        /// <summary>
        /// 儲存層級識別碼
        /// 例如: "L01", "L02", "L03"
        /// </summary>
        [JsonProperty("layer")]
        public string Layer { get; set; }

        /// <summary>
        /// 儲存軌道識別碼
        /// 例如: "T01", "T02", "T03"
        /// </summary>
        [JsonProperty("track")]
        public string Track { get; set; }

        /// <summary>
        /// 該位置的盒子資訊清單
        /// 包含該儲存位置中所有盒子的詳細資訊
        /// </summary>
        [JsonProperty("listBoxInfo")]
        public List<object> ListBoxInfo { get; set; }

        /// <summary>
        /// 儲存位置狀態
        /// 例如: "AVAILABLE", "OCCUPIED", "RESERVED", "MAINTENANCE"
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// 儲存容量 (可選)
        /// </summary>
        [JsonProperty("capacity")]
        public int? Capacity { get; set; }

        /// <summary>
        /// 目前使用量 (可選)
        /// </summary>
        [JsonProperty("currentUsage")]
        public int? CurrentUsage { get; set; }
    }

    /// <summary>
    /// 入料請求資料模型
    /// 用於封裝入料 API 的請求參數
    /// </summary>
    public class InMaterialRequest
    {
        /// <summary>
        /// 是否為連續入料模式
        /// true: 連續入料，false: 指定數量入料
        /// </summary>
        [JsonProperty("isContinue")]
        public bool IsContinue { get; set; }

        /// <summary>
        /// 指定入料盒數 (當非連續模式時必填)
        /// </summary>
        [JsonProperty("inBoxQty")]
        public int? InBoxQty { get; set; }

        /// <summary>
        /// 入料優先等級 (可選)
        /// 1: 高優先, 2: 中優先, 3: 低優先
        /// </summary>
        [JsonProperty("priority")]
        public int? Priority { get; set; }

        /// <summary>
        /// 目標儲存區域 (可選)
        /// </summary>
        [JsonProperty("targetArea")]
        public string TargetArea { get; set; }
    }

    /// <summary>
    /// 出料請求資料模型
    /// 用於封裝出料 API 的請求參數
    /// </summary>
    public class OutMaterialRequest
    {
        /// <summary>
        /// 針具規格資訊
        /// </summary>
        [JsonProperty("pin")]
        public object Pin { get; set; }

        /// <summary>
        /// 出料盒數數量
        /// </summary>
        [JsonProperty("boxQty")]
        public int BoxQty { get; set; }

        /// <summary>
        /// 出料優先等級 (可選)
        /// </summary>
        [JsonProperty("priority")]
        public int? Priority { get; set; }

        /// <summary>
        /// 指定出料區域 (可選)
        /// </summary>
        [JsonProperty("sourceArea")]
        public string SourceArea { get; set; }
    }

    #endregion

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
        private string _urlPrefix;

        /// <summary>
        /// 靜態檔案根目錄路徑
        /// 用於提供 HTML、CSS、JavaScript 等靜態檔案服務
        /// </summary>
        private string _staticFilesPath;

        /// <summary>
        /// 執行緒同步鎖定物件
        /// 用於保護共享資源的執行緒安全存取
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>
        /// MIME 類型對應表
        /// 用於根據檔案副檔名設定正確的 Content-Type
        /// </summary>
        private readonly Dictionary<string, string> _mimeTypes;

        /// <summary>
        /// 取消標記來源，用於優雅關閉伺服器
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

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

        /// <summary>
        /// 取得處理的請求總數
        /// 用於效能監控和統計
        /// </summary>
        public long TotalRequestsProcessed { get; private set; }

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
            _utilityService = utilityService;

            // 初始化 MIME 類型對應表
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

            // 初始化取消標記
            _cancellationTokenSource = new CancellationTokenSource();

            // 初始化統計資料
            ConnectedClientCount = 0;
            TotalRequestsProcessed = 0;
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
                OnServerStatusChanged(ServerStatus.Running, $"HTTP 伺服器已啟動，監聽位址: {_urlPrefix}");

                // 在背景啟動請求處理迴圈
                _ = Task.Run(ProcessRequestsAsync, _cancellationTokenSource.Token);

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
                _cancellationTokenSource?.Cancel();

                // 停止 HttpListener
                try
                {
                    _httpListener?.Stop();
                    _httpListener?.Close();
                }
                catch (Exception ex)
                {
                    OnServerStatusChanged(ServerStatus.Warning, $"關閉 HTTP 監聽器時發生警告: {ex.Message}");
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
        }

        /// <summary>
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
                    Interlocked.Increment(ref TotalRequestsProcessed);

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
        }

        /// <summary>
        /// 處理單一 HTTP 請求的完整流程
        /// 
        /// 處理流程:
        /// 1. 產生用戶端識別碼和取得 IP 位址
        /// 2. 觸發用戶端連接事件
        /// 3. 判斷請求類型 (WebSocket/HTTP)
        /// 4. 讀取請求內容
        /// 5. 路由到對應的處理方法
        /// 6. 處理異常和清理資源
        /// 
        /// </summary>
        /// <param name="context">HTTP 請求上下文，包含請求和回應物件</param>
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            // 產生唯一的用戶端識別碼，用於追蹤和日誌記錄
            string clientId = _utilityService?.GenerateUniqueId() ?? Guid.NewGuid().ToString();
            string clientIp = GetClientIpAddress(context.Request);
            string requestBody = string.Empty;

            try
            {
                // 觸發用戶端連接事件，讓外部模組可以進行連接管理
                OnClientConnected(clientId, clientIp);

                // 檢查是否為 WebSocket 升級請求
                if (context.Request.IsWebSocketRequest)
                {
                    await HandleWebSocketRequestAsync(context, clientId);
                    return;
                }

                // 讀取 HTTP 請求主體內容
                // 使用 UTF-8 編碼確保中文字元正確處理
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // 觸發訊息接收事件，讓外部模組可以進行日誌記錄或監控
                if (!string.IsNullOrEmpty(requestBody))
                {
                    OnMessageReceived(requestBody, clientId, clientIp);
                }

                // 路由請求到對應的處理方法
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
                OnServerStatusChanged(ServerStatus.Error, $"處理來自 {clientIp} 的請求時發生錯誤: {ex.Message}");
            }
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
                }

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
                    switch (path)
                    {
                        // 標準 MES API 統一端點
                        case "/api/mes":
                            var mesApiResponse = await ProcessMesApiRequestAsync(requestBody, request);
                            await SendResponseAsync(response, mesApiResponse);
                            return;

                        // 客製化倉庫管理 API
                        case "/api/in-material":
                            await HandleInMaterialRequestAsync(context, requestBody);
                            return;

                        case "/api/stop-material":
                            await HandleStopMaterialRequestAsync(context, requestBody);
                            return;

                        case "/api/out-material":
                            await HandleOutMaterialRequestAsync(context, requestBody);
                            return;

                        case "/api/getlocationbystorage":
                            await GetLocationByStorageAsync(context, requestBody);
                            return;

                        case "/api/getlocationbypin":
                            await GetLocationByPinAsync(context, requestBody);
                            return;

                        case "/api/operationclamp":
                            await HandleClampRequestAsync(context, requestBody);
                            return;

                        case "/api/changespeed":
                            await HandleSpeedRequestAsync(context, requestBody);
                            return;

                        // 系統管理 API
                        case "/api/server/status":
                            await HandleGetServerStatusAsync(context);
                            return;

                        case "/api/server/restart":
                            await HandleServerRestartAsync(context);
                            return;
                    }
                }
                // GET 請求路由
                else if (method == "GET")
                {
                    switch (path)
                    {
                        // 查詢類 API
                        case "/api/out-getpins":
                            await GetOutPinsDataAsync(context);
                            return;

                        case "/api/server/statistics":
                            await HandleGetServerStatisticsAsync(context);
                            return;

                        case "/api/health":
                            await HandleHealthCheckAsync(context);
                            return;

                        // API 文件端點
                        case "/api/docs":
                        case "/api/swagger":
                            await HandleApiDocumentationAsync(context);
                            return;

                        default:
                            // 靜態檔案服務
                            await HandleStaticFileRequestAsync(context, path);
                            return;
                    }
                }
                // OPTIONS 請求處理 (CORS 預檢)
                else if (method == "OPTIONS")
                {
                    await HandleOptionsRequestAsync(context);
                    return;
                }

                // 觸發自訂 API 處理事件，讓外部模組有機會處理未識別的請求
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
                foreach (string headerName in request.Headers.AllKeys)
                {
                    customApiArgs.Headers[headerName] = request.Headers[headerName];
                }

                CustomApiRequest?.Invoke(this, customApiArgs);

                // 檢查自訂處理是否已經處理了請求
                if (response.StatusCode != 200 || response.ContentLength64 > 0)
                {
                    return; // 已被自訂處理器處理
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

        #endregion

        #region 私有方法 - WebSocket 處理

        /// <summary>
        /// 處理 WebSocket 升級請求
        /// 
        /// WebSocket 處理流程:
        /// 1. 接受 WebSocket 升級請求
        /// 2. 建立 WebSocket 連接
        /// 3. 在背景處理 WebSocket 通訊
        /// 4. 處理連接異常
        /// 
        /// </summary>
        /// <param name="context">HTTP 請求上下文</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleWebSocketRequestAsync(HttpListenerContext context, string clientId)
        {
            try
            {
                // 接受 WebSocket 升級請求
                // 可以在這裡指定支援的子協定 (subprotocol)
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                
                // 在背景處理 WebSocket 通訊，避免阻塞其他請求
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleWebSocketClientAsync(webSocketContext.WebSocket, clientId);
                    }
                    catch (Exception ex)
                    {
                        OnServerStatusChanged(ServerStatus.Warning, $"WebSocket 用戶端處理錯誤: {ex.Message}");
                    }
                });
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
        /// 處理 WebSocket 用戶端連接的完整生命週期
        /// 
        /// 處理功能:
        /// 1. 接收和發送 WebSocket 訊息
        /// 2. 處理不同類型的 WebSocket 訊息 (文字、二進位、關閉)
        /// 3. 觸發相關事件供外部模組使用
        /// 4. 優雅處理連接關閉
        /// 
        /// </summary>
        /// <param name="webSocket">WebSocket 連接物件</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleWebSocketClientAsync(WebSocket webSocket, string clientId)
        {
            // 設定接收緩衝區大小 (4KB)
            var buffer = new byte[4096];

            try
            {
                // 持續監聽 WebSocket 訊息，直到連接關閉
                while (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        // 接收 WebSocket 訊息
                        var result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), 
                            _cancellationTokenSource.Token);

                        // 處理不同類型的訊息
                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                // 處理文字訊息
                                await HandleWebSocketTextMessageAsync(webSocket, buffer, result, clientId);
                                break;

                            case WebSocketMessageType.Binary:
                                // 處理二進位訊息
                                await HandleWebSocketBinaryMessageAsync(webSocket, buffer, result, clientId);
                                break;

                            case WebSocketMessageType.Close:
                                // 處理關閉訊息
                                await HandleWebSocketCloseMessageAsync(webSocket, result, clientId);
                                return;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 操作被取消，通常是伺服器正在關閉
                        break;
                    }
                    catch (WebSocketException wsEx)
                    {
                        // WebSocket 特定錯誤
                        OnServerStatusChanged(ServerStatus.Warning, 
                            $"WebSocket 連接錯誤 (用戶端: {clientId}): {wsEx.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Error, 
                    $"WebSocket 用戶端處理過程中發生未預期錯誤 (用戶端: {clientId}): {ex.Message}");
            }
            finally
            {
                // 確保 WebSocket 連接被正確關閉
                await CloseWebSocketSafelyAsync(webSocket, clientId);
            }
        }

        /// <summary>
        /// 處理 WebSocket 文字訊息
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="buffer">接收緩衝區</param>
        /// <param name="result">接收結果</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleWebSocketTextMessageAsync(WebSocket webSocket, byte[] buffer, 
            WebSocketReceiveResult result, string clientId)
        {
            try
            {
                // 將接收到的位元組轉換為文字
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                // 觸發 WebSocket 訊息接收事件
                OnWebSocketMessageReceived(message, webSocket, clientId);

                // 處理特殊指令 (可選)
                if (message.StartsWith("{") && message.EndsWith("}"))
                {
                    // 嘗試解析 JSON 指令
                    try
                    {
                        var jsonMessage = JsonConvert.DeserializeObject<dynamic>(message);
                        await ProcessWebSocketCommandAsync(webSocket, jsonMessage, clientId);
                    }
                    catch (JsonException)
                    {
                        // 不是有效的 JSON，當作普通文字處理
                        await SendWebSocketTextAsync(webSocket, $"收到訊息: {message}");
                    }
                }
                else
                {
                    // 簡單的回音服務
                    await SendWebSocketTextAsync(webSocket, $"伺服器收到: {message}");
                }
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Warning, 
                    $"處理 WebSocket 文字訊息時發生錯誤 (用戶端: {clientId}): {ex.Message}");
            }
        }

        /// <summary>
        /// 處理 WebSocket 二進位訊息
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="buffer">接收緩衝區</param>
        /// <param name="result">接收結果</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleWebSocketBinaryMessageAsync(WebSocket webSocket, byte[] buffer, 
            WebSocketReceiveResult result, string clientId)
        {
            try
            {
                // 取得二進位資料
                var binaryData = new byte[result.Count];
                Array.Copy(buffer, binaryData, result.Count);

                // 觸發 WebSocket 訊息接收事件
                var binaryMessage = Convert.ToBase64String(binaryData);
                OnWebSocketMessageReceived($"[BINARY:{binaryData.Length} bytes] {binaryMessage}", webSocket, clientId);

                // 回傳確認訊息
                await SendWebSocketTextAsync(webSocket, $"已接收 {binaryData.Length} 位元組的二進位資料");
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Warning, 
                    $"處理 WebSocket 二進位訊息時發生錯誤 (用戶端: {clientId}): {ex.Message}");
            }
        }

        /// <summary>
        /// 處理 WebSocket 關閉訊息
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="result">接收結果</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleWebSocketCloseMessageAsync(WebSocket webSocket, 
            WebSocketReceiveResult result, string clientId)
        {
            try
            {
                // 取得關閉原因
                var closeStatus = result.CloseStatus ?? WebSocketCloseStatus.NormalClosure;
                var closeDescription = result.CloseStatusDescription ?? "正常關閉";

                // 記錄關閉事件
                OnServerStatusChanged(ServerStatus.Info, 
                    $"WebSocket 連接關閉 (用戶端: {clientId}, 原因: {closeDescription})");
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Warning, 
                    $"處理 WebSocket 關閉訊息時發生錯誤 (用戶端: {clientId}): {ex.Message}");
            }
            finally
            {
                // 確保 WebSocket 連接被關閉
                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "正常關閉", CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    OnServerStatusChanged(ServerStatus.Warning, 
                        $"關閉 WebSocket 連接時發生錯誤 (用戶端: {clientId}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 安全關閉 WebSocket 連接
        /// 
        /// 確保在關閉 WebSocket 之前，先處理任何未完成的訊息或操作
        /// 並記錄關閉事件
        /// </summary>
        /// <param name="webSocket">WebSocket 連接物件</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task CloseWebSocketSafelyAsync(WebSocket webSocket, string clientId)
        {
            try
            {
                // 檢查 WebSocket 狀態
                if (webSocket.State == WebSocketState.Open)
                {
                    // 發送關閉訊息 (可選)
                    await webSocket.SendAsync(
                        Encoding.UTF8.GetBytes("伺服器正在關閉連接..."),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);

                    // 等待一段時間以確保訊息發送完成
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                OnServerStatusChanged(ServerStatus.Warning, 
                    $"安全關閉 WebSocket 連接時發生錯誤 (用戶端: {clientId}): {ex.Message}");
            }
            finally
            {
                try
                {
                    // 確保 WebSocket 連接被正確關閉
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "正常關閉", CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    OnServerStatusChanged(ServerStatus.Warning, 
                        $"關閉 WebSocket 連接時發生錯誤 (用戶端: {clientId}): {ex.Message}");
                }
            }
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
