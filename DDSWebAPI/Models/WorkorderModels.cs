using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 工件資訊
    /// </summary>
    public class WorkPieceInfo
    {
        [JsonProperty("pieceId")]
        public string PieceId { get; set; }

        [JsonProperty("partNumber")]
        public string PartNumber { get; set; }

        [JsonProperty("revision")]
        public string Revision { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("thickness")]
        public double Thickness { get; set; }

        [JsonProperty("material")]
        public string Material { get; set; }

        [JsonProperty("specifications")]
        public Dictionary<string, object> Specifications { get; set; }
    }

    /// <summary>
    /// 工具需求資訊
    /// </summary>
    public class ToolRequirement
    {
        [JsonProperty("stationId")]
        public string StationId { get; set; }

        [JsonProperty("spindleId")]
        public string SpindleId { get; set; }

        [JsonProperty("toolType")]
        public string ToolType { get; set; }

        [JsonProperty("toolDiameter")]
        public double ToolDiameter { get; set; }

        [JsonProperty("toolLength")]
        public double ToolLength { get; set; }

        [JsonProperty("requiredQuantity")]
        public int RequiredQuantity { get; set; }

        [JsonProperty("specifications")]
        public Dictionary<string, object> Specifications { get; set; }
    }

    /// <summary>
    /// 工單狀態資料
    /// </summary>
    public class WorkorderStatusData
    {
        [JsonProperty("workOrder")]
        public string WorkOrder { get; set; }

        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("progress")]
        public WorkorderProgress Progress { get; set; }

        [JsonProperty("currentStation")]
        public string CurrentStation { get; set; }

        [JsonProperty("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonProperty("estimatedCompletionTime")]
        public DateTime? EstimatedCompletionTime { get; set; }

        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("qualityStatus")]
        public string QualityStatus { get; set; }

        [JsonProperty("remarks")]
        public string Remarks { get; set; }
    }

    /// <summary>
    /// 工單進度資料
    /// </summary>
    public class WorkorderProgress
    {
        [JsonProperty("completedSheets")]
        public int CompletedSheets { get; set; }

        [JsonProperty("totalSheets")]
        public int TotalSheets { get; set; }

        [JsonProperty("completedHoles")]
        public int CompletedHoles { get; set; }

        [JsonProperty("totalHoles")]
        public int TotalHoles { get; set; }

        [JsonProperty("percentageComplete")]
        public double PercentageComplete { get; set; }

        [JsonProperty("stationProgress")]
        public List<StationProgress> StationProgress { get; set; }
    }

    /// <summary>
    /// 工站進度資料
    /// </summary>
    public class StationProgress
    {
        [JsonProperty("stationId")]
        public string StationId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("completedCount")]
        public int CompletedCount { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// 工單建立回應資料
    /// </summary>
    public class CreateWorkorderResponse
    {
        [JsonProperty("workOrder")]
        public string WorkOrder { get; set; }

        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonProperty("estimatedStartTime")]
        public DateTime? EstimatedStartTime { get; set; }

        [JsonProperty("estimatedDuration")]
        public TimeSpan? EstimatedDuration { get; set; }

        [JsonProperty("assignedStations")]
        public List<string> AssignedStations { get; set; }

        [JsonProperty("toolAllocation")]
        public List<ToolAllocation> ToolAllocation { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// 工具分配資料
    /// </summary>
    public class ToolAllocation
    {
        [JsonProperty("stationId")]
        public string StationId { get; set; }

        [JsonProperty("spindleId")]
        public string SpindleId { get; set; }

        [JsonProperty("toolId")]
        public string ToolId { get; set; }

        [JsonProperty("toolType")]
        public string ToolType { get; set; }

        [JsonProperty("allocationStatus")]
        public string AllocationStatus { get; set; }

        [JsonProperty("remarks")]
        public string Remarks { get; set; }
    }
}
