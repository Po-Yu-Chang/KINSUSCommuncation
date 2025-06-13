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
        /// 工單編號
        /// </summary>
        [JsonProperty("workOrderNo")]
        public string WorkOrderNo { get; set; }

        /// <summary>
        /// 產品型號
        /// </summary>
        [JsonProperty("productModel")]
        public string ProductModel { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// 優先級
        /// </summary>
        [JsonProperty("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// 預計開始時間
        /// </summary>
        [JsonProperty("scheduledStartTime")]
        public DateTime ScheduledStartTime { get; set; }

        /// <summary>
        /// 預計結束時間
        /// </summary>
        [JsonProperty("scheduledEndTime")]
        public DateTime ScheduledEndTime { get; set; }

        /// <summary>
        /// 刀具規格列表
        /// </summary>
        [JsonProperty("toolSpecs")]
        public List<ToolSpecData> ToolSpecs { get; set; } = new List<ToolSpecData>();
    }

    /// <summary>
    /// 刀具規格資料
    /// </summary>
    public class ToolSpecData
    {
        /// <summary>
        /// 刀具代碼
        /// </summary>
        [JsonProperty("toolCode")]
        public string ToolCode { get; set; }

        /// <summary>
        /// 刀具規格
        /// </summary>
        [JsonProperty("toolSpec")]
        public string ToolSpec { get; set; }

        /// <summary>
        /// 需要數量
        /// </summary>
        [JsonProperty("requiredQuantity")]
        public int RequiredQuantity { get; set; }

        /// <summary>
        /// 位置資訊
        /// </summary>
        [JsonProperty("position")]
        public string Position { get; set; }
    }

    #endregion

    #region 設備時間同步指令相關

    /// <summary>
    /// 設備時間同步指令資料
    /// </summary>
    public class DateSyncData
    {
        /// <summary>
        /// 同步時間
        /// </summary>
        [JsonProperty("syncDateTime")]
        public DateTime SyncDateTime { get; set; }

        /// <summary>
        /// 時區資訊
        /// </summary>
        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }

        /// <summary>
        /// NTP 伺服器位址
        /// </summary>
        [JsonProperty("ntpServer")]
        public string NtpServer { get; set; }
    }

    #endregion

    #region 刀具工鑽袋檔發送指令相關

    /// <summary>
    /// 刀具工鑽袋檔發送指令資料
    /// </summary>
    public class SwitchRecipeData
    {
        /// <summary>
        /// 配方檔案名稱
        /// </summary>
        [JsonProperty("recipeFileName")]
        public string RecipeFileName { get; set; }

        /// <summary>
        /// 配方版本
        /// </summary>
        [JsonProperty("recipeVersion")]
        public string RecipeVersion { get; set; }

        /// <summary>
        /// 配方內容（Base64 編碼）
        /// </summary>
        [JsonProperty("recipeContent")]
        public string RecipeContent { get; set; }

        /// <summary>
        /// 工作模式
        /// </summary>
        [JsonProperty("workMode")]
        public string WorkMode { get; set; }

        /// <summary>
        /// 配方類型
        /// </summary>
        [JsonProperty("recipeType")]
        public string RecipeType { get; set; }
    }

    #endregion

    #region 設備啟停控制指令相關

    /// <summary>
    /// 設備啟停控制指令資料
    /// </summary>
    public class DeviceControlData
    {
        /// <summary>
        /// 控制指令 (START, STOP, PAUSE, RESUME, RESET)
        /// </summary>
        [JsonProperty("command")]
        public string Command { get; set; }

        /// <summary>
        /// 目標設備
        /// </summary>
        [JsonProperty("targetDevice")]
        public string TargetDevice { get; set; }

        /// <summary>
        /// 控制參數
        /// </summary>
        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 是否強制執行
        /// </summary>
        [JsonProperty("forceExecute")]
        public bool ForceExecute { get; set; }
    }

    #endregion

    #region 倉庫資源查詢指令相關

    /// <summary>
    /// 倉庫資源查詢指令資料
    /// </summary>
    public class WarehouseResourceQueryData
    {
        /// <summary>
        /// 查詢類型 (ALL, BY_TOOL_CODE, BY_POSITION, BY_STATUS)
        /// </summary>
        [JsonProperty("queryType")]
        public string QueryType { get; set; }

        /// <summary>
        /// 刀具代碼（查詢特定刀具時使用）
        /// </summary>
        [JsonProperty("toolCode")]
        public string ToolCode { get; set; }

        /// <summary>
        /// 位置代碼（查詢特定位置時使用）
        /// </summary>
        [JsonProperty("positionCode")]
        public string PositionCode { get; set; }

        /// <summary>
        /// 狀態篩選（查詢特定狀態時使用）
        /// </summary>
        [JsonProperty("statusFilter")]
        public string StatusFilter { get; set; }
    }

    /// <summary>
    /// 倉庫資源查詢回應資料
    /// </summary>
    public class WarehouseResourceQueryResponse
    {
        /// <summary>
        /// 倉庫資源列表
        /// </summary>
        [JsonProperty("resources")]
        public List<WarehouseResourceData> Resources { get; set; } = new List<WarehouseResourceData>();

        /// <summary>
        /// 總數量
        /// </summary>
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        /// <summary>
        /// 查詢時間
        /// </summary>
        [JsonProperty("queryTime")]
        public DateTime QueryTime { get; set; }
    }

    /// <summary>
    /// 倉庫資源資料
    /// </summary>
    public class WarehouseResourceData
    {
        /// <summary>
        /// 刀具代碼
        /// </summary>
        [JsonProperty("toolCode")]
        public string ToolCode { get; set; }

        /// <summary>
        /// 刀具規格
        /// </summary>
        [JsonProperty("toolSpec")]
        public string ToolSpec { get; set; }

        /// <summary>
        /// 位置代碼
        /// </summary>
        [JsonProperty("positionCode")]
        public string PositionCode { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// 使用次數
        /// </summary>
        [JsonProperty("usageCount")]
        public int UsageCount { get; set; }

        /// <summary>
        /// 剩餘壽命
        /// </summary>
        [JsonProperty("remainingLife")]
        public int RemainingLife { get; set; }

        /// <summary>
        /// 最後更新時間
        /// </summary>
        [JsonProperty("lastUpdateTime")]
        public DateTime LastUpdateTime { get; set; }
    }

    #endregion

    #region 鑽針履歷查詢指令相關

    /// <summary>
    /// 鑽針履歷查詢指令資料
    /// </summary>
    public class ToolTraceHistoryQueryData
    {
        /// <summary>
        /// 刀具代碼
        /// </summary>
        [JsonProperty("toolCode")]
        public string ToolCode { get; set; }

        /// <summary>
        /// 開始時間
        /// </summary>
        [JsonProperty("startTime")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 結束時間
        /// </summary>
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 查詢類型 (USAGE, MOVEMENT, MAINTENANCE, ALL)
        /// </summary>
        [JsonProperty("queryType")]
        public string QueryType { get; set; }

        /// <summary>
        /// 頁碼
        /// </summary>
        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每頁筆數
        /// </summary>
        [JsonProperty("pageSize")]
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// 鑽針履歷查詢回應資料
    /// </summary>
    public class ToolTraceHistoryQueryResponse
    {
        /// <summary>
        /// 履歷記錄列表
        /// </summary>
        [JsonProperty("histories")]
        public List<ToolTraceHistoryData> Histories { get; set; } = new List<ToolTraceHistoryData>();

        /// <summary>
        /// 總筆數
        /// </summary>
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        /// <summary>
        /// 當前頁碼
        /// </summary>
        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        /// <summary>
        /// 總頁數
        /// </summary>
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// 鑽針履歷資料
    /// </summary>
    public class ToolTraceHistoryData
    {
        /// <summary>
        /// 履歷編號
        /// </summary>
        [JsonProperty("historyId")]
        public string HistoryId { get; set; }

        /// <summary>
        /// 刀具代碼
        /// </summary>
        [JsonProperty("toolCode")]
        public string ToolCode { get; set; }

        /// <summary>
        /// 操作類型 (INSTALL, REMOVE, USE, MAINTAIN)
        /// </summary>
        [JsonProperty("operationType")]
        public string OperationType { get; set; }

        /// <summary>
        /// 操作時間
        /// </summary>
        [JsonProperty("operationTime")]
        public DateTime OperationTime { get; set; }

        /// <summary>
        /// 操作人員
        /// </summary>
        [JsonProperty("operatorName")]
        public string OperatorName { get; set; }

        /// <summary>
        /// 原位置
        /// </summary>
        [JsonProperty("fromPosition")]
        public string FromPosition { get; set; }

        /// <summary>
        /// 目標位置
        /// </summary>
        [JsonProperty("toPosition")]
        public string ToPosition { get; set; }

        /// <summary>
        /// 使用次數
        /// </summary>
        [JsonProperty("usageCount")]
        public int UsageCount { get; set; }

        /// <summary>
        /// 操作結果
        /// </summary>
        [JsonProperty("operationResult")]
        public string OperationResult { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [JsonProperty("remarks")]
        public string Remarks { get; set; }
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

    #region 用戶端回報相關資料模型

    /// <summary>
    /// 刀具輸出回報資料
    /// </summary>
    public class ToolOutputReportData
    {
        /// <summary>
        /// 工單編號
        /// </summary>
        [JsonProperty("workOrderNo")]
        public string WorkOrderNo { get; set; }

        /// <summary>
        /// 刀具代碼
        /// </summary>
        [JsonProperty("toolCode")]
        public string ToolCode { get; set; }

        /// <summary>
        /// 刀具規格
        /// </summary>
        [JsonProperty("toolSpec")]
        public string ToolSpec { get; set; }

        /// <summary>
        /// 輸出數量
        /// </summary>
        [JsonProperty("outputQuantity")]
        public int OutputQuantity { get; set; }

        /// <summary>
        /// 操作時間
        /// </summary>
        [JsonProperty("operationTime")]
        public DateTime OperationTime { get; set; }

        /// <summary>
        /// 操作類型
        /// </summary>
        [JsonProperty("operationType")]
        public string OperationType { get; set; }

        /// <summary>
        /// 位置資訊
        /// </summary>
        [JsonProperty("position")]
        public string Position { get; set; }

        /// <summary>
        /// 品質狀態
        /// </summary>
        [JsonProperty("qualityStatus")]
        public string QualityStatus { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [JsonProperty("remarks")]
        public string Remarks { get; set; }
    }

    /// <summary>
    /// 錯誤回報資料
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
        /// 錯誤等級
        /// </summary>
        [JsonProperty("errorLevel")]
        public string ErrorLevel { get; set; }

        /// <summary>
        /// 發生時間
        /// </summary>
        [JsonProperty("occurrenceTime")]
        public DateTime OccurrenceTime { get; set; }

        /// <summary>
        /// 相關設備
        /// </summary>
        [JsonProperty("deviceCode")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 操作人員
        /// </summary>
        [JsonProperty("operatorName")]
        public string OperatorName { get; set; }

        /// <summary>
        /// 詳細描述
        /// </summary>
        [JsonProperty("detailDescription")]
        public string DetailDescription { get; set; }

        /// <summary>
        /// 是否已解決
        /// </summary>
        [JsonProperty("isResolved")]
        public bool IsResolved { get; set; }
    }

    /// <summary>
    /// 機台狀態回報資料
    /// </summary>
    public class MachineStatusReportData
    {
        /// <summary>
        /// 機台狀態
        /// </summary>
        [JsonProperty("machineStatus")]
        public string MachineStatus { get; set; }

        /// <summary>
        /// 運行模式
        /// </summary>
        [JsonProperty("operationMode")]
        public string OperationMode { get; set; }

        /// <summary>
        /// 當前作業
        /// </summary>
        [JsonProperty("currentJob")]
        public string CurrentJob { get; set; }

        /// <summary>
        /// 處理數量
        /// </summary>
        [JsonProperty("processedCount")]
        public int ProcessedCount { get; set; }

        /// <summary>
        /// 目標數量
        /// </summary>
        [JsonProperty("targetCount")]
        public int TargetCount { get; set; }

        /// <summary>
        /// 完成百分比
        /// </summary>
        [JsonProperty("completionPercentage")]
        public double CompletionPercentage { get; set; }

        /// <summary>
        /// 溫度資訊
        /// </summary>
        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// 壓力資訊
        /// </summary>
        [JsonProperty("pressure")]
        public double? Pressure { get; set; }

        /// <summary>
        /// 振動資訊
        /// </summary>
        [JsonProperty("vibration")]
        public double? Vibration { get; set; }

        /// <summary>
        /// 報告時間
        /// </summary>
        [JsonProperty("reportTime")]
        public DateTime ReportTime { get; set; }

        /// <summary>
        /// 警告訊息
        /// </summary>
        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    #endregion

    #region 鑽針履歷回報指令相關

    /// <summary>
    /// 鑽針履歷回報指令資料
    /// </summary>
    public class ToolTraceHistoryReportData
    {
        /// <summary>
        /// 刀具 ID
        /// </summary>
        [JsonProperty("toolId")]
        public string ToolId { get; set; }

        /// <summary>
        /// 軸向
        /// </summary>
        [JsonProperty("axis")]
        public string Axis { get; set; }

        /// <summary>
        /// 機台 ID
        /// </summary>
        [JsonProperty("machineId")]
        public string MachineId { get; set; }

        /// <summary>
        /// 產品代碼
        /// </summary>
        [JsonProperty("product")]
        public string Product { get; set; }

        /// <summary>
        /// 磨損次數
        /// </summary>
        [JsonProperty("grindCount")]
        public int GrindCount { get; set; }

        /// <summary>
        /// 托盤 ID
        /// </summary>
        [JsonProperty("trayId")]
        public string TrayId { get; set; }

        /// <summary>
        /// 托盤位置
        /// </summary>
        [JsonProperty("traySlot")]
        public int TraySlot { get; set; }

        /// <summary>
        /// 使用履歷
        /// </summary>
        [JsonProperty("history")]
        public List<ToolHistoryItem> History { get; set; } = new List<ToolHistoryItem>();

        /// <summary>
        /// 擴充資料
        /// </summary>
        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 刀具使用履歷項目
    /// </summary>
    public class ToolHistoryItem
    {
        /// <summary>
        /// 使用時間
        /// </summary>
        [JsonProperty("useTime")]
        public DateTime UseTime { get; set; }

        /// <summary>
        /// 機台 ID
        /// </summary>
        [JsonProperty("machineId")]
        public string MachineId { get; set; }

        /// <summary>
        /// 軸向
        /// </summary>
        [JsonProperty("axis")]
        public string Axis { get; set; }

        /// <summary>
        /// 產品代碼
        /// </summary>
        [JsonProperty("product")]
        public string Product { get; set; }

        /// <summary>
        /// 磨損次數
        /// </summary>
        [JsonProperty("grindCount")]
        public int GrindCount { get; set; }

        /// <summary>
        /// 托盤 ID
        /// </summary>
        [JsonProperty("trayId")]
        public string TrayId { get; set; }

        /// <summary>
        /// 托盤位置
        /// </summary>
        [JsonProperty("traySlot")]
        public int TraySlot { get; set; }    }

    #endregion
}
