// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using OWB.DataFrame;

namespace Firefly.Device.FireMonitor
{
    public class ScriptAlarmDataBO
    {
        public ScriptAlarmDataBO(string deviceID, string deviceName, int presetID, string presetName, OWBIRFrame irFrame, OWBDCFrame dcFrame, List<FireAlarmMarkerBO> alarmMarkers)
        {
            DeviceID = deviceID;
            DeviceName = deviceName;
            PresetID = presetID;
            PresetName = presetName;
            IRFrame = irFrame;
            DCFrame = dcFrame;
            AlarmMarkers = alarmMarkers;
        }

        public string DeviceID { get; set; }
        public string DeviceName { get; set; }
        public int PresetID { get; set; }
        public string PresetName { get; set; }
        public OWBIRFrame IRFrame { get; set; }
        public OWBDCFrame DCFrame { get; set; }
        public List<FireAlarmMarkerBO> AlarmMarkers { get; set; }
    }
}