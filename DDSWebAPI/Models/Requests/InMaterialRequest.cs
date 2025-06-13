///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: InMaterialRequest.cs
// 檔案描述: 入料請求資料模型
// 功能概述: 用於封裝入料 API 的請求參數
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;

namespace DDSWebAPI.Models.Requests
{
    /// <summary>
    /// 入料請求資料模型
    /// 用於封裝入料 API 的請求參數
    /// </summary>
    public class InMaterialRequest
    {
        /// <summary>
        /// 是否為連續入料模式
        /// true: 連續入料，false: 指定數量入料
        /// </summary>
        [JsonProperty("isContinue")]
        public bool IsContinue { get; set; }

        /// <summary>
        /// 指定入料盒數 (當非連續模式時必填)
        /// </summary>
        [JsonProperty("inBoxQty")]
        public int? InBoxQty { get; set; }

        /// <summary>
        /// 入料優先等級 (可選)
        /// 1: 高優先, 2: 中優先, 3: 低優先
        /// </summary>
        [JsonProperty("priority")]
        public int? Priority { get; set; }

        /// <summary>
        /// 目標儲存區域 (可選)
        /// </summary>
        [JsonProperty("targetArea")]
        public string TargetArea { get; set; }
    }
}
