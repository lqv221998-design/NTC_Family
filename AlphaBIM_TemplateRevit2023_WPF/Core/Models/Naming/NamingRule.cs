using System.Collections.Generic;
using Newtonsoft.Json;

namespace NTC.FamilyManager.Core.Models.Naming
{
    public class NamingRule
    {
        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("discipline")]
        public string Discipline { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; } = 10;
    }

    public class NamingConfig
    {
        [JsonProperty("rules")]
        public List<NamingRule> Rules { get; set; }

        [JsonProperty("default_author")]
        public string DefaultAuthor { get; set; }

        [JsonProperty("version_prefix")]
        public string VersionPrefix { get; set; }
    }
}
