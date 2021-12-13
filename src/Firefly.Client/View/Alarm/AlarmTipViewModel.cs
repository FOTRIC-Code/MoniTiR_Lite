// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Firefly.Client.Service;
using Firefly.Client.Common;
using Firefly.Client.Control;

namespace Firefly.Client
{
    public class AlarmTipViewModel : BaseViewModel
    {
        #region field

        private DateTime _alarmTime;
        private string _deviceName;
        private string _alarmType;
        private string _alarmLevel;
        private string _note;
        private string _presetName;
        private List<VideoInfoViewModel> _playbackVideoInfoVMs;
        private List<VideoInfoViewModel> _liveVideoInfoVMs;
        private List<PlaceHolderViewModel> _imageURLs;
        private bool _isImageFullScreen;
        private bool _isVideoFullScreen;
        private string _fullScreenURL;
        private bool _validPreset;
        private string _alarmDesc;

        #endregion

        #region constructor

        public AlarmTipViewModel()
        {
            _imageURLs = new List<PlaceHolderViewModel>();
            _playbackVideoInfoVMs = new List<VideoInfoViewModel>();
            _liveVideoInfoVMs = new List<VideoInfoViewModel>();
            PresetList = new ObservableCollection<Tuple<int, string>>();
            for (int i = 1; i < 256; i++)
            {
                PresetList.Add(new Tuple<int, string>(i, $"{I18n.Properties.Resources.Preset} {i}"));
            }
        }

        #endregion

        #region property

        public ObservableCollection<Tuple<int, string>> PresetList {  get; set; }

        public string AlarmDesc
        {
            get => _alarmDesc;
            set
            {
                _alarmDesc = value;
                OnPropertyChanged();
            }
        }

        public DateTime AlarmTime
        {
            get => _alarmTime;
            set
            {
                _alarmTime = value;
                OnPropertyChanged(nameof(AlarmTime));
            }
        }

        public string DeviceName
        {
            get => _deviceName;
            set
            {
                _deviceName = value;
                OnPropertyChanged(nameof(DeviceName));
            }
        }

        public string PresetName
        {
            get => _presetName;
            set
            {
                _presetName = value;
                OnPropertyChanged(nameof(PresetName));
            }
        }

        public string AlarmType
        {
            get => _alarmType;
            set
            {
                _alarmType = value;
                OnPropertyChanged(nameof(AlarmType));
            }
        }

        public string AlarmLevel
        {
            get => _alarmLevel;
            set
            {
                _alarmLevel = value;
                OnPropertyChanged(nameof(AlarmLevel));
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                _note = value;
                OnPropertyChanged(nameof(Note));
            }
        }

        public List<VideoInfoViewModel> PlaybackVideoInfoVMs
        {
            get => _playbackVideoInfoVMs;
            set
            {
                _playbackVideoInfoVMs = value;
                OnPropertyChanged(nameof(PlaybackVideoInfoVMs));
            }
        }

        public List<VideoInfoViewModel> LiveVideoInfoVMs
        {
            get => _liveVideoInfoVMs;
            set
            {
                _liveVideoInfoVMs = value;
                OnPropertyChanged(nameof(LiveVideoInfoVMs));
            }
        }

        public List<PlaceHolderViewModel> ImageURLs
        {
            get => _imageURLs;
            set
            {
                _imageURLs = value;
                OnPropertyChanged(nameof(ImageURLs));
            }
        }

        public string FullScreenURL
        {
            get => _fullScreenURL;
            set
            {
                _fullScreenURL = value;
                OnPropertyChanged(nameof(FullScreenURL));
            }
        }

        public bool IsImageFullScreen
        {
            get => _isImageFullScreen;
            set
            {
                _isImageFullScreen = value;
                OnPropertyChanged(nameof(IsImageFullScreen));
            }
        }

        public bool IsVideoFullScreen
        {
            get => _isVideoFullScreen;
            set
            {
                _isVideoFullScreen = value;
                OnPropertyChanged(nameof(IsVideoFullScreen));
            }
        }

        public bool ValidPreset
        {
            get => _validPreset;
            set
            {
                _validPreset = value;
                OnPropertyChanged(nameof(ValidPreset));
            }
        }

        #endregion

        #region method

