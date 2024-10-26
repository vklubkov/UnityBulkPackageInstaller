using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulkPackageInstaller {
    internal enum Status {
        None,
        AddScopedRegistries,
        InstallNuGetForUnity,
        InstallNuGetPackages,
        InstallUpmPackages,
        CleanupScopedRegistries,
        DownloadAssets,
        ImportAssets,
        Complete
    }

    internal class PersistentInfo {
        public static PersistentInfo Default => new() {
            Status = Status.None,
            ScopedRegistriesAddInfo = new() {
                ManifestPath = string.Empty,
                RegistriesToAdd = Array.Empty<ScopedRegistryInfo>(),
            },
            NuGetInstallationInfo = new() {
                NuGetForUnity = string.Empty,
                PackagesToRemove = Array.Empty<string>(),
                PackagesToAdd = Array.Empty<NuGetPackageInfo>(),
            },
            PackagesInstallationInfo = new() {
                PackagesToAdd = Array.Empty<string>(),
                PackagesToRemove = Array.Empty<string>(),
            },
            AssetsDownloadInfo = new() {
                CachePath = string.Empty,
                Downloads = Array.Empty<AssetInfo>(),
            },
            AssetImportInfo = new() {
                ImportQueue = new(),
            }
        };

        public Status Status { get; set; }
        public ScopedRegistriesInfo ScopedRegistriesAddInfo { get; set; }
        public NuGetInstallationInfo NuGetInstallationInfo { get; set; }
        public PackagesInstallationInfo PackagesInstallationInfo { get; set; }
        public AssetsDownloadInfo AssetsDownloadInfo { get; set; }
        public AssetImportInfo AssetImportInfo { get; set; }
    }

    internal class ScopedRegistriesInfo {
        public string ManifestPath { get; set; }
        public bool CleanupScopedRegistries { get; set; }
        public ScopedRegistryInfo[] RegistriesToAdd { get; set; }
    }

    internal class NuGetInstallationInfo {
        public string NuGetForUnity { get; set; }
        public string[] PackagesToRemove { get; set; }
        public NuGetPackageInfo[] PackagesToAdd { get; set; }
    }

    internal class NuGetPackageInfo {
        public string Id { get; set; }
        public string Version { get; set; }
    }

    internal class PackagesInstallationInfo {
        public string[] PackagesToAdd { get; set; }
        public string[] PackagesToRemove { get; set; }
    }

    internal class AssetsDownloadInfo {
        public string CachePath{ get; set; }
        public AssetInfo[] Downloads { get; set; }
    }

    [Serializable]
    internal class AssetInfo {
        [field:SerializeField] public string Name { get; set; }
        [field:SerializeField] public string Id { get; set; }
    }

    internal class AssetImportInfo {
        public int TotalAssetCount { get; set; }
        public Queue<string> ImportQueue { get; set; }
    }
}