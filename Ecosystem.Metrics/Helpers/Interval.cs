using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Helpers
{
    public class Interval
    {
        public int FirstBuildId { get; set; }
        public string FirstBuildNumber { get; set; }
        public DateTime FirstBuildDate { get; set; }
        public int NextBuildId { get; set; }
        public string NextBuildNumber { get; set; }
        public DateTime NextBuildDate { get; set; }
        public double ElapsedTime { get; set; }
    }
}
