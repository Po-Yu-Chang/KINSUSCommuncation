using Newtonsoft.Json;
using OthinCloud.Model;
using System;
using System.Collections.Generic;
using System.IO; // Added for Path
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// MES 與設備接口規範實作
/// 
/// 此命名空間提供與 MES（製造執行系統）進行通訊的所有功能類別。
/// 所有接口均使用 JSON 格式，通過 HTTP POST 方式進行請求與響應。
/// </summary>

namespace OthinCloud.API
{
    /// <summary>
    /// API 用戶端類別，用於實現與 MES 系統的通訊
    /// 
    /// 實現了以下功能：
    /// 1. 配置讀取與管理：從 INI 檔案讀取基本配置
    /// 2. 錯誤處理與重試機制：當網路異常或伺服器錯誤時自動重試
    /// 3. 斷網處理：每隔指定秒數重試
    /// 4. 實現所有接口調用：包括設備狀態、心跳、報警、參數等 15 個接口
    /// </summary>
    public class ApiClient
    {
        // 建議使用 IHttpClientFactory 或靜態實例以獲得更好的效能和資源管理
        private static readonly HttpClient httpClient = new HttpClient();
        
        /// <summary>API 基本 URL 地址，從 INI 檔案讀取</summary>
        private string baseApiUrl;
        
        /// <summary>設備編碼，遵循 MES 規範要求，從 INI 檔案讀取</summary>
        private string devCode;
        
        /// <summary>最大重試次數，從 INI 檔案讀取</summary>
        private int maxRetries;
        
        /// <summary>重試間隔秒數，從 INI 檔案讀取，根據規範預設為 5 秒</summary>
        private int retryDelaySeconds;

