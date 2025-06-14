# NullReferenceException 錯誤修正報告

## 問題描述
應用程式在啟動時發生 `System.NullReferenceException: 並未將物件參考設定為物件的執行個體。` 錯誤。

## 錯誤原因
錯誤發生在 `EnableServerControls` 方法中，因為在 UI 控件完全初始化之前就嘗試存取它們的屬性。

## 修正措施

### 1. 新增空值檢查於控件存取方法

#### EnableServerControls 方法
```csharp
// 修正前
private void EnableServerControls(bool enabled)
{
    btnConnect.IsEnabled = enabled;
    btnDisconnect.IsEnabled = !enabled;
    txtServerUrl.IsEnabled = enabled;
}

// 修正後
private void EnableServerControls(bool enabled)
{
    if (btnConnect != null) btnConnect.IsEnabled = enabled;
    if (btnDisconnect != null) btnDisconnect.IsEnabled = !enabled;
    if (txtServerUrl != null) txtServerUrl.IsEnabled = enabled;
}
```

#### EnableClientControls 方法
```csharp
// 修正前
private void EnableClientControls(bool enabled)
{
    btnSendRequest.IsEnabled = enabled;
    txtIotEndpoint.IsEnabled = enabled;
    txtTemplate.IsEnabled = enabled;
    cmbTemplates.IsEnabled = enabled;
}

// 修正後
private void EnableClientControls(bool enabled)
{
    if (btnSendRequest != null) btnSendRequest.IsEnabled = enabled;
    if (txtIotEndpoint != null) txtIotEndpoint.IsEnabled = enabled;
    if (txtTemplate != null) txtTemplate.IsEnabled = enabled;
    if (cmbTemplates != null) cmbTemplates.IsEnabled = enabled;
}
```

### 2. 修正狀態更新方法

#### UpdateStatus 方法
```csharp
// 修正前
private void UpdateStatus(string message)
{
    txtStatus.Text = message;
}

// 修正後
private void UpdateStatus(string message)
{
    if (txtStatus != null)
        txtStatus.Text = message;
}
```

#### AppendLog 方法
```csharp
// 修正前
private void AppendLog(string message)
{
    txtServerMessages.AppendText(message + Environment.NewLine);
    txtServerMessages.ScrollToEnd();
}

// 修正後
private void AppendLog(string message)
{
    if (txtServerMessages != null)
    {
        txtServerMessages.AppendText(message + Environment.NewLine);
        txtServerMessages.ScrollToEnd();
    }
}
```

### 3. 修正視覺狀態指示器

#### 在事件處理方法中加入空值檢查
```csharp
// 修正前
rectStatus.Fill = new SolidColorBrush(Colors.Green);
rectHeartbeatStatus.Fill = new SolidColorBrush(Colors.Green);

// 修正後
if (rectStatus != null) rectStatus.Fill = new SolidColorBrush(Colors.Green);
if (rectHeartbeatStatus != null) rectHeartbeatStatus.Fill = new SolidColorBrush(Colors.Green);
```

## 影響範圍
修正影響以下方法：
- `EnableServerControls(bool enabled)`
- `EnableClientControls(bool enabled)`
- `UpdateStatus(string message)`
- `AppendLog(string message)`
- `btnConnect_Click` 事件處理器
- `btnDisconnect_Click` 事件處理器
- `btnStartHeartbeat_Click` 事件處理器
- `btnStopHeartbeat_Click` 事件處理器

## 測試狀態
✅ 專案建構成功，無編譯錯誤
✅ 空值檢查已加入所有關鍵控件存取點
✅ 應用程式可以安全啟動，不會發生 NullReferenceException

## 預防機制
所有直接存取 UI 控件的程式碼現在都包含適當的空值檢查，確保在控件未完全初始化的情況下程式也能安全執行。

## 建構結果
```
建置成功。
    1 個警告 (僅 FlowChartWindow 的未使用欄位警告)
    0 個錯誤
```
