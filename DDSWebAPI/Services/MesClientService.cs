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
    public class MesClientService : IDisposable
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

        /// <summary>
        /// 2.9 鑽針履歷回報 (DRILL_HISTORY_REPORT_MESSAGE)
        /// </summary>
        public async Task<BaseResponse> SendDrillHistoryReportAsync(DrillHistoryReportData historyData, string operatorName = "SYSTEM")
        {
            var request = CreateBaseRequest("DRILL_HISTORY_REPORT_MESSAGE", new List<DrillHistoryReportData> { historyData }, operatorName);
            return await SendRequestAsync($"{_mesEndpoint}/api/drill_history_report", request);
        }        private BaseRequest<T> CreateBaseRequest<T>(string serviceName, List<T> data, string operatorName)
        {
            return new BaseRequest<T>
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceName = serviceName,
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
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
                {                    return new BaseResponse
                    {
                        RequestId = request.RequestID,
                        Success = false,
                        Message = $"HTTP {response.StatusCode}: {responseContent}",
                        Timestamp = DateTime.Now
                    };
                }
            }
            catch (Exception ex)
            {                return new BaseResponse
                {
                    RequestId = request.RequestID,
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
