using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OthinCloud
{
    /// <summary>
    /// 提供預設 API 範本的靜態類別
    /// </summary>
    public static class DefaultApiTemplates
    {
        /// <summary>
        /// 取得所有預設 API 範本
        /// </summary>
        /// <returns>包含預設範本的字典</returns>
        public static Dictionary<string, string> GetDefaultTemplates()
        {
            var templates = new Dictionary<string, string>();
            
            // 設備狀態資訊
            templates["DEVICE_STATUS_MESSAGE"] = @"{
  ""requestID"": ""STATUS_001"",
  ""serviceName"": ""DEVICE_STATUS_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""statusCode"": ""S001"",
      ""statusDesc"": ""設備正常運行中"",
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備報警資訊
            templates["DEVICE_WARNING_MESSAGE"] = @"{
  ""requestID"": ""WARNING_001"",
  ""serviceName"": ""DEVICE_WARNING_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""warningCode"": ""W001"",
      ""warningDesc"": ""溫度過高警告"",
      ""warningLevel"": ""中級"",
      ""start"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備生產實時數據監控
            templates["DEVICE_PARAM_REQUEST"] = @"{
  ""requestID"": ""PARAM_001"",
  ""serviceName"": ""DEVICE_PARAM_REQUEST"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""data"": [
    {
      ""activation"": 1,
      ""temperature"": 25.5,
      ""pressure"": 10.2,
      ""speed"": 60,
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備事件起止時間上傳
            templates["EVENT_TIME_MESSAGE"] = @"{
  ""requestID"": ""EVENT_001"",
  ""serviceName"": ""EVENT_TIME_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""startTime"": """ + DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss") + @""",
      ""endTime"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
      ""duration"": 3600,
      ""event"": 1,
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備聯網狀態監控功能
            templates["DEVICE_HEARTBEAT_MESSAGE"] = @"{
  ""requestID"": ""HEARTBEAT_001"",
  ""serviceName"": ""DEVICE_HEARTBEAT_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""heartBeat"": 1,
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備點檢參數報告
            templates["DEVICE_KEYCHECKING_REQUEST"] = @"{
  ""requestID"": ""KEYCHECK_001"",
  ""serviceName"": ""DEVICE_KEYCHECKING_REQUEST"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""data"": [
    {
      ""traceParams"": {
        ""toolA"": {
          ""usedCount"": 120,
          ""lifespan"": 1000
        },
        ""toolB"": {
          ""usedCount"": 45,
          ""lifespan"": 500
        }
      },
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 設備採集到盒子碼上報
            templates["DEVICE_VEHICLE_UPLOAD"] = @"{
  ""requestID"": ""VEHICLE_001"",
  ""serviceName"": ""DEVICE_VEHICLE_UPLOAD"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""operator"": ""OP123"",
  ""data"": [
    {
      ""vehicleCode"": ""BOX67890"",
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 盒子做完上報資訊接口
            templates["BATCH_COMPLETE_MESSAGE"] = @"{
  ""requestID"": ""COMPLETE_001"",
  ""serviceName"": ""BATCH_COMPLETE_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""data"": [
    {
      ""vehicleCode"": ""BOX67890"",
      ""okNum"": 95,
      ""ngNum"": 5,
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            // 良品盒生成完成上報
            templates["BATCH_REPORTED_MESSAGE"] = @"{
  ""requestID"": ""REPORTED_001"",
  ""serviceName"": ""BATCH_REPORTED_MESSAGE"",
  ""timeStamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @""",
  ""devCode"": ""DEVICE001"",
  ""data"": [
    {
      ""gfNum"": ""6"",
      ""pnum"": 10,
      ""vehicleCode"": ""BOX67890"",
      ""extendData"": null
    }
  ],
  ""extendData"": null
}";
            
            return templates;
        }

        /// <summary>
        /// 將預設範本儲存至檔案
        /// </summary>
        /// <param name="filePath">儲存路徑</param>
        public static void SaveDefaultTemplatesToFile(string filePath)
        {
            try
            {
                var templates = GetDefaultTemplates();
                string jsonContent = JsonConvert.SerializeObject(templates, Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"儲存預設範本失敗: {ex.Message}", ex);
            }
        }
    }
}
