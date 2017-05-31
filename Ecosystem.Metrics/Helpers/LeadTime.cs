using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Helpers
{
    public class LeadTime
    {
        public int BuildId { get; set; }
        public string BuildNumber { get; set; }
        public double BuildDuration { get; set; }
        public DateTime BuildDate { get; set; }
    }
}
