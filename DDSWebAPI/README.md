# DDSWebAPI 函式庫

## 概述

DDSWebAPI 是一個專為配針機與 MES/IoT 系統通訊而設計的 .NET 函式庫。它提供了完整的 HTTP 伺服器和用戶端功能，支援 DDSWebAPIRule.md 中定義的所有 API 規範。

## 主要功能

### 🔧 伺服端功能（接收 MES/IoT 指令）
- 遠程資訊下發指令 (SEND_MESSAGE_COMMAND)
- 派針工單建立指令 (CREATE_NEEDLE_WORKORDER_COMMAND)
- 設備時間同步指令 (DATE_MESSAGE_COMMAND)
- 刀具工鑽袋檔發送指令 (SWITCH_RECIPE_COMMAND)
- 設备啟停控制指令 (DEVICE_CONTROL_COMMAND)
- 倉庫資源查詢指令 (WAREHOUSE_RESOURCE_QUERY_COMMAND)
- 鑽針履歷查詢指令 (TOOL_TRACE_HISTORY_QUERY_COMMAND)

### 📤 用戶端功能（向 MES/IoT 發送資料）
- 配針回報上傳 (TOOL_OUTPUT_REPORT_MESSAGE)
- 錯誤回報上傳 (ERROR_REPORT_MESSAGE)
- 機臺狀態上報 (MACHINE_STATUS_REPORT_MESSAGE)

### ✨ 其他特色
- 支援平行處理和高延展性
- 完整的事件驅動架構
- 自動用戶端連接管理
- 詳細的日誌記錄功能
- 錯誤處理和重試機制

## 快速開始

### 1. 安裝函式庫

在您的專案中加入對 DDSWebAPI 專案的參考：

```xml
<ProjectReference Include="DDSWebAPI\DDSWebAPI.csproj">
  <Project>{B8A5F234-8C7D-4A9B-9E12-3F45D6E789AB}</Project>
  <Name>DDSWebAPI</Name>
</ProjectReference>
```

### 2. 基本使用範例

```csharp
using DDSWebAPI.Services;
using DDSWebAPI.Models;

// 建立 DDS API 服務
var ddsService = new DDSWebAPIService(
    serverUrl: "http://localhost:8085/",
    remoteApiUrl: "http://localhost:8086/",
    deviceCode: "KINSUS001",
    operatorName: "OP001"
);

// 註冊事件處理程式
ddsService.MessageReceived += OnMessageReceived;
ddsService.ServerStatusChanged += OnServerStatusChanged;
ddsService.LogMessage += OnLogMessage;

// 啟動 HTTP 伺服器
bool success = await ddsService.StartServerAsync();
if (success)
{
    Console.WriteLine("伺服器啟動成功！");
}
```

### 3. 發送資料到 MES/IoT 系統

```csharp
// 發送配針回報
var toolReport = new ToolOutputReportData
{
    WorkOrder = "WO001",
    ToolId = "TOOL001",
    Result = "success",
    ProcessTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
    Quantity = 100
};

var result = await ddsService.SendToolOutputReportAsync(toolReport);
if (result.IsSuccess)
{
    Console.WriteLine("配針回報發送成功！");
}
```

### 4. 處理接收到的訊息

```csharp
private void OnMessageReceived(object sender, MessageEventArgs e)
{
    Console.WriteLine($"收到來自 {e.ClientIp} 的訊息:");
    Console.WriteLine(e.Message);
    
    // 可以根據訊息內容進行相應處理
    try
    {
        dynamic jsonData = JsonConvert.DeserializeObject(e.Message);
        string serviceName = jsonData.serviceName;
        
        switch (serviceName)
        {
            case ApiServiceNames.SendMessageCommand:
                ProcessSendMessageCommand(jsonData);
                break;
            case ApiServiceNames.CreateNeedleWorkorderCommand:
                ProcessCreateWorkorderCommand(jsonData);
                break;
            // ... 其他處理邏輯
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"處理訊息時發生錯誤: {ex.Message}");
    }
}
```

