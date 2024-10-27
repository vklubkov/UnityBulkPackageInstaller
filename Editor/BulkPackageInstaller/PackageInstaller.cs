using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BulkPackageInstaller {
    [CreateAssetMenu(fileName = "NewInstaller", menuName = "Bulk Package Installer/New Installer", order = 0)]
    internal class PackageInstaller : ScriptableObject {
        const string _editorPrefsAssetStoreCachePathKey = "BulkPackageInstaller_AssetStoreCachePath";
        static readonly string _manifestPath = Application.dataPath + "/../Packages/manifest.json";

        [SerializeField] List<BasicPackageInfo> _nuGetPackages;

        [SerializeField] NuGetScopedRegistryInfo _nuGetForUnity = new() {
            _name = "package.openupm.com",
            _url = "https://package.openupm.com",
            _scope ="com.github-glitchenzo.nugetforunity",
            _version = string.Empty
        };

        [Space(30)]
        [SerializeField] List<PackageInfo> _packages;
        [SerializeField] bool _cleanupScopedRegistries = true;
        [Space(30)]
        [SerializeField] List<AssetInfo> _assetStoreAssets;

        readonly ManifestBackup _manifestBackup = new(_manifestPath);
        readonly ManifestRestore _manifestRestore = new(_manifestPath);

        public string CachePath {
            get => EditorPrefs.GetString(_editorPrefsAssetStoreCachePathKey);
            set => EditorPrefs.SetString(_editorPrefsAssetStoreCachePathKey, value);
        }

        void OnEnable() {
            var storedCachePath = CachePath;
            if (!string.IsNullOrEmpty(storedCachePath))
                return;

#if UNITY_EDITOR_WIN
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = Path.Combine(appData, "Unity", "Asset Store-5.x");
#elif UNITY_EDITOR_OSX
            // NOTE: not tested on OS X!
            var path = "~/Library/Unity/Asset Store-5.x";
#elif UNITY_EDITOR_LINUX
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var path = Path.Combine(userProfile, ".local/share/unity3d/Asset Store-5.x");
#endif

            CachePath = path;
        }

        public void BackupCurrentManifest() => _manifestBackup.Backup();

        public void LoadManifestFromBackup() {
            var backupPath = EditorUtility.OpenFilePanel("Select the manifest.json backup", "Packages", string.Empty);
            _manifestRestore.Restore(backupPath);
        }

        public void Install() {
            var assetsToAdd = PrepareAssetInfo();
            if (assetsToAdd.Length > 0 && !Directory.Exists(CachePath)) {
                Debug.LogError($"Bulk Package Installer: asset cache path {CachePath} is not reachable.");
                return;
            }

            var (scopedRegistriesToAdd, packagesToAdd, packagesToRemove) = PreparePackageInfo();
            var (nuGetPackagesToRemove, nuGetPackagesToAdd) = PrepareNuGetPackageInfo();

            Survivor.InstallAssets(
                _manifestPath, _cleanupScopedRegistries, scopedRegistriesToAdd,
                _nuGetForUnity._scope, nuGetPackagesToRemove, nuGetPackagesToAdd,
                packagesToAdd, packagesToRemove,
                CachePath, assetsToAdd);
        }

        (ScopedRegistryInfo[] ScopedRegistriesToAdd, string[] PackagesToAdd, string[] PackagesToRemove)
            PreparePackageInfo() {
            using var manifestLoader = new ManifestLoader(_manifestPath);
            var manifest = manifestLoader.ManifestInfo;

            var packagesToAdd = new List<string>();
            var packagesToRemove = new List<string>();
            var packageIds = new HashSet<string>();
            var scopedRegistriesToAdd = new Dictionary<string, ScopedRegistryInfo>();
            for (var i = 0; i < _packages.Count; i++) {
                var package = _packages[i];
                if (string.IsNullOrEmpty(package._id)) {
                    Debug.LogError("Bulk Package Installer: package id is missing for " +
                                   $"package with index {i}. Package will not be processed.");
                    continue;
                }

                if (!packageIds.Add(package._id)) {
                    Debug.LogError($"Bulk Package Installer: duplicate Unity package id '{package._id}' " +
                                   "was found. Only one version of the package will be processed.");

                    continue;
                }

                AddScopedRegistryIfNeeded(package, scopedRegistriesToAdd);

                if (package._remove) {
                    // Only remove asset if it is already in the manifest
                    if (manifest.Dependencies.ContainsKey(package._id))
                        packagesToRemove.Add(package._id);

                    continue;
                }

                // Don't add asset if it is already in the manifest
                if (manifest.Dependencies.ContainsKey(package._id))
                    continue;

#if !NUGET_FOR_UNITY
                // Don't add NuGetForUnity package, it will be added earlier
                if (package._id == _nuGetForUnity._scope)
                    continue;
#endif

                var installString = GetPackageString(package);
                packagesToAdd.Add(installString);
            }

#if !NUGET_FOR_UNITY
            AddNuGetScopedRegistryIfNeeded(scopedRegistriesToAdd);
#endif

            return (scopedRegistriesToAdd.Values.ToArray(), packagesToAdd.ToArray(), packagesToRemove.ToArray());
        }

        static void AddScopedRegistryIfNeeded(
            PackageInfo package, Dictionary<string, ScopedRegistryInfo> scopedRegistriesToAdd) {
            if (string.IsNullOrEmpty(package._scopedRegistry))
                return;

            if (string.IsNullOrEmpty(package._url)) {
                Debug.LogError($"Bulk Package Installer: scoped registry {package._scopedRegistry} " +
                               $"specified for package {package._id} has missing Url. Scoped registry " +
                               "will not be added. Package will still be processed using id & version");

                return;
            }

            if (scopedRegistriesToAdd.TryGetValue(package._scopedRegistry, out var scopedRegistryInfo)) {
                if (scopedRegistryInfo.Url != package._url) {
                    Debug.LogError($"Bulk Package Installer: scoped registry {package._scopedRegistry} has " +
                                   $"multiple urls specified: {scopedRegistryInfo.Url} vs {package._url}. " +
                                   "Only one url will be used.");
                }

                if (!scopedRegistryInfo.Scopes.Contains(package._id))
                    scopedRegistryInfo.Scopes.Add(package._id);

                return;
            }

            scopedRegistriesToAdd[package._scopedRegistry] = new ScopedRegistryInfo {
                Name = package._scopedRegistry,
                Url = package._url,
                Scopes = new List<string> { package._id }
            };
        }

        static string GetPackageString(PackageInfo package) {
            // From Unity documentation:
            // - To install a specific version of a package, use a package identifier ("name@version"). This is the only way to install a pre-release version.
            // - To install the latest compatible (released) version of a package, specify only the package name.
            // - To install a git package, specify a git url.
            // - To install a local package, specify a value in the format "file:/path/to/package/folder".
            // - To install a local tarball package, specify a value in the format "file:/path/to/package-file.tgz".

            if (!string.IsNullOrEmpty(package._scopedRegistry)) {
                return string.IsNullOrEmpty(package._version)
                    ? package._id
                    : $"{package._id}@{package._version}";
            }

            if (string.IsNullOrEmpty(package._url)) {
                return string.IsNullOrEmpty(package._version)
                    ? package._id
                    : $"{package._id}@{package._version}";
            }

            return string.IsNullOrEmpty(package._version)
                ? $"{package._id}@{package._url}"
                : $"{package._id}@{package._url}#{package._version}";
        }

#if !NUGET_FOR_UNITY
        void AddNuGetScopedRegistryIfNeeded(Dictionary<string, ScopedRegistryInfo> scopedRegistriesToAdd) {
            if (_nuGetPackages.Count == 0)
                return;

            if (scopedRegistriesToAdd.TryGetValue(_nuGetForUnity._name, out var nuGetScopeRegistry)) {
                if (!nuGetScopeRegistry.Scopes.Contains(_nuGetForUnity._scope))
                    nuGetScopeRegistry.Scopes.Add(_nuGetForUnity._scope);

                return;
            }

            scopedRegistriesToAdd.Add(_nuGetForUnity._name, new ScopedRegistryInfo() {
                Name = _nuGetForUnity._name,
                Url = _nuGetForUnity._url,
                Scopes = new List<string> { _nuGetForUnity._scope }
            });
        }
#endif

        (string[] PackagesToRemove, NuGetPackageInfo[] PackagesToAdd) PrepareNuGetPackageInfo() {
            var packagesToRemove = new List<string>();
            var packagesToAdd = new List<NuGetPackageInfo>();
            var packageIds = new HashSet<string>();
            foreach (var package in _nuGetPackages) {
                if (!packageIds.Add(package._id)) {
                    Debug.LogError($"Bulk Package Installer: duplicate NuGet package id '{package._id}' " +
                                   "was found. Only one version of the package will be processed.");

                    continue;
                }

                if (package._remove) {
                    packagesToRemove.Add(package._id);
                    continue;
                }

                packagesToAdd.Add(new NuGetPackageInfo {
                    Id = package._id,
                    Version = package._version
                });
            }

            return (packagesToRemove.ToArray(), packagesToAdd.ToArray());
        }

        AssetInfo[] PrepareAssetInfo() {
            var assetsToAdd = new List<AssetInfo>();
            var assetIds = new HashSet<string>();
            for (var i = 0; i < _assetStoreAssets.Count; i++) {
                var asset = _assetStoreAssets[i];
                if (string.IsNullOrEmpty(asset.Id)) {
                    Debug.LogError("Bulk Package Installer: asset id is missing for " +
                                   $"asset with index {i}. Asset will not be processed.");
                    continue;
                }


                if (!assetIds.Add(asset.Id)) {
                    Debug.LogError($"Bulk Package Installer: duplicate asset id '{asset.Id}' " +
                                   $"was found for asset with name {asset.Name}." +
                                   "Only one version of the asset will be processed.");

                    continue;
                }

                if (string.IsNullOrEmpty(asset.Name)) {
                    assetsToAdd.Add(new AssetInfo {
                        Name = string.Empty,
                        Id = asset.Id
                    });
                }
                else {
                    assetsToAdd.Add(asset);
                }
            }

            return assetsToAdd.ToArray();
        }
    }
}