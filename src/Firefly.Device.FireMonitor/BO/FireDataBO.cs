// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using OWB;
using OWB.DataFrame;

namespace Firefly.Device.FireMonitor
{
    public partial class FireDataBO
    {
        public FireDataBO()
        { }

        public FireDataBO(DateTime happenTime, long timestamp, string deviceID, string deviceName, string protocolType, int presetID, string presetName, List<FireMarkerBO> markers, string rootPath, List<ImageInfoBO> imageInfo, float sampleInterval, bool needRecordData, bool needRecordImage)
        {
            HappenTime = happenTime;
            Timestamp = timestamp;
            DeviceID = deviceID;
            DeviceName = deviceName;
            ProtocolType = protocolType;
            PresetID = presetID;
            PresetName = presetName;
            MarkerList = markers;
            RootPath = rootPath;
            ImageInfo = imageInfo;
            SampleInterval = sampleInterval;
            NeedRecordData = needRecordData;
            NeedRecordImage = needRecordImage;
        }

        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("happenTime")]
        public DateTime HappenTime { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("deviceID")]
        public string DeviceID { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("protocolType")]
        public string ProtocolType { get; set; }

        [JsonPropertyName("presetID")]
        public int PresetID { get; set; }

        [JsonPropertyName("presetName")]
        public string PresetName { get; set; }

        [JsonPropertyName("markerList")]
        public List<FireMarkerBO> MarkerList { get; set; }

        [JsonPropertyName("imageInfo")]
        public List<ImageInfoBO> ImageInfo { get; set; }

        [JsonPropertyName("rootPath")]
        public string RootPath { get; set; }

        [JsonPropertyName("sampleInterval")]
        public float SampleInterval { get; set; }

        [JsonPropertyName("needRecordData")]
        public bool NeedRecordData { get; set; }

        [JsonPropertyName("needRecordImage")]
        public bool NeedRecordImage { get; set; }

