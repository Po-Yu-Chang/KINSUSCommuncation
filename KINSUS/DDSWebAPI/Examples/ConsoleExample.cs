using System;
using System.Threading.Tasks;
using DDSWebAPI.Services;
using DDSWebAPI.Models;

namespace DDSWebAPI.Examples
{
    /// <summary>
    /// DDSWebAPI 使用範例
    /// 這個範例展示如何在控制台應用程式中使用 DDSWebAPI 函式庫
    /// </summary>
    public class ConsoleExample
    {
        private DDSWebAPIService _ddsService;

        /// <summary>
        /// 主要範例方法
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== DDSWebAPI 控制台範例 ===");
            Console.WriteLine();

            try
            {
                // 1. 初始化服務
                await InitializeServiceAsync();

                // 2. 啟動伺服器
                await StartServerAsync();

                // 3. 發送測試資料
                await SendTestDataAsync();

                // 4. 等待使用者輸入
                Console.WriteLine("按任意鍵停止伺服器並結束程式...");
                Console.ReadKey();

                // 5. 停止服務
                StopService();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"執行範例時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化 DDS API 服務
        /// </summary>
        private async Task InitializeServiceAsync()
        {
            Console.WriteLine("正在初始化 DDS API 服務...");

            // 建立服務實例
            _ddsService = new DDSWebAPIService(
                serverUrl: "http://localhost:8085/",
                remoteApiUrl: "http://localhost:8086/",
                deviceCode: "KINSUS_CONSOLE_001",
                operatorName: "CONSOLE_USER"
            );

            // 註冊事件處理程式
            RegisterEventHandlers();

            Console.WriteLine("DDS API 服務初始化完成");
            Console.WriteLine();

            await Task.Delay(100); // 小延遲讓初始化完成
        }

        /// <summary>
        /// 註冊事件處理程式
        /// </summary>
        private void RegisterEventHandlers()
        {
            // 訊息接收事件
            _ddsService.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[訊息接收] 來自 {e.ClientIp}: {e.Message}");
                Console.WriteLine();
            };

            // 用戶端連接事件
            _ddsService.ClientConnected += (sender, e) =>
            {
                Console.WriteLine($"[用戶端連接] IP: {e.ClientIp}, ID: {e.ClientId}");
            };

            // 用戶端斷線事件
            _ddsService.ClientDisconnected += (sender, e) =>
            {
                Console.WriteLine($"[用戶端斷線] IP: {e.ClientIp}, ID: {e.ClientId}");
            };

            // 伺服器狀態變更事件
            _ddsService.ServerStatusChanged += (sender, e) =>
            {
                Console.WriteLine($"[伺服器狀態] {e.Status}: {e.Description}");
            };            // API 呼叫成功事件
            _ddsService.ApiCallSuccess += (sender, e) =>
            {
                Console.WriteLine($"[API 成功] {e.Endpoint}: {e.Response}");
            };

            // API 呼叫失敗事件
            _ddsService.ApiCallFailure += (sender, e) =>
            {
                Console.WriteLine($"[API 失敗] {e.Endpoint}: {e.Error}");
            };

            // 日誌訊息事件
            _ddsService.LogMessage += (sender, e) =>
            {
                Console.WriteLine($"[{e.Level.ToString().ToUpper()}] {e.Message}");
            };
        }

        /// <summary>
        /// 啟動伺服器
        /// </summary>
        private async Task StartServerAsync()
        {
            Console.WriteLine("正在啟動 HTTP 伺服器...");

            bool success = await _ddsService.StartServerAsync();

            if (success)
            {
                Console.WriteLine($"HTTP 伺服器啟動成功！監聽位址: {_ddsService.ServerUrl}");
                Console.WriteLine("等待接收來自 MES/IoT 系統的請求...");
            }
            else
            {
                Console.WriteLine("HTTP 伺服器啟動失敗！");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 發送測試資料
        /// </summary>
        private async Task SendTestDataAsync()
        {
            Console.WriteLine("正在發送測試資料到遠端 API...");
            Console.WriteLine();

            // 1. 發送配針回報
            await SendToolOutputReportAsync();

            // 2. 發送錯誤回報
            await SendErrorReportAsync();

            // 3. 發送機臺狀態回報
            await SendMachineStatusReportAsync();

            Console.WriteLine("測試資料發送完成");
            Console.WriteLine();
        }

        /// <summary>
        /// 發送配針回報測試
        /// </summary>
        private async Task SendToolOutputReportAsync()
        {
            try
            {
                var reportData = new ToolOutputReportData
                {
                    WorkOrder = "WO_CONSOLE_001",
                    ToolId = "TOOL_TEST_001",
                    Result = "success",
                    ProcessTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Quantity = 150,
                    ExtendData = null
                };

                Console.WriteLine("發送配針回報...");
                var result = await _ddsService.SendToolOutputReportAsync(reportData);

                if (result.IsSuccess)
                {
                    Console.WriteLine("✓ 配針回報發送成功");
                }
                else
                {
                    Console.WriteLine($"✗ 配針回報發送失敗: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 配針回報發送異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 發送錯誤回報測試
        /// </summary>
        private async Task SendErrorReportAsync()
        {
            try
            {
                var errorData = new ErrorReportData
                {
                    ErrorCode = "E_CONSOLE_001",
                    ErrorMessage = "控制台測試錯誤",
                    ErrorLevel = "medium",
                    OccurTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    WorkOrder = "WO_CONSOLE_001",
                    ExtendData = null
                };

                Console.WriteLine("發送錯誤回報...");
                var result = await _ddsService.SendErrorReportAsync(errorData);

                if (result.IsSuccess)
                {
                    Console.WriteLine("✓ 錯誤回報發送成功");
                }
                else
                {
                    Console.WriteLine($"✗ 錯誤回報發送失敗: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 錯誤回報發送異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 發送機臺狀態回報測試
        /// </summary>
        private async Task SendMachineStatusReportAsync()
        {
            try
            {
                var statusData = new MachineStatusReportData
                {
                    Status = "running",
                    CpuUsage = 25.6f,
                    MemoryUsage = 68.2f,
                    DiskUsage = 45.8f,
                    Temperature = 38.5f,
                    ReportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ExtendData = null
                };

                Console.WriteLine("發送機臺狀態回報...");
                var result = await _ddsService.SendMachineStatusReportAsync(statusData);

                if (result.IsSuccess)
                {
                    Console.WriteLine("✓ 機臺狀態回報發送成功");
                }
                else
                {
                    Console.WriteLine($"✗ 機臺狀態回報發送失敗: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 機臺狀態回報發送異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止服務
        /// </summary>        private async Task StopService()
        {
            Console.WriteLine();
            Console.WriteLine("正在停止服務...");

            if (_ddsService != null)
            {
                await _ddsService.StopServerAsync();
                _ddsService.Dispose();
            }

            Console.WriteLine("服務已停止");
        }
    }

    /// <summary>
    /// 控制台應用程式入口點範例
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var example = new ConsoleExample();
            await example.RunExampleAsync();
        }
    }
}
