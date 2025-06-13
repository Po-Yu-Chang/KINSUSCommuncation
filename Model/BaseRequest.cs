using Newtonsoft.Json;
using System.Collections.Generic;

namespace OthinCloud.Model
{
    public class BaseRequest<T>
    {
        [JsonProperty("requestID")]
        public string RequestID { get; set; }

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        [JsonProperty("timeStamp")]
        public string TimeStamp { get; set; }

        [JsonProperty("devCode")]
        public string DevCode { get; set; }

        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("data")]
        public List<T> Data { get; set; }

        [JsonProperty("extendData")]
        public object ExtendData { get; set; } // Type can be more specific if needed
    }
}
