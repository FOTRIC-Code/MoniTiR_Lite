# Firefly.Client

产品名称是 **MoniTiR Lite**。这是 Windows 桌面客户端，用于红外监控：实时预览、告警、数据中心和火情配置。界面是 WPF，目标框架 .NET Framework 4.6.1。仓库里的启动项目是 `src/Firefly.Client`，编译出的程序集名是 `MoniTiRLite`。

## 环境

- Windows
- Visual Studio 2022（含 .NET 桌面开发工作负载）
- .NET Framework 4.6.1 目标包

解决方案文件是 `src/Firefly.Client.sln`。请使用 **Debug | x64**。主程序是 x64。`Firefly.Client.Control` 在 Any CPU 配置下是 x86，和主程序、`src/Lib` 里的 AMD64 程序集不一致。

## 编译和运行

在仓库根目录执行：

```bat
msbuild src\Firefly.Client.sln /t:Build /p:Configuration=Debug /p:Platform=x64
```

或在 Visual Studio 中打开 `src/Firefly.Client.sln`，配置选 Debug、平台选 x64，生成启动项目 `Firefly.Client`。

运行：

```text
src\Firefly.Client\bin\x64\Debug\MoniTiRLite.exe
```

生成 `Firefly.Client` 时会把 `src/Lib` 复制到输出目录。本地数据文件是可执行文件同目录下的 `lite.db`。设备访问地址在 `AppConfig.Load` 中固定为 `http://127.0.0.1:9080`。没有设备时界面可以打开，实时预览没有画面。

`src/Firefly.Client/App.config` 里相关项：

| 键 | 默认值 | 作用 |
| --- | --- | --- |
| `HWACC` | `false` | 硬件加速 |
| `SUPPORT_LIVE_GROUP` | `false` | 实时预览分组 |
| `SHOW_SPLASH` | `true` | 启动闪屏 |
| `LITE.SERVER_ROOT` | 空 | 本地服务根目录，空则用程序目录 |

## 目录

| 路径 | 作用 |
| --- | --- |
| `src/Firefly.Client` | 主程序。入口是 `App.xaml.cs` 的 `OnStartup`。界面在 `View/` |
| `src/Firefly.Client.Control` | WPF 控件，含 RTSP 播放 |
| `src/Firefly.Client.Service` | 设备、告警、配置和本地数据库访问 |
| `src/Firefly.Client.Common` | 公共类型和常量 |
| `src/Firefly.Client.I18n` | 界面文案 |
| `src/Firefly.SDK.File` | 红外文件读写，调用 `IRtekFileSDK.dll` |
| `src/Firefly.Device.FireMonitor` | 火情监测 |
| `src/Firefly.Server.StorageAgent` | 存储与 NVR 回放 |
| `src/Plugin/Firefly.Client.Control.Fire` | 火情设备配置界面 |
| `src/Lib` | 没有放进本仓库源码的 DLL，编译和运行都要保留 |

## src/Lib

不要修改、替换或删除这些文件。带源码的工程编出来的程序集以 `src` 下对应工程为准；`src/Lib` 里同名的 `Firefly.Client.Common.dll`、`Firefly.Client.Control.dll`、`Firefly.Client.Control.Fire.dll` 是随目录复制的二进制。

工程直接引用：

| 文件 | 引用方 |
| --- | --- |
| `Amur.Common.dll`、`Amur.Controls.dll` | 主程序、Control、Service、火情插件 |
| `Firefly.Device.dll`、`Firefly.I18n.dll`、`Firefly.LogFactory.dll`、`Firefly.Painter.dll`、`Firefly.Stream.Core.dll` | `Firefly.Device.FireMonitor` |
| `OWB.dll`、`OWB.DataFrame.dll`、`OWB.DCCamera.dll`、`OWB.Drawing.dll`、`OWB.IRCamera.dll`、`OWB.NetSDK.dll` | `Firefly.Device.FireMonitor` |
| `OWB.NVR.dll` | `Firefly.Server.StorageAgent` |

运行时由 P/Invoke 或整目录复制带上：

| 路径 | 作用 |
| --- | --- |
| `FFmpeg/` | RTSP 解码用的 FFmpeg 5.1 动态库 |
| `libssl-3-x64.dll`、`libcrypto-3-x64.dll` | OpenSSL 3.0.5 |
| `IRtekFileSDK.dll`、`IRtek.IRtekFileSDK.dll`、`IRtekNetSDK.dll` | 红外文件和网络 SDK |
| `FANetSDK.dll` | 红外网络 SDK |
| `RTSPServer.dll`、`ZLMediaKit.Autogen.dll`、`srtp2.dll` | RTSP 服务 |
| `HCNetSDK/` | NVR 原生库，加载路径是输出目录下的 `HCNetSDK\HCNetSDK.dll` |
| `OWB.IRFile.dll`、`OWB.Onvif.dll`、`OWB.Palette.dll`、`OWB.Radiation.dll`、`OWB.SVSDK.dll`、`OWB.CZFSDK.dll`、`OWB.MeasureSDK.dll`、`MeasureUtils.dll`、`Firefly.Mqtt.dll`、`Firefly.UNIFileSDK.dll` | 随 `src/Lib` 复制到输出目录 |
| `log4net.config` | 日志配置，复制到输出目录 |

## Third-party notices

### Tabler Icons

`src/Firefly.Client/Resources/svg_resources.xaml` 中的图标路径来自 [Tabler Icons](https://tabler-icons.io/)，命名与上游一致。

MIT License

Copyright (c) 2020-2026 Paweł Kuna

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

### FFmpeg

`src/Lib/FFmpeg/` 下的动态库（avcodec 59.37.100、avformat 59.27.100、avutil 57.28.100、avdevice 59.7.100、avfilter 8.44.100、swresample 4.7.100、swscale 6.7.100）版权归 FFmpeg Project 所有：Copyright (C) 2000-2022 FFmpeg Project。

这些库以动态链接方式使用，许可为 GNU Lesser General Public License 2.1 或更高版本：https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html 。库版本号对应 FFmpeg 5.1 发行版，源码标签为 https://github.com/FFmpeg/FFmpeg/releases/tag/n5.1 。本仓库没有记录这套二进制的 configure 参数。若编译时启用了 GPL 组件，则适用 GNU General Public License：https://www.gnu.org/licenses/gpl-3.0.html 。

### OpenSSL

`src/Lib/libssl-3-x64.dll` 与 `src/Lib/libcrypto-3-x64.dll` 为 OpenSSL 3.0.5。

Copyright 1998-2022 The OpenSSL Authors. All rights reserved.

Licensed under the Apache License, Version 2.0: https://www.apache.org/licenses/LICENSE-2.0 。OpenSSL 许可证说明见 https://www.openssl.org/source/license.html 。
