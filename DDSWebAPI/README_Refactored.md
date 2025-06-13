# DDSWebAPI 專案重構說明

## 概述

此專案已重構為現代 C# 程式碼架構，採用相依性注入設計模式，將原本的大型單一檔案拆分為多個小檔案，提升程式碼的可維護性、可測試性和可擴充性。

## 專案結構

```
DDSWebAPI/
├── Interfaces/                     # 相依性注入介面定義
│   ├── IDatabaseService.cs         # 資料庫服務介面
│   ├── IWarehouseQueryService.cs   # 倉庫查詢服務介面
│   ├── IWorkflowTaskService.cs     # 工作流程任務服務介面
│   ├── IGlobalConfigService.cs     # 全域配置服務介面
│   └── IUtilityService.cs          # 公用程式服務介面
├── Models/                         # 資料模型
│   ├── Requests/                   # API 請求模型
│   │   ├── SpeedRequest.cs         # 速度變更請求
│   │   ├── ClampRequest.cs         # 夾具操作請求
│   │   ├── InMaterialRequest.cs    # 入料請求
│   │   └── OutMaterialRequest.cs   # 出料請求
│   ├── Storage/                    # 儲存相關模型
│   │   └── StorageInfo.cs          # 儲存位置資訊模型
│   ├── ApiDataModels.cs            # API 資料模型
│   ├── BaseRequest.cs              # 基礎請求模型
│   ├── BaseResponse.cs             # 基礎回應模型
│   ├── ClientConnection.cs         # 用戶端連接模型
│   └── MessageEventArgs.cs         # 訊息事件參數
├── Events/                         # 事件參數類別
│   ├── ServerStatusChangedEventArgs.cs     # 伺服器狀態變更事件
│   ├── ClientConnectedEventArgs.cs        # 用戶端連接事件
│   ├── ClientDisconnectedEventArgs.cs     # 用戶端斷線事件
│   ├── WebSocketMessageEventArgs.cs       # WebSocket 訊息事件
│   └── CustomApiRequestEventArgs.cs       # 自訂 API 請求事件
├── Enums/                          # 列舉定義
│   └── ServerStatus.cs             # 伺服器狀態列舉
├── Services/                       # 服務實作
│   ├── Handlers/                   # 請求處理器
│   │   ├── ApiRequestHandler.cs    # API 請求處理器
│   │   ├── WebSocketHandler.cs     # WebSocket 處理器
│   │   └── StaticFileHandler.cs    # 靜態檔案處理器
│   ├── HttpServerService.cs        # HTTP 伺服器服務 (原始檔案)
│   ├── HttpServerService_New.cs    # HTTP 伺服器服務 (重構版本)
│   ├── DDSWebAPIService.cs         # DDS WebAPI 服務
│   └── ApiClientService.cs         # API 用戶端服務
└── Examples/                       # 範例程式碼
    └── ConsoleExample.cs           # 控制台範例
```

## 主要改進

### 1. 相依性注入架構

- **介面分離**: 將所有外部相依性抽象為介面，支援模擬測試
- **建構子注入**: 透過建構函式注入相依性服務
- **鬆散耦合**: 降低類別間的相依性，提升程式碼彈性

### 2. 單一職責原則

- **處理器分離**: 將不同類型的請求處理邏輯分離到獨立的處理器類別
- **模型分類**: 將資料模型依據功能分類到不同目錄
- **事件管理**: 將事件參數類別獨立管理

### 3. 詳細註解

- **XML 文件註解**: 所有公開介面、方法、屬性都有完整的 XML 註解
- **參數說明**: 詳細說明方法參數的用途、格式、預設值
- **例外處理**: 說明可能拋出的例外類型和處理方式
- **使用範例**: 提供程式碼使用範例

## 使用方式

### 基本使用

```csharp
// 建立相依性服務實例 (可以使用模擬物件進行測試)
var databaseService = new DatabaseService(connectionString);
var warehouseService = new WarehouseQueryService(databaseService);
var workflowService = new WorkflowTaskService();
var configService = new GlobalConfigService();
var utilityService = new UtilityService();

// 建立 HTTP 伺服器服務
var httpServer = new HttpServerService(
    urlPrefix: "http://localhost:8085/",
    staticFilesPath: "./wwwroot",
    databaseService: databaseService,
    warehouseQueryService: warehouseService,
    workflowTaskService: workflowService,
    globalConfigService: configService,
    utilityService: utilityService
);

// 註冊事件處理器
httpServer.ServerStatusChanged += (sender, e) => 
{
    Console.WriteLine($"[{e.Timestamp}] {e.Status}: {e.Message}");
};

httpServer.MessageReceived += (sender, e) => 
{
    Console.WriteLine($"收到來自 {e.ClientIp} 的訊息: {e.Message}");
};

// 啟動伺服器
var success = await httpServer.StartAsync();
if (success)
{
    Console.WriteLine("伺服器啟動成功");
    
    // 保持程式執行
    Console.ReadKey();
    
    // 停止伺服器
    httpServer.Stop();
}
```