        /// <summary>INI 設定檔管理器，用於讀取 API 設定</summary>
        private readonly IniManager iniManager;        /// <summary>
        /// 建構函式，從 INI 檔案讀取 API 設定
        /// </summary>
        /// <param name="iniFilePath">INI 檔案路徑，預設為 "setting.ini"</param>
        /// <remarks>
        /// 根據 MES 接口規範的需求：
        /// 1. 設備需提供 MES 開關設定，接口呼叫地址及 DevCode（設備編碼）需設為可配置選項
        /// 2. 每個接口的 DevCode 字段需傳入設定的設備編碼
        /// 3. 接口無響應或斷網處理時，設備需每隔 5 秒重試呼叫接口
        /// </remarks>
        public ApiClient(string iniFilePath = "setting.ini") // Default path
        {
            iniManager = new IniManager(iniFilePath);

            // 讀取 API 設定，包含基本 URL 和設備編碼
            baseApiUrl = iniManager.ReadIni("API", "BaseUrl", "http://localhost:8080").TrimEnd('/'); // 提供預設值如未找到設定
            devCode = iniManager.ReadIni("API", "DevCode", "DEFAULT_DEV_CODE");

            // 讀取重試設定，並處理解析錯誤
            if (!int.TryParse(iniManager.ReadIni("Retry", "MaxRetries", "3"), out maxRetries))
            {
                Console.WriteLine("警告：無法從 INI 檔案解析 MaxRetries 值，使用預設值 3。");
                maxRetries = 3;
            }
            if (!int.TryParse(iniManager.ReadIni("Retry", "RetryDelaySeconds", "5"), out retryDelaySeconds))
            {
                Console.WriteLine("警告：無法從 INI 檔案解析 RetryDelaySeconds 值，使用預設值 5。");
                retryDelaySeconds = 5; // 依照規範，預設為 5 秒
            }

            Console.WriteLine($"ApiClient 已初始化：BaseUrl='{baseApiUrl}', DevCode='{devCode}', MaxRetries={maxRetries}, RetryDelay={retryDelaySeconds}s");

            // 設定 HttpClient 預設標頭
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // httpClient.Timeout = TimeSpan.FromSeconds(10); // 設定請求逾時時間
        }        /// <summary>
        /// 通用請求發送方法，實現所有接口的基本通訊邏輯
        /// </summary>
        /// <typeparam name="TRequestData">請求資料類型</typeparam>
        /// <typeparam name="TResponseData">回應資料類型</typeparam>
        /// <param name="serviceName">服務名稱，對應接口規範中的接口標識</param>
        /// <param name="relativeUrl">相對 URL 地址，對應接口規範中的接口地址</param>
        /// <param name="data">請求資料列表</param>
        /// <param name="operatorName">操作員名稱，通常由設備提供</param>
        /// <returns>標準化的回應物件，包含狀態碼、訊息和資料</returns>
        /// <remarks>
        /// 此方法實現了接口規範中的以下需求：
        /// 1. 請求方式：POST
        /// 2. Content-Type：application/json
        /// 3. 斷網處理：每隔指定秒數重試呼叫接口
        /// 4. 錯誤處理：若接口返回報錯（Status 非 0000），設備需顯示異常資訊
        /// </remarks>
        private async Task<BaseResponse<TResponseData>> SendRequestAsync<TRequestData, TResponseData>(string serviceName, string relativeUrl, List<TRequestData> data, string operatorName = null)
        {
            // 建立符合 MES 接口規範的標準請求物件
            var request = new BaseRequest<TRequestData>
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceName = serviceName,
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DevCode = this.devCode,
                Operator = operatorName,
                Data = data,
                ExtendData = null
            };
            // 序列化請求物件為 JSON 字串
            string jsonRequest = JsonConvert.SerializeObject(request);
            // 組合完整 URL
            string fullUrl = $"{baseApiUrl}{relativeUrl}";
            int attempt = 0;
            while (attempt <= maxRetries)
            {
                attempt++;
                try
                {
                    Console.WriteLine($"嘗試 {attempt}：發送 {serviceName} 請求至 {fullUrl}");
                    using (var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json"))
                    {
                        HttpResponseMessage response = await httpClient.PostAsync(fullUrl, content);
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"{serviceName} 回應狀態: {response.StatusCode}");
                        Console.WriteLine($"{serviceName} 回應內容: {jsonResponse}");
                        if (response.IsSuccessStatusCode)
                        {
                            var baseResponse = JsonConvert.DeserializeObject<BaseResponse<TResponseData>>(jsonResponse);
                            if (baseResponse.Status != "0000")
                            {
                                Console.WriteLine($"{serviceName} 錯誤：狀態 {baseResponse.Status} - {baseResponse.Message}");
                                return baseResponse;
                            }
                            return baseResponse;
                        }
                        else
                        {
                            Console.WriteLine($"{serviceName} HTTP 錯誤 {response.StatusCode}");
                            if (attempt > maxRetries)
                            {
                                return CreateErrorResponse<TResponseData>(request.RequestID, serviceName, $"HTTP 錯誤 {response.StatusCode}");
                            }
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"{serviceName} 網路錯誤（嘗試 {attempt}）：{httpEx.Message}");
                    if (attempt > maxRetries)
                    {
                        return CreateErrorResponse<TResponseData>(request.RequestID, serviceName, $"網路錯誤：{httpEx.Message}");
                    }
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"{serviceName} JSON 解析錯誤：{jsonEx.Message}");
                    return CreateErrorResponse<TResponseData>(request.RequestID, serviceName, $"回應 JSON 解析錯誤：{jsonEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{serviceName} 未預期錯誤（嘗試 {attempt}）：{ex.Message}");
                    if (attempt > maxRetries)
                    {
                        return CreateErrorResponse<TResponseData>(request.RequestID, serviceName, $"未預期錯誤：{ex.Message}");
                    }
                }
                if (attempt <= maxRetries)
                {
                    Console.WriteLine($"等待 {retryDelaySeconds} 秒後將重試 {serviceName}...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }
            }
            return CreateErrorResponse<TResponseData>(request.RequestID, serviceName, "已達最大重試次數。");
        }

        /// <summary>
        /// 建立標準化的錯誤回應物件
        /// </summary>
        /// <typeparam name="T">回應資料的類型</typeparam>
        /// <param name="requestID">請求 ID，與原始請求相同</param>
        /// <param name="serviceName">服務名稱，與原始請求相同</param>
        /// <param name="message">錯誤訊息</param>
        /// <returns>包含錯誤資訊的標準回應物件</returns>
        /// <remarks>
        /// 根據接口規範，回應格式必須遵循標準化結構。
        /// 對於錯誤情況，使用 "9999" 作為客戶端錯誤碼。
        /// </remarks>
        private BaseResponse<T> CreateErrorResponse<T>(string requestID, string serviceName, string message)
        {
            return new BaseResponse<T>
            {
                RequestID = requestID,        // 與請求時相同
                ServiceName = serviceName,    // 與請求時相同
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), // 回應時間
                DevCode = this.devCode,       // 設備編碼
                Status = "9999",              // 自訂的客戶端錯誤碼，非 "0000" 表示失敗
                Message = message,            // 錯誤訊息
                Data = default,               // 或 new List<T>() 如果 T 是 List
                ExtendData = null             // 擴展資料，保留欄位
            };
        }

        // --- 具體 API 呼叫方法 (設備 -> IoT) ---

        // 2. 設備狀態資訊
        public async Task<BaseResponse<object>> SendDeviceStatusAsync(DeviceStatusData statusData, string operatorName = null)
        {
            return await SendRequestAsync<DeviceStatusData, object>(
                "DEVICE_STATUS_MESSAGE",
                "/api/deviceStatusMessage",
                new List<DeviceStatusData> { statusData },
                operatorName
            );
        }

