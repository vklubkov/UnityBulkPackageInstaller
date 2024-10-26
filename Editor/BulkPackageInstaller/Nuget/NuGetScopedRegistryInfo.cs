using System;

namespace BulkPackageInstaller {
    [Serializable]
    internal class NuGetScopedRegistryInfo {
        public string _name;
        public string _url;
        public string _scope;
        public string _version;
    }
}