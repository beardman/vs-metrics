using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Domain
{
    public class BuildDefinition : BaseEntity
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonIgnore]
        public List<Build> Builds { get; set; }

        public BuildDefinition()
        {
            Builds = new List<Build>();
        }
    }
}