        // 3. 設備報警資訊
        public async Task<BaseResponse<object>> SendDeviceWarningAsync(DeviceWarningData warningData, string operatorName = null)
        {
            return await SendRequestAsync<DeviceWarningData, object>(
                "DEVICE_WARNING_MESSAGE",
                "/api/deviceWarningMessage",
                new List<DeviceWarningData> { warningData },
                operatorName
            );
        }

        // 4. 設備生產實時數據監控
        public async Task<BaseResponse<object>> SendDeviceParamAsync(DeviceParamData paramData, string operatorName = null)
        {
             // 注意：DeviceParamData 可能包含動態屬性，序列化時需確保正確處理
            return await SendRequestAsync<DeviceParamData, object>(
                "DEVICE_PARAM_REQUEST",
                "/api/deviceParamMessage",
                new List<DeviceParamData> { paramData },
                operatorName
            );
        }

        // 5. 設備事件起止時間上傳
        public async Task<BaseResponse<EventTimeResponseData>> SendEventTimeAsync(EventTimeData eventData, string operatorName = null)
        {
            // 注意：響應中有 data 欄位
            return await SendRequestAsync<EventTimeData, EventTimeResponseData>(
                "EVENT_TIME_MESSAGE",
                "/api/eventTimeMessage",
                new List<EventTimeData> { eventData },
                operatorName
            );
        }

        // 7. 設備聯網狀態監控功能
        public async Task<BaseResponse<DeviceHeartbeatResponseData>> SendHeartbeatAsync(DeviceHeartbeatData heartbeatData, string operatorName = null)
        {
             // 注意：響應中有 data 欄位
            return await SendRequestAsync<DeviceHeartbeatData, DeviceHeartbeatResponseData>(
                "DEVICE_HEARTBEAT_MESSAGE",
                "/api/deviceHeartbeatMessage",
                new List<DeviceHeartbeatData> { heartbeatData },
                operatorName
            );
        }

        // 8. 設備點檢參數報告
        public async Task<BaseResponse<object>> SendKeycheckingAsync(DeviceKeycheckingData keycheckingData, string operatorName = null)
        {
            // 注意：traceParams 是 object，可能需要特定處理或確保序列化正確
            return await SendRequestAsync<DeviceKeycheckingData, object>(
                "DEVICE_KEYCHECKING_REQUEST",
                "/api/deviceKeycheckingRequest",
                new List<DeviceKeycheckingData> { keycheckingData },
                operatorName
            );
        }

        // 9. 設備採集到盒子碼上報
        public async Task<BaseResponse<object>> SendVehicleUploadAsync(DeviceVehicleUploadData vehicleData, string operatorName = null)
        {
            return await SendRequestAsync<DeviceVehicleUploadData, object>(
                "DEVICE_VEHICLE_UPLOAD",
                "/api/DeviceVehicleUpload", // 注意大小寫
                new List<DeviceVehicleUploadData> { vehicleData },
                operatorName
            );
        }

        // 11. 盒子做完上報資訊接口
        public async Task<BaseResponse<object>> SendBatchCompleteAsync(BatchCompleteData completeData, string operatorName = null)
        {
            return await SendRequestAsync<BatchCompleteData, object>(
                "BATCH_COMPLETE_MESSAGE",
                "/api/batchCompleteMessage",
                new List<BatchCompleteData> { completeData },
                operatorName
            );
        }

        // 12. 良品盒生成完成上報
        public async Task<BaseResponse<object>> SendBatchReportedAsync(BatchReportedData reportedData, string operatorName = null)
        {
            return await SendRequestAsync<BatchReportedData, object>(
                "BATCH_REPORTED_MESSAGE",
                "/api/batchReportedMessage",
                new List<BatchReportedData> { reportedData },
                operatorName
            );
        }

        // 14. 設備模式
        public async Task<BaseResponse<object>> SendDeviceModeAsync(DeviceModeData modeData, string operatorName = null)
        {
            return await SendRequestAsync<DeviceModeData, object>(
                "DEVICE_MODE_MESSAGE",
                "/api/DeviceMode", // 注意大小寫
                new List<DeviceModeData> { modeData },
                operatorName
            );
        }

        // 15. 配方參數修改報告
        public async Task<BaseResponse<object>> SendDeviceRecipeAsync(DeviceRecipeData recipeData, string operatorName = null)
        {
             // 注意：params 是 object，可能需要特定處理或確保序列化正確
            return await SendRequestAsync<DeviceRecipeData, object>(
                "DEVICE_RECIPE_MESSAGE",
                "/api/DeviceRecipe", // 注意大小寫
                new List<DeviceRecipeData> { recipeData },
                operatorName
            );
        }
    }
}
