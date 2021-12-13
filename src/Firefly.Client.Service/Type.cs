// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace Firefly.Client.Service
{
    public enum PTZCommand
    {
        FOCUS_NEAR = 0, // 焦点前调
        FOCUS_FAR = 1, // 焦点后调
        FOCUS_AUTO = 2, // 自动对焦
        ZOOM_IN = 3, // 倍率变大（仅指可见光）
        ZOOM_OUT = 4, // 倍率变小（仅指可见光）
        IRIS_OPEN = 5, // 光圈扩大 （仅指可见光）
        IRIS_CLOSE = 6, // 光圈缩小（仅指可见光）
        TILT_UP = 7, // 云台上仰
        TILT_DOWN = 8, // 云台下俯
        PAN_LEFT = 9, // 云台左转
        PAN_RIGHT = 10, // 云台右转
        UP_LEFT = 11, // 云台上仰和左转
        UP_RIGHT = 12, // 云台上仰和右转
        DOWN_LEFT = 13, // 云台下俯和左转
        DOWN_RIGHT = 14, // 云台下俯和右转
        WIPER = 15 // 执行雨刷功能
    }

    public enum DeviceStatus
    {
        Offline = 0,
        Online = 1
    }

    public enum ServerType
    {
        None = 0,
        FireMonitorAgent = 1,

        //AlarmAgent = 2,
        //LadleAgent = 3,
        StoreAgent = 4,

        EquipmentMonitorAgent = 5,
        EnvironmentMonitorAgent = 6,
        SecurityMonitorTransferAgent = 7,
        InspectionTaskAgent = 8,
        [Obsolete("Partial discharge is not supported in Lite.")]
        PartialDischargeMonitorAgent = 9,
        AcousticMonitorAgent = 12
    }

    public enum DeviceType
    {
        IRCamera,       //红外相机
        DCCamera,       //可见光相机
        Sensor,
        PDSensor,       //废弃
        [Obsolete("Partial discharge is not supported in Lite.")]
        UHFSensor,      //特高频局放传感器（废弃）
        EnvTempSensor,  //环境温度传感器
        EnvHumSensor,   //环境湿度传感器
        SSensor,        //位移传感器
        VSensor,        //速度传感器
        ASensor,        //加速度传感器
        TempSensor,     //温度传感器
        NoiseSensor,    //噪声传感器
        TcSensor,       //热电偶传感器
        ACCamera,       //声像相机
    }

    public enum PTZPresetCommand
    {
        GOTO = 0, // 移动到指定预置位
        CLE = 1, // 清除预置位
        SET = 2, // 设置预置位
    }

    public enum PTZCruiseCommand
    {
        RUN = 0,
        STOP = 1
    }

    public enum PTZControlAction
    {
        Start = 0,
        Stop = 1
    }

    public enum AlarmStatus
    {
        Normal = 0,
        Alarm = 1
    }

    public enum AlarmActionType
    {
        None = 0,
        SMS = 1,
        VoiceCall = 2,
        Email = 3,
        WeChat = 4,
        DingTalk = 5
    }

    public enum AlarmCategoryType
    {
        FireMonitorAlarm,
        EquipmentMonitorAlarm,
        EnvironmentMonitorAlarm,
        SecurityMonitorAlarm
    }

    public enum ProtocolType
    {
        FireMonitor_S,
        FireMonitor_D,
    }

    public enum ChannelCommand
    {
        Low = 0,
        High = 1
    }

    public enum FileType
    {
        None = 0,
        DCFile = 1,
        IRTekFile_IR = 2,
        UNIFile_IR = 3,
        UNIFile_PRPS = 4,
        UNIFile_PRPD = 5
    }

    public enum UserType
    {
        Operator,
        Admin,
        SuperAdmin
    }

    public enum PermissionType
    {
        Agent,
        User,
        Device,
        DeviceConf,
        Region,
        Equipment
    }

    public enum AlarmConfirmationStatus
    {
        All = 0,
        Confirmed = 1,
        Unconfirmed = 2
    }
}