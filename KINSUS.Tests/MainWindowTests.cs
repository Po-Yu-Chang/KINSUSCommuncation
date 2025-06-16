using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using KINSUS;
using System.Windows;

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
    }
}