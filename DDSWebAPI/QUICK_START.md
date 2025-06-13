# DDSWebAPI HTTP 伺服器服務 - 快速開始指南

## 🚀 快速開始

### 1. 專案概述
這是一個現代化的 C# HTTP 伺服器服務，專為 MES/IoT 系統設計，採用相依性注入和模組化架構。

### 2. 主要特性
- ✅ 相依性注入架構
- ✅ 模組化設計 (關注點分離)
- ✅ 完整的事件系統
- ✅ WebSocket 即時通訊
- ✅ 靜態檔案服務
- ✅ 豐富的 API 端點
- ✅ 詳細的 XML 註解
- ✅ 異常處理和資源管理

### 3. 專案結構
```
DDSWebAPI/
├── Interfaces/          # 服務介面
├── Models/              # 資料模型
│   ├── Requests/        # 請求模型
│   └── Storage/         # 儲存模型
├── Events/              # 事件參數類別
├── Enums/               # 列舉定義
├── Services/            # 服務實作
│   └── Handlers/        # 專門處理器
└── Examples/            # 使用範例
```

## 📋 基本使用方法

### 最簡單的用法 (無相依性注入)
```csharp
using DDSWebAPI.Services;

// 建立 HTTP 伺服器 (使用預設設定)
var server = new HttpServerService();

// 啟動伺服器
await server.StartAsync();

// 等待...
Console.ReadKey();

// 停止伺服器
server.Stop();
server.Dispose();
```

### 使用相依性注入 (建議用法)
```csharp
using DDSWebAPI.Services;
using DDSWebAPI.Examples.Services;

// 建立服務實例
var databaseService = new ExampleDatabaseService();
var warehouseService = new ExampleWarehouseQueryService(databaseService);
var workflowService = new ExampleWorkflowTaskService();
var configService = new ExampleGlobalConfigService();
var utilityService = new ExampleUtilityService();

// 建立 HTTP 伺服器並注入服務
var server = new HttpServerService(
    "http://localhost:8085/",     // 監聽位址
    "./wwwroot",                  // 靜態檔案目錄
    databaseService,              // 資料庫服務
    warehouseService,             // 倉庫查詢服務
    workflowService,              // 工作流程服務
    configService,                // 配置服務
    utilityService                // 工具服務
);

// 註冊事件處理器
server.ServerStatusChanged += (sender, e) => 
    Console.WriteLine($"狀態: {e.Status} - {e.Message}");

// 啟動伺服器
await server.StartAsync();
```

## 🔧 API 端點說明

### 標準 MES API
- `POST /api/mes` - 統一 MES API 端點，根據 serviceName 路由

### 倉庫管理 API
- `POST /api/in-material` - 入料操作
- `POST /api/out-material` - 出料操作  
- `POST /api/getlocationbystorage` - 根據儲存位置查詢
- `POST /api/getlocationbypin` - 根據 PIN 查詢位置
- `GET /api/out-getpins` - 取得出料 PIN 資料

### 設備控制 API
- `POST /api/operationclamp` - 夾爪操作控制
- `POST /api/changespeed` - 機器人速度調整

### 系統管理 API
- `GET /api/health` - 健康檢查
- `GET /api/server/statistics` - 伺服器統計資料
- `POST /api/server/status` - 伺服器狀態
- `POST /api/server/restart` - 重新啟動伺服器

## 📝 API 請求範例

### 健康檢查
```bash
curl -X GET http://localhost:8085/api/health
```

### 入料操作
```bash
curl -X POST http://localhost:8085/api/in-material \
  -H "Content-Type: application/json" \
  -d '{
    "requestID": "REQ001",
    "serviceName": "InMaterial",
    "timeStamp": "2025-06-13T10:00:00",
    "data": {
      "itemCode": "ITEM001",
      "quantity": 100,
      "location": "A01-01"
    }
  }'
```

### 速度調整
```bash
curl -X POST http://localhost:8085/api/changespeed \
  -H "Content-Type: application/json" \
  -d '{
    "requestID": "REQ002",
    "serviceName": "ChangeSpeed",
    "data": {
      "speedPercentage": 75
    }
  }'
```

