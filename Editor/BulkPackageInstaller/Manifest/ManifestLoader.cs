using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class ManifestLoader : IDisposable {
        readonly string _path;
        readonly string _originalJson;
        public ManifestInfo ManifestInfo { get; }

        public ManifestLoader(string path) {
            _path = path;
            if (!File.Exists(_path)) {
                Debug.LogError($"Bulk Package Installer: manifest.json file was not found at: {_path}");
                return;
            }

            _originalJson = File.ReadAllText(_path);
            ManifestInfo = JsonConvert.DeserializeObject<ManifestInfo>(_originalJson);
        }

        public void Dispose() {
            var json = JsonConvert.SerializeObject(ManifestInfo, Formatting.Indented);
            if (json == _originalJson)
                return;

            File.WriteAllText(_path, json);
            Debug.Log("Bulk Package Installer: manifest.json file updated");
        }
    }
}