using Newtonsoft.Json;
using System.Collections.Generic;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 基礎回應類別
    /// </summary>
    /// <typeparam name="T">資料類型</typeparam>
    public class BaseResponse<T>
    {
        /// <summary>
        /// 回應唯一識別碼
        /// </summary>
        [JsonProperty("responseID")]
        public string ResponseID { get; set; }

        /// <summary>
        /// 服務名稱
        /// </summary>
        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        /// <summary>
        /// 時間戳記
        /// </summary>
        [JsonProperty("timeStamp")]
        public string TimeStamp { get; set; }

        /// <summary>
        /// 設備代碼
        /// </summary>
        [JsonProperty("devCode")]
        public string DevCode { get; set; }

        /// <summary>
        /// 狀態碼
        /// </summary>
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// 狀態訊息
        /// </summary>
        [JsonProperty("statusMessage")]
        public string StatusMessage { get; set; }

        /// <summary>
        /// 資料內容
        /// </summary>
        [JsonProperty("data")]
        public List<T> Data { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 基礎回應類別（單一資料項目）
    /// </summary>
    /// <typeparam name="T">資料類型</typeparam>
    public class BaseSingleResponse<T>
    {
        /// <summary>
        /// 回應唯一識別碼
        /// </summary>
        [JsonProperty("responseID")]
        public string ResponseID { get; set; }

        /// <summary>
        /// 服務名稱
        /// </summary>
        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        /// <summary>
        /// 時間戳記
        /// </summary>
        [JsonProperty("timeStamp")]
        public string TimeStamp { get; set; }

        /// <summary>
        /// 設備代碼
        /// </summary>
        [JsonProperty("devCode")]
        public string DevCode { get; set; }

        /// <summary>
        /// 狀態碼
        /// </summary>
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// 狀態訊息
        /// </summary>
        [JsonProperty("statusMessage")]
        public string StatusMessage { get; set; }

        /// <summary>
        /// 資料內容
        /// </summary>
        [JsonProperty("data")]
        public T Data { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 標準回應狀態碼
    /// </summary>
    public static class ResponseStatusCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        public const int Success = 200;

        /// <summary>
        /// 參數錯誤
        /// </summary>
        public const int BadRequest = 400;

        /// <summary>
        /// 未授權
        /// </summary>
        public const int Unauthorized = 401;

        /// <summary>
        /// 禁止存取
        /// </summary>
        public const int Forbidden = 403;

        /// <summary>
        /// 找不到資源
        /// </summary>
        public const int NotFound = 404;

        /// <summary>
        /// 內部伺服器錯誤
        /// </summary>
        public const int InternalServerError = 500;

        /// <summary>
        /// 服務不可用
        /// </summary>
        public const int ServiceUnavailable = 503;
    }
}
