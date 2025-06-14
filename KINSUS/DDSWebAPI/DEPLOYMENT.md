# DDSWebAPI 函式庫建構與部署指南

## 建構步驟

### 1. 建構 DDSWebAPI 函式庫

使用 Visual Studio 或命令列建構函式庫專案：

```powershell
# 在解決方案根目錄執行
msbuild DDSWebAPI\DDSWebAPI.csproj /p:Configuration=Release

# 或使用 dotnet（如果是 .NET Core/.NET 5+ 專案）
# dotnet build DDSWebAPI\DDSWebAPI.csproj --configuration Release
```

### 2. 建構整個解決方案

```powershell
# 建構整個解決方案
msbuild KINSUS.sln /p:Configuration=Release
```

### 3. 驗證建構結果

檢查輸出目錄中是否產生了以下檔案：
- `DDSWebAPI\bin\Release\DDSWebAPI.dll`
- `DDSWebAPI\bin\Release\DDSWebAPI.pdb`
- `bin\Release\OthinCloud.exe`（主 WPF 應用程式）

## 在其他專案中使用 DDSWebAPI

### 方法 1: 專案參考（推薦）

在目標專案的 `.csproj` 檔案中加入：

```xml
<ItemGroup>
  <ProjectReference Include="相對路徑\DDSWebAPI\DDSWebAPI.csproj">
    <Project>{B8A5F234-8C7D-4A9B-9E12-3F45D6E789AB}</Project>
    <Name>DDSWebAPI</Name>
  </ProjectReference>
</ItemGroup>
```

### 方法 2: DLL 參考

將建構出的 `DDSWebAPI.dll` 複製到目標專案，然後加入參考：

```xml
<ItemGroup>
  <Reference Include="DDSWebAPI">
    <HintPath>lib\DDSWebAPI.dll</HintPath>
  </Reference>
</ItemGroup>
```

### 方法 3: NuGet 套件（進階）

可以將 DDSWebAPI 打包成 NuGet 套件：

1. 在 DDSWebAPI 專案中加入 NuSpec 檔案
2. 使用 `nuget pack` 建立套件
3. 在目標專案中安裝套件

## 使用範例

### 在新的 WPF 專案中使用

```csharp
using DDSWebAPI.Services;
using DDSWebAPI.Models;

namespace MyNewWPFApp
{
    public partial class MainWindow : Window
    {
        private DDSWebAPIService _ddsService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDDSAPI();
        }

        private void InitializeDDSAPI()
        {
            _ddsService = new DDSWebAPIService(
                "http://localhost:8085/",
                "http://mes-server:8080/api/",
                "MY_DEVICE_001",
                "OPERATOR"
            );

            _ddsService.MessageReceived += OnMessageReceived;
            _ddsService.ServerStatusChanged += OnServerStatusChanged;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            await _ddsService.StartServerAsync();
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Dispatcher.Invoke(() => {
                MessageTextBox.AppendText($"{e.Message}\n");
            });
        }

        private void OnServerStatusChanged(object sender, ServerStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() => {
                StatusLabel.Content = $"狀態: {e.Status}";
            });
        }
    }
}
```

### 在控制台應用程式中使用

```csharp
using DDSWebAPI.Services;
using DDSWebAPI.Models;

class Program
{
    static async Task Main(string[] args)
    {
        using var ddsService = new DDSWebAPIService();
        
        ddsService.LogMessage += (s, e) => Console.WriteLine($"[{e.Level}] {e.Message}");
        
        bool started = await ddsService.StartServerAsync();
        if (started)
        {
            Console.WriteLine("伺服器已啟動，按 Enter 結束...");
            Console.ReadLine();
        }
    }
}
```

### 在 Windows Service 中使用

```csharp
using System.ServiceProcess;
using DDSWebAPI.Services;

public partial class DDSService : ServiceBase
{
    private DDSWebAPIService _ddsService;

    protected override void OnStart(string[] args)
    {
        _ddsService = new DDSWebAPIService();
        _ = _ddsService.StartServerAsync();
    }

    protected override void OnStop()
    {
        _ddsService?.StopServer();
        _ddsService?.Dispose();
    }
}
```

## 部署注意事項

### 1. 相依套件

確保目標環境具備以下套件：
- .NET Framework 4.8 或更高版本
- Newtonsoft.Json 13.0.3

### 2. 防火牆設定

開放 HTTP 伺服器使用的埠號（預設 8085）：

```powershell
# Windows 防火牆設定
netsh advfirewall firewall add rule name="DDSWebAPI Server" dir=in action=allow protocol=TCP localport=8085
```

### 3. URL 保留（如果需要）

在某些環境中可能需要保留 URL：

```powershell
# 以管理員身分執行
netsh http add urlacl url=http://+:8085/ user=Everyone
```

### 4. 設定檔

建議使用設定檔管理 URL 和其他參數：

```xml
<!-- App.config -->
<configuration>
  <appSettings>
    <add key="DDSServerUrl" value="http://localhost:8085/" />
    <add key="RemoteApiUrl" value="http://mes-server:8080/api/" />
    <add key="DeviceCode" value="KINSUS001" />
  </appSettings>
</configuration>
```

```csharp
// 讀取設定
string serverUrl = ConfigurationManager.AppSettings["DDSServerUrl"];
var ddsService = new DDSWebAPIService(serverUrl, ...);
```

## 疑難排解

### 常見問題

1. **建構錯誤：找不到參考**
   - 確認 Newtonsoft.Json 套件已正確安裝
   - 檢查專案參考路徑

2. **執行時錯誤：HttpListener 權限不足**
   - 以管理員身分執行
   - 或使用 localhost 而非萬用字元

3. **網路連線問題**
   - 檢查防火牆設定
   - 確認埠號未被占用

### 偵錯技巧

```csharp
// 啟用詳細日誌
_ddsService.LogMessage += (s, e) => {
    System.Diagnostics.Debug.WriteLine($"[{e.Level}] {e.Timestamp}: {e.Message}");
};

// 檢查連線狀態
_ddsService.ServerStatusChanged += (s, e) => {
    Console.WriteLine($"伺服器狀態變更: {e.Status} - {e.Description}");
};
```

## 效能調整

### 1. 平行處理設定

```csharp
// 控制並行請求數量
ServicePointManager.DefaultConnectionLimit = 100;
```

### 2. 記憶體管理

```csharp
// 定期清理用戶端連接
Timer cleanupTimer = new Timer(CleanupOldConnections, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
```

### 3. 日誌等級控制

```csharp
// 在生產環境中降低日誌等級
_ddsService.LogMessage += (s, e) => {
    if (e.Level >= LogLevel.Warning) // 只記錄警告和錯誤
    {
        Log(e.Message);
    }
};
```

## 版本更新

當 DDSWebAPI 函式庫有新版本時：

1. 更新 DDSWebAPI 專案
2. 重新建構
3. 更新參考此函式庫的專案
4. 測試相容性
5. 部署新版本

建議使用版本控制來管理不同版本的相容性。
