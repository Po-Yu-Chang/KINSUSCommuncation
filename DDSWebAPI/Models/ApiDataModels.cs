using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DDSWebAPI.Models
{
    #region 遠程資訊下發指令相關

    /// <summary>
    /// 遠程資訊下發指令資料
    /// </summary>
    public class SendMessageData
    {
        /// <summary>
        /// 訊息內容
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// 訊息等級 (info, warning, error)
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; set; }

        /// <summary>
        /// 優先級 (low, normal, high)
        /// </summary>
        [JsonProperty("priority")]
        public string Priority { get; set; }

        /// <summary>
        /// 動作類型
        /// </summary>
        [JsonProperty("actionType")]
        public int ActionType { get; set; }

        /// <summary>
        /// 間隔時間（秒）
        /// </summary>
        [JsonProperty("intervalSecondTime")]
        public int IntervalSecondTime { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region 派針工單建立指令相關

    /// <summary>
    /// 派針工單建立指令資料
    /// </summary>
    public class CreateNeedleWorkorderData
    {
        /// <summary>
        /// 任務 ID
        /// </summary>
        [JsonProperty("taskID")]
        public string TaskID { get; set; }

        /// <summary>
        /// 工單號
        /// </summary>
        [JsonProperty("workOrder")]
        public string WorkOrder { get; set; }

        /// <summary>
        /// T Package
        /// </summary>
        [JsonProperty("tPackage")]
        public string TPackage { get; set; }

        /// <summary>
        /// 堆疊數量
        /// </summary>
        [JsonProperty("stackCount")]
        public int StackCount { get; set; }

        /// <summary>
        /// 總片數
        /// </summary>
        [JsonProperty("totalSheets")]
        public int TotalSheets { get; set; }

        /// <summary>
        /// 開始時間
        /// </summary>
        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// 結束時間
        /// </summary>
        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        /// <summary>
        /// 優先級
        /// </summary>
        [JsonProperty("priority")]
        public string Priority { get; set; }

        /// <summary>
        /// 是否為緊急工單
        /// </summary>
        [JsonProperty("isUrgent")]
        public bool IsUrgent { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 派針工單建立請求
    /// </summary>
    public class CreateNeedleWorkorderRequest : BaseRequest<CreateNeedleWorkorderData>
    {
        /// <summary>
        /// 總片數
        /// </summary>
        [JsonProperty("AllPlate")]
        public int AllPlate { get; set; }

        /// <summary>
        /// 壓板數
        /// </summary>
        [JsonProperty("Pressplatens")]
        public int Pressplatens { get; set; }
    }

    #endregion

    #region 設備時間同步指令相關

    /// <summary>
    /// 設備時間同步指令資料
    /// </summary>
    public class DateMessageData
    {
        /// <summary>
        /// 同步時間
        /// </summary>
        [JsonProperty("syncTime")]
        public string SyncTime { get; set; }

        /// <summary>
        /// 時區資訊
        /// </summary>
        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region 刀具工鑽袋檔發送指令相關

    /// <summary>
    /// 刀具工鑽袋檔發送指令資料
    /// </summary>
    public class SwitchRecipeData
    {
        /// <summary>
        /// 配方檔案路徑
        /// </summary>
        [JsonProperty("recipeFilePath")]
        public string RecipeFilePath { get; set; }

        /// <summary>
        /// 配方名稱
        /// </summary>
        [JsonProperty("recipeName")]
        public string RecipeName { get; set; }

        /// <summary>
        /// 配方版本
        /// </summary>
        [JsonProperty("recipeVersion")]
        public string RecipeVersion { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region 設備啟停控制指令相關

    /// <summary>
    /// 設備啟停控制指令資料
    /// </summary>
    public class DeviceControlData
    {
        /// <summary>
        /// 控制動作 (start, stop, pause, resume)
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        /// <summary>
        /// 控制模式 (auto, manual)
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region 倉庫資源查詢指令相關

    /// <summary>
    /// 倉庫資源查詢指令資料
    /// </summary>
    public class WarehouseResourceQueryData
    {
        /// <summary>
        /// 查詢類型 (all, byType, byStatus)
        /// </summary>
        [JsonProperty("queryType")]
        public string QueryType { get; set; }

        /// <summary>
        /// 資源類型
        /// </summary>
        [JsonProperty("resourceType")]
        public string ResourceType { get; set; }

        /// <summary>
        /// 資源狀態
        /// </summary>
        [JsonProperty("resourceStatus")]
        public string ResourceStatus { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region 鑽針履歷查詢指令相關

    /// <summary>
    /// 鑽針履歷查詢指令資料
    /// </summary>
    public class ToolTraceHistoryQueryData
    {
        /// <summary>
        /// 鑽針 ID
        /// </summary>
        [JsonProperty("toolId")]
        public string ToolId { get; set; }

        /// <summary>
        /// 查詢開始時間
        /// </summary>
        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// 查詢結束時間
        /// </summary>
        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region 配針回報上傳相關

    /// <summary>
    /// 配針回報上傳資料
    /// </summary>
    public class ToolOutputReportData
    {
        /// <summary>
        /// 工單號
        /// </summary>
        [JsonProperty("workOrder")]
        public string WorkOrder { get; set; }

        /// <summary>
        /// 刀具 ID
        /// </summary>
        [JsonProperty("toolId")]
        public string ToolId { get; set; }

        /// <summary>
        /// 配針結果 (success, failure)
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; }

        /// <summary>
        /// 配針時間
        /// </summary>
        [JsonProperty("processTime")]
        public string ProcessTime { get; set; }

        /// <summary>
        /// 配針數量
        /// </summary>
        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region 錯誤回報上傳相關

    /// <summary>
    /// 錯誤回報上傳資料
    /// </summary>
    public class ErrorReportData
    {
        /// <summary>
        /// 錯誤代碼
        /// </summary>
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 錯誤等級 (low, medium, high, critical)
        /// </summary>
        [JsonProperty("errorLevel")]
        public string ErrorLevel { get; set; }

        /// <summary>
        /// 發生時間
        /// </summary>
        [JsonProperty("occurTime")]
        public string OccurTime { get; set; }

        /// <summary>
        /// 相關工單號
        /// </summary>
        [JsonProperty("workOrder")]
        public string WorkOrder { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region 機臺狀態上報相關

    /// <summary>
    /// 機臺狀態上報資料
    /// </summary>
    public class MachineStatusReportData
    {
        /// <summary>
        /// 機臺狀態 (idle, running, error, maintenance)
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// CPU 使用率
        /// </summary>
        [JsonProperty("cpuUsage")]
        public float CpuUsage { get; set; }

        /// <summary>
        /// 記憶體使用率
        /// </summary>
        [JsonProperty("memoryUsage")]
        public float MemoryUsage { get; set; }

        /// <summary>
        /// 磁碟使用率
        /// </summary>
        [JsonProperty("diskUsage")]
        public float DiskUsage { get; set; }

        /// <summary>
        /// 溫度
        /// </summary>
        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        /// <summary>
        /// 上報時間
        /// </summary>
        [JsonProperty("reportTime")]
        public string ReportTime { get; set; }

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    #endregion

    #region API 服務名稱常數

    /// <summary>
    /// API 服務名稱常數
    /// </summary>
    public static class ApiServiceNames
    {
        #region 伺服端角色 API (接收)

        /// <summary>
        /// 遠程資訊下發指令
        /// </summary>
        public const string SendMessageCommand = "SEND_MESSAGE_COMMAND";

        /// <summary>
        /// 派針工單建立指令
        /// </summary>
        public const string CreateNeedleWorkorderCommand = "CREATE_NEEDLE_WORKORDER_COMMAND";

        /// <summary>
        /// 設備時間同步指令
        /// </summary>
        public const string DateMessageCommand = "DATE_MESSAGE_COMMAND";

        /// <summary>
        /// 刀具工鑽袋檔發送指令
        /// </summary>
        public const string SwitchRecipeCommand = "SWITCH_RECIPE_COMMAND";

        /// <summary>
        /// 設備啟停控制指令  
        /// </summary>
        public const string DeviceControlCommand = "DEVICE_CONTROL_COMMAND";

        /// <summary>
        /// 倉庫資源查詢指令
        /// </summary>
        public const string WarehouseResourceQueryCommand = "WAREHOUSE_RESOURCE_QUERY_COMMAND";

        /// <summary>
        /// 鑽針履歷查詢指令
        /// </summary>
        public const string ToolTraceHistoryQueryCommand = "TOOL_TRACE_HISTORY_QUERY_COMMAND";

        #endregion

        #region 用戶端角色 API (發送)

        /// <summary>
        /// 配針回報上傳
        /// </summary>
        public const string ToolOutputReportMessage = "TOOL_OUTPUT_REPORT_MESSAGE";

        /// <summary>
        /// 錯誤回報上傳
        /// </summary>
        public const string ErrorReportMessage = "ERROR_REPORT_MESSAGE";

        /// <summary>
        /// 機臺狀態上報
        /// </summary>
        public const string MachineStatusReportMessage = "MACHINE_STATUS_REPORT_MESSAGE";

        #endregion
    }

    #endregion
}
