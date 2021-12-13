// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Drawing;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using OWB.DataFrame;

namespace Firefly.Device.FireMonitor
{
    public class FireMonitorDeviceConfigBO
    {
        [JsonPropertyName("generalConfig")]
        public FireMonitorGeneralConfigBO GeneralConfigBO { get; set; }

        [JsonPropertyName("eventTrigger")]
        public FireMonitorEventTriggerBO EventTriggerBO { get; set; }

        [JsonPropertyName("presetConfig")]
        public FireMonitorPresetConfigBO PresetConfigBO { get; set; }

        [JsonPropertyName("alarmRuleConfig")]
        public FireMonitorAlarmRuleConfigBO AlarmRuleConfigBO { get; set; }

        [JsonPropertyName("cruiseRouteConfig")]
        public FireMonitorCruiseRouteConfigBO CruiseRouteConfigBO { get; set; }

        [JsonPropertyName("taskSchedule")]
        public FireMonitorTaskScheduleBO TaskScheduleBO { get; set; }

        [JsonPropertyName("alarmSchedule")]
        public FireMonitorAlarmScheduleBO AlarmScheduleBO { get; set; }

        [JsonPropertyName("osdConfigEnabled")]
        public bool OSDConfigEnabled { get; set; }

        [JsonPropertyName("channelConfig")]
        public ChannelConfigBO ChannelConfigBO { get; set; }

        [JsonPropertyName("ptzOSDConfig")]
        public PTZOSDConfigBO PTZOSDConfig { get; set; }

        [JsonPropertyName("markerOSDConfig")]
        public MarkerOSDConfigBO MarkerOSDConfig { get; set; }
    }
	
    public class FireMonitorGeneralConfigBO
    {
        [JsonPropertyName("sampleInterval")]
        public float SampleInterval { get; set; }

        [JsonPropertyName("dataRecordEnabled")]
        public bool DataRecordEnabled { get; set; }

        [JsonPropertyName("dataRecordInterval")]
        public int DataRecordInterval { get; set; }

        [JsonPropertyName("imageRecordEnabled")]
        public bool ImageRecordEnabled { get; set; }

        [JsonPropertyName("imageRecordInterval")]
        public int ImageRecordInterval { get; set; }

        [JsonPropertyName("fixedMonitoring")]
        public bool FixedMonitoring { get; set; }

        [JsonPropertyName("enableSuperResolution​​")]
        public bool EnableSuperResolution​​ { get; set; }

        [JsonIgnore]
        public int DataRecordValue { get => DataRecordInterval; }

        [JsonIgnore]
        public int ImageRecordValue { get => DataRecordInterval * ImageRecordInterval; }
    }

    public class FireMonitorEventTriggerBO
    {
        [JsonPropertyName("alert1Out")]
        public List<int> Alert1OutList { get; set; }

        [JsonPropertyName("alert2Out")]
        public List<int> Alert2OutList { get; set; }

        [JsonPropertyName("alarmOut")]
        public List<int> AlarmOutList { get; set; }
    }

    public class FireMonitorPresetConfigBO
    {
        [JsonPropertyName("presetList")]
        public List<FireMonitorPresetBO> PresetList { get; set; }

        internal FireMonitorPresetBO Get(int id)
        {
            FireMonitorPresetBO fireMonitorPresetBO = null;
            foreach (FireMonitorPresetBO presetBO in PresetList)
            {
                if (presetBO.ID == id)
                {
                    fireMonitorPresetBO = presetBO;
                    break;
                }
            }
            return fireMonitorPresetBO;
        }
    }

