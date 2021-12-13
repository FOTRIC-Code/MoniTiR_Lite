// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Newtonsoft.Json;

using Amur.Common;
using Firefly.Client.Common;
using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public class PresetViewModel : BaseViewModel
    {
        private ObservableCollection<PresetItemViewModel> _items;
        private PresetItemViewModel _presetItemVM;
        private DeviceDO _deviceDO;
        private string _irRTSPUrl;
        private string _dcRTSPUrl;
        private Size _deviceArea;

        public PresetViewModel()
        {
            _items = new ObservableCollection<PresetItemViewModel>();
        }

        public ObservableCollection<PresetItemViewModel> Items { get => _items; set { _items = value; OnPropertyChanged(nameof(Items)); } }

        public PresetItemViewModel SelectedItem { get => _presetItemVM; set { _presetItemVM = value; OnPropertyChanged(nameof(SelectedItem)); } }

        public bool HasVisualVideo
        {
            get
            {
                return _deviceDO?.DeviceAddress?.Any(item =>
                    item.ProtocolType == DCCameraType.HIKSDK_NET.ToString()
                    || item.ProtocolType == DCCameraType.DHSDK_NET.ToString()
                    || item.ProtocolType == DCCameraType.XMSDK_NET.ToString()
                    || item.ProtocolType == DCCameraType.TWSDK_NET.ToString()
                    || item.ProtocolType == DCCameraType.ONVIF_DC_NET.ToString()
                    ) == true;
            }
        }

        public string IRRTSPUrl
        {
            get => _irRTSPUrl;
            set
            {
                _irRTSPUrl = value;
                OnPropertyChanged();
            }
        }

        public string DCRTSPUrl
        {
            get => _dcRTSPUrl;
            set
            {
                _dcRTSPUrl = value;
                OnPropertyChanged();
            }
        }

        public Size DeviceArea
        {
            get => _deviceArea;
            set { _deviceArea = value; OnPropertyChanged(); }
        }

        public void Load(DevicePresetDataObject presetListDO, DeviceParamDataObject deviceParamDO)
        {
            ObservableCollection<PresetItemViewModel> tempItems = new ObservableCollection<PresetItemViewModel>();
            for (int i = 1; i < 256; i++)
            {
                tempItems.Add(new PresetItemViewModel { ID = i });
            }

            foreach (PresetDataObject preset in presetListDO?.PresetList ?? new List<PresetDataObject>())
            {
                PresetItemViewModel curVM = tempItems.FirstOrDefault(x => x.ID == preset.ID);
                if (curVM == null)
                {
                    continue;
                }

                curVM.Load(preset);
            }

            Items = tempItems;
            DeviceModelDataObject irModel = deviceParamDO?.GetIRDeviceModelDO();
            DeviceArea = new Size(irModel?.Width ?? 0, irModel?.Height ?? 0);
        }

        public void LoadPresetItem(int presetID, PresetDataObject presetDO)
        {
            PresetItemViewModel presetItemVM = _items.FirstOrDefault(item => item.ID == presetID);
            if (presetDO == null)
            {
                presetItemVM.Clear();
            }
            else
            {
                presetItemVM.Load(presetDO);
            }
        }

        public void SetRTSPUrl(DeviceStreamDO streamDO)
        {
            if (streamDO?.IRDeviceSteamItem != null)
            {
                IRRTSPUrl = streamDO.IRDeviceSteamItem.RTSPUrl;
            }

            if (streamDO?.DCDeviceSteamItem != null)
            {
                DCRTSPUrl = streamDO.DCDeviceSteamItem.RTSPUrl;
            }
        }

        public void CleanRTSPUrl()
        {
            DCRTSPUrl = string.Empty;
            IRRTSPUrl = string.Empty;
        }

        public void UpdateDevice(DeviceDO deviceDO)
        {
            _deviceDO = deviceDO;
            OnPropertyChanged(nameof(HasVisualVideo));
        }

        internal DevicePresetDataObject ConvertToDataObject()
        {
            List<PresetDataObject> presetList = new List<PresetDataObject>();
            foreach (PresetItemViewModel presetItemVM in Items)
            {
                if (!presetItemVM.IsUsed)//skip empty preset
                {
                    continue;
                }
                PresetDataObject presetDO = presetItemVM.ToPresetItemDO();
                presetList.Add(presetDO);
            }
            return new DevicePresetDataObject
            {
                PresetList = presetList,
            };
        }

        public string ConvertToJson()
        {
            return JsonConvert.SerializeObject(new FireAgentDataObject() { PresetConfig = ConvertToDataObject() }, Formatting.Indented);
        }
    }

    public class PresetItemViewModel : BaseViewModel
    {
        #region Fields

        private string _name;
        private int _sensitivityIndex;
        private List<Marker> _markers;
        private List<ShieldMask> _shieldMasks;

        #endregion

        #region Constructors

        public PresetItemViewModel()
        {
            Markers = new List<Marker>();
            ShieldMasks = new List<ShieldMask>();

            SensitivityRange = new List<string> { "1*1", "2*2", "3*3", "4*4", "5*5", "6*6", "7*7", "8*8", "9*9", "10*10" };
            SensitivityIndex = 0;
        }

        #endregion

        #region Properties

        public int ID { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(IsUsed));
                }
                if (NameHelper.IsNameCompliance(value))
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(IsUsed));
                }
            }
        }

        public List<string> SensitivityRange { get; set; }

        public int SensitivityIndex
        {
            get => _sensitivityIndex;
            set
            {
                _sensitivityIndex = value;
                OnPropertyChanged(nameof(SensitivityIndex));
                OnPropertyChanged(nameof(IsUsed));
            }
        }

        public List<Marker> Markers
        {
            get => _markers;
            set
            {
                _markers = value;
                OnMarkerChanged();
            }
        }

        public List<ShieldMask> ShieldMasks
        {
            get => _shieldMasks;
            set
            {
                _shieldMasks = value;
                OnMarkerChanged();
            }
        }

        public List<Marker> BindingGridMarkerList
        {
            get
            {
                List<Marker> markers = new List<Marker>();
                foreach (Marker marker in Markers)
                {
                    markers.Add(marker);
                }
                return markers;
            }
        }

        public List<ShieldMask> BindingGridShieldMaskList
        {
            get
            {
                List<ShieldMask> masks = new List<ShieldMask>();
                foreach (ShieldMask marker in ShieldMasks)
                {
                    masks.Add(marker);
                }
                return masks;
            }
        }

        public List<DeviceMarker> BindingCanvasMarkerList
        {
            get
            {
                List<DeviceMarker> markers = new List<DeviceMarker>();
                foreach (Marker marker in Markers)
                {
                    markers.Add(marker.DeviceMarker);
                }
                return markers;
            }
        }

        public List<DeviceMarker> BindingCanvasShieldMaskList
        {
            get
            {
                List<DeviceMarker> masks = new List<DeviceMarker>();
                foreach (ShieldMask mask in ShieldMasks)
                {
                    masks.Add(mask.DeviceMarker);
                }
                return masks;
            }
        }


        public bool IsUsed => !string.IsNullOrWhiteSpace(Name) || BindingCanvasMarkerList.Any() || BindingCanvasShieldMaskList.Any() || SensitivityIndex != 0; //check empty preset

        #endregion

        #region Methods

        public void Load(PresetDataObject presetDO)
        {
            ID = presetDO.ID;
            Name = presetDO.Name;
            SensitivityIndex = presetDO.Sensitivity - 1;
            List<ShieldMask> shieldMasks = new List<ShieldMask>();
            List<Marker> markers = new List<Marker>();

            foreach (ShieldMaskDataObject shieldMaskDO in presetDO.ShieldMaskList ?? new List<ShieldMaskDataObject>())
            {
                ShieldMask shieldMarker = new ShieldMask(shieldMaskDO);
                shieldMasks.Add(shieldMarker);
            }

            foreach (MarkerDataObject markerDO in presetDO.MarkerList ?? new List<MarkerDataObject>())
            {
                Marker marker = new Marker(markerDO);
                markers.Add(marker);
            }

            ShieldMasks = shieldMasks;
            Markers = markers;
        }

        public PresetDataObject ToPresetItemDO()
        {
            List<ShieldMaskDataObject> shieldMaskDOs = new List<ShieldMaskDataObject>();
            foreach (ShieldMask shieldMarker in ShieldMasks)
            {
                shieldMaskDOs.Add(shieldMarker.ToShieldMaskDO());
            }

            List<MarkerDataObject> markerList = new List<MarkerDataObject>();
            foreach (Marker marker in Markers)
            {
                markerList.Add(marker.ToMarkerDO());
            }

            return new PresetDataObject
            {
                ID = ID,
                Name = Name,
                Sensitivity = SensitivityIndex + 1,
                ShieldMaskList = shieldMaskDOs,
                MarkerList = markerList
            };
        }

        public void UpdateDeviceMarker(DeviceMarker deviceMarker)
        {
            switch (deviceMarker.CanvasType)
            {
                case CanvasType.Marker:
                    if (Markers != null)
                    {
                        for (int i = 0; i < Markers.Count; i++)
                        {
                            if (Markers[i].Name == deviceMarker.Name)
                            {
                                Markers[i].DeviceMarker = deviceMarker;
                            }
                        }
                    }
                    break;
                case CanvasType.Shield:
                    if (ShieldMasks != null)
                    {
                        for (int i = 0; i < ShieldMasks.Count; i++)
                        {
                            if (ShieldMasks[i].Name == deviceMarker.Name)
                            {
                                ShieldMasks[i].DeviceMarker = deviceMarker;
                            }
                        }
                    }
                    break;
            }
        }

        public void OnMarkerChanged()
        {
            OnPropertyChanged(nameof(BindingGridMarkerList));
            OnPropertyChanged(nameof(BindingGridShieldMaskList));
            UpdatePresetUsed();

            RefreshCanvasMarker();
        }

        public void RefreshCanvasMarker()
        {
            OnPropertyChanged(nameof(BindingCanvasMarkerList));
            OnPropertyChanged(nameof(BindingCanvasShieldMaskList));
        }

        public void RemoveMarker(Marker marker)
        {
            if (Markers != null && Markers.Contains(marker))
            {
                Markers.Remove(marker);
            }
            OnPropertyChanged(nameof(BindingCanvasMarkerList));
        }

        public void RemoveMask(ShieldMask mask)
        {
            if (ShieldMasks != null && ShieldMasks.Contains(mask))
            {
                ShieldMasks.Remove(mask);
            }
            OnPropertyChanged(nameof(BindingCanvasShieldMaskList));
        }

        public void Clear()
        {
            Markers.Clear();
            ShieldMasks.Clear();
            OnMarkerChanged();

            Name = string.Empty;
            SensitivityIndex = 0;
        }

        internal void UpdatePresetUsed()
        {
            OnPropertyChanged(nameof(IsUsed));
        }

        #endregion
    }

    public class Marker : BaseViewModel
    {
        #region Fields
        private string _name;
        private int _alarmRuleID;
        private float _emissivity;
        private float _distance;
        private float _relTemp;
        private float _offsetTemp;
        private AlarmRuleItemViewModel _alarmRule;

        public const int MAX_SHIELD_COUNT = 10;
        #endregion

        #region Constructors

        public Marker(DeviceMarker deviceMarker)
        {
            _emissivity = 0.98f;
            _distance = 1f;
            _relTemp = 20f;

            Name = deviceMarker.Name;
            DeviceMarker = deviceMarker;
        }

        public Marker(Marker marker)
        {
            if (marker == null)
            {
                return;
            }
            Name = marker.Name;
            marker.DeviceMarker.Name = marker.Name;

            Emissivity = marker.Emissivity;
            Distance = marker.Distance;
            RelTemp = marker.RelTemp;
            OffsetTemp = marker.OffsetTemp;

            DeviceMarker = marker.DeviceMarker;
        }

        public Marker(MarkerDataObject markerDO)
        {
            Name = markerDO.Name;
            AlarmRuleID = markerDO.AlarmRuleID;
            Emissivity = markerDO.Emissivity;
            Distance = markerDO.Distance;
            RelTemp = new Temperature(markerDO.ReflTemp, TemperatureUnit.Kelvin).CurrentValue;
            OffsetTemp = markerDO.OffsetTemp;

            List<Point> pointList = new List<Point>();
            if (markerDO.PointList != null)
            {
                markerDO.PointList.ForEach(item => pointList.Add(new Point(item.X, item.Y)));
            }
            DeviceMarker = new DeviceMarker(markerDO.MarkerType, CanvasType.Marker, pointList, markerDO.Name);

            AlarmRuleViewModel alarmRuleViewModel = new AlarmRuleViewModel();
            alarmRuleViewModel.Load(FireAgentDeviceManager.Instance.LoadAlarmRuleDataObject());
            foreach (AlarmRuleItemViewModel item in alarmRuleViewModel.Items)
            {
                if (item.ID == markerDO.AlarmRuleID)
                {
                    AlarmRule = item;
                    break;
                }
            }
        }
        #endregion

        #region Properties

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }

        public int AlarmRuleID { get => _alarmRuleID; set { _alarmRuleID = value; OnPropertyChanged(nameof(AlarmRuleID)); } }

        public float Emissivity { get => _emissivity; set { _emissivity = value; OnPropertyChanged(nameof(Emissivity)); } }

        public float Distance { get => _distance; set { _distance = value; OnPropertyChanged(nameof(Distance)); } }

        public float RelTemp { get => _relTemp; set { _relTemp = value; OnPropertyChanged(nameof(RelTemp)); } }

        public float OffsetTemp { get => _offsetTemp; set { _offsetTemp = value; OnPropertyChanged(nameof(OffsetTemp)); } }

        public MarkerType MarkerType => DeviceMarker.MarkerType;

        [JsonIgnore]
        public AlarmRuleItemViewModel AlarmRule
        {
            get => _alarmRule;
            set
            {
                _alarmRule = value;
                OnPropertyChanged(nameof(AlarmRule));
                OnPropertyChanged(nameof(AlarmRuleName));
            }
        }

        [JsonIgnore]
        public string AlarmRuleName { get => AlarmRule?.Name ?? I18n.Properties.Resources.Choose; }

        [JsonIgnore]
        public DeviceMarker DeviceMarker { get; set; }

        [JsonIgnore]
        public string Title { get; set; }

        [JsonIgnore]
        public string MarkTypeText
        {
            get
            {
                if (DeviceMarker.MarkerType == MarkerType.Spot)
                {
                    return I18n.Properties.Resources.Spot;
                }
                else if (DeviceMarker.MarkerType == MarkerType.Line)
                {
                    return I18n.Properties.Resources.Line;
                }
                else if (DeviceMarker.MarkerType == MarkerType.Region)
                {
                    return I18n.Properties.Resources.Region;
                }
                else
                {
                    return "";
                }
            }
        }
        #endregion

        #region Methods
        public void RefreshAlarmRule()
        {
            AlarmRuleDataObject alarmRuleDO = FireAgentDeviceManager.Instance.LoadAlarmRuleDataObject();
            AlarmRule alarmRule = alarmRuleDO.AlarmRuleList.FirstOrDefault(item => item.ID == AlarmRuleID);

            if (alarmRule == null)
            {
                AlarmRule = null;
            }
            else
            {
                AlarmRuleItemViewModel alarmRuleVM = new AlarmRuleItemViewModel();
                alarmRuleVM.Load(alarmRule);
                AlarmRule = alarmRuleVM;
            }
        }

        public MarkerDataObject ToMarkerDO()
        {
            MarkerDataObject markerDO = new MarkerDataObject
            {
                Name = Name,
                MarkerType = DeviceMarker.MarkerType,
                Emissivity = Emissivity,
                Distance = Distance,
                ReflTemp = new Temperature(RelTemp, TemperatureUnit.Celsiur).KelvinValue,
                OffsetTemp = OffsetTemp,
                AlarmRuleID = AlarmRuleID
            };

            List<MarkerPointDataObject> points = new List<MarkerPointDataObject>();
            foreach (Point point in DeviceMarker.PointList)
            {
                points.Add(new MarkerPointDataObject { X = (int)point.X, Y = (int)point.Y });
            }

            markerDO.PointList = points;

            return markerDO;
        }

        public void CopyDataObject(Marker marker)
        {
            if (marker == null)
            {
                return;
            }
            Name = marker.Name;
            marker.DeviceMarker.Name = marker.Name;
            Emissivity = marker.Emissivity;
            Distance = marker.Distance;
            RelTemp = marker.RelTemp;
            OffsetTemp = marker.OffsetTemp;

            DeviceMarker = marker.DeviceMarker;
        }
        #endregion
    }

    public class ShieldMask : BaseViewModel
    {
        #region Fields
        private string _name;

        public const int MAX_SHIELD_COUNT = 10;
        #endregion

        #region Constructors
        public ShieldMask(DeviceMarker deviceMarker)
        {
            Name = deviceMarker.Name;

            DeviceMarker = deviceMarker;
        }

        public ShieldMask(ShieldMask mask)
        {
            if (mask == null)
            {
                return;
            }
            Name = mask.Name;
            mask.DeviceMarker.Name = mask.Name;

            DeviceMarker = mask.DeviceMarker;
        }

        public ShieldMask(ShieldMaskDataObject shieldMaskDO)
        {
            Name = shieldMaskDO.Name;
            List<Point> pointList = new List<Point>();
            shieldMaskDO.PointList.ForEach(item => pointList.Add(new Point(item.X, item.Y)));
            DeviceMarker deviceMarker = new DeviceMarker(MarkerType.Region, CanvasType.Shield, pointList, shieldMaskDO.Name);
            DeviceMarker = deviceMarker;
        }

        public void CopyDataObject(ShieldMask mask)
        {
            if (mask == null)
            {
                return;
            }
            Name = mask.Name;
            mask.DeviceMarker.Name = mask.Name;

            DeviceMarker = mask.DeviceMarker;
        }
        #endregion

        #region Properties

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }

        public MarkerType MarkerType => DeviceMarker.MarkerType;

        [JsonIgnore]
        public DeviceMarker DeviceMarker { get; set; }

        [JsonIgnore]
        public string Title { get; set; }

        #endregion

        #region Methods
        public ShieldMaskDataObject ToShieldMaskDO()
        {
            ShieldMaskDataObject shieldMaskDO = new ShieldMaskDataObject
            {
                Name = Name
            };

            List<MarkerPointDataObject> points = new List<MarkerPointDataObject>();
            foreach (Point point in DeviceMarker.PointList)
            {
                points.Add(new MarkerPointDataObject { X = (int)point.X, Y = (int)point.Y });
            }

            shieldMaskDO.PointList = points;

            return shieldMaskDO;
        }
        #endregion
    }
}
