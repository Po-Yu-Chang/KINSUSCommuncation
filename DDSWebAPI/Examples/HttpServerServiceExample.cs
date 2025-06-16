///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: HttpServerServiceExample.cs
// 檔案描述: HttpServerService 使用範例，展示如何使用完整的伺服器功能
// 功能示範:
//   1. 安全性中介軟體整合
//   2. 效能控制器整合
//   3. 連線管理
//   4. 完整的事件處理
// 建立日期: 2025-06-13
// 更新日期: 2025-06-16 - 新增完整功能示範
// 版本: 2.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using DDSWebAPI.Services;
using DDSWebAPI.Interfaces;
using DDSWebAPI.Events;
using DDSWebAPI.Enums;

namespace DDSWebAPI.Examples
{
    /// <summary>
    /// HTTP 伺服器服務使用範例類別
    /// 展示如何正確建立、配置和使用重構後的 HTTP 伺服器
    /// </summary>
    public class HttpServerServiceExample
    {
        private HttpServerService _httpServer;

        /// <summary>
        /// 執行範例的主要方法
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== DDSWebAPI HTTP 伺服器服務範例 ===");
            Console.WriteLine("版本: 1.0.0 (重構版)");
            Console.WriteLine("建立日期: 2025-06-13");
            Console.WriteLine();

            try
            {
                // 1. 建立服務實例 (模擬相依性注入)
                await CreateServiceInstancesAsync();

                // 2. 建立並配置 HTTP 伺服器
                await CreateHttpServerAsync();

                // 3. 註冊事件處理器
                RegisterEventHandlers();

                // 4. 啟動伺服器
                await StartServerAsync();

                // 5. 等待使用者輸入
                await WaitForUserInputAsync();

                // 6. 關閉伺服器
                await StopServerAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"執行範例時發生錯誤: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }
            finally
            {
                // 確保資源被正確釋放
                _httpServer?.Dispose();
            }
        }

