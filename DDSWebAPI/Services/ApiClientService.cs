using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DDSWebAPI.Models;
using DDSWebAPI.Events;

namespace DDSWebAPI.Services
{
    /// <summary>
    /// API 用戶端服務，用於向 MES/IoT 系統發送資料
    /// </summary>
    public class ApiClientService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private string _baseUrl;

        #region 事件定義

        /// <summary>
        /// API 呼叫成功事件
        /// </summary>
        public event EventHandler<ApiCallSuccessEventArgs> ApiCallSuccess;

        /// <summary>
        /// API 呼叫失敗事件
        /// </summary>
        public event EventHandler<ApiCallFailureEventArgs> ApiCallFailure;

        #endregion

        #region 建構函式

        /// <summary>
        /// 初始化 API 用戶端服務
        /// </summary>
        /// <param name="baseUrl">基礎 URL</param>
        /// <param name="timeout">請求逾時時間（秒）</param>
        public ApiClientService(string baseUrl, int timeout = 30)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };

            // 設定預設標頭
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DDSWebAPI-Client/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            // 設定預設 API 金鑰
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer KINSUS-API-KEY-2024");
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 設定基礎 URL
        /// </summary>
        /// <param name="baseUrl">基礎 URL</param>
        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
        }

        /// <summary>
        /// 發送配針回報上傳
        /// </summary>
        /// <param name="request">配針回報請求</param>
        /// <returns>回應結果</returns>
        public async Task<ApiCallResult> SendToolOutputReportAsync(BaseRequest<ToolOutputReportData> request)
        {
            request.ServiceName = ApiServiceNames.ToolOutputReportMessage;
            return await SendPostRequestAsync("/api/tool-output-report", request);
        }

        /// <summary>
        /// 發送錯誤回報上傳
        /// </summary>
        /// <param name="request">錯誤回報請求</param>
        /// <returns>回應結果</returns>
        public async Task<ApiCallResult> SendErrorReportAsync(BaseRequest<ErrorReportData> request)
        {
            request.ServiceName = ApiServiceNames.ErrorReportMessage;
            return await SendPostRequestAsync("/api/error-report", request);
        }

        /// <summary>
        /// 發送機臺狀態上報
        /// </summary>
        /// <param name="request">機臺狀態請求</param>
        /// <returns>回應結果</returns>
        public async Task<ApiCallResult> SendMachineStatusReportAsync(BaseRequest<MachineStatusReportData> request)
        {
            request.ServiceName = ApiServiceNames.MachineStatusReportMessage;
            return await SendPostRequestAsync("/api/machine-status-report", request);
        }

        /// <summary>
        /// 發送自訂 API 請求
        /// </summary>
        /// <param name="endpoint">API 端點</param>
        /// <param name="request">請求物件</param>
        /// <returns>回應結果</returns>
        public async Task<ApiCallResult> SendCustomRequestAsync(string endpoint, object request)
        {
            return await SendPostRequestAsync(endpoint, request);
        }

        /// <summary>
        /// 設定 API 金鑰
        /// </summary>
        /// <param name="apiKey">API 金鑰</param>
        public void SetApiKey(string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 發送 POST 請求
        /// </summary>
        /// <param name="endpoint">API 端點</param>
        /// <param name="request">請求物件</param>
        /// <returns>回應結果</returns>
        private async Task<ApiCallResult> SendPostRequestAsync(string endpoint, object request)
        {
            var result = new ApiCallResult();
            
            try
            {
                if (string.IsNullOrEmpty(_baseUrl))
                {
                    throw new InvalidOperationException("基礎 URL 尚未設定");
                }

                string url = $"{_baseUrl}{endpoint}";
                string jsonContent = JsonConvert.SerializeObject(request, Formatting.Indented);
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                result.RequestUrl = url;
                result.RequestBody = jsonContent;
                result.RequestTime = DateTime.Now;

                var response = await _httpClient.PostAsync(url, content);
                  result.ResponseTime = DateTime.Now;
                result.StatusCode = (int)response.StatusCode;
                result.IsSuccess = response.IsSuccessStatusCode;

                if (response.Content != null)
                {
                    result.ResponseBody = await response.Content.ReadAsStringAsync();
                    result.ResponseData = result.ResponseBody; // 設定 ResponseData
                }

                if (result.IsSuccess)
                {
                    OnApiCallSuccess(result);
                }
                else
                {
                    result.ErrorMessage = $"HTTP 錯誤: {response.StatusCode} - {response.ReasonPhrase}";
                    OnApiCallFailure(result);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "請求逾時";
                result.Exception = ex;
                OnApiCallFailure(result);
            }
            catch (HttpRequestException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"網路錯誤: {ex.Message}";
                result.Exception = ex;
                OnApiCallFailure(result);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"未預期的錯誤: {ex.Message}";
                result.Exception = ex;
                OnApiCallFailure(result);
            }

            return result;
        }

        #endregion

        #region 事件觸發方法        /// <summary>
        /// 觸發 API 呼叫成功事件
        /// </summary>
        private void OnApiCallSuccess(ApiCallResult result)
        {
            ApiCallSuccess?.Invoke(this, new ApiCallSuccessEventArgs(result.RequestUrl, result.ResponseData));
        }

        /// <summary>
        /// 觸發 API 呼叫失敗事件
        /// </summary>
        private void OnApiCallFailure(ApiCallResult result)
        {
            ApiCallFailure?.Invoke(this, new ApiCallFailureEventArgs(result.RequestUrl, result.ErrorMessage));
        }

        #endregion

        #region IDisposable 實作

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion
    }

    #region 輔助類別

    /// <summary>
    /// API 呼叫結果
    /// </summary>
    public class ApiCallResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// HTTP 狀態碼
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// 請求 URL
        /// </summary>
        public string RequestUrl { get; set; }

        /// <summary>
        /// 請求內容
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// 回應內容
        /// </summary>
        public string ResponseBody { get; set; }

        /// <summary>
        /// 請求時間
        /// </summary>
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 回應時間
        /// </summary>
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 處理時間（毫秒）
        /// </summary>
        public double ProcessingTimeMs => (ResponseTime - RequestTime).TotalMilliseconds;

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }        /// <summary>
        /// 例外物件
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 回應資料        /// </summary>
        public string ResponseData { get; set; }
    }

    #endregion
}
