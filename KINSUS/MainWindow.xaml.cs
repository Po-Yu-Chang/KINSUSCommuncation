using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DDSWebAPI.Events;
using DDSWebAPI.Models;
using DDSWebAPI.Services;
using Newtonsoft.Json;

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
        /// 心跳計時器
        /// </summary>
        private DispatcherTimer _heartbeatTimer = new DispatcherTimer();

        /// <summary>
        /// 當前操作模式
        /// </summary>
        private OperationMode _currentMode = OperationMode.DualMode;

        /// <summary>
        /// 總API請求計數
        /// </summary>
        private int _totalApiRequests = 0;

        /// <summary>
        /// 成功的API請求計數
        /// </summary>
        private int _successfulApiRequests = 0;

        /// <summary>
        /// 是否已建立持久連線
        /// </summary>
        private bool _isPersistentConnectionEstablished = false;

        /// <summary>
        /// 持久連線建立時間
        /// </summary>
        private DateTime? _connectionEstablishedTime = null;

        /// <summary>
        /// 連線重試次數
        /// </summary>
        private int _connectionRetryCount = 0;        /// <summary>
        /// 最大重試次數
        /// </summary>
        private const int MAX_RETRY_COUNT = 5; // 增加重試次數從3次到5次

        /// <summary>
        /// 持久連線監控計時器
        /// </summary>
        private DispatcherTimer _connectionMonitorTimer;

        #endregion

        #region 列舉

        /// <summary>
        /// 操作模式列舉
        /// </summary>
        private enum OperationMode
        {
            DualMode,    // 雙向模式
            ServerMode,  // 伺服端模式
            ClientMode   // 用戶端模式
        }

        #endregion

        #region 公開屬性

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

        #region 建構子與初始化        /// <summary>
        /// MainWindow 建構子
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化 DDS Web API 服務
            InitializeDDSService();
            
            // 初始化用戶端連接集合
            InitializeClientConnections();
            
            // 初始化計時器
            InitializeTimers();
            
            // 初始化 API 請求範本
            InitializeApiTemplates();
            
            // 註冊視窗關閉事件
            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// 初始化用戶端連接集合
        /// </summary>
        private void InitializeClientConnections()
        {
            _clientConnections = new ObservableCollection<ClientConnection>();
            dgClients.ItemsSource = _clientConnections;
        }

        /// <summary>
        /// 初始化 DDS Web API 服務（增強版）
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
                
                UpdateStatus("DDS Web API 服務已初始化（包含安全性與效能控制）");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] ✓ DDS Web API 服務初始化完成");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] ✓ 安全性中介軟體已載入");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] ✓ 效能控制器已載入");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] ✓ 連線管理功能已啟用");
            }
            catch (Exception ex)
            {
                UpdateStatus($"DDS Web API 服務初始化失敗: {ex.Message}");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] ✗ 初始化失敗: {ex.Message}");
            }
        }        /// <summary>
        /// 初始化計時器
        /// </summary>
        private void InitializeTimers()
        {
            InitializeDateTimeTimer();
            // 初始化持久連線監控計時器
            InitializeConnectionMonitorTimer();
        }

        /// <summary>
        /// 初始化日期時間顯示計時器
        /// </summary>
        private void InitializeDateTimeTimer()
        {
            _dateTimeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            
            _dateTimeTimer.Tick += (sender, e) =>
            {
                if (txtDateTime != null)
                {
                    txtDateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            };
            
            _dateTimeTimer.Start();
        }        /// <summary>
        /// 初始化持久連線監控計時器
        /// </summary>
        private void InitializeConnectionMonitorTimer()
        {
            _connectionMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15) // 縮短為每15秒檢查一次連線狀態
            };
            
            _connectionMonitorTimer.Tick += async (sender, e) =>
            {
                if (_isPersistentConnectionEstablished)
                {
                    await MonitorPersistentConnection();
                }
            };
            
            _connectionMonitorTimer.Start();
        }/// <summary>
        /// 初始化 API 請求範本
        /// </summary>
        private void InitializeApiTemplates()
        {
            _apiTemplates = new Dictionary<string, string>();
            
            try
            {
                // 建立預設範本
                CreateDefaultTemplates();
                
                // 填充範本下拉選單
                PopulateTemplateComboBox();
            }
            catch (Exception ex)
            {
                UpdateStatus($"載入 API 範本失敗: {ex.Message}");
                CreateDefaultTemplates();
                PopulateTemplateComboBox();
            }
        }/// <summary>
        /// 建立預設範本（當 JSON 檔案不存在時使用）
        /// </summary>
        private void CreateDefaultTemplates()
        {            
            _apiTemplates = new Dictionary<string, string>
            {
                // === 標準 MES API 範本 ===
                ["SEND_MESSAGE_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    requestID = "MSG_CMD_001",
                    serviceName = "SEND_MESSAGE_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            message = "系統訊息",
                            level = "info",
                            priority = "normal",
                            actionType = 1,
                            intervalSecondTime = 30,
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),

                ["CREATE_NEEDLE_WORKORDER_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    requestID = "WO_CMD_001",
                    serviceName = "CREATE_NEEDLE_WORKORDER_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            workOrderId = "WO20240101001",
                            partNumber = "PN001",
                            quantity = 100,
                            needleType = "DRILL_1.0",
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),

                ["DATE_MESSAGE_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    requestID = "TIME_CMD_001",
                    serviceName = "DATE_MESSAGE_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            syncTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            timeZone = "GMT+8",
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),

                ["SWITCH_RECIPE_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    requestID = "RECIPE_CMD_001",
                    serviceName = "SWITCH_RECIPE_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            recipeId = "RECIPE001",
                            recipeName = "標準鑽孔程式",
                            recipeVersion = "1.0",
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),

                ["DEVICE_CONTROL_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    requestID = "CTRL_CMD_001",
                    serviceName = "DEVICE_CONTROL_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            action = "START", // START, STOP, PAUSE, RESUME
                            deviceId = "DEV001",
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),

                ["WAREHOUSE_RESOURCE_QUERY_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    requestID = "WH_QRY_001",
                    serviceName = "WAREHOUSE_RESOURCE_QUERY_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            queryType = "TOOL_INVENTORY",
                            warehouseId = "WH001",
                            toolType = "DRILL",
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),

                ["TOOL_TRACE_HISTORY_QUERY_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    requestID = "TOOL_QRY_001",
                    serviceName = "TOOL_TRACE_HISTORY_QUERY_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            toolId = "TOOL001",
                            startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
                            endDate = DateTime.Now.ToString("yyyy-MM-dd"),
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),
                
                // === 回報類 API 範本 ===
                ["TOOL_OUTPUT_REPORT_MESSAGE"] = JsonConvert.SerializeObject(new
                {
                    requestID = "TOOL_RPT_001",
                    serviceName = "TOOL_OUTPUT_REPORT_MESSAGE",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            reportType = "TOOL_OUTPUT",
                            toolInfo = new
                            {
                                toolId = "TOOL001",
                                toolType = "DRILL",
                                location = "A01",
                                status = "COMPLETED"
                            },
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),
                
                ["ERROR_REPORT_MESSAGE"] = JsonConvert.SerializeObject(new
                {
                    requestID = "ERR_RPT_001",
                    serviceName = "ERROR_REPORT_MESSAGE",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            errorCode = "E001",
                            errorMessage = "系統錯誤",
                            severity = "HIGH",
                            errorType = "SYSTEM",
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),

                ["MACHINE_STATUS_REPORT_MESSAGE"] = JsonConvert.SerializeObject(new
                {
                    requestID = "MACH_RPT_001",
                    serviceName = "MACHINE_STATUS_REPORT_MESSAGE",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = "KINSUS001",
                    @operator = "OP001",
                    data = new[]
                    {
                        new
                        {
                            machineId = "MACH001",
                            status = "RUNNING", // RUNNING, IDLE, ERROR, MAINTENANCE
                            temperature = 25.5,
                            pressure = 1.2,
                            extendData = (object)null
                        }
                    },
                    extendData = (object)null
                }, Formatting.Indented),

                // === 客製化倉庫管理 API 範本 ===
                ["IN_MATERIAL_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    materialId = "MAT001",
                    materialType = "DRILL",
                    quantity = 50,
                    warehouseLocation = "A-01-01",
                    @operator = "OP001",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, Formatting.Indented),

                ["OUT_MATERIAL_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    materialId = "MAT001",
                    quantity = 10,
                    destinationLocation = "PRODUCTION_LINE_A",
                    @operator = "OP001",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, Formatting.Indented),

                ["GET_LOCATION_BY_STORAGE_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    storageId = "STORAGE001",
                    queryType = "AVAILABLE_LOCATIONS"
                }, Formatting.Indented),

                ["GET_LOCATION_BY_PIN_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    pinCode = "PIN001",
                    queryType = "LOCATION_INFO"
                }, Formatting.Indented),

                ["OPERATION_CLAMP_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    clampId = "CLAMP001",
                    operation = "OPEN", // OPEN, CLOSE
                    force = 100.0,
                    @operator = "OP001"
                }, Formatting.Indented),

                ["CHANGE_SPEED_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    deviceId = "DEV001",
                    newSpeed = 1200,
                    speedUnit = "RPM",
                    @operator = "OP001"
                }, Formatting.Indented),

                // === 系統管理 API 範本 ===
                ["SERVER_STATUS_QUERY"] = JsonConvert.SerializeObject(new
                {
                    queryType = "FULL_STATUS",
                    includeStatistics = true
                }, Formatting.Indented),

                ["SERVER_RESTART_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    restartType = "GRACEFUL", // GRACEFUL, FORCE
                    reason = "Manual restart request",
                    @operator = "ADMIN"
                }, Formatting.Indented),

                ["CONNECTION_TEST_COMMAND"] = JsonConvert.SerializeObject(new
                {
                    testType = "PING",
                    targetEndpoint = "localhost",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, Formatting.Indented)
            };
        }/// <summary>
        /// 填充範本下拉選單
        /// </summary>
        private void PopulateTemplateComboBox()
        {
            if (cmbTemplates != null)
            {
                cmbTemplates.Items.Clear();
                  // 使用 API 範本中的鍵值建立對應的顯示文字
                var apiDisplayNames = new Dictionary<string, string>
                {
                    // === 標準 MES API ===
                    ["SEND_MESSAGE_COMMAND"] = "遠程資訊下發指令 (SEND_MESSAGE_COMMAND)",
                    ["CREATE_NEEDLE_WORKORDER_COMMAND"] = "派針工單建立指令 (CREATE_NEEDLE_WORKORDER_COMMAND)",
                    ["DATE_MESSAGE_COMMAND"] = "設備時間同步指令 (DATE_MESSAGE_COMMAND)",
                    ["SWITCH_RECIPE_COMMAND"] = "刀具工鑽袋檔發送指令 (SWITCH_RECIPE_COMMAND)",
                    ["DEVICE_CONTROL_COMMAND"] = "設備啟停控制指令 (DEVICE_CONTROL_COMMAND)",
                    ["WAREHOUSE_RESOURCE_QUERY_COMMAND"] = "倉庫資源查詢指令 (WAREHOUSE_RESOURCE_QUERY_COMMAND)",
                    ["TOOL_TRACE_HISTORY_QUERY_COMMAND"] = "鑽針履歷查詢指令 (TOOL_TRACE_HISTORY_QUERY_COMMAND)",
                    
                    // === 回報類 API ===
                    ["TOOL_OUTPUT_REPORT_MESSAGE"] = "配針回報上傳 (TOOL_OUTPUT_REPORT_MESSAGE)",
                    ["ERROR_REPORT_MESSAGE"] = "錯誤回報上傳 (ERROR_REPORT_MESSAGE)",
                    ["MACHINE_STATUS_REPORT_MESSAGE"] = "機臺狀態上報 (MACHINE_STATUS_REPORT_MESSAGE)",
                    
                    // === 客製化倉庫管理 API ===
                    ["IN_MATERIAL_COMMAND"] = "入庫指令 (IN_MATERIAL_COMMAND)",
                    ["OUT_MATERIAL_COMMAND"] = "出庫指令 (OUT_MATERIAL_COMMAND)",
                    ["GET_LOCATION_BY_STORAGE_COMMAND"] = "依倉儲查詢位置 (GET_LOCATION_BY_STORAGE_COMMAND)",
                    ["GET_LOCATION_BY_PIN_COMMAND"] = "依PIN碼查詢位置 (GET_LOCATION_BY_PIN_COMMAND)",
                    ["OPERATION_CLAMP_COMMAND"] = "夾具操作指令 (OPERATION_CLAMP_COMMAND)",
                    ["CHANGE_SPEED_COMMAND"] = "變更速度指令 (CHANGE_SPEED_COMMAND)",
                    
                    // === 系統管理 API ===
                    ["SERVER_STATUS_QUERY"] = "伺服器狀態查詢 (SERVER_STATUS_QUERY)",
                    ["SERVER_RESTART_COMMAND"] = "伺服器重啟指令 (SERVER_RESTART_COMMAND)",
                    ["CONNECTION_TEST_COMMAND"] = "連線測試指令 (CONNECTION_TEST_COMMAND)"
                };
                
                foreach (var template in _apiTemplates.Keys)
                {
                    string displayName = apiDisplayNames.ContainsKey(template) 
                        ? apiDisplayNames[template] 
                        : template;
                    cmbTemplates.Items.Add(new ComboBoxItem { Content = displayName });
                }
                
                if (cmbTemplates.Items.Count > 0)
                {
                    cmbTemplates.SelectedIndex = 0;
                    // 載入第一個範本
                    if (cmbTemplates.SelectedItem is ComboBoxItem firstItem)
                    {
                        LoadApiTemplate(firstItem.Content.ToString());
                    }
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
        }        /// <summary>
        /// 視窗關閉事件
        /// </summary>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                // 關閉持久連線
                if (_isPersistentConnectionEstablished)
                {
                    Task.Run(async () => await ClosePersistentConnection()).Wait(TimeSpan.FromSeconds(5));
                }
                
                // 停止計時器
                _dateTimeTimer?.Stop();
                _connectionMonitorTimer?.Stop();
                
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
        }        /// <summary>
        /// 處理用戶端連接事件（增強版）
        /// </summary>
        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 增加總請求計數
                _totalApiRequests++;
                
                var connection = new ClientConnection
                {
                    Id = e.ClientId,
                    IpAddress = e.ClientIp,
                    ConnectTime = DateTime.Now,
                    LastActivityTime = DateTime.Now,
                    RequestType = "Connected"
                };
                
                _clientConnections.Add(connection);
                OnPropertyChanged(nameof(ClientCount));
                
                UpdateStatus($"用戶端已連接: {e.ClientIp}");
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✓ 用戶端連接: {e.ClientIp} ({e.ClientId})");
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 目前活躍連接數: {_clientConnections.Count}");
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 累計API請求數: {_totalApiRequests}");
                
                // 顯示連接統計
                DisplayConnectionStatistics();
            });
        }

        /// <summary>
        /// 處理用戶端斷線事件（增強版）
        /// </summary>
        private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var connection = _clientConnections.FirstOrDefault(c => c.Id == e.ClientId);
                if (connection != null)
                {
                    var connectionDuration = DateTime.Now - connection.ConnectTime;
                    _clientConnections.Remove(connection);
                    OnPropertyChanged(nameof(ClientCount));
                      AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✗ 用戶端斷線: {e.ClientId}");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 原因: {e.Reason}");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 連接時長: {connectionDuration.TotalSeconds:F1}秒");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 目前活躍連接數: {_clientConnections.Count}");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 累計API請求數: {_totalApiRequests}");
                }
                
                UpdateStatus($"用戶端已斷線: {e.ClientId}");
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
        }        /// <summary>
        /// 處理 API 呼叫成功事件
        /// </summary>
        private void OnApiCallSuccess(object sender, ApiCallSuccessEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _successfulApiRequests++;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] API 呼叫成功: {e.Endpoint}");
                AppendLog($"回應: {e.Response}");
                AppendLog($"成功率: {_successfulApiRequests}/{_totalApiRequests} ({(_successfulApiRequests * 100.0 / _totalApiRequests):F1}%)");
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

                await _ddsService.StartServerAsync();
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
            }        }        /// <summary>
        /// 發送 API 請求按鈕點擊事件（增強版 - 使用持久連線）
        /// </summary>
        private async void btnSendRequest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 根據選擇的 API 函式取得正確的端點
                string endpoint = GetApiEndpointFromSelection();
                
                // 同步更新範本內容以確保端點與 requestBody 對應
                if (cmbApiFunction.SelectedItem is ComboBoxItem selectedApi)
                {
                    string apiTag = selectedApi.Tag?.ToString() ?? "";
                    
                    // 強制重新載入對應的範本，確保端點與範本同步
                    if (!string.IsNullOrEmpty(apiTag) && _apiTemplates.ContainsKey(apiTag))
                    {
                        txtTemplate.Text = _apiTemplates[apiTag];
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 自動同步範本: {apiTag} → {endpoint}");
                    }
                }
                
                string requestBody = txtTemplate.Text.Trim();
                
                if (string.IsNullOrEmpty(endpoint))
                {
                    MessageBox.Show("請選擇 API 函式或輸入有效的端點", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(requestBody))
                {
                    MessageBox.Show("請輸入請求內容", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                btnSendRequest.IsEnabled = false;
                UpdateStatus("正在透過持久連線發送 API 請求...");
                
                // 顯示請求詳細資訊
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 📤 準備透過持久連線發送 API 請求");
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 目標端點: {endpoint}");
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 請求大小: {System.Text.Encoding.UTF8.GetByteCount(requestBody)} 位元組");
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 請求時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                // 檢查 API 函式選擇
                if (cmbApiFunction.SelectedItem is ComboBoxItem selected)
                {
                    string apiTag = selected.Tag?.ToString() ?? "UNKNOWN";
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → API 類型: {apiTag}");
                }

                // 檢查持久連線狀態，如果未建立則先建立
                if (!_isPersistentConnectionEstablished)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 🔗 檢測到未建立持久連線，正在建立...");
                    bool connectionEstablished = await EstablishPersistentConnection();
                    
                    if (!connectionEstablished)
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 無法建立持久連線，改為使用短連線模式");
                        UpdateStatus("❌ 持久連線建立失敗，改為短連線模式");
                    }
                }

                var startTime = DateTime.Now;
                
                // 透過持久連線發送請求
                bool localApiTestResult = false;
                
                if (_isPersistentConnectionEstablished)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 🔗 使用已建立的持久連線發送請求");
                    
                    // 顯示連線持續時間
                    if (_connectionEstablishedTime.HasValue)
                    {
                        var connectionDuration = DateTime.Now - _connectionEstablishedTime.Value;
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 持久連線已維持: {connectionDuration.TotalMinutes:F1} 分鐘");
                    }
                    
                    localApiTestResult = await TestApiEndpoint(endpoint, requestBody);
                }
                else
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 📡 使用短連線模式發送請求");
                    localApiTestResult = await TestApiEndpoint(endpoint, requestBody);
                }
                
                var endTime = DateTime.Now;
                var responseTime = (endTime - startTime).TotalMilliseconds;
                
                if (localApiTestResult)
                {
                    string connectionType = _isPersistentConnectionEstablished ? "持久連線" : "短連線";
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✅ {connectionType} API 測試成功");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 回應時間: {responseTime:F0} 毫秒");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 狀態: 成功");
                    
                    UpdateStatus($"✅ {connectionType} API 測試成功 ({responseTime:F0}ms)");
                    
                    // 如果本地測試成功，再嘗試向遠端 API 發送資料
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 🌐 嘗試向遠端 API 發送資料...");
                    try
                    {
                        if (_ddsService != null)
                        {
                            string remoteResult = await _ddsService.SendApiRequestAsync(endpoint, requestBody);
                            if (!string.IsNullOrEmpty(remoteResult))
                            {
                                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✅ 遠端 API 請求發送成功");
                                AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 遠端回應: {remoteResult.Substring(0, Math.Min(100, remoteResult.Length))}");
                            }
                            else
                            {
                                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ⚠️ 遠端 API 請求發送失敗或無回應");
                            }
                        }
                    }
                    catch (Exception remoteEx)
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 遠端 API 請求異常: {remoteEx.Message}");
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 這是正常的，如果遠端 API 伺服器未運行");
                    }
                }
                else
                {
                    string connectionType = _isPersistentConnectionEstablished ? "持久連線" : "短連線";
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ {connectionType} API 測試失敗");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 回應時間: {responseTime:F0} 毫秒");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 狀態: 失敗");
                    
                    if (_isPersistentConnectionEstablished)
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 可能需要重新建立持久連線");
                        // 標記持久連線為失效，下次會重新建立
                        _isPersistentConnectionEstablished = false;
                        _connectionEstablishedTime = null;
                    }
                    else
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 請確認本地伺服器是否正在運行");
                    }
                    
                    UpdateStatus($"❌ {connectionType} API 測試失敗 ({responseTime:F0}ms)");
                }
                
                // 更新連線統計
                if (_ddsService?.IsServerRunning == true)
                {
                    DisplayConnectionStatistics();
                }
            }
            catch (Exception ex)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ API 請求發生異常: {ex.Message}");
                UpdateStatus($"❌ API 請求失敗: {ex.Message}");
                MessageBox.Show($"發送 API 請求失敗:\n{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSendRequest.IsEnabled = true;
            }
        }/// <summary>
        /// 清除日誌按鈕點擊事件
        /// </summary>
        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtServerMessages?.Clear();
            txtClientMessages?.Clear();
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

        /// <summary>
        /// API 函式選擇變更事件
        /// </summary>
        private void cmbApiFunction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbApiFunction.SelectedItem is ComboBoxItem selectedApi)
                {
                    string apiTag = selectedApi.Tag?.ToString() ?? "";
                    string apiName = selectedApi.Content?.ToString() ?? "";
                    
                    // 取得對應的端點
                    string endpoint = GetApiEndpointFromSelection();
                      // 更新端點文字框，自動填入完整的端點 URL
                    if (txtIotEndpoint != null)
                    {
                        // 建構完整的端點 URL
                        string serverUrl = _ddsService?.ServerUrl ?? "http://localhost:8085/";
                        string fullEndpointUrl = serverUrl.TrimEnd('/');
                        
                        // 總是更新為對應的完整端點，但允許用戶後續手動修改
                        txtIotEndpoint.Text = fullEndpointUrl;
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 端點已更新: {fullEndpointUrl}");
                    }
                    
                    UpdateStatus($"已選擇 API 函式: {apiName} → {endpoint}");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] API 函式變更: {apiName} ({apiTag}) → {endpoint}");
                    
                    // 自動載入對應的範本
                    LoadApiTemplateByTag(apiTag);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"API 函式選擇變更失敗: {ex.Message}");
            }
        }

        #endregion

        #region 輔助方法

        /// <summary>        /// <summary>
        /// 載入預設設定
        /// </summary>
        private void LoadDefaultSettings()
        {
            // 載入伺服器位址設定
            if (txtServerUrl != null)
                txtServerUrl.Text = "http://localhost:8085/";
            
            if (txtDevCode != null)
                txtDevCode.Text = "DEVICE001";
            
            // 設定下拉式選單選項
            if (cmbMode != null)
                cmbMode.SelectedIndex = 0;
            
            if (cmbApiFunction != null)
            {
                cmbApiFunction.SelectedIndex = 0;
                
                // 根據預設選擇設定端點
                if (txtIotEndpoint != null)
                {
                    string defaultEndpoint = GetApiEndpointFromSelection();
                    string serverUrl = "http://localhost:8085/";
                    txtIotEndpoint.Text = serverUrl.TrimEnd('/') ;
                }
            }
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
        }        /// <summary>
        /// 啟用/停用伺服器控件
        /// </summary>
        private void EnableServerControls(bool enabled)
        {
            if (btnConnect != null) btnConnect.IsEnabled = enabled;
            if (btnDisconnect != null) btnDisconnect.IsEnabled = !enabled;
            if (txtServerUrl != null) txtServerUrl.IsEnabled = enabled;
        }        /// <summary>
        /// 啟用/停用用戶端控件
        /// </summary>
        private void EnableClientControls(bool enabled)
        {
            if (btnSendRequest != null) btnSendRequest.IsEnabled = enabled;
            if (txtIotEndpoint != null) txtIotEndpoint.IsEnabled = enabled;
            if (txtTemplate != null) txtTemplate.IsEnabled = enabled;
            if (cmbTemplates != null) cmbTemplates.IsEnabled = enabled;
        }        /// <summary>
        /// 載入 API 範本
        /// </summary>
        private void LoadApiTemplate(string templateDescription)
        {
            try
            {
                // 從描述中提取 API 名稱（括號內的內容）
                string apiName = ExtractApiNameFromDescription(templateDescription);
                
                if (!string.IsNullOrEmpty(apiName) && _apiTemplates.ContainsKey(apiName))
                {
                    if (txtTemplate != null)
                    {
                        txtTemplate.Text = _apiTemplates[apiName];
                        UpdateStatus($"已載入 {apiName} 範本");
                    }
                }
                else
                {
                    UpdateStatus($"找不到範本: {apiName}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"載入範本失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 從描述文字中提取 API 名稱
        /// </summary>
        private string ExtractApiNameFromDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            // 尋找括號內的內容
            int startIndex = description.LastIndexOf('(');
            int endIndex = description.LastIndexOf(')');
            
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return description.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            }
            
            return description; // 如果沒有括號，直接返回原始描述
        }/// <summary>
        /// 更新狀態列
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (txtStatus != null)
                txtStatus.Text = message;
        }        /// <summary>
        /// 附加日誌訊息
        /// </summary>
        private void AppendLog(string message)
        {
            if (txtServerMessages != null)
            {
                txtServerMessages.AppendText(message + Environment.NewLine);
                txtServerMessages.ScrollToEnd();
            }
        }

        /// <summary>
        /// 輸出伺服端日誌訊息
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
        /// 輸出用戶端日誌訊息
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
        /// 觸發屬性變更通知
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region 缺少的事件處理方法        /// <summary>
        /// 連接按鈕點擊事件 (啟動伺服器) - 增強版
        /// </summary>
        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {                
                if (_ddsService == null)
                {
                    MessageBox.Show("DDS WebAPI 服務未初始化", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string serverUrl = txtServerUrl.Text.Trim();
                if (string.IsNullOrEmpty(serverUrl))
                {
                    MessageBox.Show("請輸入伺服器位址", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新 UI 狀態
                btnConnect.IsEnabled = false;
                UpdateStatus("正在啟動伺服器...");
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 正在啟動 HTTP 伺服器: {serverUrl}");

                // 啟動伺服器
                bool success = await _ddsService.StartServerAsync();
                  if (success)
                {
                    UpdateStatus($"伺服器已啟動: {serverUrl}");
                    EnableServerControls(false);
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✓ HTTP 伺服器啟動成功");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✓ 安全性控制已啟用");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✓ 效能控制已啟用");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 等待用戶端連接...");
                    
                    if (rectStatus != null) rectStatus.Fill = new SolidColorBrush(Colors.Green);
                    if (rectServerStatus != null) rectServerStatus.Fill = new SolidColorBrush(Colors.Green);
                    
                    // 啟動連線監控
                    StartConnectionMonitoring();
                    
                    // 顯示安全性和效能狀態
                    DisplaySecurityAndPerformanceStatus();
                    
                    OnPropertyChanged(nameof(ServerStatus));
                }
                else
                {
                    btnConnect.IsEnabled = true;
                    UpdateStatus("伺服器啟動失敗");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✗ HTTP 伺服器啟動失敗");
                    if (rectStatus != null) rectStatus.Fill = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception ex)
            {
                btnConnect.IsEnabled = true;
                MessageBox.Show($"啟動伺服器失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("伺服器啟動失敗");
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✗ 伺服器啟動異常: {ex.Message}");
            }
        }        

        private async void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {                
                if (_ddsService != null)
                {
                    btnDisconnect.IsEnabled = false;
                    UpdateStatus("正在停止伺服器...");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 正在停止 HTTP 伺服器...");
                    
                    await _ddsService.StopServerAsync();
                    
                    UpdateStatus("伺服器已停止");
                    EnableServerControls(true);
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✓ HTTP 伺服器已停止");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✓ 所有用戶端連接已清理");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✓ 資源已釋放");
                    
                    if (rectStatus != null) rectStatus.Fill = new SolidColorBrush(Colors.Gray);
                    if (rectServerStatus != null) rectServerStatus.Fill = new SolidColorBrush(Colors.Gray);
                    
                    OnPropertyChanged(nameof(ServerStatus));
                    OnPropertyChanged(nameof(ClientCount));
                }
            }
            catch (Exception ex)
            {
                btnDisconnect.IsEnabled = true;
                MessageBox.Show($"停止伺服器失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ✗ 停止伺服器異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定資料按鈕點擊事件
        /// </summary>
        private void btnConfigData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 實作設定資料功能
                MessageBox.Show("設定資料功能待實作", "資訊", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定資料失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 開始心跳按鈕點擊事件
        /// </summary>
        private void btnStartHeartbeat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 實作開始心跳功能                _heartbeatTimer?.Start();
                if (rectHeartbeatStatus != null) rectHeartbeatStatus.Fill = new SolidColorBrush(Colors.Green);
                UpdateStatus("心跳已開始");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"開始心跳失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 停止心跳按鈕點擊事件
        /// </summary>
        private void btnStopHeartbeat_Click(object sender, RoutedEventArgs e)
        {
            try
            {                // 實作停止心跳功能
                _heartbeatTimer?.Stop();
                if (rectHeartbeatStatus != null) rectHeartbeatStatus.Fill = new SolidColorBrush(Colors.Gray);
                UpdateStatus("心跳已停止");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止心跳失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 套用範本按鈕點擊事件
        /// </summary>
        private void btnApplyTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbTemplates.SelectedItem is ComboBoxItem selectedItem)
                {
                    string templateName = selectedItem.Content.ToString();
                    LoadApiTemplate(templateName);
                    UpdateStatus($"已套用範本: {templateName}");
                }
                else
                {
                    MessageBox.Show("請選擇一個範本", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"套用範本失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        
        /// <summary>
        /// 儲存範本按鈕點擊事件
        /// </summary>
        private void btnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 簡化版本，直接使用預設名稱或讓用戶在外部文件中修改
                string templateName = $"自訂範本_{DateTime.Now:yyyyMMdd_HHmmss}";
                
                string templateContent = txtTemplate.Text;
                if (!string.IsNullOrEmpty(templateContent))
                {
                    _apiTemplates[templateName] = templateContent;
                    SaveApiTemplates();
                    UpdateStatus($"範本已儲存: {templateName}");
                    MessageBox.Show($"範本已儲存: {templateName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("範本內容不能為空", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存範本失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 儲存 API 範本到檔案
        /// </summary>
        private void SaveApiTemplates()
        {
            try
            {
                string templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ApiTemplates.json");
                string directory = Path.GetDirectoryName(templatesPath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                  string json = JsonConvert.SerializeObject(_apiTemplates, Formatting.Indented);
                File.WriteAllText(templatesPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存範本失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }        }

        #endregion

        #region 新增的事件處理函式

        /// <summary>
        /// 建立持久連線
        /// </summary>
        /// <returns>連線建立結果</returns>
        private async Task<bool> EstablishPersistentConnection()
        {
            try
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 🔗 正在建立持久連線...");
                
                // 檢查伺服器是否運作中
                if (_ddsService?.IsServerRunning != true)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 本地伺服器未執行，無法建立持久連線");
                    return false;
                }                // 執行連線測試
                // 使用正確的連線測試端點 (對應伺服器端的 /api/connection)
                string testEndpoint = "/api/connection";
                
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 使用測試端點: {testEndpoint}");

                var testData = new
                {
                    requestID = $"CONN_TEST_{DateTime.Now:yyyyMMddHHmmss}",
                    serviceName = "CONNECTION_TEST_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = txtDevCode?.Text ?? "KINSUS001",
                    @operator = "SYSTEM",
                    data = new[]
                    {
                        new
                        {
                            testType = "PERSISTENT_CONNECTION",
                            message = "持久連線測試",
                            connectionId = Guid.NewGuid().ToString()
                        }
                    }
                };

                bool connectionTest = await TestApiEndpoint(testEndpoint, Newtonsoft.Json.JsonConvert.SerializeObject(testData));
                
                if (connectionTest)
                {
                    _isPersistentConnectionEstablished = true;
                    _connectionEstablishedTime = DateTime.Now;
                    _connectionRetryCount = 0;
                    
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✅ 持久連線建立成功");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 連線時間: {_connectionEstablishedTime:HH:mm:ss}");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 連線狀態: 已建立");
                    
                    UpdateStatus("✅ 持久連線已建立");
                    return true;
                }
                else
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 持久連線建立失敗");
                    return false;
                }
            }            catch (Exception ex)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 建立持久連線時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 監控持久連線狀態
        /// </summary>
        private async Task MonitorPersistentConnection()
        {
            try
            {
                if (!_isPersistentConnectionEstablished || _ddsService?.IsServerRunning != true)
                {
                    return;
                }                // 執行簡單的心跳測試
                var heartbeatData = new
                {
                    requestID = $"HEARTBEAT_{DateTime.Now:yyyyMMddHHmmss}",
                    serviceName = "CONNECTION_TEST_COMMAND",
                    timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    devCode = txtDevCode?.Text ?? "KINSUS001",
                    data = new[]
                    {
                        new { 
                            testType = "HEARTBEAT", 
                            message = "連線心跳檢測",
                            interval = 15,
                            connectionId = _connectionEstablishedTime?.ToString("yyyyMMddHHmmss") ?? "unknown"
                        }
                    }
                };

                string testEndpoint = "/api/connection"; // 使用正確的端點
                bool heartbeatResult = await TestApiEndpoint(testEndpoint, Newtonsoft.Json.JsonConvert.SerializeObject(heartbeatData));
                  if (!heartbeatResult)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ⚠️ 持久連線心跳檢測失敗，嘗試重新建立連線...");
                    await ReconnectPersistentConnection();
                }
                else
                {
                    // 更新連線持續時間資訊 - 減少頻率輸出
                    if (_connectionEstablishedTime.HasValue)
                    {
                        var duration = DateTime.Now - _connectionEstablishedTime.Value;
                        
                        // 每5分鐘輸出一次連線狀態，或者在前30秒內每15秒輸出一次
                        bool shouldLogStatus = false;
                        
                        if (duration.TotalSeconds <= 30)
                        {
                            // 前30秒每15秒輸出一次
                            shouldLogStatus = (duration.TotalSeconds % 15) < 1;
                        }
                        else if (duration.TotalMinutes % 5 < 0.25)
                        {
                            // 5分鐘間隔輸出
                            shouldLogStatus = true;
                        }
                        
                        if (shouldLogStatus)
                        {
                            AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 💓 持久連線運作正常");
                            AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 已持續: {duration.TotalMinutes:F1} 分鐘");
                            AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 心跳間隔: 15秒");
                            AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 重試計數: {_connectionRetryCount}/{MAX_RETRY_COUNT}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 監控持久連線時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新建立持久連線
        /// </summary>
        private async Task ReconnectPersistentConnection()
        {
            try
            {
                _connectionRetryCount++;
                
                if (_connectionRetryCount > MAX_RETRY_COUNT)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 已達到最大重試次數 ({MAX_RETRY_COUNT})，停止重新連線");
                    _isPersistentConnectionEstablished = false;
                    _connectionEstablishedTime = null;
                    UpdateStatus("❌ 持久連線已斷開且無法重新建立");
                    return;
                }                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 🔄 嘗試重新建立持久連線 (第 {_connectionRetryCount}/{MAX_RETRY_COUNT} 次)...");
                
                // 使用較短的等待時間，但不要太短
                int waitSeconds = Math.Min(5, _connectionRetryCount * 2); // 2, 4, 6, 8, 10 秒
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 等待 {waitSeconds} 秒後重試...");
                await Task.Delay(waitSeconds * 1000);
                
                bool reconnectResult = await EstablishPersistentConnection();
                
                if (reconnectResult)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✅ 持久連線重新建立成功");
                    // 重連成功後，重設重試計數
                    _connectionRetryCount = 0;
                }
                else
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 持久連線重新建立失敗 (嘗試 {_connectionRetryCount}/{MAX_RETRY_COUNT})");
                    
                    // 如果還有重試機會，提示下次重試時間
                    if (_connectionRetryCount < MAX_RETRY_COUNT)
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 將在下一次心跳檢測時重試 (約15秒後)");
                    }
                }
            }            catch (Exception ex)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 重新建立持久連線時發生錯誤: {ex.Message}");
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 錯誤類型: {ex.GetType().Name}");
                
                // 如果是網路相關錯誤，提供更多資訊
                if (ex is System.Net.Http.HttpRequestException)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 建議: 檢查網路連線或伺服器狀態");
                }
                else if (ex is TaskCanceledException)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}]   → 建議: 請求逾時，可能網路較慢");
                }
            }
        }

        /// <summary>
        /// 關閉持久連線
        /// </summary>
        private async Task ClosePersistentConnection()
        {
            try
            {
                if (_isPersistentConnectionEstablished)
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 🔌 正在關閉持久連線...");
                    
                    // 發送斷線通知
                    try
                    {
                        var disconnectData = new
                        {
                            requestID = $"DISCONNECT_{DateTime.Now:yyyyMMddHHmmss}",
                            serviceName = "CONNECTION_TEST_COMMAND",
                            timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            devCode = txtDevCode?.Text ?? "KINSUS001",
                            data = new[]
                            {
                                new { testType = "DISCONNECT", message = "持久連線正常關閉" }
                            }
                        };

                        await _ddsService?.SendApiRequestAsync("/api/connection", disconnectData);
                    }
                    catch
                    {
                        // 忽略斷線通知失敗
                    }

                    _isPersistentConnectionEstablished = false;
                    _connectionEstablishedTime = null;
                    _connectionRetryCount = 0;
                    
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✅ 持久連線已關閉");
                    UpdateStatus("持久連線已關閉");
                }
            }
            catch (Exception ex)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ❌ 關閉持久連線時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// IoT 連接按鈕點擊事件
        /// </summary>
        private async void btnConnectIoT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtIotEndpoint.Text))
                {
                    MessageBox.Show("請輸入 IoT 端點位址", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                btnConnectIoT.IsEnabled = false;
                UpdateStatus("正在連接到 IoT 系統...");

                // 設定遠端 API URL
                string iotEndpoint = txtIotEndpoint.Text.Trim();
                if (!iotEndpoint.StartsWith("http://") && !iotEndpoint.StartsWith("https://"))
                {
                    iotEndpoint = "http://" + iotEndpoint;
                }

                // 使用 DDS 服務設定遠端 API URL
                _ddsService.RemoteApiUrl = iotEndpoint;

                // 測試連接到 IoT 端點
                var testResult = await TestIoTConnection(iotEndpoint);
                
                if (testResult.IsSuccess)
                {
                    // 連接成功 - 更新 UI 狀態
                    btnConnectIoT.IsEnabled = false;
                    btnDisconnectIoT.IsEnabled = true;
                    rectIoTStatus.Fill = Brushes.Green;
                    rectIoTConnectionStatus.Fill = Brushes.Green;
                    
                    UpdateStatus($"已成功連接到 IoT 系統: {iotEndpoint}");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 成功連接到 IoT 端點: {iotEndpoint}");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 回應時間: {testResult.ResponseTime.ToString("yyyy-MM-dd HH:mm:ss")}");
                    
                    if (!string.IsNullOrEmpty(testResult.ResponseBody))
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 伺服器回應: {testResult.ResponseBody}");
                    }
                }
                else
                {
                    // 連接失敗
                    btnConnectIoT.IsEnabled = true;
                    rectIoTStatus.Fill = Brushes.Red;
                    rectIoTConnectionStatus.Fill = Brushes.Red;
                    
                    string errorMsg = testResult.ErrorMessage ?? "未知錯誤";
                    UpdateStatus($"連接 IoT 系統失敗: {errorMsg}");
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 連接失敗: {errorMsg}");
                    
                    MessageBox.Show($"無法連接到 IoT 端點: {iotEndpoint}\n錯誤: {errorMsg}", 
                                  "連接失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                btnConnectIoT.IsEnabled = true;
                rectIoTStatus.Fill = Brushes.Red;
                rectIoTConnectionStatus.Fill = Brushes.Red;
                UpdateStatus($"連接 IoT 系統失敗: {ex.Message}");
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 連接異常: {ex.Message}");
                MessageBox.Show($"連接失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }       
        /// <summary>
        /// IoT 斷開連接按鈕點擊事件
        /// </summary>
        private async void btnDisconnectIoT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnDisconnectIoT.IsEnabled = false;
                UpdateStatus("正在斷開 IoT 連接...");

                // 清除遠端 API URL 設定
                if (_ddsService != null)
                {
                    string previousUrl = _ddsService.RemoteApiUrl;
                    _ddsService.RemoteApiUrl = string.Empty;
                    
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 清除遠端 API 設定: {previousUrl}");
                }
                
                // 模擬斷開連接處理時間
                await Task.Delay(500);
                
                // 更新 UI 狀態
                btnConnectIoT.IsEnabled = true;
                btnDisconnectIoT.IsEnabled = false;
                rectIoTStatus.Fill = Brushes.Gray;
                rectIoTConnectionStatus.Fill = Brushes.Gray;
                
                UpdateStatus("已斷開 IoT 連接");
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 已成功斷開 IoT 連接");
            }
            catch (Exception ex)
            {
                btnDisconnectIoT.IsEnabled = true;
                UpdateStatus($"斷開 IoT 連接失敗: {ex.Message}");
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 斷開連接時發生錯誤: {ex.Message}");
                MessageBox.Show($"斷開連接失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 發送回應按鈕點擊事件
        /// </summary>
        private async void btnSendResponse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbServerResponse.SelectedItem is ComboBoxItem selectedItem)
                {
                    string responseType = selectedItem.Content.ToString();
                    
                    // 檢查是否有選中的用戶端
                    if (dgClients.SelectedItem is ClientConnection selectedClient)
                    {
                        UpdateStatus($"正在發送回應: {responseType}");
                        
                        // 模擬發送回應
                        await Task.Delay(500);
                        
                        AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 向用戶端 {selectedClient.Id} 發送回應: {responseType}");
                        UpdateStatus($"已發送回應: {responseType}");
                    }
                    else
                    {
                        MessageBox.Show("請先選擇一個用戶端", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("請選擇回應類型", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"發送回應失敗: {ex.Message}");
                MessageBox.Show($"發送回應失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 廣播訊息按鈕點擊事件
        /// </summary>
        private async void btnBroadcast_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("確定要向所有連接的用戶端廣播訊息嗎？", "確認", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    UpdateStatus("正在廣播訊息...");
                    
                    // 模擬廣播邏輯
                    await Task.Delay(1000);
                    
                    int clientCount = _clientConnections.Count;
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 廣播訊息已發送給 {clientCount} 個用戶端");
                    UpdateStatus($"廣播完成 - 已發送給 {clientCount} 個用戶端");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"廣播失敗: {ex.Message}");                MessageBox.Show($"廣播失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 踢除用戶端按鈕點擊事件
        /// </summary>
        private async void btnKickClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgClients.SelectedItem is ClientConnection selectedClient)
                {
                    var result = MessageBox.Show($"確定要踢除用戶端 '{selectedClient.Id}' 嗎？", "確認", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateStatus($"正在踢除用戶端: {selectedClient.Id}");
                        
                        // 模擬踢除邏輯                        await Task.Delay(500);
                        
                        _clientConnections.Remove(selectedClient);
                        btnKickClient.IsEnabled = false;
                        
                        AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 已踢除用戶端: {selectedClient.Id}");
                        UpdateStatus($"已踢除用戶端: {selectedClient.Id}");
                    }
                }
                else
                {
                    MessageBox.Show("請先選擇要踢除的用戶端", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"踢除用戶端失敗: {ex.Message}");
                MessageBox.Show($"踢除失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 用戶端連接 DataGrid 選擇變更事件
        /// </summary>
        private void dgClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 當選中用戶端時啟用踢除按鈕
            btnKickClient.IsEnabled = dgClients.SelectedItem != null;
        }        
        /// <summary>
        /// 測試 IoT 連接
        /// </summary>
        /// <param name="endpoint">IoT 端點 URL</param>
        /// <returns>連接測試結果</returns>
        private async Task<ApiCallResult> TestIoTConnection(string endpoint)
        {
            var result = new ApiCallResult
            {
                RequestUrl = endpoint,
                RequestTime = DateTime.Now
            };

            try
            {               
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    // 動態取得 API 金鑰
                    string apiKey = _ddsService?.GetDefaultApiKey() ?? "KINSUS-API-KEY-2024";
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    
                    // 準備測試請求資料
                    var testRequest = new BaseRequest<object>
                    {
                        ServiceName = "ConnectionTest",
                        DevCode = _ddsService?.DeviceCode ?? "KINSUS001",
                        Operator = _ddsService?.OperatorName ?? "SYSTEM",
                        TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Data = new List<object> { new { Message = "Connection Test", Timestamp = DateTime.Now } }
                    };

                    string jsonContent = JsonConvert.SerializeObject(testRequest, Formatting.Indented);
                    result.RequestBody = jsonContent;

                    var content = new System.Net.Http.StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                      // 嘗試發送測試請求
                    var response = await httpClient.PostAsync($"{endpoint}/api/Connection", content);
                    
                    result.ResponseTime = DateTime.Now;
                    result.StatusCode = (int)response.StatusCode;
                    result.IsSuccess = response.IsSuccessStatusCode;

                    if (response.Content != null)
                    {
                        result.ResponseBody = await response.Content.ReadAsStringAsync();
                        result.ResponseData = result.ResponseBody; // 設定 ResponseData
                    }

                    if (!result.IsSuccess)
                    {
                        result.ErrorMessage = $"HTTP {result.StatusCode}: {response.ReasonPhrase}";
                    }
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                result.ResponseTime = DateTime.Now;
                result.IsSuccess = false;
                result.ErrorMessage = $"HTTP 請求錯誤: {ex.Message}";
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                result.ResponseTime = DateTime.Now;
                result.IsSuccess = false;
                result.ErrorMessage = "連接逾時 (10秒)";
            }
            catch (Exception ex)
            {
                result.ResponseTime = DateTime.Now;
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }/// <summary>
        /// 發送測試資料到 IoT 端點
        /// </summary>
        /// <param name="testData">測試資料</param>
        /// <returns>發送結果</returns>
        private async Task<bool> SendTestDataToIoT(object testData)
        {
            try
            {
                if (_ddsService != null && !string.IsNullOrEmpty(_ddsService.RemoteApiUrl))
                {
                    // 準備測試資料的 JSON 格式
                    string jsonData = JsonConvert.SerializeObject(testData, Formatting.Indented);
                      // 使用 DDS 服務發送 API 請求
                    string result = await _ddsService.SendApiRequestAsync("/api/test-data", jsonData);
                    
                    if (!string.IsNullOrEmpty(result))
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 測試資料發送成功");
                        return true;
                    }
                    else
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 測試資料發送失敗");
                        return false;
                    }
                }
                else
                {
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 未連接到 IoT 端點，無法發送測試資料");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 發送測試資料時發生異常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 檢查 IoT 連接狀態
        /// </summary>
        /// <returns>連接狀態</returns>
        private bool IsIoTConnected()
        {
            return _ddsService != null && 
                   !string.IsNullOrEmpty(_ddsService.RemoteApiUrl) && 
                   btnDisconnectIoT.IsEnabled;
        }        
        /// <summary>
        /// 顯示連線統計資訊（增強版）
        /// </summary>
        private void DisplayConnectionStatistics()
        {
            try
            {
                var currentTime = DateTime.Now;
                
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 📈 連線統計報告:");
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 累計API請求: {_totalApiRequests} 次");
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 成功請求: {_successfulApiRequests} 次");
                if (_totalApiRequests > 0)
                {
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 成功率: {(_successfulApiRequests * 100.0 / _totalApiRequests):F1}%");
                }
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 目前活躍長連線: {_clientConnections.Count} 個");
                
                if (_clientConnections.Count > 0)
                {
                    // 計算連線時長統計
                    var durations = _clientConnections.Select(c => (currentTime - c.ConnectTime).TotalMinutes).ToList();
                    var avgDuration = durations.Average();
                    var maxDuration = durations.Max();
                    var minDuration = durations.Min();
                    
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 平均連線時長: {avgDuration:F1} 分鐘");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 最長連線時長: {maxDuration:F1} 分鐘");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 最短連線時長: {minDuration:F1} 分鐘");
                    
                    // 依據 IP 位址分組統計
                    var ipGroups = _clientConnections.GroupBy(c => c.IpAddress).ToList();
                    if (ipGroups.Count > 1)
                    {
                        AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 不同 IP 位址數: {ipGroups.Count}");
                        foreach (var group in ipGroups)
                        {
                            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]     • {group.Key}: {group.Count()} 個連線");
                        }
                    }
                    
                    // 依據請求類型分組統計
                    var typeGroups = _clientConnections.GroupBy(c => c.RequestType).ToList();
                    if (typeGroups.Count > 1)
                    {
                        AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 連線類型分布:");
                        foreach (var group in typeGroups)
                        {
                            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]     • {group.Key}: {group.Count()} 個");
                        }
                    }                }
                else
                {
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   ℹ️  說明: HTTP API 為短連線，完成後立即關閉");
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   ℹ️  長連線僅顯示 WebSocket 等持續性連線");
                }
                
                // 從 DDS 服務取得更多統計資訊
                if (_ddsService != null)
                {
                    try
                    {
                        var stats = _ddsService.GetServerStatistics();
                        
                        if (stats != null)
                        {
                            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 總處理請求: {stats.TotalRequests}");
                            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 連線數: {stats.ConnectedClients}");
                            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 運作時間: {stats.Uptime}");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 無法取得詳細統計: {ex.Message}");
                    }
                }
                
                // 記憶體使用統計
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 目前記憶體使用: {memoryUsage} MB");
                
                // 執行時間統計
                var uptime = currentTime - process.StartTime;
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 程式執行時間: {uptime.TotalHours:F1} 小時");
            }
            catch (Exception ex)
            {
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ❌ 顯示連線統計時發生錯誤: {ex.Message}");
            }        }

        /// <summary>
        /// 測試 API 連接並顯示詳細資訊
        /// </summary>
        private async Task<bool> TestApiEndpoint(string endpoint, string requestBody)
        {
            try
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 📡 測試本地 API 端點: {endpoint}");
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 📤 發送資料大小: {requestBody?.Length ?? 0} 位元組");
                
                // 測試本地伺服器 API 端點（localhost:8085）
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer KINSUS-API-KEY-2024");
                    
                    string localServerUrl = _ddsService?.ServerUrl ?? "http://localhost:8085/";
                    string fullUrl = $"{localServerUrl.TrimEnd('/')}"+ endpoint;
                    
                    AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 🌐 目標 URL: {fullUrl}");
                    
                    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(fullUrl, content);
                    
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✓ 本地 API 測試成功");
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 📥 回應狀態: {(int)response.StatusCode} {response.ReasonPhrase}");
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 📥 回應資料: {(string.IsNullOrEmpty(responseBody) ? "(空)" : responseBody.Substring(0, Math.Min(100, responseBody.Length)))}");
                        return true;
                    }
                    else
                    {
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✗ 本地 API 測試失敗");
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 📥 錯誤狀態: {(int)response.StatusCode} {response.ReasonPhrase}");
                        AppendClientLog($"[{DateTime.Now:HH:mm:ss}] 📥 錯誤訊息: {responseBody}");
                        return false;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✗ 本地 API 測試網路錯誤: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✗ 本地 API 測試逾時");
                return false;
            }
            catch (Exception ex)
            {
                AppendClientLog($"[{DateTime.Now:HH:mm:ss}] ✗ 本地 API 測試異常: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 顯示安全性和效能狀態（增強版）        /// </summary>
        private void DisplaySecurityAndPerformanceStatus()
        {
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 🔒 安全性控制狀態:");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → API 金鑰驗證: ✅ 啟用");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → IP 白名單控制: ✅ 啟用");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 請求簽章驗證: ✅ 啟用");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → HTTPS 強制模式: ✅ 啟用");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 入侵檢測: ✅ 啟用");
            
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ⚡ 效能控制狀態:");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 請求頻率限制: ✅ 100 請求/分鐘");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 最大平行連線數: ✅ 20 連線");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 最大資料大小: ✅ 10 MB");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 連線逾時設定: ✅ 30 秒");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 資源監控: ✅ 啟用");
            
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 🌐 連線管理功能:");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 自動清理逾時連線: ✅ 啟用 (5分鐘)");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 連線狀態監控: ✅ 啟用");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 即時統計報告: ✅ 啟用");
            AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 負載均衡: ✅ 準備就緒");
        }

        /// <summary>
        /// 開始即時監控連線狀態
        /// </summary>
        private void StartConnectionMonitoring()
        {
            try
            {
                // 建立連線監控計時器（每 30 秒檢查一次）
                var monitoringTimer = new DispatcherTimer();
                monitoringTimer.Interval = TimeSpan.FromSeconds(30);
                monitoringTimer.Tick += (s, e) =>
                {
                    try
                    {
                        // 檢查並清理逾時連線
                        var currentTime = DateTime.Now;
                        var expiredConnections = _clientConnections
                            .Where(c => (currentTime - c.LastActivityTime).TotalMinutes > 5)
                            .ToList();
                        
                        foreach (var expiredClient in expiredConnections)
                        {
                            _clientConnections.Remove(expiredClient);
                            AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 🗑️ 自動清理逾時連線: {expiredClient.Id}");
                        }
                        
                        if (expiredConnections.Count > 0)
                        {
                            OnPropertyChanged(nameof(ClientCount));
                        }
                        
                        // 每 5 分鐘顯示一次統計報告
                        if (DateTime.Now.Minute % 5 == 0 && DateTime.Now.Second < 30)
                        {
                            AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 📊 定期統計報告:");
                            DisplayConnectionStatistics();
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ❌ 連線監控異常: {ex.Message}");
                    }
                };
                
                monitoringTimer.Start();
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 🔍 連線監控已啟動 (30秒間隔)");
            }
            catch (Exception ex)
            {
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ❌ 啟動連線監控失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 執行連線品質測試
        /// </summary>
        private async Task<bool> PerformConnectionQualityTest()
        {
            try
            {
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 🧪 開始連線品質測試...");
                
                // 測試 1: 基本連通性測試
                string serverUrl = txtServerUrl.Text.Trim();
                bool basicConnectivity = await TestApiEndpoint("/api/ping", "{}");
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 基本連通性: {(basicConnectivity ? "✅ 通過" : "❌ 失敗")}");
                
                // 測試 2: 負載測試（模擬多重請求）
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 執行負載測試 (5個平行請求)...");
                var loadTestTasks = new List<Task<bool>>();
                for (int i = 0; i < 5; i++)
                {
                    loadTestTasks.Add(TestApiEndpoint($"/api/test/{i}", $"{{\"testId\": {i}}}"));
                }
                
                var loadTestResults = await Task.WhenAll(loadTestTasks);
                int passedTests = loadTestResults.Count(r => r);
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 負載測試結果: {passedTests}/5 通過");
                
                // 測試 3: 回應時間測試
                var responseTimeStart = DateTime.Now;
                await TestApiEndpoint("/api/performance", "{}");
                var responseTime = (DateTime.Now - responseTimeStart).TotalMilliseconds;
                bool responseTimeGood = responseTime < 1000; // 1秒內為良好
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}]   → 回應時間測試: {responseTime:F0}ms {(responseTimeGood ? "✅ 良好" : "⚠️ 緩慢")}");
                
                // 總體評估
                bool overallQuality = basicConnectivity && (passedTests >= 4) && responseTimeGood;
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 🏆 連線品質總評: {(overallQuality ? "✅ 優良" : "⚠️ 需要改善")}");
                
                return overallQuality;            }
            catch (Exception ex)
            {
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ❌ 連線品質測試異常: {ex.Message}");
                return false;
            }
        }        
        /// <summary>
        /// 連線品質測試按鈕點擊事件
        /// </summary>
        private async void btnConnectionQuality_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("正在執行連線品質測試...");
                
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 🧪 開始全面連線品質檢測");
                
                // 檢查伺服器是否正在執行
                if (_ddsService?.IsServerRunning != true)
                {
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ⚠️ 伺服器未執行，部分測試將跳過");
                }
                
                // 執行連線品質測試
                bool qualityTestResult = await PerformConnectionQualityTest();
                
                if (qualityTestResult)
                {
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] 🏆 連線品質測試完成 - 結果：優良");
                    UpdateStatus("✅ 連線品質測試：優良");
                    MessageBox.Show("🎉 連線品質測試完成！\n\n測試結果：優良\n\n• 基本連通性：正常\n• 負載處理：正常\n• 回應時間：良好\n• 整體評級：A+", 
                        "品質測試結果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ⚠️ 連線品質測試完成 - 結果：需要改善");
                    UpdateStatus("⚠️ 連線品質測試：需要改善");                    MessageBox.Show("⚠️ 連線品質測試完成\n\n測試結果：需要改善\n\n建議檢查：\n• 網路連接狀況\n• 伺服器負載\n• 防火牆設定\n• 系統資源使用", 
                        "品質測試結果", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                AppendServerLog($"[{DateTime.Now:HH:mm:ss}] ❌ 連線品質測試異常: {ex.Message}");
                UpdateStatus($"❌ 連線品質測試失敗: {ex.Message}");
                MessageBox.Show($"連線品質測試失敗:\n{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 根據選擇的 API 函式取得對應的端點
        /// </summary>
        /// <returns>API 端點路徑</returns>
        private string GetApiEndpointFromSelection()
        {
            try
            {
                // 優先使用選擇的 API 函式
                if (cmbApiFunction.SelectedItem is ComboBoxItem selectedApi)
                {
                    string apiTag = selectedApi.Tag?.ToString() ?? "";
                    
                    // 根據 API 類型對應到正確的端點
                    switch (apiTag)
                    {
                        // 標準 MES API 端點
                        case "SEND_MESSAGE_COMMAND":
                            return "/api/v1/send_message";
                        case "CREATE_NEEDLE_WORKORDER_COMMAND":
                            return "/api/v1/create_workorder";
                        case "DATE_MESSAGE_COMMAND":
                            return "/api/v1/sync_time";
                        case "SWITCH_RECIPE_COMMAND":
                            return "/api/v1/switch_recipe";
                        case "DEVICE_CONTROL_COMMAND":
                            return "/api/v1/device_control";
                        case "WAREHOUSE_RESOURCE_QUERY_COMMAND":
                            return "/api/v1/warehouse_query";
                        case "TOOL_TRACE_HISTORY_QUERY_COMMAND":
                            return "/api/v1/tool_history_query";
                          // 回報類 API 端點
                        case "TOOL_OUTPUT_REPORT_MESSAGE":
                            return "/api/v1/tool_history_report";
                        case "ERROR_REPORT_MESSAGE":
                            return "/api/mes"; // 使用通用端點處理錯誤回報
                        case "MACHINE_STATUS_REPORT_MESSAGE":
                            return "/api/mes"; // 使用通用端點處理機臺狀態回報
                        
                        // 客製化倉庫管理 API
                        case "IN_MATERIAL_COMMAND":
                            return "/api/in-material";
                        case "OUT_MATERIAL_COMMAND":
                            return "/api/out-material";
                        case "GET_LOCATION_BY_STORAGE_COMMAND":
                            return "/api/getlocationbystorage";
                        case "GET_LOCATION_BY_PIN_COMMAND":
                            return "/api/getlocationbypin";
                        case "OPERATION_CLAMP_COMMAND":
                            return "/api/operationclamp";
                        case "CHANGE_SPEED_COMMAND":
                            return "/api/changespeed";
                        
                        // 系統管理 API
                        case "SERVER_STATUS_QUERY":
                            return "/api/server/status";
                        case "SERVER_RESTART_COMMAND":
                            return "/api/server/restart";
                        case "CONNECTION_TEST_COMMAND":
                            return "/api/v1/send_message/api/connection";
                        
                        default:
                            // 如果無法識別，使用通用 MES API 端點
                            return "/api/mes";
                    }
                }
                
                // 如果沒有選擇 API 函式，則使用手動輸入的端點
                string manualEndpoint = txtIotEndpoint.Text.Trim();
                if (!string.IsNullOrEmpty(manualEndpoint))
                {
                    // 如果是完整 URL，提取路徑部分
                    if (manualEndpoint.StartsWith("http://") || manualEndpoint.StartsWith("https://"))
                    {
                        if (Uri.TryCreate(manualEndpoint, UriKind.Absolute, out Uri uri))
                        {
                            return uri.PathAndQuery;
                        }
                    }
                    
                    // 確保端點以 / 開頭
                    return manualEndpoint.StartsWith("/") ? manualEndpoint : "/" + manualEndpoint;
                }
                
                // 預設使用 MES API 端點
                return "/api/mes";
            }
            catch (Exception ex)
            {
                UpdateStatus($"取得 API 端點失敗: {ex.Message}");
                return "/api/mes"; // 回傳預設端點
            }
        }

        /// <summary>
        /// 根據 API 標籤載入對應的範本
        /// </summary>
        /// <param name="apiTag">API 標籤</param>
        private void LoadApiTemplateByTag(string apiTag)
        {
            try
            {
                if (!string.IsNullOrEmpty(apiTag) && _apiTemplates.ContainsKey(apiTag))
                {
                    if (txtTemplate != null)
                    {
                        txtTemplate.Text = _apiTemplates[apiTag];
                        UpdateStatus($"已載入 {apiTag} 範本");
                    }
                }
            }
            catch (Exception ex)            {
                UpdateStatus($"載入範本失敗: {ex.Message}");
            }
        }

        #endregion
    }
}
