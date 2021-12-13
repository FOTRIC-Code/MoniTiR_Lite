# Firefly.Client

给改这个仓库的人或编码助手看的约定。产品说明和编译步骤在 `README.md`。

## 技术栈

- C#，WPF，.NET Framework 4.6.1。
- 旧式 csproj。不要改成 SDK 风格工程，不要升级目标框架。
- 解决方案：`src/Firefly.Client.sln`。
- 生成：`msbuild src\Firefly.Client.sln /t:Build /p:Configuration=Debug /p:Platform=x64`。
- 主程序输出是 `src/Firefly.Client/bin/x64/Debug/MoniTiRLite.exe`。产品名称是 MoniTiR Lite。
- 平台用 x64。不要用 Any CPU 或 x86 作为交付配置。

## 从哪里改

- 入口：`src/Firefly.Client/App.xaml.cs` 的 `OnStartup`。
- 界面：`src/Firefly.Client/View/`。
- 设备、告警、配置、本地库：`src/Firefly.Client.Service/`。
- 控件和 RTSP：`src/Firefly.Client.Control/`。
- 火情配置界面：`src/Plugin/Firefly.Client.Control.Fire/`。
- 文案：`src/Firefly.Client.I18n/`。改完需要同步生成的 Designer，或在 Visual Studio 里保存 resx。

## 不要动

- `src/Lib` 下的 DLL 和 `HCNetSDK/`、`FFmpeg/`。没有源码，编译和运行都依赖它们。
- 源文件头的两行版权声明：`Copyright (c) IRtek.Co.Ltd.` 和 `SPDX-License-Identifier: Apache-2.0`。
- 根目录 `LICENSE` 是 Apache License 2.0 原文。