        /// <summary>
        /// 建立服務實例 (在實際應用中，這些應該透過 DI 容器注入)
        /// </summary>
        private async Task CreateServiceInstancesAsync()
        {
            Console.WriteLine("正在建立服務實例...");

            // 注意: 在實際應用中，您需要實作這些介面的具體類別
            // 這裡只是展示如何傳遞服務實例給 HttpServerService
            
            // 範例: 如果您有實際的服務實作
            // var databaseService = new DatabaseService(connectionString);
            // var warehouseService = new WarehouseQueryService(databaseService);
            // var workflowService = new WorkflowTaskService();
            // var configService = new GlobalConfigService();
            // var utilityService = new UtilityService();

            Console.WriteLine("✓ 服務實例建立完成 (使用預設值)");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 建立並配置 HTTP 伺服器
        /// </summary>
        private async Task CreateHttpServerAsync()
        {
            Console.WriteLine("正在建立 HTTP 伺服器...");

            // 建立 HTTP 伺服器實例
            // 參數說明:
            // - urlPrefix: 監聽位址 (預設: http://localhost:8085/)
            // - staticFilesPath: 靜態檔案根目錄 (預設: 目前目錄)
            // - 其他參數: 各種服務實例 (可選，為 null 時使用預設行為)
            _httpServer = new HttpServerService(
                urlPrefix: "http://localhost:8085/",
                staticFilesPath: "./wwwroot",
                databaseService: null,        // 在實際應用中傳入具體實作
                warehouseQueryService: null,  // 在實際應用中傳入具體實作
                workflowTaskService: null,    // 在實際應用中傳入具體實作
                globalConfigService: null,    // 在實際應用中傳入具體實作
                utilityService: null          // 在實際應用中傳入具體實作
            );

            Console.WriteLine($"✓ HTTP 伺服器建立完成，監聽位址: {_httpServer.UrlPrefix}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 註冊事件處理器
        /// </summary>
        private void RegisterEventHandlers()
        {
            Console.WriteLine("正在註冊事件處理器...");

            // 伺服器狀態變更事件
            _httpServer.ServerStatusChanged += OnServerStatusChanged;

            // 用戶端連接事件
            _httpServer.ClientConnected += OnClientConnected;

            // 用戶端斷線事件
            _httpServer.ClientDisconnected += OnClientDisconnected;

            // 訊息接收事件
            _httpServer.MessageReceived += OnMessageReceived;

            // WebSocket 訊息事件
            _httpServer.WebSocketMessageReceived += OnWebSocketMessageReceived;

            // 自訂 API 請求事件
            _httpServer.CustomApiRequest += OnCustomApiRequest;

            Console.WriteLine("✓ 事件處理器註冊完成");
        }

        /// <summary>
        /// 啟動伺服器
        /// </summary>
        private async Task StartServerAsync()
        {
            Console.WriteLine("正在啟動 HTTP 伺服器...");

            var success = await _httpServer.StartAsync();
              if (success)
            {
                Console.WriteLine("✓ HTTP 伺服器啟動成功！");
                Console.WriteLine("✓ 安全性中介軟體已啟用");
                Console.WriteLine("✓ 效能控制器已啟用");
                Console.WriteLine("✓ 連線管理已啟用");
                Console.WriteLine();
                
                // 顯示安全性和效能功能
                DemonstrateSecurityAndPerformanceFeatures();
                
                Console.WriteLine("伺服器資訊:");
                Console.WriteLine($"  監聽位址: {_httpServer.UrlPrefix}");
                Console.WriteLine($"  靜態檔案路徑: {_httpServer.StaticFilesPath}");
                Console.WriteLine($"  啟動時間: {_httpServer.StartTime}");
                Console.WriteLine();
                Console.WriteLine("可用的 API 端點:");
                Console.WriteLine("  POST /api/mes - 標準 MES API");
                Console.WriteLine("  POST /api/in-material - 入料 API");
                Console.WriteLine("  POST /api/out-material - 出料 API");
                Console.WriteLine("  GET  /api/server/statistics - 伺服器統計");
                Console.WriteLine("  GET  /api/health - 健康檢查");
                Console.WriteLine("  WebSocket: ws://localhost:8085/");
                Console.WriteLine();
            }
            else
            {
                throw new Exception("HTTP 伺服器啟動失敗");
            }
        }

        /// <summary>
        /// 等待使用者輸入
        /// </summary>
        private async Task WaitForUserInputAsync()
        {
            Console.WriteLine("伺服器正在運行中...");
            Console.WriteLine("您可以使用以下工具測試 API:");
            Console.WriteLine("  - Postman");
            Console.WriteLine("  - curl 指令");
            Console.WriteLine("  - 網頁瀏覽器 (靜態檔案和 GET API)");
            Console.WriteLine();
            Console.WriteLine("按任意鍵停止伺服器...");
            
            // 在實際應用中，您可能會使用不同的方式等待結束信號
            // 例如: CancellationToken, ManualResetEvent, 或監聽系統關閉事件
            await Task.Run(() => Console.ReadKey());
            Console.WriteLine();
        }

        /// <summary>
        /// 停止伺服器
        /// </summary>
        private async Task StopServerAsync()
        {
            Console.WriteLine("正在停止 HTTP 伺服器...");
            
            _httpServer.Stop();
            
            Console.WriteLine("✓ HTTP 伺服器已停止");
            
            // 顯示最終統計資訊
            var stats = _httpServer.GetServerStatistics();
            Console.WriteLine();
            Console.WriteLine("伺服器統計資訊:");
            Console.WriteLine($"  處理的請求總數: {_httpServer.TotalRequestsProcessed}");
            Console.WriteLine($"  運行時間: {(DateTime.Now - _httpServer.StartTime.GetValueOrDefault())}");
            
            await Task.CompletedTask;
        }

        #region 事件處理器        /// <summary>
        /// 處理伺服器狀態變更事件
        /// </summary>
        private void OnServerStatusChanged(object sender, ServerStatusChangedEventArgs e)
        {
            ConsoleColor color;
            switch (e.Status)
            {
                case ServerStatus.Starting:
                    color = ConsoleColor.Yellow;
                    break;
                case ServerStatus.Running:
                    color = ConsoleColor.Green;
                    break;
                case ServerStatus.Stopping:
                    color = ConsoleColor.Yellow;
                    break;
                case ServerStatus.Stopped:
                    color = ConsoleColor.Gray;
                    break;
                case ServerStatus.Error:
                    color = ConsoleColor.Red;
                    break;
                case ServerStatus.Warning:
                    color = ConsoleColor.DarkYellow;
                    break;
                default:
                    color = ConsoleColor.White;
                    break;
            }

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 伺服器狀態: {e.Status} - {e.Message}");
            Console.ForegroundColor = originalColor;
        }        /// <summary>
        /// 處理用戶端連接事件（增強版）
        /// </summary>
        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✓ 用戶端連接: {e.ClientIp} ({e.ClientId})");
            
            // 顯示目前連接數
            var clients = _httpServer.GetConnectedClients();
            Console.WriteLine($"  → 目前總連接數: {clients.Count}");
        }

        /// <summary>
        /// 處理用戶端斷線事件（增強版）
        /// </summary>
        private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ 用戶端斷線: {e.ClientId} - 原因: {e.Reason}");
            
            // 顯示目前連接數
            var clients = _httpServer.GetConnectedClients();
            Console.WriteLine($"  → 目前總連接數: {clients.Count}");
        }

        /// <summary>
        /// 處理訊息接收事件（增強版）
        /// </summary>
        private void OnMessageReceived(object sender, DDSWebAPI.Models.MessageEventArgs e)
        {
            var preview = e.Message.Length > 100 ? e.Message.Substring(0, 100) + "..." : e.Message;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📩 收到訊息 from {e.ClientIp}: {preview}");
        }

        /// <summary>
        /// 處理 WebSocket 訊息事件
        /// </summary>
        private void OnWebSocketMessageReceived(object sender, WebSocketMessageEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] WebSocket 訊息: {e.MessageType} from {e.ClientId}");
        }

