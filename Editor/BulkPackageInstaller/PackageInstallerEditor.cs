using UnityEditor;
using UnityEngine;

namespace BulkPackageInstaller {
    [CustomEditor(typeof(PackageInstaller))]
    internal class PackageInstallerEditor : Editor {
        const double _checkInterval = 1;
        double _lastCheckTime;
        string _cachePath;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            var packageInstaller = (PackageInstaller)target;

            var timeSinceStartup = EditorApplication.timeSinceStartup;
            if (timeSinceStartup - _lastCheckTime > _checkInterval) {
                _lastCheckTime = timeSinceStartup;
                _cachePath = packageInstaller.CachePath;
            }

            var newCachePath = EditorGUILayout.TextField("Asset Store Cache path", _cachePath);
            if (newCachePath != _cachePath)
                packageInstaller.CachePath = newCachePath;

            _cachePath = newCachePath;

            EditorGUILayout.Space(30);

            if(GUILayout.Button("Backup manifest.json"))
                packageInstaller.BackupCurrentManifest();

            if(GUILayout.Button("Load manifest.json From Backup"))
                packageInstaller.LoadManifestFromBackup();

            EditorGUILayout.Space(10);

            if(GUILayout.Button("Install"))
                packageInstaller.Install();
        }
    }
}