using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class ScopedRegistriesCleaner {
        readonly string _path;
        readonly bool _cleanupScopedRegistries;

        public ScopedRegistriesCleaner(string path, bool cleanupScopedRegistries) {
            _path = path;
            _cleanupScopedRegistries = cleanupScopedRegistries;
        }

        public void Cleanup(Action<bool> onComplete) {
            if (!_cleanupScopedRegistries) {
                onComplete.Invoke(false);
                return;
            }

            using var manifestLoader = new ManifestLoader(_path);
            var manifest = manifestLoader.ManifestInfo;
            if (manifest == null) {
                onComplete.Invoke(false);
                return; // No manifest
            }

            if (manifest.Dependencies == null && manifest.ScopedRegistries == null) {
                onComplete.Invoke(false);
                return; // Nothing to clean up
            }

            if (manifest.ScopedRegistries == null || manifest.ScopedRegistries.Count == 0) {
                onComplete.Invoke(false);
                return; // Nothing to clean up
            }

            var removedRegistries = 0;
            var removedScopes = 0;

            if (manifest.Dependencies == null || manifest.Dependencies.Count == 0) {
                // Clean everything
                removedRegistries = manifest.ScopedRegistries.Count;
                removedScopes = manifest.ScopedRegistries.Sum(registry => registry.Scopes?.Count ?? 0);
                manifest.ScopedRegistries.Clear();

                var pluralRegistries = removedRegistries == 1 ? "y" : "ies";
                var pluralScopes = removedScopes == 1 ? string.Empty : "s";
                Debug.Log($"Bulk Package Installer: removed {removedRegistries} UPM Scoped " +
                          $"Registr{pluralRegistries} and removed {removedScopes} Scope{pluralScopes} in total.");

                onComplete.Invoke(true);
            }
            else {
                // Targeted cleanup
                var registriesToRemove = RemoveScopes(manifest, out removedScopes);
                removedRegistries = registriesToRemove.Count;
                removedScopes += RemoveRegistries(registriesToRemove, manifest);

                var pluralRegistries = removedRegistries == 1 ? "y" : "ies";
                var pluralScopes = removedScopes == 1 ? string.Empty : "s";
                Debug.Log($"Bulk Package Installer: removed {removedRegistries} UPM Scoped " +
                          $"Registr{pluralRegistries} and removed {removedScopes} Scope{pluralScopes} in total.");

                onComplete.Invoke(true);
            }
        }

        static List<ScopedRegistryInfo> RemoveScopes(ManifestInfo manifestInfo, out int removedScopes) {
            removedScopes = 0;
            var registriesToRemove = new List<ScopedRegistryInfo>();
            foreach (var registry in manifestInfo.ScopedRegistries) {
                var scopesToRemove = registry.Scopes
                    .Where(scope => !manifestInfo.Dependencies.ContainsKey(scope))
                    .ToList();

                foreach (var scope in scopesToRemove) {
                    registry.Scopes.Remove(scope);
                    removedScopes++;
                }

                if (registry.Scopes.Count > 0)
                    continue;

                registriesToRemove.Add(registry);
            }

            return registriesToRemove;
        }

        static int RemoveRegistries(List<ScopedRegistryInfo> registriesToRemove, ManifestInfo manifestInfo) {
            var removedScopes = 0;
            foreach (var registry in registriesToRemove) {
                removedScopes += registry.Scopes?.Count ?? 0;
                manifestInfo.ScopedRegistries.Remove(registry);
            }

            return removedScopes;
        }
    }
}