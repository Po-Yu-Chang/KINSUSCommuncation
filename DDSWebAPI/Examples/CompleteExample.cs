///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: CompleteExample.cs
// 檔案描述: 完整的 HTTP 伺服器服務整合範例
// 功能概述: 展示如何使用服務實作和相依性注入建立完整的 HTTP 伺服器
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using DDSWebAPI.Services;
using DDSWebAPI.Examples.Services;
using DDSWebAPI.Events;
using DDSWebAPI.Enums;

namespace DDSWebAPI.Examples
{
    /// <summary>
    /// 完整的 HTTP 伺服器服務整合範例
    /// 展示如何使用具體的服務實作建立功能完整的 HTTP 伺服器
    /// </summary>
    public class CompleteExample
    {
        private HttpServerService _httpServer;
        private ExampleDatabaseService _databaseService;
        private ExampleWarehouseQueryService _warehouseService;
        private ExampleWorkflowTaskService _workflowService;
        private ExampleGlobalConfigService _configService;
        private ExampleUtilityService _utilityService;

        /// <summary>
        /// 執行完整範例
        /// </summary>
        public async Task RunCompleteExampleAsync()
        {
            Console.WriteLine("=== DDSWebAPI 完整整合範例 ===");
            Console.WriteLine("版本: 1.0.0 (重構版)");
            Console.WriteLine("功能: 相依性注入 + 完整服務實作");
            Console.WriteLine();

            try
            {
                // 1. 建立所有服務實例
                await CreateAllServicesAsync();

                // 2. 建立並配置 HTTP 伺服器 (使用相依性注入)
                await CreateHttpServerWithDependenciesAsync();

                // 3. 註冊事件處理器
                RegisterEventHandlers();

                // 4. 測試服務功能
                await TestServicesAsync();

                // 5. 啟動伺服器
                await StartServerAsync();

                // 6. 模擬一些 API 呼叫
                await SimulateApiCallsAsync();

                // 7. 等待使用者輸入
                await WaitForUserInputAsync();

                // 8. 關閉伺服器
                await StopServerAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"執行完整範例時發生錯誤: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }
            finally
            {
                // 確保資源被正確釋放
                _httpServer?.Dispose();
            }
        }

        /// <summary>
        /// 建立所有服務實例
        /// </summary>
        private async Task CreateAllServicesAsync()
        {
            Console.WriteLine("正在建立服務實例...");

            // 1. 建立資料庫服務
            _databaseService = new ExampleDatabaseService("mock://database");
            await _databaseService.TestConnectionAsync();

            // 2. 建立公用程式服務
            _utilityService = new ExampleUtilityService();

            // 3. 建立全域配置服務
            _configService = new ExampleGlobalConfigService();
            await _configService.ReloadConfigAsync();

            // 4. 建立倉庫查詢服務 (相依於資料庫服務)
            _warehouseService = new ExampleWarehouseQueryService(_databaseService);

            // 5. 建立工作流程任務服務
            _workflowService = new ExampleWorkflowTaskService();

            Console.WriteLine("✓ 所有服務實例建立完成");
        }

        /// <summary>
        /// 使用相依性注入建立 HTTP 伺服器
        /// </summary>
        private async Task CreateHttpServerWithDependenciesAsync()
        {
            Console.WriteLine("正在建立 HTTP 伺服器 (使用相依性注入)...");

            // 從配置服務取得設定
            var serverPort = _configService.GetConfig("ServerPort", 8085);
            var urlPrefix = $"http://localhost:{serverPort}/";

            // 建立 HTTP 伺服器並注入所有服務
            _httpServer = new HttpServerService(
                urlPrefix: urlPrefix,
                staticFilesPath: "./wwwroot",
                databaseService: _databaseService,        // 注入具體實作
                warehouseQueryService: _warehouseService, // 注入具體實作
                workflowTaskService: _workflowService,    // 注入具體實作
                globalConfigService: _configService,      // 注入具體實作
                utilityService: _utilityService           // 注入具體實作
            );

            Console.WriteLine($"✓ HTTP 伺服器建立完成，監聽位址: {_httpServer.UrlPrefix}");
            Console.WriteLine("✓ 所有服務已透過相依性注入機制注入");
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 註冊事件處理器
        /// </summary>
        private void RegisterEventHandlers()
        {
            Console.WriteLine("正在註冊事件處理器...");

            _httpServer.ServerStatusChanged += OnServerStatusChanged;
            _httpServer.ClientConnected += OnClientConnected;
            _httpServer.ClientDisconnected += OnClientDisconnected;
            _httpServer.MessageReceived += OnMessageReceived;
            _httpServer.WebSocketMessageReceived += OnWebSocketMessageReceived;
            _httpServer.CustomApiRequest += OnCustomApiRequest;

            Console.WriteLine("✓ 事件處理器註冊完成");
        }

        /// <summary>
        /// 測試各服務功能
        /// </summary>
        private async Task TestServicesAsync()
        {
            Console.WriteLine("正在測試服務功能...");

            try
            {                // 測試資料庫服務
                await _databaseService.GetAllAsync<object>("SELECT * FROM test_table");

                // 測試倉庫服務
                var storageInfo = await _warehouseService.GetStorageInfoByLocationAsync("A01-01");
                var location = await _warehouseService.GetLocationByPinAsync("PIN001");                // 測試工作流程服務
                await _workflowService.ChangeSpeedAsync("75");
                await _workflowService.ExecuteClampOperationAsync("OPEN", "A01-01");

                // 測試公用程式服務
                var uniqueId = _utilityService.GenerateUniqueId();
                var jsonValid = _utilityService.ValidateJson("{\"test\": \"value\"}");

                Console.WriteLine("✓ 所有服務功能測試通過");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ 服務測試時發生警告: {ex.Message}");
            }
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
                Console.WriteLine();
                DisplayServerInfo();
                DisplayAvailableApis();
            }
            else
            {
                throw new Exception("HTTP 伺服器啟動失敗");
            }
        }

        /// <summary>
        /// 顯示伺服器資訊
        /// </summary>
        private void DisplayServerInfo()
        {
            Console.WriteLine("伺服器資訊:");
            Console.WriteLine($"  監聽位址: {_httpServer.UrlPrefix}");
            Console.WriteLine($"  靜態檔案路徑: {_httpServer.StaticFilesPath}");
            Console.WriteLine($"  啟動時間: {_httpServer.StartTime}");
            Console.WriteLine($"  最大連接數: {_configService.GetConfig("MaxConnections", 100)}");
            Console.WriteLine($"  請求逾時: {_configService.GetConfig("RequestTimeout", 30000)}ms");
            Console.WriteLine();
        }

        /// <summary>
        /// 顯示可用的 API 端點
        /// </summary>
        private void DisplayAvailableApis()
        {
            Console.WriteLine("可用的 API 端點:");
            Console.WriteLine("【標準 MES API】");
            Console.WriteLine("  POST /api/mes - 統一 MES API 端點");
            Console.WriteLine();
            Console.WriteLine("【倉庫管理 API】");
            Console.WriteLine("  POST /api/in-material - 入料操作");
            Console.WriteLine("  POST /api/out-material - 出料操作");
            Console.WriteLine("  POST /api/getlocationbystorage - 依儲存位置查詢");
            Console.WriteLine("  POST /api/getlocationbypin - 依 PIN 查詢位置");
            Console.WriteLine("  GET  /api/out-getpins - 取得出料 PIN 資料");
            Console.WriteLine();
            Console.WriteLine("【設備控制 API】");
            Console.WriteLine("  POST /api/operationclamp - 夾爪操作");
            Console.WriteLine("  POST /api/changespeed - 速度調整");
            Console.WriteLine();
            Console.WriteLine("【系統管理 API】");
            Console.WriteLine("  POST /api/server/status - 伺服器狀態");
            Console.WriteLine("  POST /api/server/restart - 重新啟動");
            Console.WriteLine("  GET  /api/server/statistics - 統計資料");
            Console.WriteLine("  GET  /api/health - 健康檢查");
            Console.WriteLine();
            Console.WriteLine("【即時通訊】");
            Console.WriteLine($"  WebSocket: ws://localhost:{_configService.GetConfig("ServerPort", 8085)}/");
            Console.WriteLine();
        }

        /// <summary>
        /// 模擬一些 API 呼叫 (用於測試)
        /// </summary>
        private async Task SimulateApiCallsAsync()
        {
            Console.WriteLine("模擬 API 呼叫...");

            try
            {
                // 模擬背景工作
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    
                    // 模擬一些倉庫操作
                    await _warehouseService.ProcessInMaterialAsync("ITEM001", 50, "A01-01");
                    await _workflowService.ChangeSpeedAsync(80);
                    await _warehouseService.ProcessOutMaterialAsync("ITEM002", 25, "A01-02");
                });

                Console.WriteLine("✓ 背景模擬任務已啟動");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ 模擬 API 呼叫時發生警告: {ex.Message}");
            }
        }

