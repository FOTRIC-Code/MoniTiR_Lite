// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;


namespace Firefly.Client.Control.Fire
{
    public class FireAgentDataObject
    {
        public FireAgentDataObject()
        {
            AlarmRuleConfig = null;
            EventTrigger = null;
            CruiseRouteConfig = null;
            PresetConfig = null;
            TaskSchedule = null;
            AlarmSchedule = null;
            GeneralConfig = null;
            OSDConfigEnabled = false;
            ChannelConfig = null;
            PTZOSDConfig = null;
            MarkerOSDConfig = null;
        }

        [JsonProperty("osdConfigEnabled")]
        public bool OSDConfigEnabled { get; set; }

        [JsonProperty("channelConfig")]
        public ChannelConfigDataObject ChannelConfig { get; set; }

        [JsonProperty("ptzOSDConfig")]
        public PTZOSDConfigDataObject PTZOSDConfig { get; set; }

        [JsonProperty("markerOSDConfig")]
        public MarkerOSDConfigDataObject MarkerOSDConfig { get; set; }

        [JsonProperty("alarmRuleConfig")]
        public AlarmRuleDataObject AlarmRuleConfig { get; set; }

        [JsonProperty("eventTrigger")]
        public EventTriggerDataObject EventTrigger { get; set; }

        [JsonProperty("cruiseRouteConfig")]
        public CruiseRouteDataObject CruiseRouteConfig { get; set; }

        [JsonProperty("presetConfig")]
        public DevicePresetDataObject PresetConfig { get; set; }

        [JsonProperty("taskSchedule")]
        public TaskScheduleDataObject TaskSchedule { get; set; }

        [JsonProperty("alarmSchedule")]
        public AlarmScheduleDataObject AlarmSchedule { get; set; }

        [JsonProperty("generalConfig")]
        public GeneralConfigDataObject GeneralConfig { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }
}