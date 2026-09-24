# Firefly.Client

Conventions for people and coding agents changing this repository. Product description and build steps are in `README.md`.

## Stack

- C#, WPF, .NET Framework 4.6.1.
- Legacy csproj format. Do not convert projects to SDK-style, and do not change the target framework.
- Solution: `src/Firefly.Client.sln`.
- Build: `msbuild src\Firefly.Client.sln /t:Build /p:Configuration=Debug /p:Platform=x64`.
- The main executable is `src/Firefly.Client/bin/x64/Debug/MoniTiRLite.exe`. The product name is MoniTiR Lite.
- Use x64. Do not use Any CPU or x86 as the shipping configuration.

## Where to edit

- Entry point: `OnStartup` in `src/Firefly.Client/App.xaml.cs`.
- UI: `src/Firefly.Client/View/`.
- Devices, alarms, configuration, and the local database: `src/Firefly.Client.Service/`.
- Controls and RTSP: `src/Firefly.Client.Control/`.
- Device configuration UI: `src/Plugin/Firefly.Client.Control.Fire/`.
- UI strings: `src/Firefly.Client.I18n/`. After editing, update the generated Designer or save the resx in Visual Studio.

## Leave unchanged

- DLLs under `src/Lib`, including `HCNetSDK/` and `FFmpeg/`. They have no source here and are required to build and run.
- The two-line source header: `Copyright (c) IRtek.Co.Ltd.` and `SPDX-License-Identifier: Apache-2.0`.
- The root `LICENSE` file. It is the Apache License 2.0 text.
