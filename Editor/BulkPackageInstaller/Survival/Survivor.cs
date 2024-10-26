using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace BulkPackageInstaller {
    [InitializeOnLoad]
    internal class Survivor {
        class CoroutineHost {
            public void WaitForTwoFrames(Action onComplete) =>
                EditorCoroutineUtility.StartCoroutine(CoTwoEditorUpdates(onComplete), this);

            IEnumerator CoTwoEditorUpdates(Action onComplete) {
                yield return null;
                onComplete?.Invoke();
            }
        }

        const string _persistentInfoKey = "BulkPackageInstaller:PersistentInfo";
        readonly static CoroutineHost _coroutineHost = new();
        static PersistentInfo _persistentInfo = PersistentInfo.Default;
        static bool _shouldResume;
        static string _lastImportedAsset;

        static Survivor() {
            // Survival
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.update += Update;

            // *.unitypackage import
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
        }

        static void OnBeforeAssemblyReload() {
            var json = JsonConvert.SerializeObject(_persistentInfo, new StringEnumConverter());
            SessionState.SetString(_persistentInfoKey, json);
        }

        static void OnAfterAssemblyReload() {
            var defaultJson = JsonConvert.SerializeObject(PersistentInfo.Default, new StringEnumConverter());
            var json = SessionState.GetString(_persistentInfoKey, defaultJson);
            _persistentInfo = JsonConvert.DeserializeObject<PersistentInfo>(json);
            _shouldResume = true;
        }

        static void Update() {
            if (!_shouldResume)
                return;

            switch (_persistentInfo.Status) {
                case Status.AddScopedRegistries:
                    InstallNuGetForUnity();
                    break;
                case Status.InstallNuGetForUnity:
                    InstallNuGetPackages();
                    break;
                case Status.InstallNuGetPackages:
                    InstallUpmPackages();
                    break;
                case Status.InstallUpmPackages:
                    CleanupScopedRegistries();
                    break;
                case Status.CleanupScopedRegistries:
                    DownloadAssets();
                    break;
                case Status.DownloadAssets:
                    DownloadAssets();
                    break;
                case Status.ImportAssets:
                    ImportNextAsset();
                    break;
                case Status.Complete:
                    Cleanup();
                    break;
                case Status.None:
                default:
                    break;
            }

            _shouldResume = false;
        }

        static void OnImportPackageCompleted(string assetName) {
            if (_persistentInfo.Status != Status.ImportAssets)
                return;

            Debug.Log($"Bulk Package Installer: imported Asset Store asset '{assetName}'");

            _lastImportedAsset = assetName;
            if (!_shouldResume)
                ImportNextAsset();
        }

        static void OnImportPackageFailed(string assetName, string errorMessage) {
            if (_persistentInfo.Status != Status.ImportAssets)
                return;

            Debug.LogError("Bulk Package Installer: failed to import Asset Store " +
                           $"asset '{assetName}' with error: {errorMessage}");

            _lastImportedAsset = assetName;
            if (!_shouldResume)
                ImportNextAsset();
        }

        static void OnImportPackageCancelled(string assetName) {
            if (_persistentInfo.Status != Status.ImportAssets)
                return;

            Debug.LogError($"Bulk Package Installer: import of Asset Store asset '{assetName} cancelled'");

            _lastImportedAsset = assetName;
            if (!_shouldResume)
                ImportNextAsset();
        }

        public static void InstallAssets(
            string manifestPath, bool cleanupScopedRegistries, ScopedRegistryInfo[] addScopedRegistries,
            string nuGetForUnity, string[] nuGetPackagesToRemove, NuGetPackageInfo[] nuGetPackagesToAdd,
            string[] packagesToAdd, string[] packagesToRemove,
            string assetStoreCachePath, AssetInfo[] assetStoreAssets) {
            Debug.Log("Bulk Package Installer: started");

            _persistentInfo.ScopedRegistriesAddInfo.ManifestPath = manifestPath;
            _persistentInfo.ScopedRegistriesAddInfo.CleanupScopedRegistries = cleanupScopedRegistries;
            _persistentInfo.ScopedRegistriesAddInfo.RegistriesToAdd = addScopedRegistries;
            _persistentInfo.NuGetInstallationInfo.NuGetForUnity = nuGetForUnity;
            _persistentInfo.NuGetInstallationInfo.PackagesToRemove = nuGetPackagesToRemove;
            _persistentInfo.NuGetInstallationInfo.PackagesToAdd = nuGetPackagesToAdd;
            _persistentInfo.PackagesInstallationInfo.PackagesToAdd = packagesToAdd;
            _persistentInfo.PackagesInstallationInfo.PackagesToRemove = packagesToRemove;
            _persistentInfo.AssetsDownloadInfo.CachePath = assetStoreCachePath;
            _persistentInfo.AssetsDownloadInfo.Downloads = assetStoreAssets;

            AddScopedRegistries();
        }

        static void AddScopedRegistries() {
            var progressWrapper = ProgressWrapper.Create("adding scoped registries");
            _persistentInfo.Status = Status.AddScopedRegistries;
            var scopedRegistriesAdder = new ScopedRegistriesAdder(_persistentInfo.ScopedRegistriesAddInfo.ManifestPath);
            scopedRegistriesAdder.Add(_persistentInfo.ScopedRegistriesAddInfo.RegistriesToAdd, applyChanges =>
                CompleteStep(progressWrapper, applyChanges, InstallNuGetForUnity));
        }

        static void InstallNuGetForUnity() {
#if NUGET_FOR_UNITY
            InstallNuGetPackages();
#else
            var progressWrapper = ProgressWrapper.Create("installing NuGetForUnity");
            _persistentInfo.Status = Status.InstallNuGetForUnity;

            var packageInstaller = new UpmPackageInstaller(
                new[] { _persistentInfo.NuGetInstallationInfo.NuGetForUnity },
                new string[] { });

            packageInstaller.Install(
                progress => progressWrapper.Update("Please wait...", progress),
                applyChanges => CompleteStep(progressWrapper, applyChanges, InstallNuGetPackages));
#endif
        }

        static void InstallNuGetPackages() {
#if NUGET_FOR_UNITY
            var progressWrapper = ProgressWrapper.Create("installing NuGet packages");
            _persistentInfo.Status = Status.InstallNuGetPackages;

            var nuGetPackageInstaller = new NuGetPackageInstaller(
                _persistentInfo.NuGetInstallationInfo.PackagesToRemove,
                _persistentInfo.NuGetInstallationInfo.PackagesToAdd);

            nuGetPackageInstaller.Install(
                (description, progress) => progressWrapper.Update(description, progress),
                applyChanges => CompleteStep(progressWrapper, applyChanges, InstallUpmPackages, refreshAssets:true));
#else
            InstallUpmPackages();
#endif
        }

        static void InstallUpmPackages() {
            var progressWrapper = ProgressWrapper.Create("installing Unity packages");
            _persistentInfo.Status = Status.InstallUpmPackages;

            var upmPackageInstaller = new UpmPackageInstaller(
                _persistentInfo.PackagesInstallationInfo.PackagesToAdd,
                _persistentInfo.PackagesInstallationInfo.PackagesToRemove);

            upmPackageInstaller.Install(
                progress => progressWrapper.Update("Please wait...", progress),
                applyChanges => CompleteStep(progressWrapper, applyChanges, CleanupScopedRegistries));
        }

        static void CleanupScopedRegistries() {
            var progressWrapper = ProgressWrapper.Create("cleaning scoped registries");
            _persistentInfo.Status = Status.CleanupScopedRegistries;

            var scopedRegistriesCleaner = new ScopedRegistriesCleaner(
                _persistentInfo.ScopedRegistriesAddInfo.ManifestPath,
                _persistentInfo.ScopedRegistriesAddInfo.CleanupScopedRegistries);

            scopedRegistriesCleaner.Cleanup(applyChanges => CompleteStep(progressWrapper, applyChanges, DownloadAssets));
        }
        
        static void DownloadAssets() {
            var progressWrapper = ProgressWrapper.Create("downloading Asset Store assets");
            _persistentInfo.Status = Status.DownloadAssets;

            var assetDownloader = new AssetsDownloader(
                _persistentInfo.AssetsDownloadInfo.CachePath,
                _persistentInfo.AssetsDownloadInfo.Downloads);

            assetDownloader.Download((description, progress) => {
                progressWrapper.Update(description, progress);
            }, queue => {
                progressWrapper.Dispose();
                ImportAssets(queue);
            });
        }

        static void ImportAssets(Queue<string> importQueue) {
            var progressWrapper = ProgressWrapper.Create("importing Asset Store assets");
            _persistentInfo.Status = Status.ImportAssets;
            _persistentInfo.AssetImportInfo.ImportQueue = importQueue;
            _persistentInfo.AssetImportInfo.TotalAssetCount = importQueue.Count;
            if (importQueue.Count == 0) {
                Complete(progressWrapper);
                return;
            }

            ImportAsset(progressWrapper);
        }

        static void ImportNextAsset() {
            var progressWrapper = ProgressWrapper.CreateSilent("importing Asset Store assets");
            var lastImportedAsset = _lastImportedAsset;
            _lastImportedAsset = null;

            var assetImportQueue = _persistentInfo.AssetImportInfo.ImportQueue;
            if (assetImportQueue.Count == 0) {
                if (lastImportedAsset == null) {
                    Debug.LogError("Bulk Package Installer: trying to import next asset with empty queue.");
                }
                else {
                    Debug.LogError("Bulk Package Installer: trying to import next asset with " +
                                   $"empty queue. Last imported asset was: '{lastImportedAsset}'");
                }

                Complete(progressWrapper);
                return;
            }

            var lastAsset = assetImportQueue.Dequeue();
            if (lastImportedAsset != null && !lastAsset.Contains(lastImportedAsset)) {
                Debug.LogWarning($"Bulk Package Installer: last asset in the queue is '{lastAsset}') " +
                                 $"but last imported asset was '{lastImportedAsset}'");
            }

            if (assetImportQueue.Count == 0) {
                Complete(progressWrapper);
                return;
            }

            ImportAsset(progressWrapper);
        }
        
        static void ImportAsset(ProgressWrapper progressWrapper) {
            var assetImportQueue = _persistentInfo.AssetImportInfo.ImportQueue;
            var remainingAssetCount = assetImportQueue.Count;
            var totalAssetCount = _persistentInfo.AssetImportInfo.TotalAssetCount;
            var importedAssetCount = totalAssetCount - remainingAssetCount;
            var progress = importedAssetCount / (float)totalAssetCount;
            progressWrapper.Update($"{importedAssetCount}/{totalAssetCount}", progress);
            var nextAsset = assetImportQueue.Peek();
            AssetDatabase.ImportPackage(nextAsset, false);
        }

        static void Complete(ProgressWrapper progressWrapper) {
            _persistentInfo.Status = Status.Complete;
            CompleteStep(progressWrapper, applyChanges: true, Cleanup);
        }
        
        static void CompleteStep(
            ProgressWrapper progressWrapper, bool applyChanges, Action onComplete, bool refreshAssets = false) {
            progressWrapper.Dispose();
            if (!applyChanges) {
                onComplete?.Invoke();
                return;
            }

            if (refreshAssets)
                AssetDatabase.Refresh();

            EditorUtility.RequestScriptReload();
            _coroutineHost.WaitForTwoFrames(onComplete);
        }

        static void Cleanup() {
            _persistentInfo = PersistentInfo.Default;
            _shouldResume = false;
            _lastImportedAsset = null;
            EditorUtility.ClearProgressBar();
            Debug.Log("Bulk Package Installer: finished");
        }
    }
}