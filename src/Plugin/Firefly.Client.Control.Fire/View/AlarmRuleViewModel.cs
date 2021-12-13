// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

using Amur.Common;
using Firefly.Client.Common;

namespace Firefly.Client.Control.Fire
{
    public class AlarmRuleViewModel : BaseViewModel
    {
        public AlarmRuleViewModel()
        {
            Items = new ObservableCollection<AlarmRuleItemViewModel>();
        }

        private AlarmRuleItemViewModel selectedItem;

        public ObservableCollection<AlarmRuleItemViewModel> Items { get; set; }

        public AlarmRuleItemViewModel SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        #region Methods

        public void Load(AlarmRuleDataObject alarmRuleDO)
        {
            Items.Clear();
            foreach (AlarmRule alarmRule in alarmRuleDO?.AlarmRuleList ?? new List<AlarmRule>())
            {
                AlarmRuleItemViewModel alarmRuleVM = new AlarmRuleItemViewModel();
                alarmRuleVM.Load(alarmRule);
                Items.Add(alarmRuleVM);
            }
        }

        public AlarmRuleDataObject GetAlarmRuleDataObject()
        {
            List<AlarmRule> alarmRuleList = new List<AlarmRule>();
            foreach (AlarmRuleItemViewModel alarmRuleItemVM in Items)
            {
                alarmRuleList.Add(alarmRuleItemVM.GetAlarmRuleItemDataObject());
            }

            return new AlarmRuleDataObject
            {
                AlarmRuleList = alarmRuleList,
            };
        }

        public string ConvertToJson()
        {
            return JsonConvert.SerializeObject(new FireAgentDataObject() { AlarmRuleConfig = GetAlarmRuleDataObject() }, Formatting.Indented);
        }

        #endregion
    }

    public class AlarmRuleItemViewModel : BaseViewModel
    {
        #region Fields
        private int id;
        private string name;
        private AlarmRuleType ruleType;
        private float alert1Temp;
        private float alert2Temp;
        private float alarmTemp;
        private int alert1FilteringTime;
        private int alert2FilteringTime;
        private int alarmFilteringTime;
        private float toleranceTemp;

        // 高温突变功能暂未实现，暂时注释，后面实现功能后再打开
        // private bool tempSuddenChangeEnabled;
        // private float tempSuddenChangeDiff;
        // private float tempSuddenChangeThreshold;
        // private int tempSuddenChangeInterval;
        // private int tempSuddenChangeSensitivity;
        // private int tempSuddenChangeClosingFilteringTime;
        #endregion

        #region Constructors
        public AlarmRuleItemViewModel()
        {
            // 高温突变功能暂未实现，暂时注释，后面实现功能后再打开
            // SensitivityRange = new ObservableCollection<string> { "1*1", "2*2", "3*3", "4*4", "5*5", "6*6", "7*7", "8*8", "9*9", "10*10" };
            // SensitivityIndex = 0;
            // TempSuddenChangeInterval = 1;
            // TempSuddenChangeDiff = 10;

            RuleTypeRange = new ObservableCollection<string> {I18n.Properties.Resources.HighestGreater, I18n.Properties.Resources.HighestLess, I18n.Properties.Resources.LowestGreater,
            I18n.Properties.Resources.LowestLess,I18n.Properties.Resources.AverageGreater,I18n.Properties.Resources.AverageLess,I18n.Properties.Resources.DiffTempGreater,I18n.Properties.Resources.DiffTempLess};

            AlarmRule defaultAlarmRule = new AlarmRule();
            Load(defaultAlarmRule);
        }
        #endregion

        #region Properties
        public int ID { get => id; set { id = value; OnPropertyChanged(nameof(ID)); } }

        [RegularExpression(Firefly.Client.Common.Constants.NAME_REG, ErrorMessageResourceName = "VELongName", ErrorMessageResourceType = typeof(I18n.Properties.Resources))]
        public string Name
        {
            get => name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    name = value;
                    OnPropertyChanged();
                }
                if (NameHelper.IsNameCompliance(value))
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
        }

        public int RuleType { get => (int)ruleType; set { ruleType = (AlarmRuleType)(value); OnPropertyChanged(nameof(RuleType)); OnPropertyChanged(nameof(RuleTypeIndex)); } }

        [JsonIgnore]
        public int RuleTypeIndex { get => RuleType - 1; set => RuleType = value + 1; }

        public float Alert1Temp { get => alert1Temp; set { alert1Temp = value; OnPropertyChanged(nameof(Alert1Temp)); } }

        public float Alert2Temp { get => alert2Temp; set { alert2Temp = value; OnPropertyChanged(nameof(Alert2Temp)); } }

