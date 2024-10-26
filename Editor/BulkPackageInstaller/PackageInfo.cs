using System;

namespace BulkPackageInstaller {
    [Serializable]
    internal class BasicPackageInfo {
        public string _id;
        public string _version;
        public bool _remove;
    }

    [Serializable]
    internal class PackageInfo : BasicPackageInfo {
        public string _scopedRegistry;
        public string _url;
    }
}