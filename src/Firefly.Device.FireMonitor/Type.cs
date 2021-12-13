// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

namespace Firefly.Device.FireMonitor
{
    public enum FireMonitorAlarmLevel : byte
    {
        None = 0,
        Alert1 = 1,
        Alert2 = 2,
        Alarm = 3
    }

    public enum CruiseAction
    {
        None = 0,
        AutoCruiseRun = 1,
        AutoCruiseStop = 2,
        ManualCruiseRun = 3,
        ManualCruiseStop = 4,
        InstantCruiseRun = 5,
        InstantCruiseStop = 6,
        InstantCruiseWaiting = 7,
    }
}