## 🌐 WebSocket 使用

### JavaScript 用戶端範例
```javascript
const ws = new WebSocket('ws://localhost:8085/');

ws.onopen = function() {
    console.log('WebSocket 連接已建立');
    ws.send('Hello Server!');
};

ws.onmessage = function(event) {
    console.log('收到訊息:', event.data);
};

ws.onclose = function() {
    console.log('WebSocket 連接已關閉');
};
```

## 🎯 事件處理

### 註冊事件處理器
```csharp
// 伺服器狀態變更
server.ServerStatusChanged += (sender, e) => {
    Console.WriteLine($"狀態變更: {e.Status} - {e.Message}");
};

// 用戶端連接
server.ClientConnected += (sender, e) => {
    Console.WriteLine($"用戶端連接: {e.ClientIp}");
};

// 訊息接收
server.MessageReceived += (sender, e) => {
    Console.WriteLine($"收到訊息: {e.Message}");
};

// WebSocket 訊息
server.WebSocketMessageReceived += (sender, e) => {
    Console.WriteLine($"WebSocket 訊息: {e.MessageType}");
};

// 自訂 API 處理
server.CustomApiRequest += (sender, e) => {
    if (e.Path == "/api/custom") {
        // 處理自訂 API
        e.IsHandled = true;
    }
};
```

## 🔧 自訂服務實作

### 實作資料庫服務
```csharp
public class MyDatabaseService : IDatabaseService
{
    public async Task<List<T>> QueryAsync<T>(string sql)
    {
        // 實作您的資料庫查詢邏輯
        // 例如使用 Entity Framework、Dapper 等
        return new List<T>();
    }
    
    public async Task<int> ExecuteAsync(string sql)
    {
        // 實作資料庫執行邏輯
        return 0;
    }
}
```

### 實作倉庫服務
```csharp
public class MyWarehouseService : IWarehouseQueryService
{
    public async Task<bool> ProcessInMaterialAsync(string itemCode, int quantity, string location)
    {
        // 實作入料邏輯
        return true;
    }
    
    // 實作其他必要方法...
}
```

## 📂 範例程式

查看 `Examples/` 目錄下的範例程式:
- `HttpServerServiceExample.cs` - 基本使用範例
- `ServiceImplementations.cs` - 服務實作範例
- `CompleteExample.cs` - 完整整合範例

### 執行範例
```csharp
// 執行基本範例
var basicExample = new HttpServerServiceExample();
await basicExample.RunExampleAsync();

// 執行完整範例 (包含所有服務)
var completeExample = new CompleteExample();
await completeExample.RunCompleteExampleAsync();
```

## 🐛 常見問題

### Q: 伺服器啟動失敗，顯示「拒絕存取」錯誤
A: 請以系統管理員身分執行，或使用 netsh 指令配置 URL 權限:
```bash
netsh http add urlacl url=http://localhost:8085/ user=Everyone
```

### Q: 如何變更監聽埠號？
A: 在建立 HttpServerService 時指定不同的 URL:
```csharp
var server = new HttpServerService("http://localhost:9090/");
```

### Q: 如何新增自訂 API 端點？
A: 註冊 CustomApiRequest 事件處理器並在其中處理自訂邏輯。

### Q: 如何整合現有的資料庫？
A: 實作 IDatabaseService 介面，並在建立伺服器時注入您的實作。

## 📚 進階主題

### 單元測試
```csharp
[Test]
public async Task HttpServerService_Should_Start_Successfully()
{
    // Arrange
    var mockDatabase = new Mock<IDatabaseService>();
    var server = new HttpServerService(databaseService: mockDatabase.Object);
    
    // Act
    var result = await server.StartAsync();
    
    // Assert
    Assert.IsTrue(result);
}
```

### 日誌整合
在您的服務實作中整合 NLog、Serilog 等日誌函式庫。

### 效能監控
使用 .NET 效能計數器或 Application Insights 監控伺服器效能。

---

**版本**: 1.0.0  
**最後更新**: 2025年6月13日  
**技術支援**: 請參考程式碼註解和範例程式
