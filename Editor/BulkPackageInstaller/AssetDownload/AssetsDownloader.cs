using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace BulkPackageInstaller {
    internal class AssetsDownloader {
        readonly string _cachePath;
        readonly AssetInfo[] _downloads;

        public AssetsDownloader(
            string cachePath, AssetInfo[] downloads) {
            _cachePath = cachePath;
            _downloads = downloads;
        }

        public void Download(Action<string, float> onProgress, Action<Queue<string>> onComplete) {
            if (_downloads.Length == 0) {
                onComplete.Invoke(new Queue<string>());
                return;
            }

            EditorCoroutineUtility.StartCoroutine(CoDownload(onProgress, onComplete), this);
        }

        IEnumerator CoDownload(Action<string, float> onProgress, Action<Queue<string>> onComplete) {
            var paths = new List<string>();
            var downloadsCount = 0;

            foreach (var assetInfo in _downloads) {
                downloadsCount++;
                var progress = downloadsCount / (float)_downloads.Length * 0.5f;
                onProgress.Invoke(assetInfo.Name, progress);
                AssetDownloader assetDownloader = new(_cachePath);
                yield return assetDownloader.Download(assetInfo.Id, path => paths.Add(path));
            }

            while (paths.Count < downloadsCount) {
                var description = $"{paths.Count}/{downloadsCount}";
                var progress = 0.5f + paths.Count / (float)downloadsCount * 0.5f;
                onProgress.Invoke(description, progress);
                yield return null;
            }

            var validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).ToList();
            if (_downloads.Length != paths.Count) {
                Debug.LogError($"Bulk Package Installer: downloaded {paths.Count} " +
                               $"of {_downloads.Length} Asset Store assets");
            }

            var importPaths = new Queue<string>(validPaths);
            onComplete?.Invoke(importPaths);
        }
    }
}