        [JsonPropertyName("updateTime")]
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("insertTime")]
        public DateTime InsertTime { get; set; } = DateTime.UtcNow;

        public FireDataBO Clone()
        {
            return new FireDataBO
            {
                ID = ID,
                HappenTime = HappenTime,
                Timestamp = Timestamp,
                DeviceID = DeviceID,
                DeviceName = DeviceName,
                ProtocolType = ProtocolType,
                PresetID = PresetID,
                PresetName = PresetName,
                MarkerList = MarkerList,
                ImageInfo = ImageInfo,
                RootPath = RootPath,
                SampleInterval = SampleInterval,
                UpdateTime = UpdateTime,
                InsertTime = InsertTime,
                NeedRecordData = NeedRecordData,
                NeedRecordImage = NeedRecordImage
            };
        }
    }

    public class FireMarkerBO
    {
        public FireMarkerBO()
        {
            MarkerName = string.Empty;
        }

        public FireMarkerBO(FireMonitorMarkerBO fireMonitorMarkerBO, OWBMarkerTemperaturePoint value) : this()
        {
            MarkerName = fireMonitorMarkerBO.Name;
            MarkerType = fireMonitorMarkerBO.MarkerType;
            MaxTemp = value.Max.Value;
            MinTemp = value.Min.Value;
            AvgTemp = value.Avg;
            PointList = fireMonitorMarkerBO.PointList;
        }

        [JsonPropertyName("markerName")]
        public string MarkerName { get; set; }

        [JsonPropertyName("markerType")]
        public MarkerType MarkerType { get; set; }

        [JsonPropertyName("maxTemp")]
        public float MaxTemp { get; set; }

        [JsonPropertyName("minTemp")]
        public float MinTemp { get; set; }

        [JsonPropertyName("avgTemp")]
        public float AvgTemp { get; set; }

        [JsonPropertyName("pointList")]
        public List<PointBO> PointList { get; set; }

        [JsonPropertyName("markerDesc")]
        public string MarkerDesc { get; set; }

        public OWBMarker ToMarker()
        {
            OWBMarker marker = null;
            List<Point> pointList = new List<Point>();
            PointList.ForEach(a => pointList.Add(new Point(a.X, a.Y)));
            switch (MarkerType)
            {
                case MarkerType.Spot:
                    marker = new OWBSpotMarker(MarkerName, pointList[0]);
                    break;
                case MarkerType.Line:
                    marker = new OWBPolylineMarker(MarkerName, pointList.ToArray());
                    break;
                case MarkerType.Region:
                    marker = new OWBPolygonMarker(MarkerName, pointList.ToArray());
                    break;
            }
            marker.MarkerDesc = MarkerDesc;
            marker.Value = new OWBMarkerTemperaturePoint(
                        new OWBTemperaturePoint(0, 0, MaxTemp),
                        new OWBTemperaturePoint(0, 0, MinTemp),
                        AvgTemp);
            return marker;
        }

        public static FireMarkerBO FromMarker(OWBMarker marker)
        {
            FireMarkerBO markerBO = new FireMarkerBO
            {
                MarkerName = marker.Name,
                MarkerType = (MarkerType)marker.MarkerType,
                MaxTemp = marker.Value.Max.Value,
                MinTemp = marker.Value.Min.Value,
                AvgTemp = marker.Value.Avg,
                MarkerDesc = marker.MarkerDesc,
            };

            markerBO.PointList = new List<PointBO>();
            switch (marker.MarkerType)
            {
                case IRMarkerType.Spot:
                    markerBO.PointList.Add(new PointBO
                    {
                        X = (marker as OWBSpotMarker).Point.X,
                        Y = (marker as OWBSpotMarker).Point.Y
                    });
                    break;
                case IRMarkerType.Polyline:
                    (marker as OWBPolylineMarker).Points.ToList()?.ForEach(point =>
                    {
                        markerBO.PointList.Add(new PointBO
                        {
                            X = point.X,
                            Y = point.Y
                        });
                    });
                    break;
                case IRMarkerType.Polygon:
                    (marker as OWBPolygonMarker).Points.ToList()?.ForEach(point =>
                    {
                        markerBO.PointList.Add(new PointBO
                        {
                            X = point.X,
                            Y = point.Y
                        });
                    });
                    break;
            }
            return markerBO;
        }
    }

    public class FireAlarmMarkerBO
    {
        public FireAlarmMarkerBO()
        {
            MarkerName = string.Empty;
            AlarmLevel = FireMonitorAlarmLevel.None;
            RuleType = RuleType.HighestGreater;
            AlarmRule = null;
            AlarmTemp = OWBMath.KelvinKelvinZero;
        }

        [JsonPropertyName("markerName")]
        public string MarkerName { get; set; }

        [JsonPropertyName("markerType")]
        public MarkerType MarkerType { get; set; }

        [JsonPropertyName("pointList")]
        public List<PointBO> PointList { get; set; }

        [JsonPropertyName("markerDesc")]
        public string MarkerDesc { get; set; }

        [JsonPropertyName("alarmTemp")]
        public float AlarmTemp { get; set; }

        [JsonPropertyName("tempPoint")]
        public OWBMarkerTemperaturePoint TempPoint { get; set; }

        [JsonPropertyName("ruleTemp")]
        public float RuleTemp { get; private set; }

        [JsonPropertyName("alarmLevel")]
        public FireMonitorAlarmLevel AlarmLevel { get; private set; }

        [JsonPropertyName("ruleType")]
        public RuleType RuleType { get; private set; }

        [JsonPropertyName("alarmRule")]
        public FireMonitorAlarmRuleBO AlarmRule { get; private set; }

        [JsonPropertyName("isPostAlarm")]
        public bool IsPostAlarm { get; set; }

        [JsonPropertyName("eventType")]
        public DeviceEventType DeviceEventType { get; set; }

        [JsonIgnore]
        public bool HasAlarm => AlarmLevel != FireMonitorAlarmLevel.None;

        // alarm level can be set by:
        // 1: in sample task: check current temp and alarm rule to figure out which level it belong to be
        // 2: in alarm task: check predict model to compare whether alarm level wait time is timeout
        // 3: in alarm task: alarm level isn't changed if next frame alarm temp is less than tolence
        public void SetAlarmLevel(FireMonitorAlarmLevel alarmLevel)
        {
            AlarmLevel = alarmLevel;
            if (AlarmRule != null)
            {
                RuleTemp = GetRuleTemp(AlarmLevel, AlarmRule);
            }
        }

        public void SetAlarmRule(FireMonitorAlarmRuleBO alarmRule)
        {
            AlarmRule = alarmRule;
            RuleType = alarmRule.RuleType;
        }

        public FireAlarmMarkerBO Clone()
        {
            FireAlarmMarkerBO newMarker = new FireAlarmMarkerBO
            {
                MarkerName = MarkerName,
                MarkerType = MarkerType,
                MarkerDesc = MarkerDesc,
                AlarmTemp = AlarmTemp,
                AlarmLevel = AlarmLevel,
                RuleType = RuleType,
                RuleTemp = RuleTemp,
                AlarmRule = AlarmRule?.Clone(),
                TempPoint = TempPoint,
                IsPostAlarm = IsPostAlarm
            };
            newMarker.PointList = PointList?.Select(it => new PointBO
            {
                X = it.X,
                Y = it.Y
            }).ToList();
            return newMarker;
        }

        private float GetRuleTemp(FireMonitorAlarmLevel alarmLevel, FireMonitorAlarmRuleBO alarmRuleModel)
        {
            float ruleTemp = alarmRuleModel.Alert1Temp;
            switch (alarmLevel)
            {
                case FireMonitorAlarmLevel.Alert1:
                    ruleTemp = alarmRuleModel.Alert1Temp;
                    break;
                case FireMonitorAlarmLevel.Alert2:
                    ruleTemp = alarmRuleModel.Alert2Temp;
                    break;
                case FireMonitorAlarmLevel.Alarm:
                    ruleTemp = alarmRuleModel.AlarmTemp;
                    break;
            }
            return ruleTemp;
        }

        public static FireAlarmMarkerBO FromMarker(OWBMarker marker)
        {
            FireAlarmMarkerBO alarmMarkerBO = new FireAlarmMarkerBO
            {
                MarkerName = marker.Name,
                MarkerType = (MarkerType)marker.MarkerType,
                MarkerDesc = marker.MarkerDesc,
                TempPoint = marker.Value,
            };

            alarmMarkerBO.PointList = new List<PointBO>();
            switch (marker.MarkerType)
            {
                case IRMarkerType.Spot:
                    alarmMarkerBO.PointList.Add(new PointBO
                    {
                        X = (marker as OWBSpotMarker).Point.X,
                        Y = (marker as OWBSpotMarker).Point.Y
                    });
                    break;
                case IRMarkerType.Polyline:
                    (marker as OWBPolylineMarker).Points.ToList()?.ForEach(point =>
                    {
                        alarmMarkerBO.PointList.Add(new PointBO
                        {
                            X = point.X,
                            Y = point.Y
                        });
                    });
                    break;
                case IRMarkerType.Polygon:
                    (marker as OWBPolygonMarker).Points.ToList()?.ForEach(point =>
                    {
                        alarmMarkerBO.PointList.Add(new PointBO
                        {
                            X = point.X,
                            Y = point.Y
                        });
                    });
                    break;
            }
            return alarmMarkerBO;
        }

        public void ToMarker(ref OWBMarker marker)
        {
            List<Point> pointList = new List<Point>();
            PointList.ForEach(a => pointList.Add(new Point(a.X, a.Y)));

            if(marker == null)
            {
                switch (MarkerType)
                {
                    case MarkerType.Spot:
                        marker = new OWBSpotMarker(MarkerName, pointList[0]);
                        break;
                    case MarkerType.Line:
                        marker = new OWBPolylineMarker(MarkerName, pointList.ToArray());
                        break;
                    case MarkerType.Region:
                        marker = new OWBPolygonMarker(MarkerName, pointList.ToArray());
                        break;
                }
            }
            else
            {
                switch (MarkerType)
                {
                    case MarkerType.Spot:
                        marker = new OWBSpotMarker(MarkerName, pointList[0], marker.IndependentEmissivity, marker.Emissivity, marker.ReflectedTemperature, marker.Distance, marker.OffsetTemp);
                        break;
                    case MarkerType.Line:
                        marker = new OWBPolylineMarker(MarkerName, pointList.ToArray(), marker.IndependentEmissivity, marker.Emissivity, marker.ReflectedTemperature, marker.Distance, marker.OffsetTemp);
                        break;
                    case MarkerType.Region:
                        marker = new OWBPolygonMarker(MarkerName, pointList.ToArray(), marker.IndependentEmissivity, marker.Emissivity, marker.ReflectedTemperature, marker.Distance, marker.OffsetTemp);
                        break;
                }
            }

            marker.MarkerDesc = MarkerDesc;
            marker.Value = TempPoint;
        }
    }

    public class FireMonitorPredictAlarmBO
    {
        public FireMonitorPredictAlarmBO()
        {
            LastAlarmLevel = FireMonitorAlarmLevel.None;
            CurrentAlarmLevel = FireMonitorAlarmLevel.None;
            OutputAlarmTemp = OWBMath.KelvinKelvinZero;
            Model = new Dictionary<FireMonitorAlarmLevel, double> { { FireMonitorAlarmLevel.Alert1, -1 }, { FireMonitorAlarmLevel.Alert2, -1 }, { FireMonitorAlarmLevel.Alarm, -1 } };
        }

        public FireMonitorAlarmLevel LastAlarmLevel { get; set; }

        public FireMonitorAlarmLevel CurrentAlarmLevel { get; set; }

        public float OutputAlarmTemp { get; set; }

        public Dictionary<FireMonitorAlarmLevel, double> Model { get; set; }
    }

    public class FireMonitorAlarmBO
    {
        public FireMonitorAlarmBO()
        { }

        public FireMonitorAlarmBO(long timestamp, string deviceID, string name, DateTime happenTime, DateTime arrivalTime, FireMonitorPresetBO presetBO, string protocolType, string rootPath)
        {
            Timestamp = timestamp;
            DeviceID = deviceID;
            DeviceName = name;
            AlarmTime = happenTime;
            ArrivalTime = arrivalTime;
            Preset = presetBO;
            ProtocolType = protocolType;
            RootPath = rootPath;
        }

        public FireMonitorAlarmBO(long timestamp, string deviceID, string name, DateTime happenTime, DateTime arrivalTime, FireMonitorPresetBO presetBO, string protocolType, string rootPath, List<FireAlarmMarkerBO> fireAlarmMarkerBOs) : this(timestamp, deviceID, name, happenTime, arrivalTime, presetBO, protocolType, rootPath)
        {
            MarkerList = fireAlarmMarkerBOs;
        }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("deviceID")]
        public string DeviceID { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("alarmTime")]
        public DateTime AlarmTime { get; set; }

        [JsonPropertyName("arrivalTime")]
        public DateTime ArrivalTime { get; set; }

        [JsonPropertyName("markerList")]
        public List<FireAlarmMarkerBO> MarkerList { get; set; }

        [JsonPropertyName("preset")]
        public FireMonitorPresetBO Preset { get; set; }

        [JsonPropertyName("protocolType")]
        public string ProtocolType { get; set; }

        [JsonPropertyName("rootPath")]
        public string RootPath { get; set; }
    }

    public class FireMonitorCruiseBO
    {
        public FireMonitorCruiseBO()
        { }

        public FireMonitorCruiseBO(int cruiseID, PauseType pauseType, string cruiseName, bool isRunning)
        {
            CruiseID = cruiseID;
            PauseType = pauseType;
            CruiseName = cruiseName;
            IsRunning = isRunning;
        }

        [JsonPropertyName("cruiseID")]
        public int CruiseID { get; set; }

        [JsonPropertyName("pauseType")]
        public PauseType PauseType { get; set; }

        [JsonPropertyName("cruiseName")]
        public string CruiseName { get; set; }

        [JsonPropertyName("isRunning")]
        public bool IsRunning { get; set; }
    }

    public class FireMonitorStatusChangedBO
    {
        public string DeviceID { get; set; }

        public List<DeviceInfoBO> DeviceInfoBOList { get; set; }

        public bool IsConnected { get; set; }

        public List<bool> AlarmInStatus { get; set; }
    }

    public class FireCruisePositionBO
    {
        public FireCruisePositionBO()
        { }

        public FireCruisePositionBO(int presetID, string presetName)
        {
            PresetID = presetID;
            PresetName = presetName;
        }

        [JsonPropertyName("presetID")]
        public int PresetID { get; set; }

        [JsonPropertyName("presetName")]
        public string PresetName { get; set; }
    }

    public class DeviceStreamEventBO
    {
        public string DeviceID { get; set; }

        public int ChannelID { get; set; }

        public string StreamType { get; set; }

        public object Data { get; set; }
    }
}
