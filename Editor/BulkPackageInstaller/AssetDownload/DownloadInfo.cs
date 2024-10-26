using Newtonsoft.Json;

namespace BulkPackageInstaller {
    internal class DownloadStatusInfo {
        [JsonProperty("in_progress")]
        public bool IsInProgress { get; set; }

        [JsonProperty("download")]
        public DownloadInfo Download { get; set; }
    }

    internal class DownloadInfo {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }
    }
}