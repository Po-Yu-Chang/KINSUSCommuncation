using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DDSWebAPI.Models
{    /// <summary>
    /// 配針回報上傳資料 (TOOL_OUTPUT_REPORT_MESSAGE)
    /// 注意：此類別已移至 ApiDataModels.cs，請使用該檔案中的定義
    /// </summary>
    [Obsolete("請使用 ApiDataModels.cs 中的 ToolOutputReportData")]
    public class ToolOutputReportData_Obsolete
    {
        [JsonProperty("workorder")]
        public string WorkOrder { get; set; }

        [JsonProperty("recipe")]
        public string Recipe { get; set; }

        [JsonProperty("station")]
        public string Station { get; set; }

        [JsonProperty("spindle")]
        public string Spindle { get; set; }

        [JsonProperty("toolcode")]
        public string ToolCode { get; set; }

        [JsonProperty("tooltype")]
        public string ToolType { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("starttime")]
        public DateTime StartTime { get; set; }

        [JsonProperty("endtime")]
        public DateTime EndTime { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }    /// <summary>
    /// 錯誤回報上傳資料 (ERROR_REPORT_MESSAGE)
    /// </summary>
    public class ClientErrorReportData
    {
        [JsonProperty("workorder")]
        public string WorkOrder { get; set; }

        [JsonProperty("recipe")]
        public string Recipe { get; set; }

        [JsonProperty("station")]
        public string Station { get; set; }

        [JsonProperty("spindle")]
        public string Spindle { get; set; }

        [JsonProperty("errorcode")]
        public string ErrorCode { get; set; }

        [JsonProperty("errormessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }
    }    /// <summary>
    /// 機臺狀態上報資料 (MACHINE_STATUS_REPORT_MESSAGE)
    /// </summary>
    public class ClientMachineStatusReportData
    {
        [JsonProperty("station")]
        public string Station { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("workorder")]
        public string WorkOrder { get; set; }

        [JsonProperty("recipe")]
        public string Recipe { get; set; }

        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("details")]
        public Dictionary<string, object> Details { get; set; }
    }

    /// <summary>
    /// 鑽針履歷回報資料 (DRILL_HISTORY_REPORT_MESSAGE)
    /// </summary>
    public class DrillHistoryReportData
    {
        [JsonProperty("workorder")]
        public string WorkOrder { get; set; }

        [JsonProperty("recipe")]
        public string Recipe { get; set; }

        [JsonProperty("station")]
        public string Station { get; set; }

        [JsonProperty("spindle")]
        public string Spindle { get; set; }

        [JsonProperty("drillcode")]
        public string DrillCode { get; set; }

        [JsonProperty("drilltype")]
        public string DrillType { get; set; }

        [JsonProperty("diameter")]
        public double Diameter { get; set; }

        [JsonProperty("depth")]
        public double Depth { get; set; }

        [JsonProperty("speed")]
        public int Speed { get; set; }

        [JsonProperty("feed")]
        public double Feed { get; set; }

        [JsonProperty("totalcount")]
        public int TotalCount { get; set; }

        [JsonProperty("currentcount")]
        public int CurrentCount { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("coordinates")]
        public DrillCoordinates Coordinates { get; set; }
    }

    /// <summary>
    /// 鑽針座標資料
    /// </summary>
    public class DrillCoordinates
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }
    }
}
