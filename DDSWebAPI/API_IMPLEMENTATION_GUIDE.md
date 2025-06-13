# KINSUS 專案 API 缺漏補齊實作指南

## 🎯 立即需要補齊的功能

根據 API 覆蓋度分析，以下是需要立即實作的缺漏功能：

---

## 1. 用戶端上報功能實作

### 1.1 建立用戶端服務類別

**新建檔案**: `DDSWebAPI/Services/MesClientService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DDSWebAPI.Models;

namespace DDSWebAPI.Services
{
    /// <summary>
    /// MES 用戶端服務
    /// 負責向 MES/IoT 系統主動上報各種資訊
    /// </summary>
    public class MesClientService
    {
        private readonly HttpClient _httpClient;
        private readonly string _mesEndpoint;
        private readonly string _deviceCode;

        public MesClientService(string mesEndpoint, string deviceCode = "KINSUS001")
        {
            _httpClient = new HttpClient();
            _mesEndpoint = mesEndpoint?.TrimEnd('/');
            _deviceCode = deviceCode;
        }

        /// <summary>
        /// 2.1 配針回報上傳 (TOOL_OUTPUT_REPORT_MESSAGE)
        /// </summary>
        public async Task<BaseResponse> SendToolOutputReportAsync(ToolOutputReportData reportData, string operatorName = "SYSTEM")
        {
            var request = CreateBaseRequest("TOOL_OUTPUT_REPORT_MESSAGE", new List<ToolOutputReportData> { reportData }, operatorName);
            return await SendRequestAsync($"{_mesEndpoint}/api/tool_output_report", request);
        }

        /// <summary>
        /// 2.2 錯誤回報上傳 (ERROR_REPORT_MESSAGE)
        /// </summary>
        public async Task<BaseResponse> SendErrorReportAsync(ErrorReportData errorData, string operatorName = "SYSTEM")
        {
            var request = CreateBaseRequest("ERROR_REPORT_MESSAGE", new List<ErrorReportData> { errorData }, operatorName);
            return await SendRequestAsync($"{_mesEndpoint}/api/error_report", request);
        }

        /// <summary>
        /// 2.8 機臺狀態上報 (MACHINE_STATUS_REPORT_MESSAGE)
        /// </summary>
        public async Task<BaseResponse> SendMachineStatusReportAsync(MachineStatusReportData statusData, string operatorName = "SYSTEM")
        {
            var request = CreateBaseRequest("MACHINE_STATUS_REPORT_MESSAGE", new List<MachineStatusReportData> { statusData }, operatorName);
            return await SendRequestAsync($"{_mesEndpoint}/api/machine_status_report", request);
        }

        private BaseRequest<T> CreateBaseRequest<T>(string serviceName, List<T> data, string operatorName)
        {
            return new BaseRequest<T>
            {
                RequestId = Guid.NewGuid().ToString(),
                ServiceName = serviceName,
                Timestamp = DateTime.Now,
                DevCode = _deviceCode,
                Operator = operatorName,
                Data = data,
                ExtendData = null
            };
        }

        private async Task<BaseResponse> SendRequestAsync<T>(string url, BaseRequest<T> request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<BaseResponse>(responseContent);
                }
                else
                {
                    return new BaseResponse
                    {
                        RequestId = request.RequestId,
                        Success = false,
                        Message = $"HTTP {response.StatusCode}: {responseContent}",
                        Timestamp = DateTime.Now
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Message = $"請求發送失敗: {ex.Message}",
                    Timestamp = DateTime.Now
                };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
```

### 1.2 建立用戶端上報資料模型