        /// <summary>
        /// 等待使用者輸入
        /// </summary>
        private async Task WaitForUserInputAsync()
        {
            Console.WriteLine("伺服器正在運行中...");
            Console.WriteLine();
            Console.WriteLine("您可以使用以下方法測試:");
            Console.WriteLine("1. 使用 Postman 發送 POST 請求到 API 端點");
            Console.WriteLine("2. 使用 curl 指令測試:");
            Console.WriteLine($"   curl -X GET http://localhost:{_configService.GetConfig("ServerPort", 8085)}/api/health");
            Console.WriteLine("3. 使用瀏覽器訪問:");
            Console.WriteLine($"   http://localhost:{_configService.GetConfig("ServerPort", 8085)}/api/server/statistics");
            Console.WriteLine();
            Console.WriteLine("按任意鍵停止伺服器...");
            
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
            DisplayFinalStatistics();
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 顯示最終統計資訊
        /// </summary>
        private void DisplayFinalStatistics()
        {
            Console.WriteLine();
            Console.WriteLine("伺服器統計資訊:");
            Console.WriteLine($"  處理的請求總數: {_httpServer.TotalRequestsProcessed}");
            Console.WriteLine($"  運行時間: {(DateTime.Now - _httpServer.StartTime.GetValueOrDefault())}");
            Console.WriteLine($"  產生的唯一 ID 範例: {_utilityService.GenerateUniqueId()}");
            
            var allConfig = _configService.GetAllConfig();
            Console.WriteLine($"  載入的配置項目數: {allConfig.Count}");        }

        #region 事件處理器

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
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔧 伺服器狀態: {e.Status} - {e.Message}");
            Console.ForegroundColor = originalColor;
        }

        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📱 用戶端連接: {e.ClientIp} ({e.ClientId})");
        }

        private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📴 用戶端斷線: {e.ClientIp} ({e.ClientId})");
        }

        private void OnMessageReceived(object sender, DDSWebAPI.Models.MessageEventArgs e)
        {
            var truncatedMessage = e.Message.Length > 50 ? 
                e.Message.Substring(0, 50) + "..." : e.Message;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 💬 收到訊息: {truncatedMessage}");
        }

        private void OnWebSocketMessageReceived(object sender, WebSocketMessageEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🌐 WebSocket: {e.MessageType} from {e.ClientId}");
        }

        private void OnCustomApiRequest(object sender, CustomApiRequestEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔧 自訂 API: {e.Method} {e.Path}");
            
            // 處理自訂 API 的邏輯
            HandleCustomApiRequest(e);
        }

        /// <summary>
        /// 處理自訂 API 請求
        /// </summary>
        private void HandleCustomApiRequest(CustomApiRequestEventArgs e)
        {
            try
            {
                var response = new
                {
                    message = "自訂 API 處理成功",
                    path = e.Path,
                    method = e.Method,
                    timestamp = DateTime.Now,
                    serverId = _utilityService.GenerateUniqueId()
                };

                var jsonResponse = _utilityService.SerializeToJson(response, true);
                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonResponse);
                
                e.Context.Response.ContentType = "application/json; charset=utf-8";
                e.Context.Response.StatusCode = 200;
                e.Context.Response.ContentLength64 = bytes.Length;
                e.Context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                
                e.IsHandled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"處理自訂 API 時發生錯誤: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 完整範例程式入口點
    /// </summary>
    public class CompleteProgram
    {
        /// <summary>
        /// 應用程式主要入口點
        /// </summary>
        /// <param name="args">命令列參數</param>
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            var example = new CompleteExample();
            await example.RunCompleteExampleAsync();
            
            Console.WriteLine();
            Console.WriteLine("感謝使用 DDSWebAPI HTTP 伺服器服務！");
        }
    }
}
