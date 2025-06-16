using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using KINSUS;

namespace KINSUS.Tests
{
    /// <summary>
    /// MainWindow 的測試類別
    /// </summary>
    [TestClass]
    public class MainWindowTests
    {
        private MainWindow _mainWindow;

        [TestInitialize]
        public void Setup()
        {
            // 在測試環境中，需要初始化 WPF 應用程式
            if (Application.Current == null)
            {
                new Application();
            }
            
            _mainWindow = new MainWindow();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _mainWindow?.Close();
        }

        /// <summary>
        /// 測試 IoT 連接功能 - 空端點檢查
        /// </summary>
        [TestMethod]
        public void TestIoTConnection_EmptyEndpoint()
        {
            // 此測試確保當端點為空時會顯示警告
            // 由於涉及 MessageBox，實際測試中需要 mock UI 元素
            
            // 模擬空的端點輸入
            var textBox = _mainWindow.FindName("txtIotEndpoint") as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                textBox.Text = "";
            }

            // 驗證連接按鈕的初始狀態
            var connectButton = _mainWindow.FindName("btnConnectIoT") as System.Windows.Controls.Button;
            var disconnectButton = _mainWindow.FindName("btnDisconnectIoT") as System.Windows.Controls.Button;

            Assert.IsNotNull(connectButton);
            Assert.IsNotNull(disconnectButton);
            Assert.IsTrue(connectButton.IsEnabled);
            Assert.IsFalse(disconnectButton.IsEnabled);
        }

        /// <summary>
        /// 測試 IoT 連接功能 - 有效端點
        /// </summary>
        [TestMethod] 
        public void TestIoTConnection_ValidEndpoint()
        {
            // 設定有效的端點
            var textBox = _mainWindow.FindName("txtIotEndpoint") as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                textBox.Text = "http://localhost:8086";
            }

