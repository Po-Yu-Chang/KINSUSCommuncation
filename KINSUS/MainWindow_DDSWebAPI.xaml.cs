using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DDSWebAPI.Services;
using DDSWebAPI.Models;
using Newtonsoft.Json;
using System.IO;

namespace KINSUS
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯 - 使用 DDSWebAPI 函式庫版本
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region 私有欄位

        /// <summary>
        /// DDS Web API 服務實例
        /// </summary>
        private DDSWebAPIService _ddsService;

        /// <summary>
        /// 用戶端連接資訊集合
        /// </summary>
        private ObservableCollection<ClientConnection> _clientConnections;

        /// <summary>
        /// 日期時間顯示計時器
        /// </summary>
        private DispatcherTimer _dateTimeTimer;

        /// <summary>
        /// API 請求範本字典
        /// </summary>
        private Dictionary<string, string> _apiTemplates;

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

        #region 屬性

        /// <summary>
        /// 伺服器狀態
        /// </summary>
        public string ServerStatus
        {
            get => _ddsService?.IsServerRunning == true ? "執行中" : "已停止";
        }

        /// <summary>
        /// 用戶端連接數量
        /// </summary>
        public int ClientCount => _clientConnections?.Count ?? 0;

        #endregion

        #region 事件

        /// <summary>
        /// 屬性變更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region 建構子與初始化

        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化 DDS Web API 服務
            InitializeDDSService();
            
            // 初始化用戶端連接集合
            _clientConnections = new ObservableCollection<ClientConnection>();
            dgClients.ItemsSource = _clientConnections;
            
            // 初始化計時器
            InitializeTimers();
            
            // 初始化 API 請求範本
            InitializeApiTemplates();
            
            // 視窗關閉事件處理
            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// 初始化 DDS Web API 服務
        /// </summary>
        private void InitializeDDSService()
        {
            try
            {
                _ddsService = new DDSWebAPIService();
                
                // 註冊事件處理器
                _ddsService.MessageReceived += OnMessageReceived;
                _ddsService.ClientConnected += OnClientConnected;
                _ddsService.ClientDisconnected += OnClientDisconnected;
                _ddsService.ServerStatusChanged += OnServerStatusChanged;
                _ddsService.ApiCallSuccess += OnApiCallSuccess;
                _ddsService.ApiCallFailure += OnApiCallFailure;
                _ddsService.LogMessage += OnLogMessage;
                
                UpdateStatus("DDS Web API 服務已初始化");
            }
            catch (Exception ex)
            {
                UpdateStatus($"DDS Web API 服務初始化失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化計時器
        /// </summary>
        private void InitializeTimers()
        {
            // 日期時間顯示計時器
            _dateTimeTimer = new DispatcherTimer();
            _dateTimeTimer.Interval = TimeSpan.FromSeconds(1);
            _dateTimeTimer.Tick += (s, e) =>
            {
                // 假設有 lblDateTime 控件
                if (lblDateTime != null)
                {
                    lblDateTime.Content = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            };
            _dateTimeTimer.Start();
        }

        /// <summary>
        /// 初始化 API 請求範本
        /// </summary>
        private void InitializeApiTemplates()
        {
            _apiTemplates = new Dictionary<string, string>();
            
            try
            {
                // 從 Templates 目錄載入範本檔案
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ApiTemplates.json");
                
                if (File.Exists(templatePath))
                {
                    string json = File.ReadAllText(templatePath);
                    _apiTemplates = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
                else
                {
                    // 建立預設範本
                    CreateDefaultTemplates();
                }
                
                // 填充範本下拉選單
                PopulateTemplateComboBox();
            }
            catch (Exception ex)
            {
                UpdateStatus($"載入 API 範本失敗: {ex.Message}");
                CreateDefaultTemplates();
                PopulateTemplateComboBox();
            }
        }

        /// <summary>
        /// 建立預設範本
        /// </summary>
        private void CreateDefaultTemplates()
        {
            _apiTemplates = new Dictionary<string, string>
            {
                ["查詢設備狀態"] = JsonConvert.SerializeObject(new
                {
                    DeviceCode = "DEVICE001",
                    RequestType = "GetDeviceStatus"
                }, Formatting.Indented),
                
                ["建立工單"] = JsonConvert.SerializeObject(new
                {
                    DeviceCode = "DEVICE001",
                    RequestType = "CreateWorkOrder",
                    WorkOrderData = new
                    {
                        OrderId = "WO001",
                        PartNumber = "PART001",
                        Quantity = 100
                    }
                }, Formatting.Indented),
                
                ["結束工單"] = JsonConvert.SerializeObject(new
                {
                    DeviceCode = "DEVICE001",
                    RequestType = "CompleteWorkOrder",
                    OrderId = "WO001"
                }, Formatting.Indented)
            };
        }

        /// <summary>
        /// 填充範本下拉選單
        /// </summary>
        private void PopulateTemplateComboBox()
        {
            if (cmbTemplates != null)
            {
                cmbTemplates.Items.Clear();
                foreach (var template in _apiTemplates.Keys)
                {
                    cmbTemplates.Items.Add(new ComboBoxItem { Content = template });
                }
                
                if (cmbTemplates.Items.Count > 0)
                {
                    cmbTemplates.SelectedIndex = 0;
                }
            }
        }

        #endregion

        #region 視窗事件

        /// <summary>
        /// 視窗載入事件
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 載入預設設定
            LoadDefaultSettings();
            
            // 更新 UI 狀態
            UpdateUIByMode();
            
            // 更新狀態
            UpdateStatus("準備就緒，請設定操作模式並啟動所需功能");
        }

        /// <summary>
        /// 視窗關閉事件
        /// </summary>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                // 停止計時器
                _dateTimeTimer?.Stop();
                
                // 釋放 DDS 服務資源
                _ddsService?.Dispose();
                
                UpdateStatus("應用程式已關閉");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"關閉應用程式時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region DDS 服務事件處理

        /// <summary>
        /// 處理訊息接收事件
        /// </summary>
        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"收到訊息: {e.Message}");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] 收到訊息: {e.Message}");
            });
        }

        /// <summary>
        /// 處理用戶端連接事件
        /// </summary>
        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var connection = new ClientConnection
                {
                    Id = e.ClientId,
                    IpAddress = e.IpAddress,
                    ConnectTime = DateTime.Now,
                    LastActivityTime = DateTime.Now,
                    RequestType = "Connected"
                };
                
                _clientConnections.Add(connection);
                OnPropertyChanged(nameof(ClientCount));
                
                UpdateStatus($"用戶端已連接: {e.IpAddress}");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] 用戶端連接: {e.IpAddress}");
            });
        }

        /// <summary>
        /// 處理用戶端斷線事件
        /// </summary>
        private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var connection = _clientConnections.FirstOrDefault(c => c.Id == e.ClientId);
                if (connection != null)
                {
                    _clientConnections.Remove(connection);
                    OnPropertyChanged(nameof(ClientCount));
                }
                
                UpdateStatus($"用戶端已斷線: {e.ClientId}");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] 用戶端斷線: {e.ClientId}");
            });
        }

        /// <summary>
        /// 處理伺服器狀態變更事件
        /// </summary>
        private void OnServerStatusChanged(object sender, ServerStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(ServerStatus));
                UpdateStatus($"伺服器狀態: {(e.IsRunning ? "啟動" : "停止")}");
            });
        }

        /// <summary>
        /// 處理 API 呼叫成功事件
        /// </summary>
        private void OnApiCallSuccess(object sender, ApiCallSuccessEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog($"[{DateTime.Now:HH:mm:ss}] API 呼叫成功: {e.Endpoint}");
                AppendLog($"回應: {e.Response}");
            });
        }

        /// <summary>
        /// 處理 API 呼叫失敗事件
        /// </summary>
        private void OnApiCallFailure(object sender, ApiCallFailureEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog($"[{DateTime.Now:HH:mm:ss}] API 呼叫失敗: {e.Endpoint}");
                AppendLog($"錯誤: {e.Error}");
                UpdateStatus($"API 呼叫失敗: {e.Error}");
            });
        }

        /// <summary>
        /// 處理日誌訊息事件
        /// </summary>
        private void OnLogMessage(object sender, LogMessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog($"[{DateTime.Now:HH:mm:ss}] [{e.Level}] {e.Message}");
            });
        }

        #endregion

        #region 按鈕事件處理

        /// <summary>
        /// 啟動伺服器按鈕點擊事件
        /// </summary>
        private async void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string serverUrl = txtServerUrl.Text.Trim();
                
                if (string.IsNullOrEmpty(serverUrl))
                {
                    MessageBox.Show("請輸入伺服器 URL", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _ddsService.StartServerAsync(serverUrl);
                UpdateStatus("伺服器啟動中...");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"啟動伺服器失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 停止伺服器按鈕點擊事件
        /// </summary>
        private async void btnStopServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _ddsService.StopServerAsync();
                UpdateStatus("伺服器已停止");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止伺服器失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 發送 API 請求按鈕點擊事件
        /// </summary>
        private async void btnSendRequest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string endpoint = txtIotEndpoint.Text.Trim();
                string requestBody = txtRequestBody.Text.Trim();
                
                if (string.IsNullOrEmpty(endpoint))
                {
                    MessageBox.Show("請輸入 API 端點", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(requestBody))
                {
                    MessageBox.Show("請輸入請求內容", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _ddsService.SendApiRequestAsync(endpoint, requestBody);
                UpdateStatus("API 請求已發送");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"發送 API 請求失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清除日誌按鈕點擊事件
        /// </summary>
        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (txtLog != null)
            {
                txtLog.Clear();
            }
        }

        /// <summary>
        /// 開啟 API 指南視窗
        /// </summary>
        private void btnApiGuide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var apiGuideWindow = new ApiGuideWindow();
                apiGuideWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"開啟 API 指南視窗失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 開啟流程圖視窗
        /// </summary>
        private void btnFlowChart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var flowChartWindow = new FlowChartWindow();
                flowChartWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"開啟流程圖視窗失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 下拉選單事件處理

        /// <summary>
        /// 操作模式變更事件
        /// </summary>
        private void cmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMode.SelectedItem is ComboBoxItem selectedItem)
            {
                string mode = selectedItem.Content.ToString();
                
                switch (mode)
                {
                    case "雙向模式":
                        _currentMode = OperationMode.DualMode;
                        break;
                    case "伺服端模式":
                        _currentMode = OperationMode.ServerMode;
                        break;
                    case "用戶端模式":
                        _currentMode = OperationMode.ClientMode;
                        break;
                }
                
                UpdateUIByMode();
            }
        }

        /// <summary>
        /// API 範本選擇變更事件
        /// </summary>
        private void cmbTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTemplates.SelectedItem is ComboBoxItem selectedItem)
            {
                string templateName = selectedItem.Content.ToString();
                LoadApiTemplate(templateName);
            }
        }

        #endregion

        #region 輔助方法

        /// <summary>
        /// 載入預設設定
        /// </summary>
        private void LoadDefaultSettings()
        {
            // 載入伺服器位址設定
            if (txtServerUrl != null)
                txtServerUrl.Text = "http://localhost:8085/";
            
            if (txtIotEndpoint != null)
                txtIotEndpoint.Text = "http://localhost:8085/";
            
            if (txtDevCode != null)
                txtDevCode.Text = "DEVICE001";
            
            // 設定下拉式選單選項
            if (cmbMode != null)
                cmbMode.SelectedIndex = 0;
            
            if (cmbApiFunction != null)
                cmbApiFunction.SelectedIndex = 0;
        }

        /// <summary>
        /// 根據操作模式更新 UI
        /// </summary>
        private void UpdateUIByMode()
        {
            // 根據不同模式啟用/停用相關控件
            switch (_currentMode)
            {
                case OperationMode.DualMode:
                    // 雙向模式：啟用所有功能
                    EnableServerControls(true);
                    EnableClientControls(true);
                    break;
                
                case OperationMode.ServerMode:
                    // 伺服端模式：只啟用伺服器功能
                    EnableServerControls(true);
                    EnableClientControls(false);
                    break;
                
                case OperationMode.ClientMode:
                    // 用戶端模式：只啟用用戶端功能
                    EnableServerControls(false);
                    EnableClientControls(true);
                    break;
            }
        }

        /// <summary>
        /// 啟用/停用伺服器控件
        /// </summary>
        private void EnableServerControls(bool enabled)
        {
            if (btnStartServer != null) btnStartServer.IsEnabled = enabled;
            if (btnStopServer != null) btnStopServer.IsEnabled = enabled;
            if (txtServerUrl != null) txtServerUrl.IsEnabled = enabled;
        }

        /// <summary>
        /// 啟用/停用用戶端控件
        /// </summary>
        private void EnableClientControls(bool enabled)
        {
            if (btnSendRequest != null) btnSendRequest.IsEnabled = enabled;
            if (txtIotEndpoint != null) txtIotEndpoint.IsEnabled = enabled;
            if (txtRequestBody != null) txtRequestBody.IsEnabled = enabled;
            if (cmbTemplates != null) cmbTemplates.IsEnabled = enabled;
        }

        /// <summary>
        /// 載入 API 範本
        /// </summary>
        private void LoadApiTemplate(string templateName)
        {
            if (_apiTemplates.ContainsKey(templateName))
            {
                if (txtRequestBody != null)
                {
                    txtRequestBody.Text = _apiTemplates[templateName];
                }
            }
        }

        /// <summary>
        /// 更新狀態列
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (lblStatus != null)
            {
                lblStatus.Content = message;
            }
        }

        /// <summary>
        /// 附加日誌訊息
        /// </summary>
        private void AppendLog(string message)
        {
            if (txtLog != null)
            {
                txtLog.AppendText(message + Environment.NewLine);
                txtLog.ScrollToEnd();
            }
        }

        /// <summary>
        /// 觸發屬性變更通知
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
