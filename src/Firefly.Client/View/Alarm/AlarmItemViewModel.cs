using Firefly.Client.Control;
using Firefly.Client.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Firefly.Client.Common;

namespace Firefly.Client
{
    public class AlarmItemViewModel : BaseViewModel
    {
        #region Fields

        private string _note = string.Empty;
        private bool _isConfirmed;
        private bool _isExported;
        private VideoViewModel _selectedAlarmRecordVideoVM;
        private bool _hasVideoRecord;
        private string _channelName;
        private AlarmDO _alarmDO;
        private bool _isSelected;

        #endregion Fields

        #region Constructors

        public AlarmItemViewModel()
        {
            LiveVideoViewModels = new ObservableCollection<VideoViewModel>();
            RecordVideoList = new ObservableCollection<VideoViewModel>();
            AlarmPictureList = new List<PlaceHolderViewModel>();
        }

        #endregion Constructors

        #region Properties

        public int ID { get; set; }
        public long Timestamp { get; set; }
        public string DeviceID { get; set; }
        public int ChannelID { get; set; }
        public string DeviceName { get; set; }
        public string AlarmType { get; set; }
        public string AlarmRegion { get; set; }
        public string AlarmLevel { get; set; }
        public DateTime AlarmTime { get; set; }
        public string Description { get; set; }

        public bool IsConfirmed
        {
            get => _isConfirmed;
            set
            {
                _isConfirmed = value;
                OnPropertyChanged(nameof(IsConfirmed));
            }
        }

        public bool IsExported
        {
            get => _isExported;
            set
            {
                _isExported = value;
                OnPropertyChanged(nameof(IsExported));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                _note = value ?? string.Empty;
                OnPropertyChanged(nameof(Note));
            }
        }

        public DateTime ArrivalTime { get; set; }
        public ObservableCollection<VideoViewModel> LiveVideoViewModels { get; set; }
        public List<PlaceHolderViewModel> AlarmPictureList { get; set; }
        public ObservableCollection<VideoViewModel> RecordVideoList { get; set; }

        public VideoViewModel SelectedAlarmRecordVideoViewModel
        {
            get => _selectedAlarmRecordVideoVM;
            set
            {
                _selectedAlarmRecordVideoVM = value;
                OnPropertyChanged(nameof(SelectedAlarmRecordVideoViewModel));
            }
        }

        public bool HasRecordVideos
        {
            get => _hasVideoRecord;
            set { _hasVideoRecord = value; OnPropertyChanged(); }
        }

        public AlarmDO AlarmDO => _alarmDO;

        public string ChannelName
        {
            get => _channelName;
            set
            {
                _channelName = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        public string ConvertToCSVLineData()
        {
            string confirmStr = IsConfirmed ? I18n.Properties.Resources.Confirmed : I18n.Properties.Resources.NoConfirm;
            string exportedStr = IsExported ? I18n.Properties.Resources.Exported : I18n.Properties.Resources.NoExport;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("{0},{1},{2},{3},{4},{5:yyyy/MM/dd HH:mm:ss},{6},{7},{8},{9}", DeviceName, ChannelName, AlarmRegion, AlarmType, AlarmLevel, AlarmTime, Description, confirmStr, exportedStr, Note);
            return stringBuilder.ToString();
        }

        public void LoadAlarmDataObject(AlarmDO alarmDO)
        {
            DeviceDO deviceDO = DeviceManager.Instance.GetCached(alarmDO.DeviceID);
            Enum.TryParse(deviceDO.DefaultAgent().Type, out ServerType serverType);
            _alarmDO = alarmDO;
            Timestamp = alarmDO.Timestamp;
            ID = alarmDO.ID;
            DeviceID = alarmDO.DeviceID;
            DeviceName = alarmDO.DeviceName;
            AlarmType = GetAgentAlarmType(serverType) + "_" + alarmDO.AlarmType;
            AlarmRegion = alarmDO.AlarmRegion;
            AlarmLevel = alarmDO.AlarmLevel;
            AlarmTime = alarmDO.AlarmTime.ToLocalTime();
            Description = alarmDO.AlarmDescription;
            IsConfirmed = alarmDO.IsConfirmed;
            IsExported = alarmDO.IsExported;
            Note = alarmDO.Note;
            ChannelID = alarmDO.ChannelID;
            ChannelName = alarmDO.ChannelName;

            AlarmPictureList.Clear();

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
                    AlarmPictureList.Add(placeHolderVM);
                }
            });
            while (AlarmPictureList.Count < 2)
            {
                AlarmPictureList.Add(new PlaceHolderViewModel
                {
                    IsEnable = false
                });
            }

            UpdateRecordVideoViewModels();
        }