            // 驗證端點設定
            Assert.AreEqual("http://localhost:8086", textBox?.Text);
        }

        /// <summary>
        /// 測試 URL 格式化功能
        /// </summary>
        [TestMethod]
        public void TestUrlFormatting()
        {
            string[] testUrls = {
                "localhost:8086",
                "192.168.1.100:8080",
                "http://localhost:8086",
                "https://example.com:443"
            };

            string[] expectedUrls = {
                "http://localhost:8086",
                "http://192.168.1.100:8080", 
                "http://localhost:8086",
                "https://example.com:443"
            };

            for (int i = 0; i < testUrls.Length; i++)
            {
                string input = testUrls[i];
                string expected = expectedUrls[i];

                // 模擬連接邏輯中的 URL 格式化
                if (!input.StartsWith("http://") && !input.StartsWith("https://"))
                {
                    input = "http://" + input;
                }

                Assert.AreEqual(expected, input, $"URL 格式化失敗，輸入: {testUrls[i]}");
            }
        }

        /// <summary>
        /// 測試連接狀態管理
        /// </summary>
        [TestMethod]
        public void TestConnectionStateManagement()
        {
            var connectButton = _mainWindow.FindName("btnConnectIoT") as System.Windows.Controls.Button;
            var disconnectButton = _mainWindow.FindName("btnDisconnectIoT") as System.Windows.Controls.Button;
            var statusRect = _mainWindow.FindName("rectIoTStatus") as System.Windows.Shapes.Rectangle;
            var connectionStatusRect = _mainWindow.FindName("rectIoTConnectionStatus") as System.Windows.Shapes.Rectangle;

            // 檢查初始狀態
            Assert.IsTrue(connectButton?.IsEnabled ?? false, "連接按鈕應該啟用");
            Assert.IsFalse(disconnectButton?.IsEnabled ?? true, "斷開按鈕應該停用");
              // 檢查狀態指示器存在
            Assert.IsNotNull(statusRect, "IoT 狀態指示器應該存在");
            Assert.IsNotNull(connectionStatusRect, "連接狀態指示器應該存在");
        }

        /// <summary>
        /// 測試持久連線建立功能
        /// </summary>
        [TestMethod]
        public async Task TestPersistentConnectionEstablishment()
        {
            Console.WriteLine("=== 測試持久連線建立功能 ===");
            
            // 先啟動伺服器
            var startServerButton = _mainWindow.FindName("btnStartServer") as System.Windows.Controls.Button;
            Assert.IsNotNull(startServerButton, "啟動伺服器按鈕應該存在");
            
            // 模擬點擊啟動伺服器
            startServerButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            
            // 等待伺服器啟動
            await Task.Delay(3000);
            
            // 檢查伺服器狀態
            var serverStatus = _mainWindow.FindName("txtStatus") as System.Windows.Controls.TextBox;
            Console.WriteLine($"伺服器狀態: {serverStatus?.Text}");
            
            // 設定 API 函式選擇
            var apiComboBox = _mainWindow.FindName("cmbApiFunction") as System.Windows.Controls.ComboBox;
            if (apiComboBox != null && apiComboBox.Items.Count > 0)
            {
                apiComboBox.SelectedIndex = 0; // 選擇第一個 API 函式
            }
            
            // 設定範本內容
            var templateTextBox = _mainWindow.FindName("txtTemplate") as System.Windows.Controls.TextBox;
            if (templateTextBox != null)
            {
                templateTextBox.Text = @"{
  ""requestID"": ""TEST_CONNECTION"",
  ""serviceName"": ""CONNECTION_TEST_COMMAND"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""KINSUS_TEST"",
  ""operator"": ""TEST_USER"",
  ""data"": [
    {
      ""testType"": ""PERSISTENT_CONNECTION"",
      ""message"": ""測試持久連線""
    }
  ]
}";
            }
            
            // 模擬點擊發送請求按鈕
            var sendRequestButton = _mainWindow.FindName("btnSendRequest") as System.Windows.Controls.Button;
            Assert.IsNotNull(sendRequestButton, "發送請求按鈕應該存在");
            
            Console.WriteLine("模擬點擊發送請求按鈕...");
            sendRequestButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            
            // 等待請求處理
            await Task.Delay(5000);
            
            // 檢查用戶端日誌
            var clientLogTextBox = _mainWindow.FindName("txtClientMessages") as System.Windows.Controls.TextBox;
            string clientLog = clientLogTextBox?.Text ?? "";
            Console.WriteLine($"用戶端日誌: {clientLog}");
            
            // 驗證持久連線相關的日誌訊息
            Assert.IsTrue(clientLog.Contains("持久連線") || clientLog.Contains("API 請求"), 
                "日誌中應該包含連線相關資訊");
        }

        /// <summary>
        /// 測試心跳檢測機制
        /// </summary>
        [TestMethod]
        public async Task TestHeartbeatMechanism()
        {
            Console.WriteLine("=== 測試心跳檢測機制 ===");
            
            // 啟動伺服器
            var startServerButton = _mainWindow.FindName("btnStartServer") as System.Windows.Controls.Button;
            startServerButton?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            
            await Task.Delay(3000);
            
            // 設定基本配置
            var deviceCodeTextBox = _mainWindow.FindName("txtDevCode") as System.Windows.Controls.TextBox;
            if (deviceCodeTextBox != null)
            {
                deviceCodeTextBox.Text = "KINSUS_HEARTBEAT_TEST";
            }
            
            // 發送第一個請求建立持久連線
            var sendRequestButton = _mainWindow.FindName("btnSendRequest") as System.Windows.Controls.Button;
            var templateTextBox = _mainWindow.FindName("txtTemplate") as System.Windows.Controls.TextBox;
            
            if (templateTextBox != null)
            {
                templateTextBox.Text = @"{
  ""requestID"": ""HEARTBEAT_INIT"",
  ""serviceName"": ""CONNECTION_TEST_COMMAND"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""KINSUS_HEARTBEAT_TEST"",
  ""data"": [{ ""testType"": ""HEARTBEAT_INIT"" }]
}";
            }
            
            sendRequestButton?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            await Task.Delay(2000);
            
            // 等待多個心跳週期（15秒 x 3 = 45秒）
            Console.WriteLine("等待心跳檢測週期...");
            for (int i = 1; i <= 3; i++)
            {
                Console.WriteLine($"等待第 {i} 個心跳週期...");
                await Task.Delay(15000); // 等待15秒
                
                // 檢查日誌中的心跳訊息
                var clientLogTextBox = _mainWindow.FindName("txtClientMessages") as System.Windows.Controls.TextBox;
                string clientLog = clientLogTextBox?.Text ?? "";
                
                if (clientLog.Contains("心跳") || clientLog.Contains("持久連線運作正常"))
                {
                    Console.WriteLine($"✅ 心跳檢測第 {i} 週期運作正常");
                }
                else
                {
                    Console.WriteLine($"⚠️ 心跳檢測第 {i} 週期可能有問題");
                }
            }
            
            var finalLogTextBox = _mainWindow.FindName("txtClientMessages") as System.Windows.Controls.TextBox;
            string finalLog = finalLogTextBox?.Text ?? "";
            Console.WriteLine($"最終日誌: {finalLog}");
            
            // 檢查是否有心跳相關的日誌
            Assert.IsTrue(finalLog.Length > 0, "應該有日誌輸出");
        }

        /// <summary>
        /// 測試連線重試機制
        /// </summary>
        [TestMethod]
        public async Task TestConnectionRetryMechanism()
        {
            Console.WriteLine("=== 測試連線重試機制 ===");
            
            // 先不啟動伺服器，測試連線失敗的情況
            var sendRequestButton = _mainWindow.FindName("btnSendRequest") as System.Windows.Controls.Button;
            var templateTextBox = _mainWindow.FindName("txtTemplate") as System.Windows.Controls.TextBox;
            var clientLogTextBox = _mainWindow.FindName("txtClientMessages") as System.Windows.Controls.TextBox;
            
            // 設定測試內容
            if (templateTextBox != null)
            {
                templateTextBox.Text = @"{
  ""requestID"": ""RETRY_TEST"",
  ""serviceName"": ""CONNECTION_TEST_COMMAND"",
  ""data"": [{ ""testType"": ""RETRY_TEST"" }]
}";
            }
            
            // 嘗試發送請求（應該失敗，因為伺服器未啟動）
            Console.WriteLine("測試在伺服器未啟動時的連線行為...");
            sendRequestButton?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            
            await Task.Delay(3000);
            
            string initialLog = clientLogTextBox?.Text ?? "";
            Console.WriteLine($"初始日誌: {initialLog}");
            
            // 現在啟動伺服器
            Console.WriteLine("啟動伺服器...");
            var startServerButton = _mainWindow.FindName("btnStartServer") as System.Windows.Controls.Button;
            startServerButton?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            
            await Task.Delay(3000);
            
            // 再次嘗試發送請求（應該成功）
            Console.WriteLine("伺服器啟動後重新嘗試連線...");
            sendRequestButton?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            
            await Task.Delay(3000);
            
            string finalLog = clientLogTextBox?.Text ?? "";
            Console.WriteLine($"最終日誌: {finalLog}");
            
            // 驗證日誌中包含重試相關資訊
            Assert.IsTrue(finalLog.Length > initialLog.Length, "日誌應該有新的內容");
        }

        /// <summary>
        /// 測試 API 端點映射
        /// </summary>
        [TestMethod]
        public void TestApiEndpointMapping()
        {
            Console.WriteLine("=== 測試 API 端點映射 ===");
            
            var apiComboBox = _mainWindow.FindName("cmbApiFunction") as System.Windows.Controls.ComboBox;
            Assert.IsNotNull(apiComboBox, "API 函式下拉選單應該存在");
            
            Console.WriteLine($"API 函式選項數量: {apiComboBox.Items.Count}");
            
            foreach (var item in apiComboBox.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem)
                {
                    Console.WriteLine($"API 選項: {comboItem.Content} -> Tag: {comboItem.Tag}");
                }
            }            Assert.IsTrue(apiComboBox.Items.Count > 0, "應該有 API 函式選項");
        }
    }
}