        public float AlarmTemp { get => alarmTemp; set { alarmTemp = value; OnPropertyChanged(nameof(AlarmTemp)); } }

        public int Alert1FilteringTime { get => alert1FilteringTime; set { alert1FilteringTime = value; OnPropertyChanged(nameof(Alert1FilteringTime)); } }

        public int Alert2FilteringTime { get => alert2FilteringTime; set { alert2FilteringTime = value; OnPropertyChanged(nameof(Alert2FilteringTime)); } }

        public int AlarmFilteringTime { get => alarmFilteringTime; set { alarmFilteringTime = value; OnPropertyChanged(nameof(AlarmFilteringTime)); } }

        public float ToleranceTemp { get => toleranceTemp; set { toleranceTemp = value; OnPropertyChanged(nameof(ToleranceTemp)); } }

        // 高温突变功能暂未实现，暂时注释，后面实现功能后再打开
        // public bool TempSuddenChangeEnabled { get => tempSuddenChangeEnabled; set { tempSuddenChangeEnabled = value; OnPropertyChanged(nameof(TempSuddenChangeEnabled)); } }

        // [Range(typeof(float), "10", "100", ErrorMessageResourceName = "VEDiffTemperature", ErrorMessageResourceType = typeof(Resources))]
        // public float TempSuddenChangeDiff { get => tempSuddenChangeDiff; set { tempSuddenChangeDiff = value; OnPropertyChanged(nameof(TempSuddenChangeDiff)); Validate(value); } }

        // [Range(typeof(float), "-100", "1000", ErrorMessageResourceName = "VETemperature", ErrorMessageResourceType = typeof(Resources))]
        // public float TempSuddenChangeThreshold { get => tempSuddenChangeThreshold; set { tempSuddenChangeThreshold = value; OnPropertyChanged(nameof(TempSuddenChangeThreshold)); Validate(value); } }

        // [Range(1, 60, ErrorMessageResourceName = "VEIntervalTime", ErrorMessageResourceType = typeof(Resources))]
        // public int TempSuddenChangeInterval { get => tempSuddenChangeInterval; set { tempSuddenChangeInterval = value; OnPropertyChanged(nameof(TempSuddenChangeInterval)); Validate(value); } }

        // public int TempSuddenChangeSensitivity { get => tempSuddenChangeSensitivity; set { tempSuddenChangeSensitivity = value; OnPropertyChanged(nameof(TempSuddenChangeSensitivity)); OnPropertyChanged(nameof(SensitivityIndex)); } }

        // [JsonIgnore]
        // public int SensitivityIndex { get => TempSuddenChangeSensitivity - 1; set { TempSuddenChangeSensitivity = value + 1; } }

        // [Range(0, 200, ErrorMessageResourceName = "VEAlarmFilteringTime", ErrorMessageResourceType = typeof(Resources))]
        // public int TempSuddenChangeClosingFilteringTime { get => tempSuddenChangeClosingFilteringTime; set { tempSuddenChangeClosingFilteringTime = value; OnPropertyChanged(nameof(TempSuddenChangeClosingFilteringTime)); Validate(value); } }

        // [JsonIgnore]
        // public ObservableCollection<string> SensitivityRange { get; set; }

        [JsonIgnore]
        public ObservableCollection<string> RuleTypeRange { get; set; }
        #endregion

