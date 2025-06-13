using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using KINSUS.API; // 添加對 API 命名空間的引用
using KINSUS.Model; // 添加對資料模型命名空間的引用
using Newtonsoft.Json; // 添加 JSON 序列化支援
using Newtonsoft.Json.Linq; // 加入 Linq 支援以進行更安全的 JSON 操作
using System.IO; // Ensure System.IO is included
using Path = System.IO.Path; // Ensure Path alias is set if needed

namespace KINSUS
{
    /// <summary>
    /// 用戶端連接資訊類別
    /// </summary>
    public class ClientConnection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _id;
        private string _ipAddress;
        private DateTime _connectTime;
        private DateTime _lastActivityTime;
        private string _requestType;

        public string Id 
        { 
            get { return _id; }
            set 
            { 
                _id = value;
                OnPropertyChanged("Id");
            }
        }

        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                _ipAddress = value;
                OnPropertyChanged("IpAddress");
            }
        }

        public DateTime ConnectTime
        {
            get { return _connectTime; }
            set
            {
                _connectTime = value;
                OnPropertyChanged("ConnectTime");
            }
        }

        public DateTime LastActivityTime
        {
            get { return _lastActivityTime; }
            set
            {
                _lastActivityTime = value;
                OnPropertyChanged("LastActivityTime");
            }
        }

        public string RequestType
        {
            get { return _requestType; }
            set
            {
                _requestType = value;
                OnPropertyChanged("RequestType");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// HTTP 伺服器實例，用於接收 MES/IoT 系統的請求
        /// </summary>
        private HttpServer httpServer;

        /// <summary>
        /// 用戶端連接資訊集合
        /// </summary>
        private ObservableCollection<ClientConnection> clientConnections;

        /// <summary>
        /// 訊息顯示委派
        /// </summary>
        private delegate void AppendLogDelegate(string message);

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
        private OperationMode currentMode = OperationMode.DualMode;

        /// <summary>
        /// 心跳計時器
        /// </summary>
        private DispatcherTimer heartbeatTimer;

        /// <summary>
        /// 日期時間顯示計時器
        /// </summary>
        private DispatcherTimer dateTimeTimer;

        /// <summary>
        /// API 請求範本字典
        /// </summary>
        private Dictionary<string, string> apiTemplates;

        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化用戶端連接集合
            clientConnections = new ObservableCollection<ClientConnection>();
            dgClients.ItemsSource = clientConnections;
            
            // 初始化計時器
            InitializeTimers();
            
            // 初始化 API 請求範本
            InitializeApiTemplates();
            
            // 視窗關閉事件處理
            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// 視窗載入事件
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 載入伺服器位址設定
            txtServerUrl.Text = "http://localhost:8085/";
            txtIotEndpoint.Text = "http://localhost:8085/"; // 修改 IoT 端點預設值
            txtDevCode.Text = "DEVICE001";
            
            // 設定下拉式選單選項 (cmbApiFunction)
            cmbMode.SelectedIndex = 0;
            cmbApiFunction.SelectedIndex = 0;
            // cmbTemplates 會在 InitializeApiTemplates 完成後填充並設定 SelectedIndex
            // cmbTemplates.SelectedIndex = 0; // 移除此行

            // 初始化訊息顯示
            UpdateUIByMode();

            // 載入 API 範本 (這應該在建構函式中呼叫了)
            // InitializeApiTemplates();

            // 載入第一個 API 範本 (現在由 PopulateTemplateComboBox 處理預設選中)
            // if (cmbTemplates.SelectedItem != null)
            // {
            //     LoadApiTemplate((cmbTemplates.SelectedItem as ComboBoxItem).Content.ToString());
            // }

            // 更新狀態
            UpdateStatus("準備就緒，請設定操作模式並啟動所需功能");
        }        /// <summary>
        /// 初始化並啟動 HTTP 伺服器
        /// </summary>
        private void InitializeHttpServer()
        {
            try
            {
                // 從輸入框獲取 URL 前綴
                string urlPrefix = txtServerUrl.Text;
                  // 建立 HTTP 伺服器
                httpServer = new HttpServer(urlPrefix);
                
                // 註冊訊息接收事件 - 在 HttpServer 類別中加入這些事件
                httpServer.MessageReceived += HttpServer_MessageReceived;
                httpServer.ClientConnected += HttpServer_ClientConnected;
                
                // 啟動伺服器
                httpServer.Start();                
                // 更新 UI 狀態
                rectStatus.Fill = Brushes.Green;
                rectServerStatus.Fill = Brushes.Green;
                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = true;
                txtServerUrl.IsEnabled = false;
                
                UpdateStatus("伺服器已啟動，監聽位址: " + urlPrefix);
                
                // 清除伺服器訊息區
                txtServerMessages.Clear();
                AppendServerLog("===== 伺服器已啟動 =====");
                AppendServerLog("監聽位址: " + urlPrefix);
                AppendServerLog("等待連接中...");
                AppendServerLog("");
            }
            catch (Exception ex)
            {
                // 處理伺服器啟動錯誤
                rectStatus.Fill = Brushes.Red;
                rectServerStatus.Fill = Brushes.Red;
                UpdateStatus($"HTTP 伺服器啟动失敗: {ex.Message}");
                AppendServerLog($"錯誤: {ex.Message}");
                MessageBox.Show($"HTTP 伺服器啟動失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        /// <summary>
        /// 停止 HTTP 伺服器
        /// </summary>
        private void StopHttpServer()
        {
            try
            {
                if (httpServer != null)
                {
                    // 取消註冊事件
                    httpServer.MessageReceived -= HttpServer_MessageReceived;
                    httpServer.ClientConnected -= HttpServer_ClientConnected;
                    
                    // 停止 HTTP 伺服器
                    httpServer.Stop();
                    httpServer = null;
                    
                    // 更新 UI 狀態
                    rectStatus.Fill = Brushes.Gray;
                    rectServerStatus.Fill = Brushes.Gray;
                    btnConnect.IsEnabled = true;
                    btnDisconnect.IsEnabled = false;
                    txtServerUrl.IsEnabled = true;
                    
                    UpdateStatus("伺服器已停止");
                    AppendServerLog("===== 伺服器已停止 =====");
                    
                    // 清空用戶端連接列表
                    clientConnections.Clear();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"停止伺服器時發生錯誤: {ex.Message}");
                AppendServerLog($"停止伺服器錯誤: {ex.Message}");
                MessageBox.Show($"停止 HTTP 伺服器失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 視窗關閉時停止 HTTP 伺服器
        /// </summary>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            StopHttpServer();
        }

        /// <summary>
        /// 連接按鈕點擊事件
        /// </summary>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            InitializeHttpServer();
        }

        /// <summary>
        /// 中斷連接按鈕點擊事件
        /// </summary>
        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            StopHttpServer();
        }        /// <summary>
        /// HTTP 伺服器接收訊息事件處理
        /// </summary>
        private void HttpServer_MessageReceived(object sender, MessageEventArgs e)
        {
            // 使用委派在UI執行緒上更新訊息
            this.Dispatcher.Invoke(() => 
            {
                AppendServerLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 收到來自 {e.ClientIp} 的訊息:");
                AppendServerLog($"{e.Message}");
                AppendServerLog(string.Empty); // 空行分隔
                
                // 嘗試解析 API 類型
                try
                {
                    dynamic jsonData = JsonConvert.DeserializeObject(e.Message);
                    string serviceName = jsonData.serviceName;
                    
                    // 更新用戶端連接資訊中的請求類型
                    UpdateClientLastActivity(e.ClientId, e.ClientIp, serviceName);
                }
                catch
                {
                    // 無法解析 JSON 或沒有 serviceName 欄位
                    UpdateClientLastActivity(e.ClientId, e.ClientIp);
                }
            });
        }        /// <summary>
        /// HTTP 伺服器用戶端連接事件處理
        /// </summary>
        private void HttpServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            // 使用委派在UI執行緒上更新用戶端連接資訊
            this.Dispatcher.Invoke(() => 
            {
                AppendServerLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 新用戶端連接:");
                AppendServerLog($"ID: {e.ClientId}");
                AppendServerLog($"IP: {e.ClientIp}");
                AppendServerLog(string.Empty); // 空行分隔
                
                // 新增到用戶端連接列表
                clientConnections.Add(new ClientConnection
                {
                    Id = e.ClientId,
                    IpAddress = e.ClientIp,
                    ConnectTime = DateTime.Now,
                    LastActivityTime = DateTime.Now,
                    RequestType = "新連接"
                });
            });
        }/// <summary>
        /// 更新用戶端最後活動時間及請求類型
        /// </summary>
        private void UpdateClientLastActivity(string clientId, string clientIp, string requestType = null)
        {
            this.Dispatcher.Invoke(() =>
            {
                var client = clientConnections.FirstOrDefault(c => c.Id == clientId);
                if (client == null)
                {
                    // 如果找不到對應ID的用戶端，可能是新連接，添加到列表
                    clientConnections.Add(new ClientConnection
                    {
                        Id = clientId,
                        IpAddress = clientIp,
                        ConnectTime = DateTime.Now,
                        LastActivityTime = DateTime.Now,
                        RequestType = requestType
                    });
                }
                else
                {
                    // 更新最後活動時間和請求類型
                    client.LastActivityTime = DateTime.Now;
                    if (!string.IsNullOrEmpty(requestType))
                    {
                        client.RequestType = requestType;
                    }
                }
            });
        }        /// <summary>
        /// 清除訊息顯示區
        /// </summary>
        private void ClearMessages()
        {
            txtServerMessages.Clear();
            txtClientMessages.Clear();
        }

        /// <summary>
        /// 更新狀態列文字
        /// </summary>
        private void UpdateStatus(string status)
        {
            txtStatus.Text = status;
        }

        private void btnApiGuide_Click(object sender, RoutedEventArgs e)
        {
            ApiGuideWindow apiGuideWindow = new ApiGuideWindow();
            apiGuideWindow.Show();
        }

        private void btnFlowChart_Click(object sender, RoutedEventArgs e)
        {
            FlowChartWindow flowChartWindow = new FlowChartWindow();
            flowChartWindow.Show();
        }

        /// <summary>
        /// 初始化計時器
        /// </summary>
        private void InitializeTimers()
        {
            // 初始化心跳計时器
            heartbeatTimer = new DispatcherTimer();
            heartbeatTimer.Interval = TimeSpan.FromSeconds(30); // 預設 30 秒一次心跳
            heartbeatTimer.Tick += HeartbeatTimer_Tick;

            // 初始化日期時間顯示計時器
            dateTimeTimer = new DispatcherTimer();
            dateTimeTimer.Interval = TimeSpan.FromSeconds(1);
            dateTimeTimer.Tick += DateTimeTimer_Tick;
            dateTimeTimer.Start();
        }        /// <summary>
        /// 初始化 API 請求範本
        /// </summary>
        private void InitializeApiTemplates()
        {
            try
            {
                apiTemplates = new Dictionary<string, string>();
                string templateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ApiTemplates.json");

                // 檢查範本檔是否存在
                if (!File.Exists(templateFilePath))
                {
                    // 如果檔案不存在，建立目錄
                    string templateDir = Path.GetDirectoryName(templateFilePath);
                    if (!Directory.Exists(templateDir))
                    {
                        Directory.CreateDirectory(templateDir);
                    }
                    // 使用預設範本
                    apiTemplates = new Dictionary<string, string>();
                    InitializeDefaultTemplates(apiTemplates); // Fills Dictionary<string, string>

                    // 儲存預設範本到檔案
                    var templatesToSave = new Dictionary<string, JObject>();
                    foreach(var kvp in apiTemplates)
                    {
                        try
                        {
                            templatesToSave[kvp.Key] = JObject.Parse(kvp.Value);
                        }
                        catch (JsonReaderException ex)
                        {
                            AppendServerLog($"警告：預設範本 '{kvp.Key}' 不是有效的 JSON，無法儲存。錯誤: {ex.Message}");
                        }
                    }
                    string jsonToSave = JsonConvert.SerializeObject(templatesToSave, Formatting.Indented);
                    File.WriteAllText(templateFilePath, jsonToSave);

                    AppendServerLog($"已建立預設 API 範本檔案：{templateFilePath}");
                    // Populate cmbTemplates after creating default templates
                    PopulateTemplateComboBox();
                }
                else
                {
                    // 從檔案讀取範本
                    string json = File.ReadAllText(templateFilePath);
                    // *** 修改開始：使用 JToken 進行更靈活的反序列化 ***
                    var rawTemplates = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json);
                    apiTemplates = new Dictionary<string, string>(); // 重設最終的字典

                    foreach (var kvp in rawTemplates)
                    {
                        string key = kvp.Key;
                        JToken token = kvp.Value;
                        JObject currentTemplate = null;

                        if (token is JObject jobject) // 如果本身就是 JObject
                        {
                            currentTemplate = jobject;
                        }
                        else if (token is JValue jValue && jValue.Type == JTokenType.String) // 如果是包含字串的 JValue
                        {
                            // 嘗試將字串內容解析為 JObject
                            try
                            {
                                currentTemplate = JObject.Parse((string)jValue.Value);
                            }
                            catch (JsonReaderException ex)
                            {
                                AppendServerLog($"警告：範本 '{key}' 的值不是有效的 JSON 物件，將跳過。錯誤：{ex.Message}");
                                continue; // 跳過此範本
                            }
                        }
                        else // 其他無法處理的類型
                        {
                            AppendServerLog($"警告：範本 '{key}' 的值不是預期的 JSON 物件或包含 JSON 的字串，將跳過。類型：{token?.Type}");
                            continue; // 跳過此範本
                        }

                        // 如果成功獲得 JObject，則進行處理
                        if (currentTemplate != null)
                        {
                            // 安全地更新時間戳記
                            if (currentTemplate.ContainsKey("timeStamp"))
                            {
                                currentTemplate["timeStamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            }

                            // 安全地檢查和更新 data 陣列中的時間
                            if (currentTemplate.TryGetValue("data", out JToken dataToken) && dataToken is JArray dataArray && dataArray.Count > 0)
                            {
                                if (dataArray[0] is JObject firstDataItem) // 確保第一個元素是物件
                                {
                                    // 更新開始和結束時間（針對 EVENT_TIME_MESSAGE）
                                    if (key == "EVENT_TIME_MESSAGE")
                                    {
                                        if (firstDataItem.ContainsKey("startTime"))
                                        {
                                            firstDataItem["startTime"] = DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss");
                                        }
                                        if (firstDataItem.ContainsKey("endTime"))
                                        {
                                             firstDataItem["endTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                        }
                                    }

                                    // 更新報警開始時間（針對 DEVICE_WARNING_MESSAGE）
                                    if (key == "DEVICE_WARNING_MESSAGE")
                                    {
                                         if (firstDataItem.ContainsKey("start"))
                                         {
                                            firstDataItem["start"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                         }
                                    }
                                }
                            }

                            // 將修改後的 JObject 轉換回 JSON 字串並儲存
                            apiTemplates[key] = currentTemplate.ToString(Formatting.Indented);
                        }
                    }
                    // *** 修改結束 ***

                    AppendServerLog($"已載入 API 範本檔案：{templateFilePath}");
                    // Populate cmbTemplates after loading templates from file
                    PopulateTemplateComboBox();
                }
            }
            catch (Exception ex)
            {
                 // 顯示更詳細的錯誤訊息，包含內部例外狀況
                string errorMessage = $"載入 API 範本時發生錯誤：{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\r\n內部錯誤：{ex.InnerException.Message}";
                }
                errorMessage += "\r\n將使用預設範本。";

                MessageBox.Show(errorMessage, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                apiTemplates = new Dictionary<string, string>();
                InitializeDefaultTemplates(apiTemplates); // 確保這裡傳入的是 Dictionary<string, string>
                // Populate cmbTemplates even if loading failed and using defaults
                PopulateTemplateComboBox();
            }
        }

        /// <summary>
        /// 將載入的 API 範本填充到下拉選單中
        /// </summary>
        private void PopulateTemplateComboBox()
        {
             // 檢查是否需要在UI執行緒上執行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => PopulateTemplateComboBox());
                return;
            }

            cmbTemplates.Items.Clear(); // 清空現有項目
            if (apiTemplates != null)
            {
                foreach (var key in apiTemplates.Keys)
                {
                    // 查找對應的 API 功能名稱 (假設 ComboBoxItem 的 Content 是 "中文描述 (KEY)")
                    string displayName = key; // 預設使用 Key
                    foreach (ComboBoxItem funcItem in cmbApiFunction.Items)
                    {
                        if (funcItem.Tag?.ToString() == key)
                        {
                            displayName = funcItem.Content.ToString();
                            break;
                        }
                    }
                    cmbTemplates.Items.Add(new ComboBoxItem { Content = displayName, Tag = key });
                }
            }
             // 預設選中第一個範本 (如果有的話)
            if (cmbTemplates.Items.Count > 0)
            {
                cmbTemplates.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 心跳計時器事件處理
        /// </summary>
        private async void HeartbeatTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 產生心跳時間戳
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 更新心跳訊息中的時間戳
                dynamic heartbeatData = JsonConvert.DeserializeObject(apiTemplates["DEVICE_HEARTBEAT_MESSAGE"]);
                heartbeatData.timeStamp = timeStamp;
                heartbeatData.requestID = $"HEARTBEAT_{Guid.NewGuid().ToString().Substring(0, 8)}";
                
                // 發送心跳請求
                string jsonData = JsonConvert.SerializeObject(heartbeatData);
                await SendClientRequest("DEVICE_HEARTBEAT_MESSAGE", jsonData);
                
                // 更新 UI
                rectHeartbeatStatus.Fill = Brushes.Green;
                AppendClientLog($"[{timeStamp}] 已發送心跳訊息");
            }
            catch (Exception ex)
            {
                rectHeartbeatStatus.Fill = Brushes.Red;
                AppendClientLog($"發送心跳錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化預設的 API 範本
        /// </summary>
        /// <param name="templates">要填充的範本字典</param>
        private void InitializeDefaultTemplates(Dictionary<string, string> templates) // 確保參數類型正確
        {
            // 設備狀態資訊
            templates["DEVICE_STATUS_MESSAGE"] = @"{
  ""requestID"": ""STATUS_001"",
  ""serviceName"": ""DEVICE_STATUS_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""statusCode"": ""S001"",
      ""statusDesc"": ""設備正常運行中"",
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備報警資訊
            templates["DEVICE_WARNING_MESSAGE"] = @"{
  ""requestID"": ""WARNING_001"",
  ""serviceName"": ""DEVICE_WARNING_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""warningCode"": ""W001"",
      ""warningDesc"": ""溫度過高警告"",
      ""warningLevel"": ""中級"",
      ""start"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備生產實時數據監控
            templates["DEVICE_PARAM_REQUEST"] = @"{
  ""requestID"": ""PARAM_001"",
  ""serviceName"": ""DEVICE_PARAM_REQUEST"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""data"": [
    {
      ""activation"": 1,
      ""temperature"": 25.5,
      ""pressure"": 10.2,
      ""speed"": 60,
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備事件起止時間上傳
            templates["EVENT_TIME_MESSAGE"] = @"{
  ""requestID"": ""EVENT_001"",
  ""serviceName"": ""EVENT_TIME_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""startTime"": """ + DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss") + @""",
      ""endTime"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
      ""duration"": 3600,
      ""event"": 1,
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備聯網狀態監控功能
            templates["DEVICE_HEARTBEAT_MESSAGE"] = @"{
  ""requestID"": ""HEARTBEAT_001"",
  ""serviceName"": ""DEVICE_HEARTBEAT_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""heartBeat"": 1,
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備點檢參數報告
            templates["DEVICE_KEYCHECKING_REQUEST"] = @"{
  ""requestID"": ""KEYCHECK_001"",
  ""serviceName"": ""DEVICE_KEYCHECKING_REQUEST"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""data"": [
    {
      ""traceParams"": {
        ""toolA"": {
          ""usedCount"": 120,
          ""lifespan"": 1000
        },
        ""toolB"": {
          ""usedCount"": 45,
          ""lifespan"": 500
        }
      },
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備採集到盒子碼上報
            templates["DEVICE_VEHICLE_UPLOAD"] = @"{
  ""requestID"": ""VEHICLE_001"",
  ""serviceName"": ""DEVICE_VEHICLE_UPLOAD"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""vehicleCode"": ""BOX67890"",
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 盒子做完上報資訊接口
            templates["BATCH_COMPLETE_MESSAGE"] = @"{
  ""requestID"": ""COMPLETE_001"",
  ""serviceName"": ""BATCH_COMPLETE_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""data"": [
    {
      ""vehicleCode"": ""BOX67890"",
      ""okNum"": 95,
      ""ngNum"": 5,
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 良品盒生成完成上報
            templates["BATCH_REPORTED_MESSAGE"] = @"{
  ""requestID"": ""REPORTED_001"",
  ""serviceName"": ""BATCH_REPORTED_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""data"": [
    {
      ""gfNum"": ""6"",
      ""pnum"": 10,
      ""vehicleCode"": ""BOX67890"",
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
        }

        /// <summary>
        /// 日期時間計時器事件處理
        /// </summary>
        private void DateTimeTimer_Tick(object sender, EventArgs e)
        {
            txtDateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 模式選擇變更事件處理
        /// </summary>
        private void cmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 根據選擇的模式設定當前操作模式
            switch (cmbMode.SelectedIndex)
            {
                case 0:
                    currentMode = OperationMode.DualMode;
                    break;
                case 1:
                    currentMode = OperationMode.ServerMode;
                    break;
                case 2:
                    currentMode = OperationMode.ClientMode;
                    break;
            }
            
            // 更新 UI
            UpdateUIByMode();
        }

        /// <summary>
        /// 根據選擇的模式更新 UI 狀態
        /// </summary>
        private void UpdateUIByMode()
        {
            // 清空訊息顯示 (加入空值檢查避免 NullReferenceException)
            if (txtServerMessages != null)
                txtServerMessages.Clear();

            if (txtClientMessages != null)
                txtClientMessages.Clear();
            if(grpServerControl!=null && grpClientControl != null)
            {
                switch (currentMode)
                {
                    case OperationMode.DualMode:
                        // 雙向模式下，伺服端和用戶端都可用
                        grpServerControl.IsEnabled = true;
                        grpClientControl.IsEnabled = true;
                        UpdateStatus("雙向模式：可同時作為伺服端接收請求和作為用戶端發送請求");
                        break;

                    case OperationMode.ServerMode:
                        // 伺服端模式下，只有伺服端控制可用
                        grpServerControl.IsEnabled = true;
                        grpClientControl.IsEnabled = false;
                        StopHeartbeat();
                        UpdateStatus("伺服端模式：只能接收請求，不能發送請求");
                        break;

                    case OperationMode.ClientMode:
                        // 用戶端模式下，只有用戶端控制可用
                        grpServerControl.IsEnabled = false;
                        grpClientControl.IsEnabled = true;
                        StopHttpServer();
                        UpdateStatus("用戶端模式：只能發送請求，不能接收請求");
                        break;
                }
            }
           
        }

        /// <summary>
        /// 範本選擇變更事件處理
        /// </summary>
        private void cmbTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTemplates.SelectedItem != null)
            {
                // 載入選中的 API 範本
                string templateName = (cmbTemplates.SelectedItem as ComboBoxItem).Content.ToString();
                LoadApiTemplate(templateName);
            }
        }

        /// <summary>
        /// 載入 API 範本
        /// </summary>
        private void LoadApiTemplate(string templateName)
        {
            // 從範本名稱提取 API 類型
            string apiType = templateName;
            if (templateName.Contains("(") && templateName.Contains(")"))
            {
                apiType = templateName.Substring(templateName.IndexOf("(") + 1, templateName.IndexOf(")") - templateName.IndexOf("(") - 1);
            }

            // 設定範本內容
            if (apiTemplates.ContainsKey(apiType))
            {
                txtTemplate.Text = apiTemplates[apiType];
                
                // 同時將 API 功能下拉選單選中相同的項目
                for (int i = 0; i < cmbApiFunction.Items.Count; i++)
                {
                    ComboBoxItem item = cmbApiFunction.Items[i] as ComboBoxItem;
                    if (item.Tag.ToString() == apiType)
                    {
                        cmbApiFunction.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 套用範本按鈕點擊事件
        /// </summary>
        private void btnApplyTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (cmbApiFunction.SelectedItem != null && cmbTemplates.SelectedItem != null)
            {
                string apiType = (cmbApiFunction.SelectedItem as ComboBoxItem).Tag.ToString();
                
                // 更新 API 範本
                if (!string.IsNullOrWhiteSpace(txtTemplate.Text))
                {
                    try
                    {
                        // 檢查 JSON 格式是否有效
                        dynamic jsonObj = JsonConvert.DeserializeObject(txtTemplate.Text);
                        apiTemplates[apiType] = txtTemplate.Text;
                        
                        MessageBox.Show("範本已成功套用至選中的 API 功能", "套用成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"JSON 格式錯誤: {ex.Message}", "格式錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 儲存範本按鈕點擊事件
        /// </summary>
        private void btnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtTemplate.Text))
            {
                try
                {
                    // 檢查 JSON 格式是否有效
                    dynamic jsonObj = JsonConvert.DeserializeObject(txtTemplate.Text);
                    
                    // 建立儲存對話框
                    Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
                    saveDialog.Filter = "JSON 檔案 (*.json)|*.json|所有檔案 (*.*)|*.*";
                    saveDialog.Title = "儲存 API 請求範本";
                    saveDialog.FileName = $"API_Template_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    
                    // 顯示儲存對話框
                    if (saveDialog.ShowDialog() == true)
                    {
                        // 儲存檔案
                        using (StreamWriter writer = new StreamWriter(saveDialog.FileName))
                        {
                            writer.Write(txtTemplate.Text);
                        }
                        
                        MessageBox.Show($"範本已儲存至: {saveDialog.FileName}", "儲存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"JSON 格式錯誤: {ex.Message}", "格式錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"儲存檔案時發生錯誤: {ex.Message}", "儲存錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 設定資料按鈕點擊事件
        /// </summary>
        private void btnConfigData_Click(object sender, RoutedEventArgs e)
        {
            // 獲取當前選中的 API 功能
            if (cmbApiFunction.SelectedItem != null)
            {
                string apiType = (cmbApiFunction.SelectedItem as ComboBoxItem).Tag.ToString();
                
                // 顯示資料設定視窗 (這裡使用範本編輯作為替代)
                if (apiTemplates.ContainsKey(apiType))
                {
                    txtTemplate.Text = apiTemplates[apiType];
                }                // 切換到 API 範本編輯頁籤
                // 使用 VisualTreeHelper 取得父元素，避免 Parent 屬性不存在的問題
                try
                {
                    // 尋找 TabControl 父元素
                    DependencyObject current = txtTemplate;
                    TabControl tabControl = null;
                    
                    // 向上尋找視覺化樹中的 TabControl 元素
                    while (current != null && tabControl == null)
                    {
                        current = VisualTreeHelper.GetParent(current);
                        tabControl = current as TabControl;
                    }
                    
                    // 如果找到 TabControl，則切換到 API 範本頁籤
                    if (tabControl != null)
                    {
                        tabControl.SelectedIndex = 3; // 假設 API 請求範本是第 4 個頁籤
                    }
                    else
                    {
                        // 如果找不到 TabControl，顯示警告但不終止程式
                        System.Diagnostics.Debug.WriteLine("警告：無法找到包含 txtTemplate 的 TabControl");
                    }
                }
                catch (Exception ex)
                {
                    // 處理任何其他例外狀況
                    System.Diagnostics.Debug.WriteLine($"切換頁籤時發生錯誤: {ex.Message}");
                }
                
                MessageBox.Show("請在範本編輯區修改 API 請求資料，修改後點擊「套用範本」按鈕儲存。", "設定資料", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 發送請求按鈕點擊事件
        /// </summary>
        private async void btnSendRequest_Click(object sender, RoutedEventArgs e)
        {
            if (cmbApiFunction.SelectedItem == null)
            {
                MessageBox.Show("請先選擇 API 功能", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 獲取當前選中的 API 功能類型
                string apiType = (cmbApiFunction.SelectedItem as ComboBoxItem).Tag.ToString();
                
                // 獲取對應的範本資料
                string jsonData = apiTemplates[apiType];
                
                // 更新請求 ID 和時間戳
                dynamic requestData = JsonConvert.DeserializeObject(jsonData);
                requestData.requestID = $"{apiType.Substring(0, 4)}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                requestData.timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                requestData.devCode = txtDevCode.Text;
                
                // 發送請求
                string updatedJsonData = JsonConvert.SerializeObject(requestData);
                await SendClientRequest(apiType, updatedJsonData);
                
                // 更新用戶端狀態指示器
                rectClientStatus.Fill = Brushes.Green;
                await Task.Delay(500); // 閃爍效果
                rectClientStatus.Fill = Brushes.Gray;
            }
            catch (Exception ex)
            {
                rectClientStatus.Fill = Brushes.Red;
                AppendClientLog($"發送請求錯誤: {ex.Message}");
                MessageBox.Show($"發送請求失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 開始心跳按鈕點擊事件
        /// </summary>
        private void btnStartHeartbeat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 設定心跳間隔 (預設 30 秒)
                heartbeatTimer.Interval = TimeSpan.FromSeconds(30);
                
                // 啟動心跳
                heartbeatTimer.Start();
                
                // 更新 UI
                btnStartHeartbeat.IsEnabled = false;
                btnStopHeartbeat.IsEnabled = true;
                rectHeartbeatStatus.Fill = Brushes.Yellow;
                
                // 記錄日誌
                AppendClientLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 已啟動心跳服務，間隔 30 秒");
                
                // 立即發送一次心跳
                HeartbeatTimer_Tick(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"啟動心跳服務失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 停止心跳按鈕點擊事件
        /// </summary>
        private void btnStopHeartbeat_Click(object sender, RoutedEventArgs e)
        {
            StopHeartbeat();
        }

        /// <summary>
        /// 停止心跳服務
        /// </summary>
        private void StopHeartbeat()
        {
            if (heartbeatTimer.IsEnabled)
            {
                heartbeatTimer.Stop();
                rectHeartbeatStatus.Fill = Brushes.Gray;
                btnStartHeartbeat.IsEnabled = true;
                btnStopHeartbeat.IsEnabled = false;
                AppendClientLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 已停止心跳服務");
            }
        }
        
        /// <summary>
        /// 發送客戶端請求
        /// </summary>
        private async Task<BaseResponse<object>> SendClientRequest(string apiType, string jsonData)
        {
            string iotEndpoint = txtIotEndpoint.Text.TrimEnd('/');
            string devCode = txtDevCode.Text;
            
            // 記錄發送的請求
            AppendClientLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 發送 {apiType} 請求:");
            AppendClientLog($"{jsonData}");
            
            try
            {
                // 根據 API 類型決定調用哪個方法
                BaseResponse<object> response = null;
                
                // 解析 JSON 數據
                dynamic requestData = JsonConvert.DeserializeObject(jsonData);
                
                switch (apiType)
                {
                    case "DEVICE_STATUS_MESSAGE":
                        var statusData = new DeviceStatusData
                        {
                            StatusCode = requestData.data[0].statusCode,
                            StatusDesc = requestData.data[0].statusDesc,
                            ExtendData = requestData.data[0].extendData
                        };
                        response = await httpServer.SendDeviceStatus(statusData, iotEndpoint, devCode);
                        break;
                        
                    case "DEVICE_WARNING_MESSAGE":
                        var warningData = new DeviceWarningData
                        {
                            WarningCode = requestData.data[0].warningCode,
                            WarningDesc = requestData.data[0].warningDesc,
                            WarningLevel = requestData.data[0].warningLevel,
                            Start = requestData.data[0].start,
                            ExtendData = requestData.data[0].extendData
                        };
                        response = await httpServer.SendDeviceWarning(warningData, iotEndpoint, devCode);
                        break;                    
                    case "DEVICE_PARAM_REQUEST":
                        // 建立一個動態參數物件
                        var paramData = new DeviceParamData
                        {
                            Activation = requestData.data[0].activation,
                            AdditionalData = new Dictionary<string, JToken>() // 確保 AdditionalData 已初始化為正確類型
                        };
                        // 複製其他動態屬性
                        foreach (var property in requestData.data[0])
                        {
                            if (property.Name != "activation" && property.Name != "extendData")
                            {
                                paramData.AdditionalData[property.Name] = property.Value;
                            }
                        }
                        response = await httpServer.SendDeviceParameters(paramData, iotEndpoint, devCode);
                        break;
                        
                    case "EVENT_TIME_MESSAGE":
                        var eventTimeData = new EventTimeData
                        {
                            StartTime = requestData.data[0].startTime,
                            EndTime = requestData.data[0].endTime,
                            Duration = requestData.data[0].duration,
                            Event = requestData.data[0].@event,
                            ExtendData = requestData.data[0].extendData
                        };
                        string eventOperator = requestData.@operator == null ? string.Empty : requestData.@operator.ToString();
                        var eventTimeResp = await httpServer.SendEventTime(eventTimeData, iotEndpoint, devCode, eventOperator);
                        response = new BaseResponse<object> {
                            Status = eventTimeResp.Status,
                            Message = eventTimeResp.Message,
                            Data = eventTimeResp.Data != null ? eventTimeResp.Data.Cast<object>().ToList() : null,
                            RequestID = eventTimeResp.RequestID,
                            ServiceName = eventTimeResp.ServiceName,
                            TimeStamp = eventTimeResp.TimeStamp,
                            DevCode = eventTimeResp.DevCode,
                            ExtendData = eventTimeResp.ExtendData
                        };
                        break;
                    case "DEVICE_HEARTBEAT_MESSAGE":
                        var heartbeatData = new DeviceHeartbeatData
                        {
                            HeartBeat = requestData.data[0].heartBeat,
                            ExtendData = requestData.data[0].extendData
                        };
                        string heartbeatOperator = requestData.@operator == null ? string.Empty : requestData.@operator.ToString();
                        var heartbeatResp = await httpServer.SendHeartbeat(heartbeatData, iotEndpoint, devCode, heartbeatOperator);
                        response = new BaseResponse<object> {
                            Status = heartbeatResp.Status,
                            Message = heartbeatResp.Message,
                            Data = heartbeatResp.Data != null ? heartbeatResp.Data.Cast<object>().ToList() : null,
                            RequestID = heartbeatResp.RequestID,
                            ServiceName = heartbeatResp.ServiceName,
                            TimeStamp = heartbeatResp.TimeStamp,
                            DevCode = heartbeatResp.DevCode,
                            ExtendData = heartbeatResp.ExtendData
                        };
                        break;
                        
                    case "DEVICE_KEYCHECKING_REQUEST":
                        var keyCheckingData = new DeviceKeycheckingData
                        {
                            TraceParams = requestData.data[0].traceParams,
                            ExtendData = requestData.data[0].extendData
                        };
                        response = await httpServer.SendDeviceKeyChecking(keyCheckingData, iotEndpoint, devCode);
                        break;
                        
                    case "DEVICE_VEHICLE_UPLOAD":
                        var vehicleData = new DeviceVehicleUploadData
                        {
                            VehicleCode = requestData.data[0].vehicleCode,
                            ExtendData = requestData.data[0].extendData
                        };
                        // 修正：若 requestData.@operator 為 null，則傳空字串
                        string operatorValue = requestData.@operator == null ? string.Empty : requestData.@operator.ToString();
                        response = await httpServer.UploadVehicleCode(vehicleData, iotEndpoint, devCode, operatorValue);
                        break;
                        
                    case "BATCH_COMPLETE_MESSAGE":
                        var batchCompleteData = new BatchCompleteData
                        {
                            VehicleCode = requestData.data[0].vehicleCode,
                            OkNum = requestData.data[0].okNum,
                            NgNum = requestData.data[0].ngNum,
                            ExtendData = requestData.data[0].extendData
                        };
                        response = await httpServer.SendBatchCompleteInfo(batchCompleteData, iotEndpoint, devCode);
                        break;
                        
                    case "BATCH_REPORTED_MESSAGE":
                        var batchReportedData = new BatchReportedData
                        {
                            GfNum = requestData.data[0].gfNum,
                            Pnum = requestData.data[0].pnum,
                            VehicleCode = requestData.data[0].vehicleCode,
                            ExtendData = requestData.data[0].extendData
                        };
                        response = await httpServer.SendBatchReportedInfo(batchReportedData, iotEndpoint, devCode);
                        break;
                        
                    default:
                        AppendClientLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 錯誤: 未知的 API 類型 {apiType}");
                        throw new ArgumentException($"不支援的 API 類型: {apiType}");
                }
                
                // 記錄回應
                if (response != null)
                {
                    AppendClientLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 收到回應:");
                    AppendClientLog($"狀態碼: {response.Status}");
                    AppendClientLog($"訊息: {response.Message}");
                    AppendClientLog(JsonConvert.SerializeObject(response, Formatting.Indented));
                    AppendClientLog(string.Empty); // 空行分隔
                }
                
                return response;
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                AppendClientLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 請求失敗: {ex.Message}");
                AppendClientLog(ex.StackTrace);
                AppendClientLog(string.Empty); // 空行分隔
                
                throw; // 重新拋出異常以便上層處理
            }
        }
        
        /// <summary>
        /// 將日誌添加到伺服端記錄區
        /// </summary>
        private void AppendServerLog(string message)
        {
            // 檢查是否需要在UI執行緒上執行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendServerLog(message));
                return;
            }

            txtServerMessages.AppendText(message + Environment.NewLine);
            txtServerMessages.ScrollToEnd();
        }
        
        /// <summary>
        /// 將日誌添加到用戶端記錄區
        /// </summary>
        private void AppendClientLog(string message)
        {
            // 檢查是否需要在UI執行緒上執行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendClientLog(message));
                return;
            }

            txtClientMessages.AppendText(message + Environment.NewLine);
            txtClientMessages.ScrollToEnd();
        }
    }
}
