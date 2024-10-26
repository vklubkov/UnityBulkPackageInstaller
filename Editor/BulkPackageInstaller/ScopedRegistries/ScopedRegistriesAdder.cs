using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class ScopedRegistriesAdder {
        readonly string _path;
        public ScopedRegistriesAdder(string path) => _path = path;

        public void Add(ScopedRegistryInfo[] registriesToAdd, Action<bool> onComplete) {
            if (registriesToAdd.Length == 0) {
                onComplete.Invoke(false);
                return;
            }

            var addedRegistries = 0;
            var updatedRegistries = 0;
            var addedScopes = 0;
            using var manifestLoader = new ManifestLoader(_path);
            var manifest = manifestLoader.ManifestInfo;
            foreach (var registryToAdd in registriesToAdd) {
                manifest.ScopedRegistries ??= new List<ScopedRegistryInfo>();
                var existingRegistry = manifest.ScopedRegistries.Find(item => item.Name == registryToAdd.Name);
                if (existingRegistry == null) {
                    manifest.ScopedRegistries.Add(registryToAdd);
                    addedRegistries++;
                    addedScopes += registryToAdd.Scopes?.Count ?? 0;
                }
                else {
                    addedScopes += UpdateExistingRegistry(existingRegistry, registryToAdd);
                    updatedRegistries++;
                }
            }

            var pluralAddedRegistries = addedRegistries == 1 ? "y" : "ies";
            var pluralUpdatedRegistries = updatedRegistries == 1 ? "y" : "ies";
            var pluralScopes = addedScopes == 1 ? string.Empty : "s";

            Debug.Log($"Bulk Package Installer: added {addedRegistries} UPM Scoped Registr{pluralAddedRegistries}, " +
                      $"updated {updatedRegistries} UPM Scoped Registr{pluralUpdatedRegistries} " +
                      $"and added {addedScopes} Scope{pluralScopes} in total.");

            onComplete.Invoke(true);
        }

        static int UpdateExistingRegistry(ScopedRegistryInfo existingRegistry, ScopedRegistryInfo newRegistry) {
            existingRegistry.Url = newRegistry.Url;

            var addedScopes = 0;
            if (existingRegistry.Scopes == null || existingRegistry.Scopes.Count == 0) {
                existingRegistry.Scopes = newRegistry.Scopes;
                addedScopes += existingRegistry.Scopes.Count;
                return addedScopes;
            }

            foreach (var newRegistryScope in newRegistry.Scopes) {
                if (!existingRegistry.Scopes.Contains(newRegistryScope)) {
                    existingRegistry.Scopes.Add(newRegistryScope);
                    addedScopes++;
                }
            }

            return addedScopes;
        }
    }
}