## 架構設計

### 核心類別

#### DDSWebAPIService
主要服務類別，提供完整的 API 功能管理：
- HTTP 伺服器管理
- API 用戶端管理  
- 用戶端連接追蹤
- 事件統一處理

#### HttpServerService
HTTP 伺服器服務，負責：
- 接收和處理 HTTP 請求
- 用戶端連接管理
- API 請求路由和處理

#### ApiClientService
API 用戶端服務，負責：
- 向外部系統發送 HTTP 請求
- 請求結果處理和錯誤處理
- 連接超時和重試管理

### 資料模型

所有 API 資料模型都位於 `DDSWebAPI.Models` 命名空間中：

- `BaseRequest<T>` / `BaseSingleRequest<T>` - 基礎請求類別
- `BaseResponse<T>` / `BaseSingleResponse<T>` - 基礎回應類別
- `ApiDataModels.cs` - 包含所有具體的 API 資料模型
- `ClientConnection` - 用戶端連接資訊
- `MessageEventArgs` 等 - 事件參數類別

## WPF 整合範例

參考 `MainWindow_Refactored.xaml.cs` 檔案，了解如何在 WPF 應用程式中使用 DDSWebAPI：

```csharp
public partial class MainWindow : Window
{
    private DDSWebAPIService _ddsApiService;

    public MainWindow()
    {
        InitializeComponent();
        
        // 初始化 DDS API 服務
        _ddsApiService = new DDSWebAPIService();
        
        // 註冊事件
        _ddsApiService.MessageReceived += OnDDSAPIMessageReceived;
        _ddsApiService.ServerStatusChanged += OnDDSAPIServerStatusChanged;
        
        // 綁定資料
        dgClients.ItemsSource = _ddsApiService.ClientConnections;
    }

    private async void btnConnect_Click(object sender, RoutedEventArgs e)
    {
        bool success = await _ddsApiService.StartServerAsync();
        // 更新 UI 狀態...
    }
}
```

## 事件處理

### 伺服端事件

```csharp
// 訊息接收
ddsService.MessageReceived += (sender, e) => {
    Console.WriteLine($"收到訊息: {e.Message}");
};

// 用戶端連接
ddsService.ClientConnected += (sender, e) => {
    Console.WriteLine($"用戶端連接: {e.ClientIp}");
};

// 伺服器狀態變更
ddsService.ServerStatusChanged += (sender, e) => {
    Console.WriteLine($"伺服器狀態: {e.Status} - {e.Description}");
};
```

### 用戶端事件

```csharp
// API 呼叫成功
ddsService.ApiCallSuccess += (sender, e) => {
    Console.WriteLine($"API 呼叫成功: {e.Result.RequestUrl}");
};

// API 呼叫失敗
ddsService.ApiCallFailure += (sender, e) => {
    Console.WriteLine($"API 呼叫失敗: {e.Result.ErrorMessage}");
};
```

## 設定參數

### 基本設定

```csharp
ddsService.ServerUrl = "http://localhost:8085/";      // 伺服器監聽位址
ddsService.RemoteApiUrl = "http://localhost:8086/";   // 遠端 API 位址
ddsService.DeviceCode = "KINSUS001";                  // 設備代碼
ddsService.OperatorName = "OP001";                    // 操作人員
```

### 進階設定

```csharp
// 使用自訂參數建立服務
var ddsService = new DDSWebAPIService(
    serverUrl: "http://0.0.0.0:8085/",
    remoteApiUrl: "http://mes-system:8080/api/",
    deviceCode: "KINSUS_PROD_001",
    operatorName: "SYSTEM_AUTO"
);

// 動態更新設定
ddsService.ReloadConfiguration();
```

## 錯誤處理

### API 呼叫錯誤處理

