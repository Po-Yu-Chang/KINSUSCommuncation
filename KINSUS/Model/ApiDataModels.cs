using Newtonsoft.Json;

namespace OthinCloud.Model
{
    // 1. 遠程資訊下發指令
    public class SendMessageCommandData
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("actionType")]
        public int ActionType { get; set; }

        [JsonProperty("intervalSecondTime")]
        public int IntervalSecondTime { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 2. 設備狀態資訊
    public class DeviceStatusData
    {
        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("statusDesc")]
        public string StatusDesc { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 3. 設備報警資訊
    public class DeviceWarningData
    {
        [JsonProperty("warningCode")]
        public string WarningCode { get; set; }

        [JsonProperty("warningDesc")]
        public string WarningDesc { get; set; }

        [JsonProperty("warningLevel")]
        public string WarningLevel { get; set; }

        [JsonProperty("start")]
        public string Start { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 4. 設備生產實時數據監控
    public class DeviceParamData
    {
        [JsonProperty("activation")]
        public int Activation { get; set; }

        // Note: Other properties are dynamic based on supplier definition
        // Consider using a Dictionary<string, object> or JObject for flexibility
        // For simplicity, we'll add extendData here.

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }

        // Example of handling dynamic properties with JsonExtensionData
        [JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, Newtonsoft.Json.Linq.JToken> AdditionalData { get; set; }
    }

    // 5. 設備事件起止時間上傳
    public class EventTimeData
    {
        [JsonProperty("startTime")]
        public string StartTime { get; set; } // Using string for simplicity, consider DateTime

        [JsonProperty("endTime")]
        public string EndTime { get; set; } // Using string for simplicity, consider DateTime

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("event")]
        public int Event { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    public class EventTimeResponseData
    {
        [JsonProperty("result")]
        public int Result { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 6. 設備時間同步指令
    public class DateMessageCommandData
    {
        [JsonProperty("time")]
        public string Time { get; set; } // Using string for simplicity, consider DateTime

        [JsonProperty("week")]
        public int Week { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 7. 設備聯網狀態監控功能
    public class DeviceHeartbeatData
    {
        [JsonProperty("heartBeat")]
        public int HeartBeat { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    public class DeviceHeartbeatResponseData
    {
        [JsonProperty("heartBeat")]
        public int HeartBeat { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 8. 設備點檢參數報告
    public class DeviceKeycheckingData
    {
        [JsonProperty("traceParams")]
        public object TraceParams { get; set; } // Use Dictionary<string, object> or specific class if structure is known

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 9. 設備採集到盒子碼上報
    public class DeviceVehicleUploadData
    {
        [JsonProperty("vehicleCode")]
        public string VehicleCode { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 10. 盒子碼下發命令
    public class SwitchRecipeCommandData
    {
        [JsonProperty("start")]
        public string Start { get; set; }

        [JsonProperty("gfNum")]
        public string GfNum { get; set; }

        [JsonProperty("pnum")]
        public int Pnum { get; set; }

        [JsonProperty("vehicleCode")]
        public string VehicleCode { get; set; }

        [JsonProperty("recipeParams")]
        public object RecipeParams { get; set; } // Use Dictionary<string, object> or specific class if structure is known

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 11. 盒子做完上報資訊接口
    public class BatchCompleteData
    {
        [JsonProperty("vehicleCode")]
        public string VehicleCode { get; set; }

        [JsonProperty("okNum")]
        public int OkNum { get; set; }

        [JsonProperty("ngNum")]
        public int NgNum { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 12. 良品盒生成完成上報
    public class BatchReportedData
    {
        [JsonProperty("gfNum")]
        public string GfNum { get; set; }

        [JsonProperty("pnum")]
        public int Pnum { get; set; }

        [JsonProperty("vehicleCode")]
        public string VehicleCode { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 13. 設備啟停控制指令
    public class DeviceControlCommandData
    {
        [JsonProperty("command")]
        public int Command { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 14. 設備模式
    public class DeviceModeData
    {
        [JsonProperty("devModel")]
        public string DevModel { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    // 15. 配方參數修改報告
    public class DeviceRecipeData
    {
        [JsonProperty("params")]
        public object Params { get; set; } // Use Dictionary<string, object> or specific class if structure is known

        [JsonProperty("modifyType")]
        public string ModifyType { get; set; }

        [JsonProperty("recipeName")]
        public string RecipeName { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }
}