        #region Methods
        public void Load(AlarmRule alarmRuleDO)
        {
            ID = alarmRuleDO.ID;
            Name = alarmRuleDO.Name;
            RuleType = alarmRuleDO.RuleType;
            switch (ruleType)
            {
                case AlarmRuleType.HighestGreater:
                case AlarmRuleType.HighestLess:
                case AlarmRuleType.LowestGreater:
                case AlarmRuleType.LowestLess:
                case AlarmRuleType.AverageGreater:
                case AlarmRuleType.AverageLess:
                    Alert1Temp = new Temperature(alarmRuleDO.Alert1Temp, TemperatureUnit.Kelvin).CurrentValue;
                    Alert2Temp = new Temperature(alarmRuleDO.Alert2Temp, TemperatureUnit.Kelvin).CurrentValue;
                    AlarmTemp = new Temperature(alarmRuleDO.AlarmTemp, TemperatureUnit.Kelvin).CurrentValue;
                    break;
                case AlarmRuleType.DiffTempGreater:
                case AlarmRuleType.DiffTempLess:
                    Alert1Temp = new TemperatureDifference(alarmRuleDO.Alert1Temp, TemperatureUnit.Kelvin).CelsiurDifference;
                    Alert2Temp = new TemperatureDifference(alarmRuleDO.Alert2Temp, TemperatureUnit.Kelvin).CelsiurDifference;
                    AlarmTemp = new TemperatureDifference(alarmRuleDO.AlarmTemp, TemperatureUnit.Kelvin).CelsiurDifference;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Alert1FilteringTime = alarmRuleDO.Alert1FilteringTime;
            Alert2FilteringTime = alarmRuleDO.Alert2FilteringTime;
            AlarmFilteringTime = alarmRuleDO.AlarmFilteringTime;
            ToleranceTemp = new TemperatureDifference(alarmRuleDO.ToleranceTemp, TemperatureUnit.Kelvin).CelsiurDifference;
            // 高温突变功能暂未实现，暂时注释，后面实现功能后再打开
            // TempSuddenChangeEnabled = alarmRuleDO.TempSuddenChange.Enable;
            // TempSuddenChangeDiff = alarmRuleDO.TempSuddenChange.TempDiff;
            // TempSuddenChangeThreshold = (float)Math.Round(new Temperature(alarmRuleDO.TempSuddenChange.ThresholdTemp, TemperatureUnit.Kelvin).CurrentValue, 1);
            // TempSuddenChangeInterval = alarmRuleDO.TempSuddenChange.Interval;
            // TempSuddenChangeSensitivity = alarmRuleDO.TempSuddenChange.Sensitivity;
            // TempSuddenChangeClosingFilteringTime = alarmRuleDO.TempSuddenChange.CloseFilteringTime;
        }

        public AlarmRule GetAlarmRuleItemDataObject()
        {
            AlarmRule result = new AlarmRule
            {
                ID = ID,
                Name = Name,
                RuleType = RuleType,
                Alert1FilteringTime = Alert1FilteringTime,
                Alert2FilteringTime = Alert2FilteringTime,
                AlarmFilteringTime = AlarmFilteringTime,
                ToleranceTemp = new TemperatureDifference(ToleranceTemp, TemperatureUnit.Kelvin).CelsiurDifference,

                // 高温突变功能暂未实现，暂时注释，后面实现功能后再打开
                // TempSuddenChange = new TempSuddenChange
                // {
                // Enable = TempSuddenChangeEnabled,
                // TempDiff = TempSuddenChangeDiff,
                // ThresholdTemp = (float)Math.Round(new Temperature(TempSuddenChangeThreshold, TemperatureUnit.Celsiur).KelvinValue, 1),
                // Interval = TempSuddenChangeInterval,
                // Sensitivity = TempSuddenChangeSensitivity,
                // CloseFilteringTime = TempSuddenChangeClosingFilteringTime
                // }
            };
            switch (ruleType)
            {
                case AlarmRuleType.HighestGreater:
                case AlarmRuleType.HighestLess:
                case AlarmRuleType.LowestGreater:
                case AlarmRuleType.LowestLess:
                case AlarmRuleType.AverageGreater:
                case AlarmRuleType.AverageLess:
                    result.Alert1Temp = new Temperature(Alert1Temp, TemperatureUnit.Celsiur).KelvinValue;
                    result.Alert2Temp = new Temperature(Alert2Temp, TemperatureUnit.Celsiur).KelvinValue;
                    result.AlarmTemp = new Temperature(AlarmTemp, TemperatureUnit.Celsiur).KelvinValue;
                    break;
                case AlarmRuleType.DiffTempGreater:
                case AlarmRuleType.DiffTempLess:
                    result.Alert1Temp = new TemperatureDifference(Alert1Temp, TemperatureUnit.Kelvin).CelsiurDifference;
                    result.Alert2Temp = new TemperatureDifference(Alert2Temp, TemperatureUnit.Kelvin).CelsiurDifference;
                    result.AlarmTemp = new TemperatureDifference(AlarmTemp, TemperatureUnit.Kelvin).CelsiurDifference; ;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return result;
        }

        public AlarmRuleItemViewModel Clone()
        {
            return JsonConvert.DeserializeObject<AlarmRuleItemViewModel>(JsonConvert.SerializeObject(this));
        }

        private int GetDecimalPlaces(float number)
        {
            string[] temp = number.ToString().Split('.');
            if (temp.Length == 2)
            {
                return temp[1].Length;
            }

            return 0;
        }

        #endregion
    }

    public enum AlarmRuleType
    {
        HighestGreater = 1,
        HighestLess = 2,
        LowestGreater = 3,
        LowestLess = 4,
        AverageGreater = 5,
        AverageLess = 6,
        DiffTempGreater = 7,
        DiffTempLess = 8
    }
}
