using System.IO;
using UnityEditor;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class ManifestRestore {
        readonly string _path;

        public ManifestRestore(string path) => _path = path;

        public void Restore(string backupPath) {
            if (!File.Exists(backupPath)) {
                Debug.LogError($"Bulk Package Installer: manifest.json file was not found at: {backupPath}");
                return;
            }

            File.Copy(backupPath, _path, overwrite: true);
            EditorUtility.RequestScriptReload();
            Debug.Log($"Bulk Package Installer: manifest.json file restored from backup: {backupPath}");
        }
    }
}