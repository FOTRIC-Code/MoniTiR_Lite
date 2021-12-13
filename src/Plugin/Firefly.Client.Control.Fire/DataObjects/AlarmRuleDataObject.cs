// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using Newtonsoft.Json;

using Amur.Common;

namespace Firefly.Client.Control.Fire
{
    public class AlarmRuleDataObject
    {
        public AlarmRuleDataObject()
        {
            AlarmRuleList = new List<AlarmRule>();
        }

        [JsonProperty("alarmRuleList")]
        public List<AlarmRule> AlarmRuleList { get; set; }
    }

    public class AlarmRule
    {
        public AlarmRule()
        {
            ID = 0;
            Name = string.Empty;
            RuleType = (int)AlarmRuleType.HighestGreater;
            Alert1Temp = new Temperature(5f, TemperatureUnit.Celsiur).KelvinValue;
            Alert1FilteringTime = 3;
            Alert2Temp = new Temperature(10f, TemperatureUnit.Celsiur).KelvinValue;
            Alert2FilteringTime = 3;
            AlarmTemp = new Temperature(15f, TemperatureUnit.Celsiur).KelvinValue;
            AlarmFilteringTime = 3;
            ToleranceTemp = new TemperatureDifference(2f, TemperatureUnit.Celsiur).KelvinDifference;
            TempSuddenChange = new TempSuddenChange();
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ruleType")]
        public int RuleType { get; set; }

        [JsonProperty("alert1Temp")]
        public float Alert1Temp { get; set; }

        [JsonProperty("alert1FilteringTime")]
        public int Alert1FilteringTime { get; set; }

        [JsonProperty("alert2Temp")]
        public float Alert2Temp { get; set; }

        [JsonProperty("alert2FilteringTime")]
        public int Alert2FilteringTime { get; set; }

        [JsonProperty("alarmTemp")]
        public float AlarmTemp { get; set; }

        [JsonProperty("alarmFilteringTime")]
        public int AlarmFilteringTime { get; set; }

        [JsonProperty("toleranceTemp")]
        public float ToleranceTemp { get; set; }

        [JsonIgnore]
        [JsonProperty("tempSuddenChange")]
        public TempSuddenChange TempSuddenChange { get; set; }
    }

    public class TempSuddenChange
    {
        public TempSuddenChange()
        {
            Enable = false;
            TempDiff = 0f;
            Interval = 0;
            Sensitivity = 0;
            CloseFilteringTime = 0;
        }

        [JsonProperty("enabled")]
        public bool Enable { get; set; }

        [JsonProperty("tempDiff")]
        public float TempDiff { get; set; }

        [JsonProperty("thresholdTemp")]
        public float ThresholdTemp { get; set; }

        [JsonProperty("interval")]
        public int Interval { get; set; }

        [JsonProperty("sensitivity")]
        public int Sensitivity { get; set; }

        [JsonProperty("closeFilteringTime")]
        public int CloseFilteringTime { get; set; }
    }
}