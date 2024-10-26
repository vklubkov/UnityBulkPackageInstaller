using System.Collections.Generic;
using Newtonsoft.Json;

namespace BulkPackageInstaller {
    internal class ManifestInfo {
        [JsonProperty("dependencies")]
        public Dictionary<string, string> Dependencies { get; set; }

        [JsonProperty("scopedRegistries")]
        public List<ScopedRegistryInfo> ScopedRegistries { get; set; }
    }
}