        /// <summary>
        /// 處理自訂 API 請求事件
        /// </summary>
        private void OnCustomApiRequest(object sender, CustomApiRequestEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 自訂 API 請求: {e.Method} {e.Path}");
            
            // 在這裡您可以處理自訂的 API 端點
            // 例如: 回傳自訂回應或轉發到其他服務
            
            // 標記為已處理 (避免回傳 404)
            e.IsHandled = true;
            
            // 範例: 回傳簡單的回應
            try
            {
                var response = "{ \"message\": \"自訂 API 處理成功\", \"path\": \"" + e.Path + "\" }";
                var bytes = System.Text.Encoding.UTF8.GetBytes(response);
                
                e.Context.Response.ContentType = "application/json; charset=utf-8";
                e.Context.Response.StatusCode = 200;
                e.Context.Response.ContentLength64 = bytes.Length;
                e.Context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"處理自訂 API 時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 顯示目前連接的用戶端（新增功能）
        /// </summary>
        private void DisplayConnectedClients()
        {
            var clients = _httpServer.GetConnectedClients();
            Console.WriteLine($"目前連接用戶端數量: {clients.Count}");
            
            if (clients.Count > 0)
            {
                Console.WriteLine("已連接用戶端清單:");
                foreach (var client in clients)
                {
                    var duration = DateTime.Now - client.ConnectTime;
                    Console.WriteLine($"  - {client.IpAddress} ({client.Id}) - 連接時間: {duration.TotalSeconds:F1}秒");
                    Console.WriteLine($"    最後活動: {client.LastActivityTime:HH:mm:ss}，請求類型: {client.RequestType}");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 示範安全性和效能功能
        /// </summary>
        private void DemonstrateSecurityAndPerformanceFeatures()
        {
            Console.WriteLine("=== 安全性和效能功能說明 ===");
            Console.WriteLine("✓ 安全性中介軟體功能:");
            Console.WriteLine("  - API 金鑰驗證");
            Console.WriteLine("  - IP 白名單檢查");
            Console.WriteLine("  - 請求簽章驗證");
            Console.WriteLine();
            Console.WriteLine("✓ 效能控制器功能:");
            Console.WriteLine("  - 請求頻率限制 (100/分鐘)");
            Console.WriteLine("  - 平行連線數限制 (20個)");
            Console.WriteLine("  - 資料大小限制 (10MB)");
            Console.WriteLine("  - 請求逾時控制 (30秒)");
            Console.WriteLine();
            Console.WriteLine("✓ 連線管理功能:");
            Console.WriteLine("  - 即時連線監控");
            Console.WriteLine("  - 自動清理過期連接");
            Console.WriteLine("  - 連線統計和追蹤");
            Console.WriteLine();
        }

        #endregion
    }

    // 移除重複 Program/Main 類別，僅保留 ConsoleExample.cs 之入口點
}
