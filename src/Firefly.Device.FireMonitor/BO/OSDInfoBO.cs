// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections.Generic;

using OWB.DataFrame;

namespace Firefly.Device.FireMonitor
{
    public class OSDInfoBO
    {
        public bool Enabled { get; private set; }

        public ChannelConfigBO ChannelConfig { get; private set; }

        public PTZOSDConfigBO PTZOSDConfig { get; private set; }

        public string PresetName { get; private set; }

        public MarkerOSDConfigBO MarkerOSDConfig { get; private set; }

        public OWBIRFrame IRFrame { get; private set; }

        public OWBDCFrame DCFrame { get; private set; }

        public List<OWBMarker> Markers { get; private set; }

        public AlarmOSDConfigBO AlarmOSDConfig { get; private set; }

        public CustomerOSDBO CustomerOSD { get; set; }

        public OSDInfoBO()
        {
            PresetName = string.Empty;
            AlarmOSDConfig = new AlarmOSDConfigBO();
            CustomerOSD = new CustomerOSDBO();
        }

        public void RefreshFrame(OWBIRFrame irFrame, OWBDCFrame dcFrame, List<OWBMarker> markers, string presetName)
        {
            IRFrame = irFrame;
            DCFrame = dcFrame;
            Markers = markers;
            PresetName = presetName;
        }

        public void RefreshPresetName(string presetName)
        {
            PresetName = presetName ?? string.Empty;
        }

        public void RefreshAlarm(string alarmContent)
        {
            AlarmOSDConfig.RefreshAlarm(alarmContent);
        }

        public void RefreshConfig(bool enabled, ChannelConfigBO channelConfigBO, PTZOSDConfigBO ptzOSDConfig, MarkerOSDConfigBO markerOSDConfig)
        {
            Enabled = enabled;
            ChannelConfig = channelConfigBO;
            PTZOSDConfig = ptzOSDConfig;
            MarkerOSDConfig = markerOSDConfig;
        }

        public ChannelBO GetFirstChannel()
        {
            ChannelBO channelBO = new ChannelBO();
            if (ChannelConfig != null && ChannelConfig.ChannelItems?.Count > 0)
            {
                channelBO = ChannelConfig.ChannelItems.FirstOrDefault();
            }

            return channelBO;
        }

        public PTZOSDConfigBO GetPTZConfig()
        {
            return PTZOSDConfig == null ? new PTZOSDConfigBO() : PTZOSDConfig;
        }

        public MarkerOSDConfigBO GetMarkerConfig()
        {
            return MarkerOSDConfig == null ? new MarkerOSDConfigBO() : MarkerOSDConfig;
        }

        public AlarmOSDConfigBO GetAlarmOSDConfig()
        {
            AlarmOSDConfig.Status = AlarmOSDConfig.Status == AlarmOSDStatus.Black ? AlarmOSDStatus.Red : AlarmOSDStatus.Black;

            return AlarmOSDConfig;
        }
    }

    public class AlarmOSDConfigBO
    {
        public AlarmOSDStatus Status { get; internal set; }

        public string AlarmContent { get; private set; }

        internal void RefreshAlarm(string alarmContent)
        {
            AlarmContent = alarmContent;
        }
    }
}