```csharp
var result = await ddsService.SendToolOutputReportAsync(reportData);

if (!result.IsSuccess)
{
    Console.WriteLine($"發送失敗: {result.ErrorMessage}");
    
    if (result.Exception != null)
    {
        Console.WriteLine($"異常詳情: {result.Exception}");
    }
    
    // 根據狀態碼進行不同處理
    switch (result.StatusCode)
    {
        case 400:
            // 請求參數錯誤
            break;
        case 500:
            // 伺服器內部錯誤
            break;
        case 0:
            // 網路連接錯誤
            break;
    }
}
```

### 全域錯誤處理

```csharp
ddsService.LogMessage += (sender, e) => {
    if (e.Level == LogLevel.Error)
    {
        // 記錄錯誤日誌
        LogError(e.Message);
        
        // 發送錯誤通知
        NotifyError(e.Message);
    }
};
```

## 最佳實務

### 1. 資源管理

```csharp
// 使用 using 語句確保資源正確釋放
using (var ddsService = new DDSWebAPIService())
{
    // 使用服務...
}

// 或在應用程式關閉時手動釋放
private void Application_Exit(object sender, ExitEventArgs e)
{
    _ddsApiService?.StopServer();
    _ddsApiService?.Dispose();
}
```

### 2. 執行緒安全

DDSWebAPI 函式庫內部已處理執行緒安全問題，但在 UI 更新時仍需注意：

```csharp
private void OnMessageReceived(object sender, MessageEventArgs e)
{
    // 在 UI 執行緒上更新介面
    Dispatcher.Invoke(() => {
        txtMessages.AppendText(e.Message);
    });
}
```

### 3. 效能最佳化

```csharp
// 批次發送多個請求
var tasks = new List<Task<ApiCallResult>>();
foreach (var report in reports)
{
    tasks.Add(ddsService.SendToolOutputReportAsync(report));
}

var results = await Task.WhenAll(tasks);
```

## 故障排除

### 常見問題

1. **伺服器啟動失敗**
   - 檢查埠號是否被占用
   - 確認防火牆設定
   - 檢查 URL 前綴格式

2. **API 呼叫失敗**
   - 檢查網路連線
   - 確認遠端 API 位址正確
   - 檢查請求資料格式

3. **記憶體洩漏**
   - 確保正確釋放 DDSWebAPIService
   - 取消註冊事件處理程式

### 偵錯技巧

```csharp
// 啟用詳細日誌
ddsService.LogMessage += (sender, e) => {
    Debug.WriteLine($"[{e.Level}] {e.Message}");
};

// 檢查 API 請求內容
ddsService.ApiCallSuccess += (sender, e) => {
    Debug.WriteLine($"請求: {e.Result.RequestBody}");
    Debug.WriteLine($"回應: {e.Result.ResponseBody}");
};
```

## API 參考

詳細的 API 規範請參考 `Document/DDSWebAPIRule.md` 檔案。

## 授權

此函式庫由 KINSUS 開發，僅供授權使用。

## 支援

如有問題或建議，請聯繫開發團隊。

## 🧪 測試專案狀態 (2025-06-14 更新)

### ✅ 重大進展
- **編譯錯誤**: 從 77 個減少到 **0 個** ✅
- **測試專案**: 完全可編譯，具備完整測試覆蓋
- **資料模型**: 統一使用 `ApiDataModels.cs` 中的正確類別
- **屬性映射**: 修正所有資料模型屬性名稱對應

### 📊 測試覆蓋範圍
- **單元測試**: 60+ 測試方法涵蓋核心模型與服務
- **整合測試**: 10+ 測試方法驗證系統互動
- **測試檔案**: 
  - Models: BaseResponse, BaseRequest, WorkorderModels
  - Services: ApiClient, MesClient, ApiRequestHandler

### ⚠️ 已知問題
- NUnit 框架與 .NET Framework 4.8 相容性問題
- 需要調整測試適配器版本或升級目標框架

詳細資訊請參考: [測試專案最終狀態報告](DDSWebAPI.Tests/FINAL_STATUS_REPORT.md)

---
