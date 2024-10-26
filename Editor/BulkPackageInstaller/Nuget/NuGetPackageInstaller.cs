#if NUGET_FOR_UNITY

using System;
using System.Linq;
using NugetForUnity;
using NugetForUnity.Models;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class NuGetPackageInstaller {
        readonly string[] _packagesToRemove;
        readonly NuGetPackageInfo[] _packagesToAdd;

        public NuGetPackageInstaller(string[] packagesToRemove, NuGetPackageInfo[] packagesToAdd) {
            _packagesToRemove = packagesToRemove;
            _packagesToAdd = packagesToAdd;
        }

        public void Install(Action<string, float> onProgress, Action<bool> onComplete) {
            var installedPackages = InstalledPackagesManager.InstalledPackages
                .ToDictionary(package => package.Id, package => package);

            var uninstalledCount = 0;
            if (_packagesToRemove.Length > 0) {
                var packagesToUninstall = _packagesToRemove
                    .Where(package => installedPackages.ContainsKey(package))
                    .Select(package => installedPackages[package])
                    .ToList();

                uninstalledCount = packagesToUninstall.Count;
                if (uninstalledCount > 0)
                    NugetPackageUninstaller.UninstallAll(packagesToUninstall);
            }

            if (_packagesToAdd.Length == 0) {
                onComplete.Invoke(uninstalledCount != 0);
                return;
            }

            var packagesToInstall = _packagesToAdd
                .Where(package => !installedPackages.ContainsKey(package.Id))
                .ToArray();

            if (packagesToInstall.Length == 0) {
                onComplete.Invoke(uninstalledCount != 0);
                return;
            }

            for (var i = 0; i < packagesToInstall.Length; i++) {
                var nextPackage = packagesToInstall[i];
                var progress = i / (float)packagesToInstall.Length;
                onProgress.Invoke($"{i}/{packagesToInstall.Length}", progress);
                var version = string.IsNullOrEmpty(nextPackage.Version) ? null : nextPackage.Version;
                var nuGetPackageIdentifier = new NugetPackageIdentifier(nextPackage.Id, version);
                NugetPackageInstaller.InstallIdentifier(nuGetPackageIdentifier, refreshAssets: false);
            }

            var pluralAdded = packagesToInstall.Length == 1 ? string.Empty : "s";
            var pluralRemoved = packagesToInstall.Length == 1 ? string.Empty : "s";

            Debug.Log($"Bulk Package Installer: added {packagesToInstall.Length} NuGet " +
                      $"package{pluralAdded} and removed {uninstalledCount} NuGet package{pluralRemoved}.");

            onComplete.Invoke(true);
        }
    }
}

#endif