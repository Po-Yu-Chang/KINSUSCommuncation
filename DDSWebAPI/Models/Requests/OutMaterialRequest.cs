///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: OutMaterialRequest.cs
// 檔案描述: 出料請求資料模型
// 功能概述: 用於封裝出料 API 的請求參數
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;

namespace DDSWebAPI.Models.Requests
{
    /// <summary>
    /// 出料請求資料模型
    /// 用於封裝出料 API 的請求參數
    /// </summary>
    public class OutMaterialRequest
    {
        /// <summary>
        /// 針具規格資訊
        /// </summary>
        [JsonProperty("pin")]
        public object Pin { get; set; }

        /// <summary>
        /// 出料盒數數量
        /// </summary>
        [JsonProperty("boxQty")]
        public int BoxQty { get; set; }

        /// <summary>
        /// 出料優先等級 (可選)
        /// </summary>
        [JsonProperty("priority")]
        public int? Priority { get; set; }

        /// <summary>
        /// 指定出料區域 (可選)
        /// </summary>
        [JsonProperty("sourceArea")]
        public string SourceArea { get; set; }
    }
}
