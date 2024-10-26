# Bulk Package Installer for Unity

Install and remove NuGet packages, UPM packages and Asset Store assets with single click of a button.

## License

[MIT](LICENSE.md)

## Installation

- Via Unity Package Manager:
   Press the plus sign and choose `Add package from git URL...`. There, use `https://github.com/marked-one/UnityBulkPackageInstaller.git`, or, with version: `https://github.com/marked-one/UnityBulkPackageInstaller.git#1.0.0`
- You can also clone this repository and then add it as a local package using `Add package from disk...` option.
- Another way is to manually edit the `manifest.json` file in your `Packages` folder. Add `"com.vklubkov.bulkpackageinstaller" : "https://github.com/marked-one/UnityBulkPackageInstaller.git"`, or, with version: `"com.vklubkov.bulkpackageinstaller" : "https://github.com/marked-one/UnityBulkPackageInstaller.git#1.0.0"`
- Alternatively, you can download the package into your `Assets` folder

## Getting started

1. Use the `Assets` menu or the right-click menu in the `Project` window and select `Create->Bulk Package Installer->New Installer`
2. Name the installer file
3. Setup the packages you want to install or remove
4. *Optional:* press the `Backup manifest.json` button to save your UPM packages manifest before the install.
5. Press the `Install` button

## Examples

### UPM packages

An installer that handles multiple UPM packages:

- removes the [Unity Version Control](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.collab-proxy.html) package `com.unity.collab-proxy`, as it is not needed when you use Git
- installs a useful [Generic Serializable Dictionary](https://github.com/upscalebaby/generic-serializable-dictionary) with id `com.upscalebaby.generic-serializable-dictionary` from git Url `https://github.com/upscalebaby/generic-serializable-dictionary.git`
- installs [Graphy](https://github.com/Tayx94/graphy), a popular FPS counter, from `package.openupm.com` scoped registry with URL `https://package.openupm.com` and scope `com.openupm com.tayx.graphy`

![UpmPackagesInstaller](.github/UpmPackagesInstaller.png)

### NuGet packages

An installer for the [R3](https://github.com/Cysharp/R3) reactive extensions, which consists of an UPM package `com.cysharp.R3` installed from a Git url `https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity`, and a NuGet package `R3`:

![R3Installer](.github/R3Installer.png)

### Asset Store assets

An installer for the [Free Fly Camera](https://assetstore.unity.com/packages/tools/camera/free-fly-camera-140739) asset.

- the package id `140739` is taken from its URL: `https://assetstore.unity.com/packages/tools/camera/free-fly-camera-140739`, 

- the name of the package can be any, but I recommend to simply copy it from the Asset Store for convenience.

![FreeFlyCameraInstaller](.github/FreeFlyCameraInstaller.png)