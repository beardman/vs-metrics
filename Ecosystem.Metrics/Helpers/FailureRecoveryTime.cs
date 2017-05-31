using Ecosystem.Metrics.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Helpers
{
    public class FailureRecoveryTime
    {
        public Build FailedBuild { get; set; }
        public Build SuccessBuild { get; set; }
    }
}
