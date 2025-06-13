using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using DDSWebAPI.Services;
using DDSWebAPI.Models;

namespace DDSWebAPI.Examples
{    /// <summary>
    /// DDSWebAPI 使用範例
    /// 這個範例展示如何在控制台應用程式中使用 DDSWebAPI 函式庫
    /// 包含安全性功能（API 金鑰、簽章驗證、IP 白名單）的展示
    /// </summary>
    public class ConsoleExample
    {
        private DDSWebAPIService _ddsService;
        private AppConfiguration _appConfig;

        /// <summary>
        /// 主要範例方法
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== DDSWebAPI 控制台範例 ===");
            Console.WriteLine("展示安全性功能與效能控制");
            Console.WriteLine();

            try
            {
                // 1. 載入設定檔
                LoadConfiguration();

                // 2. 初始化服務
                await InitializeServiceAsync();

                // 3. 展示安全性功能
                await DemonstrateSecurityFeaturesAsync();

                // 4. 啟動伺服器
                await StartServerAsync();

                // 5. 發送測試資料
                await SendTestDataAsync();

                // 6. 等待使用者輸入
                Console.WriteLine("按任意鍵停止伺服器並結束程式...");
                Console.ReadKey();

                // 7. 停止服務
                StopService();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"執行範例時發生錯誤: {ex.Message}");
            }
        }        /// <summary>
        /// 載入設定檔
        /// </summary>
        private void LoadConfiguration()
        {
            Console.WriteLine("正在載入設定檔...");

            try
            {
                // 使用設定檔載入設定
                var configManager = new ConfigurationManager("config.ini");
                _appConfig = AppConfiguration.LoadFromConfigManager(configManager);

                Console.WriteLine("✓ 設定檔載入成功");
                Console.WriteLine($"  - 伺服器 URL: {_appConfig.Server.ServerUrl}");
                Console.WriteLine($"  - 遠端 API URL: {_appConfig.Server.RemoteApiUrl}");
                Console.WriteLine($"  - 設備代碼: {_appConfig.Server.DeviceCode}");
                Console.WriteLine($"  - 操作人員: {_appConfig.Server.OperatorName}");
                Console.WriteLine($"  - API 金鑰驗證: {(_appConfig.Security.EnableApiKeyValidation ? "啟用" : "停用")}");
                Console.WriteLine($"  - 簽章驗證: {(_appConfig.Security.EnableSignatureValidation ? "啟用" : "停用")}");
                Console.WriteLine($"  - IP 白名單: {(_appConfig.Security.EnableIpWhitelist ? "啟用" : "停用")}");
                Console.WriteLine($"  - 最大請求頻率: {_appConfig.Performance.MaxRequestsPerMinute}/分鐘");
                Console.WriteLine($"  - 最大併發連線: {_appConfig.Performance.MaxConcurrentConnections}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 載入設定檔失敗: {ex.Message}");
                Console.WriteLine("使用預設設定...");

                // 使用預設設定
                _appConfig = new AppConfiguration
                {
                    Server = new ServerConfiguration
                    {
                        ServerUrl = "http://localhost:8085/",
                        RemoteApiUrl = "http://localhost:8086/",
                        DeviceCode = "KINSUS_CONSOLE_001",
                        OperatorName = "CONSOLE_USER"
                    },
                    Security = new SecurityConfiguration
                    {
                        ApiKey = "KINSUS_API_KEY_2024",
                        SignatureSecret = "KINSUS_SIGNATURE_SECRET_KEY_2024",
                        EnableApiKeyValidation = false, // 範例中先停用
                        EnableSignatureValidation = false, // 範例中先停用
                        EnableIpWhitelist = false, // 範例中先停用
                        AllowedIpAddresses = new string[] { "127.0.0.1" }
                    },
                    Performance = new PerformanceConfiguration
                    {
                        MaxRequestsPerMinute = 100,
                        MaxConcurrentConnections = 50,
                        MaxRequestSizeBytes = 10485760,
                        RequestTimeoutSeconds = 30,
                        MaxResponseSizeBytes = 52428800
                    }
                };
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 初始化 DDS API 服務
        /// </summary>
        private async Task InitializeServiceAsync()
        {
            Console.WriteLine("正在初始化 DDS API 服務...");

            // 使用設定檔建立服務實例
            _ddsService = new DDSWebAPIService(_appConfig);

            // 註冊事件處理程式
            RegisterEventHandlers();

            Console.WriteLine("✓ DDS API 服務初始化完成");
            Console.WriteLine($"  - 使用設定檔設定");
            Console.WriteLine($"  - 安全性功能已設定");
            Console.WriteLine($"  - 效能控制已設定");
            Console.WriteLine();

            await Task.Delay(100); // 小延遲讓初始化完成
        }

        /// <summary>
        /// 展示安全性功能
        /// </summary>
        private async Task DemonstrateSecurityFeaturesAsync()
        {
            Console.WriteLine("=== 安全性功能展示 ===");

            // 展示 API 金鑰生成
            DemonstrateApiKeyGeneration();

            // 展示簽章生成
            DemonstrateSignatureGeneration();

            // 展示 IP 白名單設定
            DemonstrateIpWhitelistSettings();

            Console.WriteLine();
            await Task.Delay(1000); // 讓使用者有時間閱讀
        }

        /// <summary>
        /// 展示 API 金鑰生成
        /// </summary>
        private void DemonstrateApiKeyGeneration()
        {
            Console.WriteLine("1. API 金鑰範例:");
            Console.WriteLine($"   設定檔中的 API 金鑰: {_appConfig.Security.ApiKey}");
            Console.WriteLine($"   使用方式: Authorization: Bearer {_appConfig.Security.ApiKey}");
            Console.WriteLine();
        }

        /// <summary>
        /// 展示簽章生成
        /// </summary>
        private void DemonstrateSignatureGeneration()
        {
            Console.WriteLine("2. 請求簽章範例:");
            
            var testContent = "{\"DevCode\":\"KINSUS_CONSOLE_001\",\"Data\":[]}";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var signature = GenerateSignature(testContent, timestamp, _appConfig.Security.SignatureSecret);

            Console.WriteLine($"   請求內容: {testContent}");
            Console.WriteLine($"   時間戳記: {timestamp}");
            Console.WriteLine($"   簽章密鑰: {_appConfig.Security.SignatureSecret}");
            Console.WriteLine($"   生成的簽章: {signature}");
            Console.WriteLine($"   HTTP 標頭: X-Signature: {signature}");
            Console.WriteLine($"   HTTP 標頭: X-Timestamp: {timestamp}");
            Console.WriteLine();
        }

        /// <summary>
        /// 展示 IP 白名單設定
        /// </summary>
        private void DemonstrateIpWhitelistSettings()
        {
            Console.WriteLine("3. IP 白名單範例:");
            Console.WriteLine($"   允許的 IP 位址:");
            
            if (_appConfig.Security.AllowedIpAddresses?.Length > 0)
            {
                foreach (var ip in _appConfig.Security.AllowedIpAddresses)
                {
                    Console.WriteLine($"     - {ip}");
                }
            }
            else
            {
                Console.WriteLine("     - 所有 IP（白名單為空）");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 生成請求簽章
        /// </summary>
        /// <param name="content">請求內容</param>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="secretKey">密鑰</param>
        /// <returns>簽章</returns>
        private string GenerateSignature(string content, string timestamp, string secretKey)
        {
            try
            {
                var data = $"{content}:{timestamp}";
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
                {
                    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                    return Convert.ToBase64String(hash);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成簽章失敗: {ex.Message}");
                return "";
            }
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
            };

            // API 呼叫成功事件
            _ddsService.ApiCallSuccess += (sender, e) =>
            {
                Console.WriteLine($"[API 成功] {e.Result.RequestUrl} (耗時: {e.Result.ProcessingTimeMs:F2}ms)");
            };

            // API 呼叫失敗事件
            _ddsService.ApiCallFailure += (sender, e) =>
            {
                Console.WriteLine($"[API 失敗] {e.Result.RequestUrl}: {e.Result.ErrorMessage}");
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
            {                var reportData = new ToolOutputReportData
                {
                    WorkOrderNo = "WO_CONSOLE_001",
                    ToolCode = "TOOL_TEST_001",
                    QualityStatus = "success",
                    OperationTime = DateTime.Now,
                    OutputQuantity = 150,
                    OperationType = "配針",
                    Position = "A001",
                    Remarks = "Console 測試"
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
            {                var errorData = new ErrorReportData
                {
                    ErrorCode = "E_CONSOLE_001",
                    ErrorMessage = "控制台測試錯誤",
                    ErrorLevel = "medium",
                    OccurrenceTime = DateTime.Now,
                    DeviceCode = "KINSUS_CONSOLE",
                    OperatorName = "CONSOLE_USER",
                    DetailDescription = "Console 測試錯誤",
                    IsResolved = false
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
            {                var statusData = new MachineStatusReportData
                {
                    MachineStatus = "running",
                    OperationMode = "auto",
                    CurrentJob = "WO_CONSOLE_001",
                    ProcessedCount = 150,
                    TargetCount = 200,
                    CompletionPercentage = 75.0,
                    Temperature = 38.5,
                    Pressure = 1.2,
                    Vibration = 0.05,
                    ReportTime = DateTime.Now,
                    Warnings = new List<string> { "溫度稍高", "建議檢查冷卻系統" }
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
        /// </summary>
        private void StopService()
        {
            Console.WriteLine();
            Console.WriteLine("正在停止服務...");

            _ddsService?.StopServer();
            _ddsService?.Dispose();

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
