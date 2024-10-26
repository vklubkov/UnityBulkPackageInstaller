using System.IO;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class ManifestBackup {
        readonly string _path;

        public ManifestBackup(string path) => _path = path;

        public void Backup() {
            if (!File.Exists(_path)) {
                Debug.LogError($"Bulk Package Installer: manifest.json file was not found at: {_path}");
                return;
            }

            var backupPath = $"{_path}.bak";
            var backupIndex = 0;
            while (File.Exists(backupPath))
                backupPath = $"{_path}.bak{backupIndex++}";

            File.Copy(_path, backupPath, overwrite: false);
            Debug.Log($"Bulk Package Installer: manifest.json file backup stored as: {backupPath}");
        }
    }
}