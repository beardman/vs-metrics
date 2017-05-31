using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Helpers
{
    public class FailureRate
    {
        public DateTime QueueTime { get; set; }
        public int FailedCount { get; set; }
        public int SucceededCount { get; set; }
        public int TotalBuilds { get; set; }
    }
}
