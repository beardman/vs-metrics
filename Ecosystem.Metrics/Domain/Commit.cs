using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics.Domain
{
    public class Commit
    {
        [JsonProperty(PropertyName = "commitId")]
        public string CommitId { get; set; }
        [JsonProperty(PropertyName = "author")]
        public CommitPerson Author { get; set; }
        [JsonProperty(PropertyName = "committer")]
        public CommitPerson Committer { get; set; }
    }

    public class CommitPerson
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }
    }
}
