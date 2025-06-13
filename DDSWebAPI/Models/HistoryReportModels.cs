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

        [JsonProperty("totalHoles")]
        public int TotalHoles { get; set; }

        [JsonProperty("currentHoles")]
        public int CurrentHoles { get; set; }

        [JsonProperty("diameter")]
        public double Diameter { get; set; }

        [JsonProperty("speed")]
        public int Speed { get; set; }

        [JsonProperty("feed")]
        public double Feed { get; set; }

        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("toolCondition")]
        public string ToolCondition { get; set; }

        [JsonProperty("warningCode")]
        public string WarningCode { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 工具履歷統計資料
    /// </summary>
    public class ToolHistorySummary
    {
        [JsonProperty("toolId")]
        public string ToolId { get; set; }

        [JsonProperty("totalUsageTime")]
        public TimeSpan TotalUsageTime { get; set; }

        [JsonProperty("totalHoles")]
        public int TotalHoles { get; set; }

        [JsonProperty("grindHistory")]
        public List<GrindRecord> GrindHistory { get; set; }

        [JsonProperty("warningHistory")]
        public List<WarningRecord> WarningHistory { get; set; }

        [JsonProperty("lastMaintenanceDate")]
        public DateTime? LastMaintenanceDate { get; set; }

        [JsonProperty("nextMaintenanceDate")]
        public DateTime? NextMaintenanceDate { get; set; }
    }

    /// <summary>
    /// 研磨記錄
    /// </summary>
    public class GrindRecord
    {
        [JsonProperty("grindDate")]
        public DateTime GrindDate { get; set; }

        [JsonProperty("grindType")]
        public string GrindType { get; set; }

        [JsonProperty("beforeDiameter")]
        public double BeforeDiameter { get; set; }

        [JsonProperty("afterDiameter")]
        public double AfterDiameter { get; set; }

        [JsonProperty("operator")]
        public string Operator { get; set; }
    }

    /// <summary>
    /// 警告記錄
    /// </summary>
    public class WarningRecord
    {
        [JsonProperty("warningDate")]
        public DateTime WarningDate { get; set; }

        [JsonProperty("warningCode")]
        public string WarningCode { get; set; }

        [JsonProperty("warningMessage")]
        public string WarningMessage { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("resolved")]
        public bool Resolved { get; set; }
    }
}
