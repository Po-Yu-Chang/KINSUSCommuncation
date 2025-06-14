# DDSWebAPI 整合指南

## 📋 整合步驟

### 1. 專案參考設定
已在 `KINSUS.csproj` 中加入對 `DDSWebAPI` 的專案參考：

```xml
<ProjectReference Include="DDSWebAPI\DDSWebAPI.csproj">
  <Project>{B8A5F234-8C7D-4A9B-9E12-3F45D6E789AB}</Project>
  <Name>DDSWebAPI</Name>
</ProjectReference>
```

### 2. 程式碼重構

#### 原始實作 vs 新實作

**原始實作 (MainWindow.xaml.cs)**:
- 直接在 MainWindow 中實作 HTTP 伺服器邏輯
- 使用 `KINSUS.API.HttpServer` 類別
- 程式碼較為複雜，難以重複使用

**新實作 (MainWindow_DDSWebAPI.xaml.cs)**:
- 使用 `DDSWebAPI.Services.DDSWebAPIService` 統一服務
- 透過事件驅動架構處理各種通訊事件
- 程式碼更簡潔，易於維護

### 3. 主要功能對照

| 功能 | 原始實作 | DDSWebAPI 實作 |
|------|----------|----------------|
| HTTP 伺服器 | 直接使用 HttpListener | DDSWebAPIService.StartServerAsync() |
| API 呼叫 | 手動 HttpClient 處理 | DDSWebAPIService.SendApiRequestAsync() |
| 事件處理 | 自定義委派 | 標準 EventHandler 模式 |
| 錯誤處理 | 分散在各處 | 統一在服務層處理 |
| 連接管理 | 手動管理 | 自動追蹤與管理 |

### 4. 使用方式

#### 初始化服務
```csharp
private DDSWebAPIService _ddsService;

private void InitializeDDSService()
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
}
```

#### 啟動伺服器
```csharp
private async void btnStartServer_Click(object sender, RoutedEventArgs e)
{
    string serverUrl = txtServerUrl.Text.Trim();
    await _ddsService.StartServerAsync(serverUrl);
}
```

#### 發送 API 請求
```csharp
private async void btnSendRequest_Click(object sender, RoutedEventArgs e)
{
    string endpoint = txtIotEndpoint.Text.Trim();
    string requestBody = txtRequestBody.Text.Trim();
    await _ddsService.SendApiRequestAsync(endpoint, requestBody);
}
```

### 5. 切換到新實作

要使用新的 DDSWebAPI 實作，您需要：

1. **備份原始檔案**：
   ```
   MainWindow.xaml.cs -> MainWindow_Original.xaml.cs
   ```

2. **使用新實作**：
   ```
   MainWindow_DDSWebAPI.xaml.cs -> MainWindow.xaml.cs
   ```

3. **建構專案**：
   ```powershell
   msbuild KINSUS.sln /p:Configuration=Release
   ```

### 6. 優勢

#### 🎯 **程式碼重複使用**
- DDSWebAPI 可以在其他專案中重複使用
- 統一的 API 處理邏輯

#### 🔧 **維護性提升**
- 分離關注點，各司其職
- 錯誤處理集中化

#### 📈 **擴展性增強**
- 易於新增新的 API 功能
- 支援更多通訊協定

#### 🛡️ **穩定性改善**
- 完整的錯誤處理機制
- 自動重連與容錯能力

### 7. 注意事項

1. **命名空間變更**：
   - 原本：`using KINSUS.API;`
   - 現在：`using DDSWebAPI.Services;`

2. **事件處理方式**：
   - 使用標準的 EventHandler 模式
   - 需要在 Dispatcher.Invoke 中處理 UI 更新

3. **資源管理**：
   - 記得在視窗關閉時呼叫 `_ddsService.Dispose()`
   - 適當的異常處理

## 🚀 下一步

1. 測試新實作的功能
2. 驗證所有 API 呼叫正常工作
3. 進行壓力測試確保穩定性
4. 更新相關文件

## 📞 支援

如果在整合過程中遇到任何問題，請檢查：
- 專案參考是否正確設定
- DDSWebAPI 是否成功建構
- 事件處理器是否正確註冊
