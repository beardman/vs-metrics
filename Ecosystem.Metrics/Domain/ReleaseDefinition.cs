using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Domain
{
    public class ReleaseDefinition : BaseEntity
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public List<Release> Releases { get; set; }
    }
}
