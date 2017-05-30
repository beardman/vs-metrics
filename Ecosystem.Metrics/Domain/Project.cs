using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Domain
{
    public class Project
    {
        [JsonProperty(PropertyName = "id" )]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonIgnore]
        public List<BuildDefinition> BuildDefinitions { get; set; }
        [JsonIgnore]
        public List<ReleaseDefinition> ReleaseDefinitions { get; set; }
        [JsonIgnore]
        public List<Repository> Repositories { get; set; }

        public Project()
        {
            BuildDefinitions = new List<BuildDefinition>();
            ReleaseDefinitions = new List<ReleaseDefinition>();
            Repositories = new List<Repository>();
        }
    }
}
