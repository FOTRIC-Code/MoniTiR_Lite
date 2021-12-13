// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public class OSDConfigViewModel : BaseViewModel
    {
        #region fields

        private const int DEFAULT_ID = 0;
        private string _rtspUrl;
        private OSDStatus _markerOSDStatus;
        private bool _enable;
        private bool _enableOSD;
        private bool _showMax;
        private bool _showMin;
        private bool _showAvg;
        private OSDItem _timeOSD;
        private OSDItem _channelOSD;
        private OSDItem _ptzOSD;

        #endregion

        #region property

        public bool Enable
        {
            get => _enable;
            set
            {
                _enable = value;
                OnPropertyChanged();
            }
        }

        public OSDItem TimeOSD
        {
            get
            {
                if (_timeOSD != null)
                {
                    return new OSDItem(_timeOSD.ID, _timeOSD.StandardArea, _timeOSD.Name, _timeOSD.Positon, _timeOSD.OSDType, _timeOSD.OSDStatus);
                }
                return null;
            }
        }

        public OSDItem ChannelOSD
        {
            get
            {
                if (_channelOSD != null)
                {
                    return new OSDItem(_channelOSD.ID, _channelOSD.StandardArea, _channelOSD.Name, _channelOSD.Positon, _channelOSD.OSDType, _channelOSD.OSDStatus);
                }
                return null;
            }
        }

        public OSDItem PTZOSD
        {
            get
            {
                if (_ptzOSD != null)
                {
                    return new OSDItem(_ptzOSD.ID, _ptzOSD.StandardArea, _ptzOSD.Name, _ptzOSD.Positon, _ptzOSD.OSDType, _ptzOSD.OSDStatus);
                }
                return null;
            }
        }

        public string Name
        {
            get => _channelOSD?.Name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = I18n.Properties.Resources.ChannelZero;
                }
                _channelOSD.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChannelOSD));
            }
        }

        public string RTSPUrl
        {
            get => _rtspUrl;
            set
            {
                _rtspUrl = value;
                OnPropertyChanged();
            }
        }

        public bool EnableOSD
        {
            get => _enableOSD;
            set
            {
                _enableOSD = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EnableChannelName));
                OnPropertyChanged(nameof(EnableShowMax));
                OnPropertyChanged(nameof(EnableShowMin));
                OnPropertyChanged(nameof(EnableShowAvg));
            }
        }

        public bool ShowChannel
        {
            get => _channelOSD?.OSDStatus == OSDStatus.Enable;
            set
            {
                if (value)
                {
                    _channelOSD.OSDStatus = OSDStatus.Enable;
                }
                else
                {
                    _channelOSD.OSDStatus = OSDStatus.Disable;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChannelOSD));
                OnPropertyChanged(nameof(EnableChannelName));
            }
        }

        public bool ShowTime
        {
            get => _timeOSD?.OSDStatus == OSDStatus.Enable;
            set
            {
                if (value)
                {
                    _timeOSD.OSDStatus = OSDStatus.Enable;
                }
                else
                {
                    _timeOSD.OSDStatus = OSDStatus.Disable;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeOSD));
            }
        }

        public bool ShowPTZ
        {
            get => _ptzOSD?.OSDStatus == OSDStatus.Enable;
            set
            {
                if (value)
                {
                    _ptzOSD.OSDStatus = OSDStatus.Enable;
                }
                else
                {
                    _ptzOSD.OSDStatus = OSDStatus.Disable;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(PTZOSD));
            }
        }

        public bool ShowTemp
        {
            get => _markerOSDStatus == OSDStatus.Enable;
            set
            {
                if (value)
                {
                    _markerOSDStatus = OSDStatus.Enable;
                }
                else
                {
                    _markerOSDStatus = OSDStatus.Disable;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(EnableShowMax));
                OnPropertyChanged(nameof(EnableShowMin));
                OnPropertyChanged(nameof(EnableShowAvg));
            }
        }

        public bool ShowMax
        {
            get => _showMax;
            set
            {
                _showMax = value;
                OnPropertyChanged();
            }
        }

        public bool ShowMin
        {
            get => _showMin;
            set
            {
                _showMin = value;
                OnPropertyChanged();
            }
        }

        public bool ShowAvg
        {
            get => _showAvg;
            set
            {
                _showAvg = value;
                OnPropertyChanged();
            }
        }

        public bool EnableChannelName => _enableOSD && ShowChannel;

        public bool EnableShowMax => _enableOSD && ShowTemp;

        public bool EnableShowMin => _enableOSD && ShowTemp;

        public bool EnableShowAvg => _enableOSD && ShowTemp;

        #endregion

        #region method

        internal void SetRTSPUrl(DeviceStreamDO streamDO)
        {
            if (streamDO?.IRDeviceSteamItem != null)
            {
                RTSPUrl = streamDO.IRDeviceSteamItem.RTSPUrl;
            }
        }

        internal void CleanRTSPUrl()
        {
            RTSPUrl = string.Empty;
        }

        internal void RefreshOSD(OSDItem channelOSD, OSDItem timeOSD, OSDItem ptzOSD)
        {
            _channelOSD = channelOSD;
            _timeOSD = timeOSD;
            _ptzOSD = ptzOSD;
        }

        internal void Load(string protocolType, bool osdConfigDO, DeviceModelDataObject deviceModelDO, ChannelConfigDataObject channelConfigDO, PTZOSDConfigDataObject ptzOSDConfigDO, MarkerOSDConfigDataObject markerOSDConfigDO)
        {
            Rect thermalArea = new Rect(new Size(deviceModelDO.Width, deviceModelDO.Height));

            ChannelDataObject channelDataObject = channelConfigDO?.ChannelDataObjectList?.FirstOrDefault();
            if (channelDataObject == null)
            {
                channelDataObject = new ChannelDataObject();
            }
            if (ptzOSDConfigDO.Positon == null)
            {
                ptzOSDConfigDO.Positon = new OSDPoint();
            }
            TimeDataObject timeDataObject = channelDataObject.TimeDataObject;

            _channelOSD = new OSDItem(channelDataObject.ID, thermalArea, channelDataObject.Name, new Point(channelDataObject.Positon.X, channelDataObject.Positon.Y), OSDType.ChannelInfo, (OSDStatus)channelDataObject.Status);
            _timeOSD = new OSDItem(DEFAULT_ID, thermalArea, string.Empty, new Point(timeDataObject.Positon.X, timeDataObject.Positon.Y), OSDType.Time, (OSDStatus)timeDataObject.Status);
            _ptzOSD = new OSDItem(DEFAULT_ID, thermalArea, I18n.Properties.Resources.PresetOne, new Point(ptzOSDConfigDO.Positon.X, ptzOSDConfigDO.Positon.Y), OSDType.PresetTitle, (OSDStatus)ptzOSDConfigDO.Status);

            Name = channelDataObject.Name;
            _markerOSDStatus = (OSDStatus)markerOSDConfigDO.Status;
            EnableOSD = osdConfigDO;
            ShowMax = markerOSDConfigDO.ShowMax;
            ShowMin = markerOSDConfigDO.ShowMin;
            ShowAvg = markerOSDConfigDO.ShowAvg;
            OnPropertyChanged(nameof(ShowTemp));
            OnPropertyChanged(nameof(ShowChannel));
            OnPropertyChanged(nameof(ShowTime));
            OnPropertyChanged(nameof(ShowPTZ));
            OnPropertyChanged(nameof(ChannelOSD));
            OnPropertyChanged(nameof(TimeOSD));
            OnPropertyChanged(nameof(PTZOSD));

            Enable = true;
        }

        internal ChannelConfigDataObject ToChannelConfigDataObject(OSDItem channelOSD, OSDItem timeOSD)
        {
            TimeDataObject timeDataObject = new TimeDataObject(timeOSD.OSDStatus, timeOSD.Positon);
            ChannelDataObject channelDataObject = new ChannelDataObject(channelOSD.ID, channelOSD.OSDStatus, channelOSD.Name, channelOSD.Positon, timeDataObject);

            return new ChannelConfigDataObject(new List<ChannelDataObject> { channelDataObject });
        }

        internal PTZOSDConfigDataObject ToPTZOSDConfigDataObject(OSDItem ptzOSD)
        {
            return new PTZOSDConfigDataObject(ptzOSD.OSDStatus, ptzOSD.Positon);
        }

        internal MarkerOSDConfigDataObject ToMarkerOSDConfigDataObject()
        {
            return new MarkerOSDConfigDataObject(_markerOSDStatus, _showMax, _showMin, _showAvg);
        }

        #endregion
    }
}