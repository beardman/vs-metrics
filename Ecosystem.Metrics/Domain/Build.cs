using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Domain
{
    public class Build : BaseEntity
    {
        [JsonProperty(PropertyName = "buildNumber")]
        public string BuildNumber { get; set; }
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "result")]
        public string Result { get; set; }
        [JsonProperty(PropertyName = "queueTime")]
        public DateTime QueueTime { get; set; }
        [JsonProperty(PropertyName = "startTime")]
        public DateTime StartTime { get; set; }
        [JsonProperty(PropertyName = "finishTime")]
        public DateTime FinishTime { get; set; }
    }
}
