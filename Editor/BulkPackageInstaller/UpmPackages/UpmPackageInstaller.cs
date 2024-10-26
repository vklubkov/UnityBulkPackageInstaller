using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class UpmPackageInstaller {
        readonly string[] _packagesToAdd;
        readonly string[] _packagesToRemove;

        public UpmPackageInstaller(string[] packagesToAdd, string[] packagesToRemove) {
            _packagesToAdd = packagesToAdd;
            _packagesToRemove = packagesToRemove;
        }

        public void Install(Action<float> onProgress, Action<bool> onComplete) {
            if (_packagesToAdd.Length == 0 && _packagesToRemove.Length == 0) {
                onComplete.Invoke(false);
                return;
            }

            EditorCoroutineUtility.StartCoroutine(CoInstall(onProgress, onComplete), this);
        }

        IEnumerator CoInstall(Action<float> onProgress, Action<bool> onCompleted) {
            AddAndRemoveRequest addAndRemoveRequest;
            try {
                addAndRemoveRequest = Client.AddAndRemove(_packagesToAdd, _packagesToRemove);
            }
            catch (Exception e) {
                Debug.LogException(e);
                onCompleted.Invoke(false);
                yield break;
            }

            var progress = 0f;
            while (!addAndRemoveRequest.IsCompleted) {
                progress += 0.01f;
                if (progress > 1)
                    progress = 0f;

                onProgress.Invoke(progress);
                yield return null;
            }

            if (addAndRemoveRequest.Status != StatusCode.Success) {
                if (addAndRemoveRequest.Error != null && !string.IsNullOrEmpty(addAndRemoveRequest.Error.message)) {
                    Debug.LogError("Bulk Package Installer: package installation failed " +
                                   $"with error: {addAndRemoveRequest.Error.message}");
                }

                onCompleted.Invoke(false);
            }

            var pluralAdded = _packagesToAdd.Length == 1 ? string.Empty : "s";
            var pluralRemoved = _packagesToRemove.Length == 1 ? string.Empty : "s";

            Debug.Log($"Bulk Package Installer: added {_packagesToAdd.Length} UPM " +
                      $"package{pluralAdded} and removed {_packagesToRemove.Length} UPM package{pluralRemoved}.");

            onCompleted.Invoke(true);
        }
    }
}