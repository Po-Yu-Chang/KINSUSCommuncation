# HttpServerService 完整功能修改報告

## 修改日期
2025年6月16日

## 修改範圍
基於用戶要求，完整修改了 `HttpServerService` 類別，整合了 `SecurityMiddleware` 和 `PerformanceController`，並完善了連線管理功能。

## 主要修改內容

### 1. 新增的欄位和屬性

```csharp
// 安全性中介軟體
private readonly SecurityMiddleware _securityMiddleware;

// 效能控制器
private readonly PerformanceController _performanceController;

// 連線管理
private readonly Dictionary<string, ClientConnection> _connectedClients;
private readonly object _clientsLock = new object();
```

### 2. 建構函式增強

- 初始化 `SecurityMiddleware` 與預設的 API 金鑰、IP 白名單和密鑰
- 初始化 `PerformanceController` 與效能限制參數
- 初始化連線管理字典

### 3. 請求處理流程完善

修改 `HandleRequestAsync` 方法，新增完整的安全性和效能檢查：

1. **效能控制 - 連線限制檢查**
   - 使用 `PerformanceController.TryAcquireConnectionAsync()` 檢查連線數限制
   - 超過限制時回傳 503 Service Unavailable

2. **安全性檢查 - IP 白名單驗證**
   - 使用 `SecurityMiddleware.ValidateIpAddress()` 驗證 IP 位址
   - 不在白名單時回傳 403 Forbidden

3. **連線管理**
   - 呼叫 `RegisterClientConnection()` 註冊新連線
   - 觸發 `OnClientConnected` 事件

4. **效能控制 - 資料大小限制**
   - 使用 `PerformanceController.ValidateDataSize()` 檢查資料大小
   - 超過限制時回傳 413 Request Entity Too Large

5. **效能控制 - 請求頻率限制**
   - 使用 `PerformanceController.IsRequestAllowed()` 檢查請求頻率
   - 超過限制時回傳 429 Too Many Requests

6. **安全性檢查 - API 金鑰驗證**
   - 對 `/api/` 路徑的請求進行 API 金鑰驗證
   - 驗證失敗時回傳 401 Unauthorized

### 4. 連線管理方法

新增完整的連線管理功能：

```csharp
// 註冊用戶端連接
private void RegisterClientConnection(string clientId, string clientIp)

// 取消註冊用戶端連接
private void UnregisterClientConnection(string clientId)

// 取得所有已連接的用戶端
public List<ClientConnection> GetConnectedClients()

// 清理過期的連接記錄
public void CleanupExpiredConnections(int timeoutMinutes = 5)
```

### 5. 資源管理改善

- **啟動時**: 新增定期清理過期連接的背景任務
- **停止時**: 清理所有連接並觸發斷線事件
- **請求結束時**: 釋放效能控制器的連線資源

### 6. 事件處理完善

所有事件都有完整的實作：
- `MessageReceived`: 訊息接收事件
- `ClientConnected`: 用戶端連接事件
- `ClientDisconnected`: 用戶端斷線事件
- `ServerStatusChanged`: 伺服器狀態變更事件
- `WebSocketMessageReceived`: WebSocket 訊息事件
- `CustomApiRequest`: 自訂 API 請求事件

## 安全性功能

### SecurityMiddleware 整合
- **API 金鑰驗證**: 支援多個有效的 API 金鑰
- **IP 白名單**: 只允許指定的 IP 位址存取
- **請求簽章驗證**: 支援 HMAC-SHA256 簽章驗證
- **彈性配置**: 可以個別啟用/停用各項安全功能

### 預設安全配置
```csharp
var validApiKeys = new[] { "default-api-key", "kinsus-api-key" };
var ipWhitelist = new[] { "127.0.0.1", "localhost", "::1" };
var secretKey = "kinsus-secret-key-2025";
```

## 效能控制功能

### PerformanceController 整合
- **請求頻率限制**: 每分鐘最多 100 個請求
- **平行連線限制**: 最多 20 個同時連線
- **資料大小限制**: 最大 10MB 資料大小
- **請求逾時控制**: 30 秒請求逾時

### 效能監控
- 即時連線數統計
- 請求處理總數統計
- 自動釋放過期連線資源

## 連線管理功能

### 即時連線監控
- 追蹤所有活躍連線
- 記錄連線時間和最後活動時間
- 提供連線統計資訊

### 自動清理機制
- 每分鐘自動清理過期連線
- 可設定逾時時間（預設 5 分鐘）
- 清理時觸發適當的斷線事件

## 測試建議

### 安全性測試
1. 測試無效 API 金鑰的請求
2. 測試來自非白名單 IP 的請求
3. 測試請求簽章驗證

### 效能測試
1. 測試請求頻率限制（每分鐘超過 100 個請求）
2. 測試平行連線限制（超過 20 個同時連線）
3. 測試資料大小限制（超過 10MB 的請求）

### 連線管理測試
1. 建立多個連線並檢查統計
2. 測試連線逾時自動清理
3. 測試伺服器停止時的連線清理

## API 相容性

所有原有的 API 端點都保持相容：
- 標準 MES API 端點
- 客製化倉庫管理 API
- 系統管理 API
- WebSocket 支援

## 使用範例

更新了 `HttpServerServiceExample.cs` 來展示新功能：
- 安全性和效能功能說明
- 連線管理功能展示
- 增強的事件處理
- 互動式伺服器控制

## 結論

`HttpServerService` 現在是一個功能完整的企業級 HTTP 伺服器，具備：
- ✅ 完整的安全性控制
- ✅ 效能和資源管理
- ✅ 即時連線監控
- ✅ 自動資源清理
- ✅ 完善的事件系統
- ✅ 企業級錯誤處理

所有修改都已完成，可以正常進行測試和部署。