    public class FireMonitorPresetBO
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("sensitivity")]
        public int Sensitivity { get; set; }

        [JsonPropertyName("shieldMaskList")]
        public List<FireMonitorShieldMaskBO> ShieldMaskList { get; set; }

        [JsonPropertyName("markerList")]
        public List<FireMonitorMarkerBO> MarkerList { get; set; }

        internal List<OWBMarker> GetDeviceMarker()
        {
            List<OWBMarker> markerList = new List<OWBMarker>();
            foreach (FireMonitorMarkerBO markerModel in MarkerList)
            {
                List<Point> pointList = new List<Point>();
                markerModel.PointList.ForEach(a => pointList.Add(new Point(a.X, a.Y)));
                switch (markerModel.MarkerType)
                {
                    case MarkerType.Spot:
                        OWBSpotMarker spotMarker = new OWBSpotMarker(markerModel.Name, pointList[0], true, markerModel.Emissivity, markerModel.ReflTemp, markerModel.Distance, markerModel.OffsetTemp);
                        markerList.Add(spotMarker);
                        break;
                    case MarkerType.Line:
                        OWBPolylineMarker lineMarker = new OWBPolylineMarker(markerModel.Name, pointList.ToArray(), true, markerModel.Emissivity, markerModel.ReflTemp, markerModel.Distance, markerModel.OffsetTemp);
                        markerList.Add(lineMarker);
                        break;
                    case MarkerType.Region:
                        OWBPolygonMarker polygonMarker = new OWBPolygonMarker(markerModel.Name, pointList.ToArray(), true, markerModel.Emissivity, markerModel.ReflTemp, markerModel.Distance, markerModel.OffsetTemp);
                        markerList.Add(polygonMarker);
                        break;
                }
            }
            return markerList;
        }
    }

    public class FireMonitorShieldMaskBO
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("pointList")]
        public List<PointBO> PointList { get; set; }
    }

    public class FireMonitorMarkerBO
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("alarmRuleID")]
        public int AlarmRuleID { get; set; }

        [JsonPropertyName("emissivity")]
        public float Emissivity { get; set; }

        [JsonPropertyName("distance")]
        public float Distance { get; set; }

        [JsonPropertyName("reflTemp")]
        public float ReflTemp { get; set; }

        [JsonPropertyName("offsetTemp")]
        public float OffsetTemp { get; set; }

        [JsonPropertyName("pointList")]
        public List<PointBO> PointList { get; set; }

        [JsonPropertyName("markerType")]
        public MarkerType MarkerType { get; set; }
    }

    public class FireMonitorAlarmRuleConfigBO
    {
        [JsonPropertyName("alarmRuleList")]
        public List<FireMonitorAlarmRuleBO> AlarmRuleList { get; set; }
    }

    public class FireMonitorAlarmRuleBO
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("ruleType")]
        public RuleType RuleType { get; set; }

        [JsonPropertyName("alert1Temp")]
        public float Alert1Temp { get; set; }

        [JsonPropertyName("alert2Temp")]
        public float Alert2Temp { get; set; }

        [JsonPropertyName("alarmTemp")]
        public float AlarmTemp { get; set; }

        [JsonPropertyName("alert1FilteringTime")]
        public int Alert1FilteringTime { get; set; }

        [JsonPropertyName("alert2FilteringTime")]
        public int Alert2FilteringTime { get; set; }

        [JsonPropertyName("alarmFilteringTime")]
        public int AlarmFilteringTime { get; set; }

        [JsonPropertyName("toleranceTemp")]
        public float ToleranceTemp { get; set; }

        public float GetThreadholdTemp(FireMonitorAlarmLevel alarmLevel)
        {
            float alarmTemp = 0.0f;

            switch (alarmLevel)
            {
                case FireMonitorAlarmLevel.Alert1:
                    alarmTemp = Alert1Temp;
                    break;
                case FireMonitorAlarmLevel.Alert2:
                    alarmTemp = Alert2Temp;
                    break;
                case FireMonitorAlarmLevel.Alarm:
                    alarmTemp = AlarmTemp;
                    break;
            }

            return alarmTemp;
        }

        public FireMonitorAlarmRuleBO Clone()
        {
            return new FireMonitorAlarmRuleBO
            {
                ID = ID,
                Name = Name,
                RuleType = RuleType,
                Alert1Temp = Alert1Temp,
                Alert2Temp = Alert2Temp,
                AlarmTemp = AlarmTemp,
                Alert1FilteringTime = Alert1FilteringTime,
                Alert2FilteringTime = Alert2FilteringTime,
                AlarmFilteringTime = AlarmFilteringTime,
                ToleranceTemp = ToleranceTemp
            };
        }
    }

    public class FireMonitorCruiseRouteConfigBO
    {
        [JsonPropertyName("cruiseRouteList")]
        public List<FireMonitorCruiseRouteBO> CruiseRouteList { get; set; }
    }

    public class FireMonitorCruiseRouteBO
    {
        public FireMonitorCruiseRouteBO()
        {
        }

        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("cruisePointList")]
        public List<FireMonitorCruisePointBO> CruisePointList { get; set; }

        [JsonPropertyName("pauseType")]
        public FireMonitorAlarmLevel PauseType { get; set; }

        [JsonPropertyName("resumeIn")]
        public int ResumeIn { get; set; }
    }

    public class FireMonitorCruisePointBO
    {
        public FireMonitorCruisePointBO()
        { }

        [JsonPropertyName("presetID")]
        public int PresetID { get; set; }

        [JsonPropertyName("waitTime")]
        public int WaitTime { get; set; }

        [JsonPropertyName("speed")]
        public int Speed { get; set; }

        [JsonPropertyName("autoFocus")]
        public bool AutoFocus { get; set; }
    }

    public class FireMonitorTaskScheduleBO
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("restoreTime")]
        public int RestoreTime { get; set; }

        [JsonPropertyName("taskDayList")]
        public List<FireMonitorTaskDayBO> TaskDayList { get; set; }
    }

    public class FireMonitorTaskDayBO
    {
        [JsonPropertyName("dayOfWeek")]
        public int DayOfWeek { get; set; }

        [JsonPropertyName("taskTimeList")]
        public List<FireMonitorTaskTimeBO> TaskTimeList { get; set; }
    }

    public class FireMonitorTaskTimeBO
    {
        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("to")]
        public int To { get; set; }

        [JsonPropertyName("cruiseRouteID")]
        public int CruiseRouteID { get; set; }
    }

    public class FireMonitorAlarmScheduleBO
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("alarmDayList")]
        public List<FireMonitorAlarmDayBO> AlarmDayList { get; set; }
    }

    public class FireMonitorAlarmDayBO
    {
        [JsonPropertyName("dayOfWeek")]
        public int DayOfWeek { get; set; }

        [JsonPropertyName("alarmTimeList")]
        public List<FireMonitorAlarmTimeBO> AlarmTimeList { get; set; }
    }

    public class FireMonitorAlarmTimeBO
    {
        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("to")]
        public int To { get; set; }

        [JsonPropertyName("pauseType")]
        public FireMonitorAlarmLevel PauseType { get; set; }
    }

    public class ChannelConfigBO
    {
        [JsonPropertyName("channelList")]
        public List<ChannelBO> ChannelItems { get; set; }
    }

    public class ChannelBO
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("status")]
        public OSDStatus Status { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("position")]
        public PointBO Position { get; set; }

        [JsonPropertyName("time")]
        public TimeBO TimeBO { get; set; }
    }

    public class TimeBO
    {
        [JsonPropertyName("status")]
        public OSDStatus Status { get; set; }

        [JsonPropertyName("position")]
        public PointBO Position { get; set; }
    }

    public class PTZOSDConfigBO
    {
        [JsonPropertyName("status")]
        public OSDStatus Status { get; set; }

        [JsonPropertyName("position")]
        public PointBO Position { get; set; }
    }

    public class MarkerOSDConfigBO
    {
        [JsonPropertyName("status")]
        public OSDStatus Status { get; set; }

        [JsonPropertyName("showMax")]
        public bool ShowMax { get; set; }

        [JsonPropertyName("showMin")]
        public bool ShowMin { get; set; }

        [JsonPropertyName("showAvg")]
        public bool ShowAvg { get; set; }
    }
}
