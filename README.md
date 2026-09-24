# Firefly.Client

The product name is **MoniTiR Lite**. It is a Windows desktop client for infrared monitoring: live preview, alarms, a data center, and device configuration. The UI is WPF, and the target framework is .NET Framework 4.6.1. The startup project in this repository is `src/Firefly.Client`. The compiled assembly name is `MoniTiRLite`.

## Requirements

- Windows
- Visual Studio 2022 with the .NET desktop development workload
- The .NET Framework 4.6.1 targeting pack

The solution file is `src/Firefly.Client.sln`. Use **Debug | x64**. The main application is x64. `Firefly.Client.Control` is x86 under the Any CPU configuration, which does not match the main application or the AMD64 assemblies in `src/Lib`.

## Build and run

From the repository root:

```bat
msbuild src\Firefly.Client.sln /t:Build /p:Configuration=Debug /p:Platform=x64
```

Or open `src/Firefly.Client.sln` in Visual Studio, select Debug and x64, and build the startup project `Firefly.Client`.

Run:

```text
src\Firefly.Client\bin\x64\Debug\MoniTiRLite.exe
```

Building `Firefly.Client` copies `src/Lib` into the output directory. The local database is `lite.db` next to the executable. The device address is fixed in `AppConfig.Load` to `http://127.0.0.1:9080`. The UI opens with no device connected. Live preview then has no video.

Settings in `src/Firefly.Client/App.config`:

| Key | Default | Purpose |
| --- | --- | --- |
| `HWACC` | `false` | Hardware acceleration |
| `SUPPORT_LIVE_GROUP` | `false` | Live preview groups |
| `SHOW_SPLASH` | `true` | Startup splash screen |
| `LITE.SERVER_ROOT` | empty | Local server root. Empty uses the application directory |

## Layout

| Path | Role |
| --- | --- |
| `src/Firefly.Client` | Main application. Entry point is `OnStartup` in `App.xaml.cs`. Views are under `View/` |
| `src/Firefly.Client.Control` | WPF controls, including RTSP playback |
| `src/Firefly.Client.Service` | Device, alarm, configuration, and local database access |
| `src/Firefly.Client.Common` | Shared types and constants |
| `src/Firefly.Client.I18n` | UI strings |
| `src/Firefly.SDK.File` | Infrared file read and write. Calls `IRtekFileSDK.dll` |
| `src/Firefly.Device.FireMonitor` | Fire monitoring |
| `src/Firefly.Server.StorageAgent` | Storage and NVR playback |
| `src/Plugin/Firefly.Client.Control.Fire` | Device configuration UI |
| `src/Lib` | DLLs that are not built from source in this repository. Required to build and run |

## src/Lib

Do not modify, replace, or delete these files. Assemblies that have source projects are built from those projects under `src`. The copies in `src/Lib` named `Firefly.Client.Common.dll`, `Firefly.Client.Control.dll`, and `Firefly.Client.Control.Fire.dll` are binaries copied with the directory.

Referenced directly by projects:

| File | Referenced by |
| --- | --- |
| `Amur.Common.dll`, `Amur.Controls.dll` | Main application, Control, Service, and the device configuration plugin |
| `Firefly.Device.dll`, `Firefly.I18n.dll`, `Firefly.LogFactory.dll`, `Firefly.Painter.dll`, `Firefly.Stream.Core.dll` | `Firefly.Device.FireMonitor` |
| `OWB.dll`, `OWB.DataFrame.dll`, `OWB.DCCamera.dll`, `OWB.Drawing.dll`, `OWB.IRCamera.dll`, `OWB.NetSDK.dll` | `Firefly.Device.FireMonitor` |
| `OWB.NVR.dll` | `Firefly.Server.StorageAgent` |

Loaded by P/Invoke or copied with the whole directory:

| Path | Role |
| --- | --- |
| `FFmpeg/` | FFmpeg 5.1 libraries used to decode RTSP |
| `libssl-3-x64.dll`, `libcrypto-3-x64.dll` | OpenSSL 3.0.5 |
| `IRtekFileSDK.dll`, `IRtek.IRtekFileSDK.dll`, `IRtekNetSDK.dll` | Infrared file and network SDKs |
| `FANetSDK.dll` | Infrared network SDK |
| `RTSPServer.dll`, `ZLMediaKit.Autogen.dll`, `srtp2.dll` | RTSP server |
| `HCNetSDK/` | NVR native libraries. Loaded from `HCNetSDK\HCNetSDK.dll` in the output directory |
| `OWB.IRFile.dll`, `OWB.Onvif.dll`, `OWB.Palette.dll`, `OWB.Radiation.dll`, `OWB.SVSDK.dll`, `OWB.CZFSDK.dll`, `OWB.MeasureSDK.dll`, `MeasureUtils.dll`, `Firefly.Mqtt.dll`, `Firefly.UNIFileSDK.dll` | Copied to the output directory with `src/Lib` |
| `log4net.config` | Log configuration. Copied to the output directory |

## Third-party notices

### Tabler Icons

Icon paths in `src/Firefly.Client/Resources/svg_resources.xaml` come from [Tabler Icons](https://tabler-icons.io/). Names match the upstream icons.

MIT License

Copyright (c) 2020-2026 Paweł Kuna

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

### FFmpeg

Libraries under `src/Lib/FFmpeg/` (avcodec 59.37.100, avformat 59.27.100, avutil 57.28.100, avdevice 59.7.100, avfilter 8.44.100, swresample 4.7.100, swscale 6.7.100) are Copyright (C) 2000-2022 FFmpeg Project.

They are used by dynamic linking under the GNU Lesser General Public License 2.1 or later: https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html . The library versions match the FFmpeg 5.1 release. Source tag: https://github.com/FFmpeg/FFmpeg/releases/tag/n5.1 . This repository does not record the configure flags used to build these binaries. If the build enabled GPL components, the GNU General Public License applies: https://www.gnu.org/licenses/gpl-3.0.html .

### OpenSSL

`src/Lib/libssl-3-x64.dll` and `src/Lib/libcrypto-3-x64.dll` are OpenSSL 3.0.5.

Copyright 1998-2022 The OpenSSL Authors. All rights reserved.

Licensed under the Apache License, Version 2.0: https://www.apache.org/licenses/LICENSE-2.0 . OpenSSL license notes: https://www.openssl.org/source/license.html .
