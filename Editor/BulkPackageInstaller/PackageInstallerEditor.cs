using UnityEditor;
using UnityEngine;

namespace BulkPackageInstaller {
    [CustomEditor(typeof(PackageInstaller))]
    internal class PackageInstallerEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var packageInstaller = (PackageInstaller)target;
            var newCachePath = EditorGUILayout.TextField("Asset Store Cache path", packageInstaller.CachePath);
            if (newCachePath != packageInstaller.CachePath)
                packageInstaller.UpdateAssetStoreCachePath(newCachePath);

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