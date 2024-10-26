using Newtonsoft.Json;

namespace BulkPackageInstaller {
    internal class AssetDownloadInfoResponse {
        [JsonProperty("result")]
        public AssetDownloadInfoResult Result { get; set; }
    }

    internal class AssetDownloadInfoResult {
        [JsonProperty("download")]
        public AssetDownloadInfo Download { get; set; }
    }

    internal class AssetDownloadInfo {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("filename_safe_category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("filename_safe_package_name")]
        public string PackageName { get; set; }

        [JsonProperty("filename_safe_publisher_name")]
        public string PublisherName { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}