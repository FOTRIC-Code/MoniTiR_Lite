// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Collections.ObjectModel;

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public class CruiseRouteViewModel : BaseViewModel
    {
        #region Fields

        private ObservableCollection<CruiseRouteItemViewModel> items;
        private CruiseRouteItemViewModel selectedItem;
        private string _irRTSPUrl;
        private string _dcRTSPUrl;

        #endregion

        #region Constructors

        public CruiseRouteViewModel()
        {
            items = new ObservableCollection<CruiseRouteItemViewModel>();
        }

        #endregion

        #region Properties

        public ObservableCollection<CruiseRouteItemViewModel> Items
        {
            get => items;
            set
            {
                items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        public CruiseRouteItemViewModel SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
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

        #endregion

        #region Methods

        public void Load(CruiseRouteDataObject dataObject)
        {
            items.Clear();
            foreach (CruiseRoute routeDO in dataObject.CruiseRouteList)
            {
                ObservableCollection<CruisePresetViewModel> cruisePointList = new ObservableCollection<CruisePresetViewModel>();

                foreach (var cruisePoint in routeDO.CruisePointList)
                {
                    cruisePointList.Add(new CruisePresetViewModel()
                    {
                        ID = cruisePoint.PresetID,
                        Name = I18n.Properties.Resources.Preset + cruisePoint.PresetID,
                        Speed = cruisePoint.Speed,
                        WaitTime = cruisePoint.WaitTime,
                        AutoFocus = cruisePoint.AutoFocus,
                    });
                }

                items.Add(new CruiseRouteItemViewModel()
                {
                    ID = routeDO.ID,
                    Name = routeDO.Name,
                    Presets = cruisePointList
                });
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

        internal CruiseRouteDataObject ConvertToDataObject()
        {
            List<CruiseRoute> cruiseRouteList = new List<CruiseRoute>();
            foreach (CruiseRouteItemViewModel cruisePresetItemVM in Items)
            {
                List<CruisePoint> cruisePointList = new List<CruisePoint>();
                foreach (CruisePresetViewModel cruisePresetVM in cruisePresetItemVM.Presets)
                {
                    cruisePointList.Add(new CruisePoint
                    {
                        PresetID = cruisePresetVM.ID,
                        Speed = cruisePresetVM.Speed,
                        WaitTime = cruisePresetVM.WaitTime,
                        AutoFocus = cruisePresetVM.AutoFocus,
                    });
                }

                cruiseRouteList.Add(new CruiseRoute
                {
                    ID = cruisePresetItemVM.ID,
                    Name = cruisePresetItemVM.Name,
                    CruisePointList = cruisePointList
                });
            }

            return new CruiseRouteDataObject
            {
                CruiseRouteList = cruiseRouteList
            };
        }

        public string ConvertToJson()
        {
            return JsonConvert.SerializeObject(new FireAgentDataObject() { CruiseRouteConfig = ConvertToDataObject() }, Formatting.Indented);
        }

        #endregion
    }

    public class CruiseRouteItemViewModel : BaseViewModel
    {
        #region Fields

        private int id;
        private string name;
        private string alarmDescribe;
        private ObservableCollection<CruisePresetViewModel> presets;

        #endregion

        #region Constructors

        public CruiseRouteItemViewModel()
        {
            presets = new ObservableCollection<CruisePresetViewModel>();
        }

        #endregion

        #region Properties

        public string Title => ID > 0 ? I18n.Properties.Resources.Edit : I18n.Properties.Resources.Add;

        public int ID
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string AlarmDescribe
        {
            get => alarmDescribe;
            set
            {
                alarmDescribe = value;
                OnPropertyChanged(nameof(AlarmDescribe));
            }
        }

        public ObservableCollection<CruisePresetViewModel> Presets
        {
            get => presets;
            set
            {
                presets = value;
                OnPropertyChanged(nameof(Presets));
            }
        }

        #endregion

        #region Methods

        public CruiseRouteItemViewModel Clone()
        {
            return JsonConvert.DeserializeObject<CruiseRouteItemViewModel>(JsonConvert.SerializeObject(this));
        }

        public void CopyFrom(CruiseRouteItemViewModel otherViewModel)
        {
            if (otherViewModel == null)
            {
                return;
            }

            this.ID = otherViewModel.id;
            this.Name = otherViewModel.name;
            this.AlarmDescribe = otherViewModel.alarmDescribe;
            this.Presets = otherViewModel.presets;
            OnPropertyChanged(nameof(Title));
        }

        #endregion
    }

    public class CruisePresetViewModel : BaseViewModel
    {
        #region Fields

        private int id;
        private string name;
        private int waitTime;
        private int speed;
        private bool canUp;
        private bool canDown;
        private bool _autoFocus;

        #endregion

        #region Constructors

        public CruisePresetViewModel()
        {
            waitTime = 60;
            speed = 6;
        }

        #endregion

        #region Properties

        public string Title => string.IsNullOrWhiteSpace(Name) ? I18n.Properties.Resources.Add : I18n.Properties.Resources.Edit;

        public int ID
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Name));
            }
        }

        public int WaitTime
        {
            get => waitTime;
            set
            {
                waitTime = value;
                OnPropertyChanged(nameof(WaitTime));
            }
        }

        public int Speed
        {
            get => speed;
            set
            {
                speed = value;
                OnPropertyChanged(nameof(Speed));
            }
        }

        public bool CanUp
        {
            get => canUp;
            set
            {
                canUp = value;
                OnPropertyChanged(nameof(CanUp));
            }
        }

        public bool CanDown
        {
            get => canDown;
            set
            {
                canDown = value;
                OnPropertyChanged(nameof(CanDown));
            }
        }

        public bool AutoFocus
        {
            get => _autoFocus;
            set
            {
                _autoFocus = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public CruisePresetViewModel Clone()
        {
            return new CruisePresetViewModel
            {
                ID = ID,
                Name = Name,
                WaitTime = WaitTime,
                Speed = Speed,
                AutoFocus = AutoFocus,
            };
        }

        internal void Copy(CruisePresetViewModel cruisePresetVM)
        {
            if (cruisePresetVM == null)
            {
                return;
            }

            ID = cruisePresetVM.ID;
            Name = cruisePresetVM.name;
            WaitTime = cruisePresetVM.waitTime;
            Speed = cruisePresetVM.speed;
            AutoFocus = cruisePresetVM.AutoFocus;
        }

        #endregion
    }
}