        public void Load(AlarmDO alarmDO, PlaybackStreamDO playbackStreamDO, DeviceStreamDO mianStreamDO, DeviceStreamDO subStreamDO)
        {
            AlarmTime = alarmDO.AlarmTime.ToLocalTime();
            DeviceName = alarmDO.DeviceName;
            AlarmType = alarmDO.AlarmType;
            AlarmLevel = alarmDO.AlarmLevel;
            Note = alarmDO.Note ?? string.Empty;
            PresetName = $"{I18n.Properties.Resources.Preset}{alarmDO.PresetID}";
            ValidPreset = alarmDO.PresetID > 0;
            AlarmDesc = alarmDO.AlarmDescription;
            List<VideoInfoViewModel> playbackVideoVMs = new List<VideoInfoViewModel>();
            List<VideoInfoViewModel> liveVideoInfoVMs = new List<VideoInfoViewModel>();
            List<PlaceHolderViewModel> imageURLs = new List<PlaceHolderViewModel>();

            DeviceDO deviceDO = DeviceManager.Instance.GetCached(alarmDO.DeviceID);
            alarmDO.ImageInfo?.ForEach(it =>
            {
                if (!string.IsNullOrWhiteSpace(it.Url))
                {
                    PlaceHolderViewModel placeHolderVM = new PlaceHolderViewModel
                    {
                        Url = Regex.Replace(it.Url, Constants.IPS_REG, deviceDO.DefaultAgent().IP)
                    };
                    if (deviceDO.DeviceAddress.Count > it.ChannelID)
                    {
                        if (DeviceAddressDataObject.IsIRProtocolType(deviceDO.DeviceAddress[it.ChannelID].ProtocolType))
                        {
                            placeHolderVM.IsIR = true;
                        }
                    }
                    imageURLs.Add(placeHolderVM);
                }
            });
            while (imageURLs.Count < 2)
            {
                imageURLs.Add(new PlaceHolderViewModel
                {
                    IsEnable = false
                });
            }

            if (playbackStreamDO.PlaybackStreamItems != null)
            {
                foreach (PlaybackStreamItemDO playbackStreamItemDO in playbackStreamDO.PlaybackStreamItems)
                {
                    playbackVideoVMs.Add(new VideoInfoViewModel(playbackStreamItemDO.RtspUrl, playbackStreamItemDO.RtspUrl));
                }
            }
            if (playbackVideoVMs.Count == 0)
            {
                playbackVideoVMs.Add(new VideoInfoViewModel("", ""));
                playbackVideoVMs.Add(new VideoInfoViewModel("", ""));
            }

            if (mianStreamDO?.StreamItems != null)
            {
                foreach (DeviceStreamItemDO mianStreamItemDO in mianStreamDO.StreamItems)
                {
                    string subStreamUrl = subStreamDO.StreamItems.FirstOrDefault(item => item.ChannelID == mianStreamItemDO.ChannelID).RTSPUrl;
                    liveVideoInfoVMs.Add(new VideoInfoViewModel(mianStreamItemDO.RTSPUrl, subStreamUrl));
                }
            }

            PlaybackVideoInfoVMs = playbackVideoVMs;
            LiveVideoInfoVMs = liveVideoInfoVMs;
            ImageURLs = imageURLs;
        }

        public void StopPlayVideo()
        {
            foreach (VideoInfoViewModel playbackVideo in PlaybackVideoInfoVMs)
            {
                playbackVideo.Stop();
            }
            foreach (VideoInfoViewModel liveVideoInfo in LiveVideoInfoVMs)
            {
                liveVideoInfo.Stop();
            }
            if (IsVideoFullScreen)
            {
                FullScreenURL = string.Empty;
            }
        }

        public void RestoreScreen()
        {
            IsImageFullScreen = false;
            IsVideoFullScreen = false;
            FullScreenURL = string.Empty;
        }

        #endregion
    }

    public class VideoInfoViewModel : BaseViewModel
    {
        #region field

        private string _mainStreamURL;
        private string _subStreamURL;
        private bool _isPaused = true;
        private bool _isStopped = false;

        #endregion

        #region constructor

        public VideoInfoViewModel(string mainStreamURL, string subStreamURL)
        {
            _mainStreamURL = mainStreamURL;
            _subStreamURL = subStreamURL;
        }

        #endregion

        #region property

        public string MainStreamURL
        {
            get => _mainStreamURL;
            set
            {
                _mainStreamURL = value;
                OnPropertyChanged();
            }
        }

        public string SubStreamURL
        {
            get => _subStreamURL;
            set
            {
                _subStreamURL = value;
                OnPropertyChanged();
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsStopped
        {
            get => _isStopped;
            set
            {
                if (_isStopped != value)
                {
                    _isStopped = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region method

        public void Stop()
        {
            IsPaused = true;
            IsStopped = true;
            MainStreamURL = string.Empty;
            SubStreamURL = string.Empty;
        }

        #endregion
    }
}