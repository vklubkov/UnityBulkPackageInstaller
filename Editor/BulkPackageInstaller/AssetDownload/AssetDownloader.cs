using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BulkPackageInstaller {
    internal class AssetDownloader {
        const string _url = "https://packages-v2.unity.com/-/api/legacy-package-download-info/";
        readonly static Regex _invalidPathCharsRegExp = new(@"[^a-zA-Z0-9() _-]");
        readonly string _cachePath;

        AssetDownloadInfo _assetDownloadInfo;
        Action<string> _onComplete;

        Action<string, string, int, int> _downloadCallback;

        public AssetDownloader(string cachePath) => _cachePath = cachePath;

        public IEnumerator Download(string assetId, Action<string> onComplete) {
            var url = _url + assetId;
            using var assetInfoRequest = UnityWebRequest.Get(url);
            assetInfoRequest.SetRequestHeader("Content-Type", "application/json");
            assetInfoRequest.SetRequestHeader("Authorization", $"Bearer {CloudProjectSettings.accessToken}");
            yield return assetInfoRequest.SendWebRequest();
            if (assetInfoRequest.result != UnityWebRequest.Result.Success) {
                Debug.LogError(assetInfoRequest.error);
                yield break;
            }

            var text = assetInfoRequest.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<AssetDownloadInfoResponse>(text);

            _assetDownloadInfo = response.Result.Download;
            _onComplete = onComplete;
            DownloadAsset();
        }

        void DownloadAsset() {
            var editorAssembly = Assembly.Load("UnityEditor.CoreModule");
            if (editorAssembly == null) {
                Debug.LogError("Bulk Package Installer: UnityEditor.CoreModule assembly not found");
                _onComplete?.Invoke(null);
                return;
            }

            var assetStoreUtilsType = editorAssembly.GetType("UnityEditor.AssetStoreUtils");
            if (assetStoreUtilsType == null) {
                Debug.LogError("Bulk Package Installer: UnityEditor.AssetStoreUtils assembly not found");
                _onComplete?.Invoke(null);
                return;
            }

            var downloadMethod = assetStoreUtilsType.GetMethod("Download", BindingFlags.Public | BindingFlags.Static);
            if (downloadMethod == null) {
                Debug.LogError("Bulk Package Installer: UnityEditor.AssetStoreUtils.Download method not found");
                _onComplete?.Invoke(null);
                return;
            }

            var checkDownloadMethod =
                assetStoreUtilsType.GetMethod("CheckDownload", BindingFlags.Public | BindingFlags.Static);

            if (checkDownloadMethod == null) {
                Debug.LogError("Bulk Package Installer: UnityEditor.AssetStoreUtils.CheckDownload method not found");
                _onComplete?.Invoke(null);
                return;
            }

            var downloadCallback = editorAssembly.GetType("UnityEditor.AssetStoreUtils+DownloadDoneCallback");
            if (downloadCallback == null) {
                Debug.LogError("Bulk Package Installer: UnityEditor.AssetStoreUtils+DownloadDoneCallback type not found");
                _onComplete?.Invoke(null);
                return;
            }

            _downloadCallback = DownloadCallback;

            var downloadDelegate = Delegate.CreateDelegate(
                downloadCallback, _downloadCallback.Target, _downloadCallback.Method);

            var requestPart = BuildRequestPart(_assetDownloadInfo);

            var checkDownloadRequest = new object[] {
                _assetDownloadInfo.Id, _assetDownloadInfo.Url, requestPart, _assetDownloadInfo.Key
            };

            var downloadStatusJson = (string)checkDownloadMethod.Invoke(null, checkDownloadRequest);
            var downloadStatus = JsonConvert.DeserializeObject<DownloadStatusInfo>(downloadStatusJson);
            if (downloadStatus.IsInProgress) {
                _downloadCallback = null;

                Debug.LogWarning($"Bulk Package Installer: {_assetDownloadInfo.PackageName}" +
                                 " download is already in progress");

                _onComplete?.Invoke(null);
                return;
            }

            var shouldResume = downloadStatus.Download?.Url == _assetDownloadInfo.Url &&
                               downloadStatus.Download?.Key == _assetDownloadInfo.Key;

            var download = new DownloadInfo { Url = _assetDownloadInfo.Url, Key = _assetDownloadInfo.Key };
            var downloadJson = JsonConvert.SerializeObject(download);

            downloadMethod.Invoke(null, new object[] {
                _assetDownloadInfo.Id, _assetDownloadInfo.Url, requestPart,
                _assetDownloadInfo.Key, downloadJson, shouldResume, downloadDelegate
            });
        }

        void DownloadCallback(string packageId, string message, int bytes, int total) {
            _downloadCallback = null;
            if (message != "ok") {
                Debug.LogError($"Bulk Package Installer: asset download failed with message: {message}");
                _onComplete?.Invoke(null);
                return;
            }

            Debug.Log($"Bulk Package Installer: downloaded Asset Store asset '{_assetDownloadInfo.PackageName}'.");

            var path = _cachePath + "/" +
                       _assetDownloadInfo.PublisherName + "/" +
                       _assetDownloadInfo.CategoryName + "/" +
                       _assetDownloadInfo.PackageName + ".unitypackage";

            _onComplete.Invoke(path);
        }

        static string[] BuildRequestPart(AssetDownloadInfo assetDownloadInfo) => new[] {
            Sanitize(assetDownloadInfo.PublisherName),
            Sanitize(assetDownloadInfo.CategoryName),
            SanitizePackageIdentifier(assetDownloadInfo.PackageName, assetDownloadInfo.Id, assetDownloadInfo.Url)
        };

        static string SanitizePackageIdentifier(string packageName, string packageId, string url) {
            var sanitizedPackageName = Sanitize(packageName);
            if (!string.IsNullOrEmpty(sanitizedPackageName))
                return sanitizedPackageName;

            sanitizedPackageName = Sanitize(packageId);
            return string.IsNullOrEmpty(sanitizedPackageName) ? Sanitize(url) : sanitizedPackageName;
        }

        static string Sanitize(string source) => _invalidPathCharsRegExp.Replace(source, string.Empty);
    }
}