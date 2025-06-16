using System;
using System.Threading.Tasks;
using DDSWebAPI.Services;

namespace DDSWebAPI.Test
{
    /// <summary>
    /// 簡單的配置測試程式
    /// </summary>
    class ConfigTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== DDSWebAPI 設定檔測試 ===");
            Console.WriteLine();

            try
            {
                Console.WriteLine("1. 測試設定檔讀取...");
                var configManager = new ConfigurationManager("config.ini");
                
                var serverUrl = configManager.GetString("Server", "ServerUrl");
                var apiKey = configManager.GetString("Security", "ApiKey");
                var maxRequests = configManager.GetInt("Performance", "MaxRequestsPerMinute");
                var enableApiKey = configManager.GetBool("Security", "EnableApiKeyValidation");
                
                Console.WriteLine($"✓ 伺服器 URL: {serverUrl}");
                Console.WriteLine($"✓ API 金鑰: {apiKey}");
                Console.WriteLine($"✓ 最大請求數: {maxRequests}");
                Console.WriteLine($"✓ API 金鑰驗證: {enableApiKey}");
                Console.WriteLine();

                Console.WriteLine("2. 測試應用程式設定載入...");
                var appConfig = AppConfiguration.LoadFromConfigManager(configManager);
                
                Console.WriteLine($"✓ 設備代碼: {appConfig.Server.DeviceCode}");
                Console.WriteLine($"✓ 操作人員: {appConfig.Server.OperatorName}");
                Console.WriteLine($"✓ 簽章驗證: {appConfig.Security.EnableSignatureValidation}");
                Console.WriteLine($"✓ IP 白名單: {appConfig.Security.EnableIpWhitelist}");
                Console.WriteLine();

                Console.WriteLine("3. 測試 DDSWebAPIService 初始化...");
                var service = new DDSWebAPIService(appConfig);
                
                Console.WriteLine($"✓ 服務建立成功");
                Console.WriteLine($"✓ 伺服器 URL: {service.ServerUrl}");
                Console.WriteLine($"✓ 遠端 API URL: {service.RemoteApiUrl}");
                Console.WriteLine();

                Console.WriteLine("✅ 所有測試通過！設定檔功能正常運作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 測試失敗: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意鍵結束...");
            Console.ReadKey();
        }
    }
}
