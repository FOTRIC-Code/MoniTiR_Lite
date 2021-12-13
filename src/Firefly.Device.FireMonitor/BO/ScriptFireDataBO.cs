// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

using OWB.DataFrame;

namespace Firefly.Device.FireMonitor
{
    public class ScriptFireDataBO
    {
        public ScriptFireDataBO(DateTime happenTime, long timestamp, string deviceID, string deviceName, int presetID, string presetName, OWBIRFrame irFrame, OWBDCFrame dcFrame, List<FireAlarmMarkerBO> alarmMarkerList)
        {
            HappenTime = happenTime;
            Timestamp = timestamp;
            DeviceID = deviceID;
            DeviceName = deviceName;
            PresetID = presetID;
            PresetName = presetName;
            IRFrame = irFrame;
            DCFrame = dcFrame;
            AlarmMarkerList = alarmMarkerList;
        }

        public DateTime HappenTime { get; set; }

        public long Timestamp { get; set; }

        public string DeviceID { get; set; }

        public string DeviceName { get; set; }

        public int PresetID { get; set; }

        public string PresetName { get; set; }

        public OWBIRFrame IRFrame { get; set; }

        public OWBDCFrame DCFrame { get; set; }

        public List<FireAlarmMarkerBO> AlarmMarkerList { get; set; }

        public bool IsDataRecord { get; set; }

        public bool IsImageRecord { get; set; }

        public bool IsPostAlarm { get; set; }
    }
}
