// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

namespace Firefly.Client.Common
{
    public enum NVRType
    {
        Invalid = 0,
        //hikvision
        HIKSDK_NET = 1,
        // the device for 大华
        DHSDK_NET = 2,
        XMSDK_NET = 3,
        TWSDK_NET = 4
    }

    public enum DCCameraType
    {
        Invalid = 0,
        //hikvision
        HIKSDK_NET = 1,
        // the device for 大华
        DHSDK_NET = 2,
        // 雄迈
        XMSDK_NET = 3,
        // 同为
        TWSDK_NET = 4,
        // 创智飞
        ONVIF_DC_NET = 5
    }

    public enum IRCameraType
    {
        Invalid = 0,
        // the file of infrared
        IR_IRSFile = 1,
        // all device for new online device in our company
        IRTEKSDK_NET = 2,
        // old device: 800
        FASDK_NET = 3,
        // the device for 舜程
        SVSDK_NET = 4,
        // old device: 600
        RDSDK_NET = 5,
        IRTEKJPG_FLIE = 6,
        // 创智飞
        CZF_ONVIF_IR_NET = 7,
        IRTEKSDK2_NET = 8
    }

    public enum ACCameraType
    {
        Invalid = 0,

        IRTEKACSDK_NET = 1,
    }

    public enum SensorType
    {
        SRCSDK_NET = 0,
        MODBUS_TCP_MASTER_NET = 1,
    }
}
