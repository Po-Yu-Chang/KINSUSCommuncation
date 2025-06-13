using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 發送訊息資料 (SEND_MESSAGE_COMMAND)
    /// </summary>
    public class SendMessageData
    {
        [JsonProperty("messageType")]
        public string MessageType { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("targetStation")]
        public string TargetStation { get; set; }

        [JsonProperty("sender")]
        public string Sender { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 日期時間同步資料 (DATE_MESSAGE_COMMAND)
    /// </summary>
    public class DateTimeData
    {
        [JsonProperty("currentTime")]
        public DateTime CurrentTime { get; set; }

        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }

        [JsonProperty("syncType")]
        public string SyncType { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 切換配方資料 (SWITCH_RECIPE_COMMAND)
    /// </summary>
    public class SwitchRecipeData
    {
        [JsonProperty("stationId")]
        public string StationId { get; set; }

        [JsonProperty("recipeName")]
        public string RecipeName { get; set; }

        [JsonProperty("recipeVersion")]
        public string RecipeVersion { get; set; }

        [JsonProperty("workOrder")]
        public string WorkOrder { get; set; }

        [JsonProperty("switchTime")]
        public DateTime SwitchTime { get; set; }

        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 設備控制資料 (DEVICE_CONTROL_COMMAND)
    /// </summary>
    public class DeviceControlData
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("stationId")]
        public string StationId { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; }
    }

    /// <summary>
    /// 設備控制回應資料
    /// </summary>
    public class DeviceControlResponse
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("stationId")]
        public string StationId { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("executionTime")]
        public DateTime ExecutionTime { get; set; }

        [JsonProperty("result")]
        public Dictionary<string, object> Result { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
