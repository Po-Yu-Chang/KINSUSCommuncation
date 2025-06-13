using Newtonsoft.Json;
using System.Collections.Generic;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 基礎請求類別
    /// </summary>
    /// <typeparam name="T">資料類型</typeparam>
    public class BaseRequest<T>
    {
        /// <summary>
        /// 請求唯一識別碼
        /// </summary>
        [JsonProperty("requestID")]
        public string RequestID { get; set; }

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
        /// 操作人員
        /// </summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }

        /// <summary>
        /// 操作類型 (相容性屬性)
        /// </summary>
        [JsonProperty("operationType")]
        public string OperationType { get; set; }

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
    /// 基礎請求類別（單一資料項目）
    /// </summary>
    /// <typeparam name="T">資料類型</typeparam>
    public class BaseSingleRequest<T>
    {
        /// <summary>
        /// 請求唯一識別碼
        /// </summary>
        [JsonProperty("requestID")]
        public string RequestID { get; set; }

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
        /// 操作人員
        /// </summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }

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
    /// 非泛型基礎請求類別（用於動態處理）
    /// </summary>
    public class BaseRequest
    {
        /// <summary>
        /// 請求唯一識別碼
        /// </summary>
        [JsonProperty("requestID")]
        public string RequestId { get; set; }

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
        /// 操作人員
        /// </summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }

        /// <summary>
        /// 資料內容（動態物件）
        /// </summary>
        [JsonProperty("data")]
        public object Data { get; set; }

        /// <summary>
        /// 操作類型 (相容性屬性)
        /// </summary>
        [JsonProperty("operationType")]
        public string OperationType { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }
}
