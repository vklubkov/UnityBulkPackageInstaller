using System;
using UnityEditor;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class ProgressWrapper : IDisposable {
        readonly string _message;

        public static ProgressWrapper CreateSilent(string message) => new(message);

        public static ProgressWrapper Create(string message) {
            EditorUtility.DisplayProgressBar($"Bulk Package Installer: {message}", "Please wait...", 0);
            Debug.Log($"Bulk Package Installer: {message}");
            return new ProgressWrapper(message);
        }

        ProgressWrapper(string message) => _message = message;

        public void Update(string description, float progress) =>
            EditorUtility.DisplayProgressBar($"Bulk Package Installer: {_message}", description, progress);

        public void Dispose() {
            EditorUtility.DisplayProgressBar($"Bulk Package Installer: {_message}", "finished", 1f);
            Debug.Log($"Bulk Package Installer: finished {_message}");
        }
    }
}