        public async void UpdateLiveVideoViewModels(bool autoplayLiveVideo)
        {
            DeviceDO deviceDO = DeviceManager.Instance.GetCached(DeviceID);

            OnPropertyChanged(nameof(AlarmPictureList));
            ClearLiveVideo();

            if (deviceDO?.DeviceAddress != null && !string.Equals(deviceDO?.DefaultAgent().Type, ServerType.EnvironmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                bool isOnline = DeviceManager.Instance.IsOnline(deviceDO.ID);
                if (isOnline)
                {
                    DeviceStreamDO streamDO = await DeviceManager.Instance.GetStream(deviceDO, StreamType.SubStream);
                    if (streamDO?.IRDeviceSteamItem != null)
                    {
                        if (!autoplayLiveVideo)
                        {
                            LiveVideoViewModels.Add(new PlaceHolderViewModel
                            {
                                IsEnable = true,
                                RTSPUrl = streamDO.IRDeviceSteamItem.RTSPUrl,
                            });
                        }
                        else
                        {
                            LiveVideoViewModels.Add(new VideoViewModel { RTSPUrl = streamDO.IRDeviceSteamItem.RTSPUrl, IsPaused = false });
                        }
                    }
                    if (streamDO?.DCDeviceSteamItem != null)
                    {
                        if (!autoplayLiveVideo)
                        {
                            LiveVideoViewModels.Add(new PlaceHolderViewModel
                            {
                                IsEnable = true,
                                RTSPUrl = streamDO.DCDeviceSteamItem.RTSPUrl,
                            });
                        }
                        else
                        {
                            LiveVideoViewModels.Add(new VideoViewModel { RTSPUrl = streamDO.DCDeviceSteamItem.RTSPUrl, IsPaused = false });
                        }
                    }
                }

                while (LiveVideoViewModels.Count < 2)
                {
                    LiveVideoViewModels.Add(new PlaceHolderViewModel
                    {
                        IsEnable = false
                    });
                }
            }
        }

        public void ClearLiveVideo()
        {
            foreach (VideoViewModel liveVideoBaseViewModel in LiveVideoViewModels)
            {
                liveVideoBaseViewModel.IsPaused = true;
                liveVideoBaseViewModel.RTSPUrl = string.Empty;
            }

            LiveVideoViewModels.Clear();
        }

        public void ClearRecordVideo()
        {
            foreach (VideoViewModel alarmVideoViewModel in RecordVideoList)
            {
                alarmVideoViewModel.IsPaused = true;
                alarmVideoViewModel.RTSPUrl = string.Empty;
            }
            RecordVideoList.Clear();
        }

        public async void UpdateRecordVideoViewModels()
        {
            DeviceDO deviceDO = DeviceManager.Instance.GetCached(DeviceID);
            if (deviceDO != null)
            {
                ClearRecordVideo();
                ServerDO storeAgentDO = deviceDO.GetAgent(ServerType.StoreAgent);

                if (storeAgentDO != null)
                {
                    int irChannelID = 0;
                    int dcChannelID = 1;
                    DeviceStreamDO streamDO = await DeviceManager.Instance.GetStream(deviceDO, StreamType.SubStream);
                    if (streamDO != null)
                    {
                        RecordVideoList.Add(new PlaceHolderViewModel { NVRChannelID = irChannelID, IsEnable = streamDO.IRDeviceSteamItem != null });
                        RecordVideoList.Add(new PlaceHolderViewModel { NVRChannelID = dcChannelID, IsEnable = streamDO.DCDeviceSteamItem != null });

                        if (streamDO.IRDeviceSteamItem != null || streamDO.DCDeviceSteamItem != null)
                        {
                            HasRecordVideos = true;
                        }
                    }
                }
            }
        }

        public void ReplaceToVideoViewModel(VideoViewModel viewModel)
        {
            int index = LiveVideoViewModels.IndexOf(viewModel);
            VideoViewModel videoViewModel = new VideoViewModel()
            {
                RTSPUrl = viewModel.RTSPUrl
            };
            videoViewModel.IsPaused = false;
            LiveVideoViewModels.Insert(index, videoViewModel);
            LiveVideoViewModels.Remove(viewModel);
        }

        public async void ReplaceToRecordVideoViewModel(PlaceHolderViewModel placeHolderViewModel)
        {
            DeviceDO deviceDO = DeviceManager.Instance.GetCached(DeviceID);
            if (deviceDO != null)
            {
                ServerDO storeAgentDO = deviceDO.GetAgent(ServerType.StoreAgent);
                DateTime startTime = AlarmTime.AddSeconds(-30);
                DateTime endTime = AlarmTime.AddSeconds(30);
                PlaybackStreamDO playbackStreamDO = await StoreManager.Instance.GetPlaybackStream(storeAgentDO.GetServerAddress(), deviceDO.ID, startTime, endTime);
                int index = RecordVideoList.IndexOf(placeHolderViewModel);
                if (index >= 0)
                {
                    RecordVideoList[index] = new VideoViewModel
                    {
                        RTSPUrl = playbackStreamDO?.PlaybackStreamItems.FirstOrDefault(item => item.ChannelID == placeHolderViewModel.NVRChannelID)?.RtspUrl
                    };
                    RecordVideoList[index].IsPaused = false;
                }
            }
        }

        private string GetAgentAlarmType(ServerType serverType)
        {
            string agentAlarmType = string.Empty;
            switch (serverType)
            {
                case ServerType.FireMonitorAgent:
                    agentAlarmType = I18n.Properties.Resources.IRAlarm;
                    break;
                case ServerType.EnvironmentMonitorAgent:
                    agentAlarmType = I18n.Properties.Resources.ENVAlarm;
                    break;
                case ServerType.SecurityMonitorTransferAgent:
                    agentAlarmType = I18n.Properties.Resources.SecurityAlarm;
                    break;
            }
            return agentAlarmType;
        }

        #endregion Methods
    }

    public class PlaceHolderViewModel : VideoViewModel
    {
        private string _url;

        // For multi itemsControl dataTemplate
        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged();
            }
        }

        public int NVRChannelID { get; set; }
    }

    public class VideoViewModel : BaseViewModel
    {
        private string _name;
        private bool _isEnabled = true;
        private string _rtspUrl;
        private bool _isPaused = true;
        private bool _isIR = false;

        public bool IsIR
        {
            get => _isIR;
            set
            {
                _isIR = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnable
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
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

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public class ImageViewModel : BaseViewModel
    {
        private bool _isEnabled = true;
        private string _url;

        public bool IsEnable
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged();
            }
        }
    }
}