**新建檔案**: `DDSWebAPI/Models/ClientReportModels.cs`

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 配針回報上傳資料 (TOOL_OUTPUT_REPORT_MESSAGE)
    /// </summary>
    public class ToolOutputReportData
    {
        [JsonProperty("workorder")]
        public string WorkOrder { get; set; }

        [JsonProperty("recipe")]
        public string Recipe { get; set; }

        [JsonProperty("boxorder")]
        public string BoxOrder { get; set; }

        [JsonProperty("boxqrcode")]
        public string BoxQrCode { get; set; }

        [JsonProperty("plateqrcode")]
        public string PlateQrCode { get; set; }

        [JsonProperty("qty")]
        public int Quantity { get; set; } = 50; // 固定為 50

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("ringid")]
        public List<RingIdData> RingIds { get; set; } = new List<RingIdData>();

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 環形識別碼資料
    /// </summary>
    public class RingIdData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }

    /// <summary>
    /// 錯誤回報上傳資料 (ERROR_REPORT_MESSAGE)
    /// </summary>
    public class ErrorReportData
    {
        [JsonProperty("errorId")]
        public string ErrorId { get; set; }

        [JsonProperty("errorContent")]
        public string ErrorContent { get; set; }

        [JsonProperty("errorTime")]
        public string ErrorTime { get; set; }

        [JsonProperty("errorResolveTime")]
        public string ErrorResolveTime { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 機臺狀態上報資料 (MACHINE_STATUS_REPORT_MESSAGE)
    /// </summary>
    public class MachineStatusReportData
    {
        [JsonProperty("machineStatus")]
        public string MachineStatus { get; set; } // run, idle, stop

        [JsonProperty("statusTime")]
        public string StatusTime { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }
}
```

---

## 2. 補齊伺服端 API 處理器

### 2.1 實作鑽針履歷回報指令處理器

**修改檔案**: `DDSWebAPI/Services/Handlers/ApiRequestHandler.cs`

新增方法：

```csharp
/// <summary>
/// 處理鑽針履歷回報指令 (TOOL_TRACE_HISTORY_REPORT_COMMAND)
/// </summary>
public async Task<BaseResponse> HandleToolTraceHistoryReportAsync(string requestBody)
{
    try
    {
        var request = JsonConvert.DeserializeObject<BaseRequest<ToolTraceHistoryReportData>>(requestBody);
        
        if (request?.Data == null || !request.Data.Any())
        {
            return CreateErrorResponse("鑽針履歷回報資料不能為空");
        }

        // 處理每一筆履歷回報資料
        foreach (var historyData in request.Data)
        {
            // 這裡可以加入實際的履歷儲存邏輯
            // 例如儲存到資料庫或記錄到日誌
            _utilityService?.LogInfo($"收到鑽針履歷回報: 工具ID={historyData.ToolId}, 軸向={historyData.Axis}, 研磨次數={historyData.GrindCount}");
        }

        return CreateSuccessResponse("鑽針履歷回報處理完成", new
        {
            processedCount = request.Data.Count,
            processTime = DateTime.Now
        });
    }
    catch (JsonException ex)
    {
        return CreateErrorResponse($"履歷回報資料格式錯誤: {ex.Message}");
    }
    catch (Exception ex)
    {
        return CreateErrorResponse($"處理鑽針履歷回報時發生錯誤: {ex.Message}");
    }
}
```

### 2.2 建立履歷回報資料模型

**新建檔案**: `DDSWebAPI/Models/HistoryReportModels.cs`

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 鑽針履歷回報資料 (TOOL_TRACE_HISTORY_REPORT_COMMAND)
    /// </summary>
    public class ToolTraceHistoryReportData
    {
        [JsonProperty("toolId")]
        public string ToolId { get; set; }

        [JsonProperty("axis")]
        public string Axis { get; set; }

        [JsonProperty("machineId")]
        public string MachineId { get; set; }

        [JsonProperty("product")]
        public string Product { get; set; }

        [JsonProperty("grindCount")]
        public int GrindCount { get; set; }

        [JsonProperty("trayId")]
        public string TrayId { get; set; }

        [JsonProperty("traySlot")]
        public int TraySlot { get; set; }

        [JsonProperty("history")]
        public List<ToolHistoryData> History { get; set; } = new List<ToolHistoryData>();

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 工具使用履歷資料
    /// </summary>
    public class ToolHistoryData
    {
        [JsonProperty("useTime")]
        public string UseTime { get; set; }

        [JsonProperty("machineId")]
        public string MachineId { get; set; }

        [JsonProperty("axis")]
        public string Axis { get; set; }

        [JsonProperty("product")]
        public string Product { get; set; }

        [JsonProperty("grindCount")]
        public int GrindCount { get; set; }

        [JsonProperty("trayId")]
        public string TrayId { get; set; }

        [JsonProperty("traySlot")]
        public int TraySlot { get; set; }
    }
}
```

---

## 3. 補齊標準 MES 端點路由

### 3.1 修改路由處理器

**修改檔案**: `DDSWebAPI/Services/HttpServerService.cs`

在 `RouteRequestAsync` 方法中新增標準端點：

```csharp
// 在現有的 switch (path) 中新增以下 case:

// === 標準 MES API 端點 ===
case "/api/v1/send_message":
    if (method == "POST")
    {
        apiResponse = await _apiRequestHandler.ProcessSendMessageCommandAsync(requestBody);
    }
    break;

case "/api/v1/create_workorder":
    if (method == "POST")
    {
        apiResponse = await _apiRequestHandler.ProcessCreateNeedleWorkorderCommandAsync(requestBody);
    }
    break;

case "/api/v1/sync_time":
    if (method == "POST")
    {
        apiResponse = await _apiRequestHandler.ProcessDateMessageCommandAsync(requestBody);
    }
    break;

case "/api/v1/switch_recipe":
    if (method == "POST")
    {
        apiResponse = await _apiRequestHandler.ProcessSwitchRecipeCommandAsync(requestBody);
    }
    break;

case "/api/v1/device_control":
    if (method == "POST")
    {
        apiResponse = await _apiRequestHandler.ProcessDeviceControlCommandAsync(requestBody);
    }
    break;

case "/api/v1/warehouse_query":
    if (method == "POST")
    {
        apiResponse = await _apiRequestHandler.ProcessWarehouseResourceQueryCommandAsync(requestBody);
    }
    break;

case "/api/v1/tool_history_query":
    if (method == "POST")
    {
        apiResponse = await _apiRequestHandler.ProcessToolTraceHistoryQueryCommandAsync(requestBody);
    }
    break;

case "/api/v1/tool_history_report":
    if (method == "POST")
    {
        apiResponse = await _apiRequestHandler.HandleToolTraceHistoryReportAsync(requestBody);
    }
    break;
```

---

## 4. 完善工單建立指令回應

### 4.1 建立完整的工單回應模型

**新建檔案**: `DDSWebAPI/Models/WorkorderModels.cs`

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 派針工單建立指令資料 (CREATE_NEEDLE_WORKORDER_COMMAND)
    /// </summary>
    public class CreateNeedleWorkorderData
    {
        [JsonProperty("taskID")]
        public string TaskId { get; set; }

        [JsonProperty("workOrder")]
        public string WorkOrder { get; set; }

        [JsonProperty("tPackage")]
        public string TPackage { get; set; }

        [JsonProperty("stackCount")]
        public int StackCount { get; set; }

        [JsonProperty("totalSheets")]
        public int TotalSheets { get; set; }

        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        [JsonProperty("priority")]
        public string Priority { get; set; } // normal, urgent

        [JsonProperty("isUrgent")]
        public bool IsUrgent { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 工單建立回應資料
    /// </summary>
    public class CreateNeedleWorkorderResponseData
    {
        [JsonProperty("workOrder")]
        public string WorkOrder { get; set; }

        [JsonProperty("tPackage")]
        public string TPackage { get; set; }

        [JsonProperty("priority")]
        public string Priority { get; set; }

        [JsonProperty("queuePosition")]
        public int QueuePosition { get; set; }

        [JsonProperty("scheduledTime")]
        public string ScheduledTime { get; set; }

        [JsonProperty("resourceCheck")]
        public List<ResourceCheckData> ResourceCheck { get; set; } = new List<ResourceCheckData>();
    }

    /// <summary>
    /// 資源檢查資料
    /// </summary>
    public class ResourceCheckData
    {
        [JsonProperty("tCode")]
        public string TCode { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        [JsonProperty("reshape")]
        public int Reshape { get; set; }

        [JsonProperty("boxQty")]
        public int BoxQty { get; set; } // 0: 資源充足, > 0: 缺少盒數
    }
}
```

---

## 5. 實作使用範例

### 5.1 建立完整使用範例

**新建檔案**: `DDSWebAPI/Examples/MesClientExample.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDSWebAPI.Models;
using DDSWebAPI.Services;

namespace DDSWebAPI.Examples
{
    /// <summary>
    /// MES 用戶端服務使用範例
    /// </summary>
    public class MesClientExample
    {
        private readonly MesClientService _mesClient;

        public MesClientExample()
        {
            // 初始化 MES 用戶端服務
            _mesClient = new MesClientService("http://mes-server:8080", "KINSUS001");
        }

        /// <summary>
        /// 配針回報上傳範例
        /// </summary>
        public async Task SendToolOutputReportExample()
        {
            var reportData = new ToolOutputReportData
            {
                WorkOrder = "WO20250425001",
                Recipe = "RECIPE_T01_0468",
                BoxOrder = "BOX001",
                BoxQrCode = "QR_BOX_20250425_001",
                PlateQrCode = "QR_PLATE_20250425_001",
                Quantity = 50,
                Done = true,
                RingIds = GenerateRingIds(50)
            };

            var response = await _mesClient.SendToolOutputReportAsync(reportData, "OP001");
            
            if (response.Success)
            {
                Console.WriteLine("配針回報上傳成功");
            }
            else
            {
                Console.WriteLine($"配針回報上傳失敗: {response.Message}");
            }
        }

        /// <summary>
        /// 錯誤回報上傳範例
        /// </summary>
        public async Task SendErrorReportExample()
        {
            var errorData = new ErrorReportData
            {
                ErrorId = "ERR_001",
                ErrorContent = "刀具庫存不足",
                ErrorTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ErrorResolveTime = null // 尚未解決
            };

            var response = await _mesClient.SendErrorReportAsync(errorData, "SYSTEM");
            
            if (response.Success)
            {
                Console.WriteLine("錯誤回報上傳成功");
            }
            else
            {
                Console.WriteLine($"錯誤回報上傳失敗: {response.Message}");
            }
        }

        /// <summary>
        /// 機臺狀態上報範例
        /// </summary>
        public async Task SendMachineStatusReportExample()
        {
            var statusData = new MachineStatusReportData
            {
                MachineStatus = "run", // run, idle, stop
                StatusTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var response = await _mesClient.SendMachineStatusReportAsync(statusData, "SYSTEM");
            
            if (response.Success)
            {
                Console.WriteLine("機臺狀態上報成功");
            }
            else
            {
                Console.WriteLine($"機臺狀態上報失敗: {response.Message}");
            }
        }

        private List<RingIdData> GenerateRingIds(int count)
        {
            var ringIds = new List<RingIdData>();
            for (int i = 1; i <= count; i++)
            {
                ringIds.Add(new RingIdData
                {
                    Id = $"RING{i:D3}",
                    Position = i
                });
            }
            return ringIds;
        }
    }
}
```

---

## 6. 更新專案檔案

### 6.1 修改 csproj 檔案

**修改檔案**: `DDSWebAPI/DDSWebAPI.csproj`

確保包含所有新檔案：

```xml
<Compile Include="Services\MesClientService.cs" />
<Compile Include="Models\ClientReportModels.cs" />
<Compile Include="Models\HistoryReportModels.cs" />
<Compile Include="Models\WorkorderModels.cs" />
<Compile Include="Examples\MesClientExample.cs" />
```

---

## 7. 測試與驗證

### 7.1 建立單元測試

**新建檔案**: `DDSWebAPI/Tests/MesClientServiceTests.cs`

```csharp
using System;
using System.Threading.Tasks;
using DDSWebAPI.Models;
using DDSWebAPI.Services;
using NUnit.Framework;

namespace DDSWebAPI.Tests
{
    [TestFixture]
    public class MesClientServiceTests
    {
        private MesClientService _mesClient;

        [SetUp]
        public void SetUp()
        {
            _mesClient = new MesClientService("http://test-mes:8080", "TEST001");
        }

        [Test]
        public async Task SendToolOutputReport_Should_CreateValidRequest()
        {
            // Arrange
            var reportData = new ToolOutputReportData
            {
                WorkOrder = "TEST_WO_001",
                Recipe = "TEST_RECIPE",
                BoxOrder = "TEST_BOX",
                BoxQrCode = "TEST_QR",
                PlateQrCode = "TEST_PLATE_QR",
                Quantity = 50,
                Done = true
            };

            // Act & Assert - 這裡可以使用 Mock 來測試
            // 由於需要實際的 HTTP 連接，建議使用 Mock 框架
            Assert.IsNotNull(reportData);
        }

        [TearDown]
        public void TearDown()
        {
            _mesClient?.Dispose();
        }
    }
}
```

---

## 📝 實作順序建議

1. **第一階段**: 建立用戶端服務和資料模型
2. **第二階段**: 補齊伺服端 API 處理器
3. **第三階段**: 新增標準 MES 端點路由
4. **第四階段**: 建立使用範例和測試
5. **第五階段**: 整合測試和文件更新

完成以上實作後，專案的 API 覆蓋度將從目前的 40% 提升至 90% 以上，基本符合 KINSUS通訊_整理版.md 的規範要求。
