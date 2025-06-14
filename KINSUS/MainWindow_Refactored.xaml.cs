using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DDSWebAPI.Services;
using DDSWebAPI.Models;
using Newtonsoft.Json;

namespace KINSUS
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯 - 重構版本
    /// 使用 DDSWebAPI 函式庫來處理所有通訊邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 私有欄位

        /// <summary>
        /// DDS Web API 服務實例
        /// </summary>
        private DDSWebAPIService _ddsApiService;

        /// <summary>
        /// 日期時間顯示計時器
        /// </summary>
        private DispatcherTimer _dateTimeTimer;

        /// <summary>
        /// 操作模式列舉
        /// </summary>
        private enum OperationMode
        {
            DualMode,    // 雙向模式
            ServerMode,  // 伺服端模式
            ClientMode   // 用戶端模式
        }

        /// <summary>
        /// 當前操作模式
        /// </summary>
        private OperationMode _currentMode = OperationMode.DualMode;

        #endregion

        #region 建構函式

        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化 DDS API 服務
            InitializeDDSAPIService();
            
            // 初始化計時器
            InitializeTimers();
            
            // 視窗關閉事件處理
            this.Closed += MainWindow_Closed;
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化 DDS API 服務
        /// </summary>
        private void InitializeDDSAPIService()
        {
            try
            {
                // 建立 DDS API 服務實例
                _ddsApiService = new DDSWebAPIService();
                
                // 設定基本配置
                _ddsApiService.ServerUrl = "http://localhost:8085/";
                _ddsApiService.RemoteApiUrl = "http://localhost:8086/";
                _ddsApiService.DeviceCode = "KINSUS001";
                _ddsApiService.OperatorName = "OP001";

                // 註冊事件處理程式
                RegisterDDSAPIEvents();

                // 綁定用戶端連接列表到 DataGrid
                dgClients.ItemsSource = _ddsApiService.ClientConnections;
                
                UpdateStatus("DDS API 服務初始化完成");
            }
            catch (Exception ex)
            {
                UpdateStatus($"初始化 DDS API 服務失敗: {ex.Message}");
                MessageBox.Show($"初始化 DDS API 服務失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 註冊 DDS API 事件處理程式
        /// </summary>
        private void RegisterDDSAPIEvents()
        {
            // 訊息接收事件
            _ddsApiService.MessageReceived += OnDDSAPIMessageReceived;
            
            // 用戶端連接事件
            _ddsApiService.ClientConnected += OnDDSAPIClientConnected;
            _ddsApiService.ClientDisconnected += OnDDSAPIClientDisconnected;
            
            // 伺服器狀態變更事件
            _ddsApiService.ServerStatusChanged += OnDDSAPIServerStatusChanged;
            
            // API 呼叫事件
            _ddsApiService.ApiCallSuccess += OnDDSAPICallSuccess;
            _ddsApiService.ApiCallFailure += OnDDSAPICallFailure;
            
            // 日誌訊息事件
            _ddsApiService.LogMessage += OnDDSAPILogMessage;
        }

        /// <summary>
        /// 初始化計時器
        /// </summary>
        private void InitializeTimers()
        {
            // 日期時間顯示計時器
            _dateTimeTimer = new DispatcherTimer();
            _dateTimeTimer.Interval = TimeSpan.FromSeconds(1);
            _dateTimeTimer.Tick += DateTimeTimer_Tick;
            _dateTimeTimer.Start();
        }

        #endregion

        #region 視窗事件處理

        /// <summary>
        /// 視窗載入事件
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 載入預設設定
            LoadDefaultSettings();
            
            // 更新 UI 狀態
            UpdateUIByMode();
            
            UpdateStatus("系統準備就緒，請選擇操作模式並啟動所需功能");
        }

        /// <summary>
        /// 視窗關閉事件
        /// </summary>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                // 停止 DDS API 服務
                _ddsApiService?.StopServer();
                
                // 釋放資源
                _dateTimeTimer?.Stop();
                _ddsApiService?.Dispose();
                
                UpdateStatus("系統已關閉");
            }
            catch (Exception ex)
            {
                // 記錄關閉時的錯誤，但不顯示訊息框
                System.Diagnostics.Debug.WriteLine($"關閉應用程式時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region UI 事件處理

        /// <summary>
        /// 連接按鈕點擊事件
        /// </summary>
        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 更新服務配置
                UpdateServiceConfiguration();
                
                // 啟動伺服器
                bool success = await _ddsApiService.StartServerAsync();
                
                if (success)
                {
                    // 更新 UI 狀態
                    rectStatus.Fill = Brushes.Green;
                    rectServerStatus.Fill = Brushes.Green;
                    btnConnect.IsEnabled = false;
                    btnDisconnect.IsEnabled = true;
                    txtServerUrl.IsEnabled = false;
                    
                    UpdateStatus($"伺服器已啟動，監聽位址: {_ddsApiService.ServerUrl}");
                    
                    // 清除伺服器訊息區
                    txtServerMessages.Clear();
                    AppendServerLog("===== 伺服器已啟動 =====");
                    AppendServerLog($"監聽位址: {_ddsApiService.ServerUrl}");
                    AppendServerLog("等待連接中...");
                    AppendServerLog("");
                }
                else
                {
                    // 更新 UI 狀態為錯誤
                    rectStatus.Fill = Brushes.Red;
                    rectServerStatus.Fill = Brushes.Red;
                    UpdateStatus("伺服器啟動失敗");
                }
            }
            catch (Exception ex)
            {
                rectStatus.Fill = Brushes.Red;
                rectServerStatus.Fill = Brushes.Red;
                UpdateStatus($"啟動伺服器時發生錯誤: {ex.Message}");
                MessageBox.Show($"啟動伺服器失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 中斷連接按鈕點擊事件
        /// </summary>
        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 停止伺服器
                _ddsApiService.StopServer();
                
                // 更新 UI 狀態
                rectStatus.Fill = Brushes.Gray;
                rectServerStatus.Fill = Brushes.Gray;
                btnConnect.IsEnabled = true;
                btnDisconnect.IsEnabled = false;
                txtServerUrl.IsEnabled = true;
                
                UpdateStatus("伺服器已停止");
                AppendServerLog("===== 伺服器已停止 =====");
            }
            catch (Exception ex)
            {
                UpdateStatus($"停止伺服器時發生錯誤: {ex.Message}");
                MessageBox.Show($"停止伺服器失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 發送測試按鈕點擊事件
        /// </summary>
        private async void btnSendTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 更新服務配置
                UpdateServiceConfiguration();
                
                // 建立測試資料
                var testData = CreateTestData();
                
                // 發送測試請求
                await SendTestRequest(testData);
            }
            catch (Exception ex)
            {
                UpdateStatus($"發送測試請求時發生錯誤: {ex.Message}");
                AppendClientLog($"錯誤: {ex.Message}");
                MessageBox.Show($"發送測試請求失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清除訊息按鈕點擊事件
        /// </summary>
        private void btnClearMessages_Click(object sender, RoutedEventArgs e)
        {
            txtServerMessages.Clear();
            txtClientMessages.Clear();
            UpdateStatus("訊息記錄已清除");
        }

        /// <summary>
        /// 模式選擇變更事件
        /// </summary>
        private void cmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMode.SelectedIndex >= 0)
            {
                _currentMode = (OperationMode)cmbMode.SelectedIndex;
                UpdateUIByMode();
            }
        }

        /// <summary>
        /// API 指南按鈕點擊事件
        /// </summary>
        private void btnApiGuide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApiGuideWindow apiGuideWindow = new ApiGuideWindow();
                apiGuideWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"開啟 API 指南視窗失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region DDS API 事件處理

        /// <summary>
        /// DDS API 訊息接收事件處理
        /// </summary>
        private void OnDDSAPIMessageReceived(object sender, MessageEventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                AppendServerLog($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] 收到來自 {e.ClientIp} 的訊息:");
                AppendServerLog($"{e.Message}");
                AppendServerLog(string.Empty); // 空行分隔
            });
        }

        /// <summary>
        /// DDS API 用戶端連接事件處理
        /// </summary>
        private void OnDDSAPIClientConnected(object sender, ClientConnectedEventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                AppendServerLog($"[{e.ConnectTime:yyyy-MM-dd HH:mm:ss}] 新用戶端連接:");
                AppendServerLog($"ID: {e.ClientId}");
                AppendServerLog($"IP: {e.ClientIp}");
                AppendServerLog(string.Empty); // 空行分隔
            });
        }

        /// <summary>
        /// DDS API 用戶端斷線事件處理
        /// </summary>
        private void OnDDSAPIClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                AppendServerLog($"[{e.DisconnectTime:yyyy-MM-dd HH:mm:ss}] 用戶端斷線:");
                AppendServerLog($"ID: {e.ClientId}");
                AppendServerLog($"IP: {e.ClientIp}");
                if (!string.IsNullOrEmpty(e.Reason))
                {
                    AppendServerLog($"原因: {e.Reason}");
                }
                AppendServerLog(string.Empty); // 空行分隔
            });
        }

        /// <summary>
        /// DDS API 伺服器狀態變更事件處理
        /// </summary>
        private void OnDDSAPIServerStatusChanged(object sender, ServerStatusChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                AppendServerLog($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] 伺服器狀態: {e.Status}");
                AppendServerLog($"描述: {e.Description}");
                AppendServerLog(string.Empty);
                
                // 根據狀態更新 UI
                UpdateServerStatusUI(e.Status);
            });
        }

        /// <summary>
        /// DDS API 呼叫成功事件處理
        /// </summary>
        private void OnDDSAPICallSuccess(object sender, ApiCallSuccessEventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                AppendClientLog($"[{e.Result.RequestTime:yyyy-MM-dd HH:mm:ss}] API 呼叫成功:");
                AppendClientLog($"URL: {e.Result.RequestUrl}");
                AppendClientLog($"狀態碼: {e.Result.StatusCode}");
                AppendClientLog($"處理時間: {e.Result.ProcessingTimeMs:F2}ms");
                AppendClientLog($"回應: {e.Result.ResponseBody}");
                AppendClientLog(string.Empty);
            });
        }

        /// <summary>
        /// DDS API 呼叫失敗事件處理
        /// </summary>
        private void OnDDSAPICallFailure(object sender, ApiCallFailureEventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                AppendClientLog($"[{e.Result.RequestTime:yyyy-MM-dd HH:mm:ss}] API 呼叫失敗:");
                AppendClientLog($"URL: {e.Result.RequestUrl}");
                AppendClientLog($"錯誤: {e.Result.ErrorMessage}");
                if (e.Result.Exception != null)
                {
                    AppendClientLog($"異常: {e.Result.Exception.Message}");
                }
                AppendClientLog(string.Empty);
            });
        }

        /// <summary>
        /// DDS API 日誌訊息事件處理
        /// </summary>
        private void OnDDSAPILogMessage(object sender, LogMessageEventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                string levelText = e.Level.ToString().ToUpper();
                AppendServerLog($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{levelText}] {e.Message}");
            });
        }

        #endregion

        #region 輔助方法

        /// <summary>
        /// 載入預設設定
        /// </summary>
        private void LoadDefaultSettings()
        {
            txtServerUrl.Text = _ddsApiService.ServerUrl;
            txtIotEndpoint.Text = _ddsApiService.RemoteApiUrl;
            txtDevCode.Text = _ddsApiService.DeviceCode;
            
            // 設定下拉式選單選項
            cmbMode.SelectedIndex = 0;
            if (cmbApiFunction != null) cmbApiFunction.SelectedIndex = 0;
        }

        /// <summary>
        /// 更新服務配置
        /// </summary>
        private void UpdateServiceConfiguration()
        {
            _ddsApiService.ServerUrl = txtServerUrl.Text;
            _ddsApiService.RemoteApiUrl = txtIotEndpoint.Text;
            _ddsApiService.DeviceCode = txtDevCode.Text;
        }

        /// <summary>
        /// 根據模式更新 UI
        /// </summary>
        private void UpdateUIByMode()
        {
            // 根據不同模式顯示/隱藏相關控制項
            switch (_currentMode)
            {
                case OperationMode.DualMode:
                    // 雙向模式：顯示所有功能
                    break;
                case OperationMode.ServerMode:
                    // 伺服端模式：主要顯示接收功能
                    break;
                case OperationMode.ClientMode:
                    // 用戶端模式：主要顯示發送功能
                    break;
            }
        }

        /// <summary>
        /// 更新伺服器狀態 UI
        /// </summary>
        private void UpdateServerStatusUI(ServerStatus status)
        {
            switch (status)
            {
                case ServerStatus.Running:
                    rectStatus.Fill = Brushes.Green;
                    rectServerStatus.Fill = Brushes.Green;
                    break;
                case ServerStatus.Stopped:
                    rectStatus.Fill = Brushes.Gray;
                    rectServerStatus.Fill = Brushes.Gray;
                    break;
                case ServerStatus.Error:
                    rectStatus.Fill = Brushes.Red;
                    rectServerStatus.Fill = Brushes.Red;
                    break;
                default:
                    rectStatus.Fill = Brushes.Yellow;
                    rectServerStatus.Fill = Brushes.Yellow;
                    break;
            }
        }

        /// <summary>
        /// 建立測試資料
        /// </summary>
        private object CreateTestData()
        {
            // 根據選擇的 API 功能建立對應的測試資料
            if (cmbApiFunction?.SelectedIndex >= 0)
            {
                switch (cmbApiFunction.SelectedIndex)
                {
                    case 0: // 配針回報
                        return new ToolOutputReportData
                        {
                            WorkOrder = "WO001",
                            ToolId = "TOOL001",
                            Result = "success",
                            ProcessTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            Quantity = 100
                        };
                    
                    case 1: // 錯誤回報
                        return new ErrorReportData
                        {
                            ErrorCode = "E001",
                            ErrorMessage = "測試錯誤訊息",
                            ErrorLevel = "low",
                            OccurTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            WorkOrder = "WO001"
                        };
                    
                    case 2: // 機臺狀態
                        return new MachineStatusReportData
                        {
                            Status = "running",
                            CpuUsage = 45.2f,
                            MemoryUsage = 62.8f,
                            DiskUsage = 78.5f,
                            Temperature = 35.6f,
                            ReportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")  
                        };
                }
            }

            // 預設回傳機臺狀態資料
            return new MachineStatusReportData
            {
                Status = "idle",
                CpuUsage = 12.5f,
                MemoryUsage = 45.2f,
                DiskUsage = 65.8f,
                Temperature = 32.1f,
                ReportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        /// <summary>
        /// 發送測試請求
        /// </summary>
        private async Task SendTestRequest(object testData)
        {
            AppendClientLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 開始發送測試請求...");
            AppendClientLog($"資料類型: {testData.GetType().Name}");
            AppendClientLog($"資料內容: {JsonConvert.SerializeObject(testData, Formatting.Indented)}");
            AppendClientLog("");

            ApiCallResult result = null;

            // 根據資料類型發送對應的請求
            switch (testData)
            {
                case ToolOutputReportData toolData:
                    result = await _ddsApiService.SendToolOutputReportAsync(toolData);
                    break;
                
                case ErrorReportData errorData:
                    result = await _ddsApiService.SendErrorReportAsync(errorData);
                    break;
                
                case MachineStatusReportData statusData:
                    result = await _ddsApiService.SendMachineStatusReportAsync(statusData);
                    break;
            }

            if (result != null && result.IsSuccess)
            {
                UpdateStatus("測試請求發送成功");
            }
            else
            {
                UpdateStatus($"測試請求發送失敗: {result?.ErrorMessage}");
            }
        }

        /// <summary>
        /// 更新狀態列文字
        /// </summary>
        private void UpdateStatus(string status)
        {
            if (txtStatus != null)
            {
                txtStatus.Text = status;
            }
        }

        /// <summary>
        /// 將日誌添加到伺服端記錄區
        /// </summary>
        private void AppendServerLog(string message)
        {
            if (txtServerMessages != null)
            {
                txtServerMessages.AppendText(message + Environment.NewLine);
                txtServerMessages.ScrollToEnd();
            }
        }
        
        /// <summary>
        /// 將日誌添加到用戶端記錄區
        /// </summary>
        private void AppendClientLog(string message)
        {
            if (txtClientMessages != null)
            {
                txtClientMessages.AppendText(message + Environment.NewLine);
                txtClientMessages.ScrollToEnd();
            }
        }

        /// <summary>
        /// 日期時間計時器 Tick 事件
        /// </summary>
        private void DateTimeTimer_Tick(object sender, EventArgs e)
        {
            // 更新日期時間顯示（如果有相關 UI 控制項）
            // 這裡可以添加日期時間更新的邏輯
        }

        #endregion
    }
}