### 單元測試範例

```csharp
[Test]
public async Task TestApiRequestHandler_HandleInMaterialRequest_Success()
{
    // Arrange
    var mockWarehouseService = new Mock<IWarehouseQueryService>();
    var mockConfigService = new Mock<IGlobalConfigService>();
    
    var handler = new ApiRequestHandler(
        null, mockWarehouseService.Object, null, 
        mockConfigService.Object, null);
    
    var requestJson = JsonConvert.SerializeObject(new InMaterialRequest
    {
        IsContinue = false,
        InBoxQty = 10
    });
    
    // Act
    var response = await handler.HandleInMaterialRequestAsync(requestJson);
    
    // Assert
    Assert.IsTrue(response.IsSuccess);
    Assert.AreEqual("入料作業已啟動", response.Message);
    
    // 驗證服務呼叫
    mockConfigService.Verify(x => x.SaveConfigAsync(), Times.Once);
}
```

## API 端點

### 標準 MES API

- `POST /api/mes` - 統一 MES API 端點，支援多種操作類型

### 客製化倉庫管理 API

- `POST /api/in-material` - 入料作業
- `POST /api/out-material` - 出料作業
- `POST /api/operationclamp` - 夾具操作
- `POST /api/changespeed` - 速度變更
- `POST /api/getlocationbystorage` - 根據儲存位置查詢
- `POST /api/getlocationbypin` - 根據針具查詢位置

### 查詢 API

- `GET /api/out-getpins` - 取得針具資料
- `GET /api/server/statistics` - 伺服器統計資訊
- `GET /api/health` - 健康檢查

### 系統管理 API

- `POST /api/server/restart` - 重新啟動伺服器
- `GET /api/server/status` - 伺服器狀態

## WebSocket 支援

支援 WebSocket 即時通訊，提供以下指令：

- `ping` - 心跳檢測
- `get_time` - 取得伺服器時間
- `get_status` - 取得伺服器狀態

## 靜態檔案服務

- 支援 HTML、CSS、JavaScript、圖片等靜態檔案
- 自動 MIME 類型檢測
- 快取標頭設定
- 目錄瀏覽功能
- 路徑安全檢查

## 錯誤處理

- 完整的異常處理機制
- 詳細的錯誤日誌記錄
- 用戶友善的錯誤訊息
- HTTP 狀態碼正確設定

## 安全性

- 路徑遍歷攻擊防護
- CORS 支援
- 檔案大小限制
- 輸入驗證

## 效能最佳化

- 非同步處理
- 執行緒安全
- 連接池管理
- 記憶體使用最佳化

## 監控與統計

- 請求計數統計
- 連接管理
- 運行時間追蹤
- 效能監控事件

## 部署說明

1. 編譯專案
2. 確保目標伺服器已安裝 .NET Framework
3. 設定 URL 權限 (如需要)
4. 部署相依性服務實作
5. 設定配置檔案
6. 啟動服務

## 開發建議

1. **介面優先**: 先定義介面，再實作具體類別
2. **單元測試**: 為每個處理器撰寫單元測試
3. **日誌記錄**: 適當使用事件進行日誌記錄
4. **錯誤處理**: 處理所有可能的例外情況
5. **文件維護**: 保持註解和文件的同步更新

## 版本資訊

- **版本**: 1.0.0
- **建立日期**: 2025-06-13
- **作者**: GitHub Copilot
- **授權**: 專案內部使用

## 後續改進建議

1. 加入設定檔案支援 (appsettings.json)
2. 整合 Logging 框架 (NLog, Serilog)
3. 加入效能監控 (Application Insights)
4. 實作健康檢查端點
5. 加入 Swagger/OpenAPI 文件
6. 實作 JWT 身份驗證
7. 加入快取機制 (Redis)
8. 實作背景任務服務
9. 加入設定管理介面
10. 實作自動部署腳本
