// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class DeviceDetailViewModel : BaseViewModel
    {
        private const int MAX_SENSOR_COUNT = 10;
        private const int MAX_LITE_FIRE_CHANNEL_COUNT = 2;
        private const string ProtocolFireMonitorS = "FireMonitor_S";
        private const string ProtocolFireMonitorD = "FireMonitor_D";
        public static int MAX_SUBDEVICE_COUNT = MAX_SENSOR_COUNT;

        #region Fields

        private DeviceDO _deviceDO;
        private List<ServerDO> _agentDOs;
        private List<AgentProtocol> _protocolDOs;

        private string _name;

        private AgentViewModel _selectedStoreVM;
        private ServerType _selectedServerType;
        private AgentViewModel _selectedAgentVM;
        private string _selectedProtocolType;
        private ObservableCollection<SubDeviceViewModel> _subDeviceVMs;

        private Visibility _deleteChannelBtnVisibility = Visibility.Collapsed;
        private Visibility _addChannelButtonVisibility = Visibility.Collapsed;
        private bool _addChannelButtonEnabled = true;
        private bool _isSaveEnabled = true;
        private bool _liteFireChannelMode;

        private bool _showDiscovery = false;
        private ObservableCollection<DiscoveryCameraViewModel> _discoveryCameraVMs;

        #endregion Fields

        #region Constructors

        public DeviceDetailViewModel(DeviceDO deviceDO, List<ServerDO> agentDOs)
        {
            _deviceDO = deviceDO;
            _agentDOs = agentDOs ?? new List<ServerDO>();
            _subDeviceVMs = new ObservableCollection<SubDeviceViewModel>();
            _discoveryCameraVMs = new ObservableCollection<DiscoveryCameraViewModel>();
            _name = _deviceDO?.Name;
            InitServerSelection();
        }

        /// <summary>
        /// Store / FireMonitor are no longer shown in UI; pick local Lite defaults (or existing device agents on Edit).
        /// </summary>
        private void InitServerSelection()
        {
            if (!IsAddOperation)
            {
                ServerDO storeDO = _deviceDO.AgentAddress?.FirstOrDefault(item =>
                    string.Equals(item.Type, ServerType.StoreAgent.ToString(), StringComparison.OrdinalIgnoreCase));
                SelectedStore = storeDO != null
                    ? new AgentViewModel(storeDO)
                    : new AgentViewModel();

                ServerDO monitorDO = _deviceDO.DefaultAgent();
                if (monitorDO != null)
                {
                    Enum.TryParse(monitorDO.Type, out ServerType serverType);
                    _selectedServerType = serverType;
                    SelectedAgent = new AgentViewModel(monitorDO);
                }

                return;
            }

            ServerDO localStore = FindLocalOrFirstOnline(ServerType.StoreAgent, LiteAgentBootstrap.StorePort);
            SelectedStore = localStore != null ? new AgentViewModel(localStore) : new AgentViewModel();

            _selectedServerType = ServerType.FireMonitorAgent;
            ServerDO localFire = FindLocalOrFirstOnline(ServerType.FireMonitorAgent, LiteAgentBootstrap.FirePort);
            if (localFire != null)
            {
                SelectedAgent = new AgentViewModel(localFire);
            }
        }

        private ServerDO FindLocalOrFirstOnline(ServerType type, int preferredPort)
        {
            string typeName = type.ToString();
            List<ServerDO> candidates = _agentDOs
                .Where(x => string.Equals(x.Type, typeName, StringComparison.OrdinalIgnoreCase)
                    && x.Status == (int)DeviceStatus.Online)
                .ToList();

            return candidates.FirstOrDefault(x =>
                    (string.Equals(x.IP, LiteAgentBootstrap.LocalIp, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(x.IP, "localhost", StringComparison.OrdinalIgnoreCase))
                    && x.Port == preferredPort)
                ?? candidates.FirstOrDefault();
        }

        #endregion Constructors

        #region Proterties

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public List<AgentViewModel> StoreList
        {
            get
            {
                List<AgentViewModel> agentVMs = new List<AgentViewModel> { new AgentViewModel() };
                List<ServerDO> serverDOs = _agentDOs.Where(x => x.Type.Equals(ServerType.StoreAgent.ToString()) && x.Status == (int)DeviceStatus.Online).ToList();
                serverDOs.ForEach(item => agentVMs.Add(new AgentViewModel(item)));
                if (_deviceDO != null)
                {
                    ServerDO serverDataObject = _deviceDO.AgentAddress.FirstOrDefault(item => item.Type.Equals(ServerType.StoreAgent.ToString()));
                    if (serverDataObject != null)
                    {
                        SelectedStore = agentVMs.FirstOrDefault(item => item.ID == serverDataObject.ID);
                    }
                    else
                    {
                        SelectedStore = agentVMs.FirstOrDefault(item => item.ID > 0) ?? agentVMs.FirstOrDefault();
                    }
                }
                else
                {
                    SelectedStore = agentVMs.FirstOrDefault(item => item.ID > 0) ?? agentVMs.FirstOrDefault();
                }

                return agentVMs;
            }
        }

        public AgentViewModel SelectedStore
        {
            get => _selectedStoreVM;
            set
            {
                _selectedStoreVM = value;

                OnPropertyChanged(nameof(SelectedStore));
            }
        }

        public List<ServerType> ServerTypeList
        {
            get
            {
                List<ServerType> serverTypes = new List<ServerType>();
                List<ServerDO> serverDOs = _agentDOs.Where(x => !ServerTypeConverter.IsBaseAgent(x.Type) && (x.Status == (int)DeviceStatus.Online || !IsAddOperation)).ToList();
                foreach (ServerDO serverDO in serverDOs)
                {
                    Enum.TryParse(serverDO.Type, out ServerType serverType);

                    if (serverType != ServerType.FireMonitorAgent)
                    {
                        continue;
                    }

                    if (!serverTypes.Contains(serverType))
                    {
                        serverTypes.Add(serverType);
                    }
                }

                if (!IsAddOperation)
                {
                    ServerDO monitorAgentDO = _deviceDO.DefaultAgent();
                    Enum.TryParse(monitorAgentDO.Type, out ServerType serverType);
                    SelectedServerType = serverType;
                }
                else
                {
                    SelectedServerType = serverTypes.FirstOrDefault();
                }

                return serverTypes;
            }
        }

        public ServerType SelectedServerType
        {
            get => _selectedServerType;
            set
            {
                _selectedServerType = value;
                OnPropertyChanged(nameof(SelectedServerType));
                OnPropertyChanged(nameof(AgentList));
            }
        }

        public List<AgentViewModel> AgentList
        {
            get
            {
                List<AgentViewModel> agentVMs = new List<AgentViewModel>();

                List<ServerDO> serverDOs = _agentDOs.Where(x => !ServerTypeConverter.IsBaseAgent(x.Type) && (x.Status == (int)DeviceStatus.Online || !IsAddOperation)).ToList();
                serverDOs.ForEach(item =>
                {
                    Enum.TryParse(item.Type, out ServerType serverType);
                    if (serverType == _selectedServerType)
                    {
                        agentVMs.Add(new AgentViewModel(item));
                    }
                });
                if (!IsAddOperation)
                {
                    ServerDO monitorAgentDO = _deviceDO.DefaultAgent();
                    SelectedAgent = agentVMs.FirstOrDefault(item => item.ID == monitorAgentDO.ID);
                }
                else
                {
                    SelectedAgent = agentVMs.FirstOrDefault();
                }

                UpdateAgentConfig();
                return agentVMs;
            }
        }

        public AgentViewModel SelectedAgent
        {
            get => _selectedAgentVM;
            set
            {
                _selectedAgentVM = value;
                UpdateAgentConfig();
                OnPropertyChanged(nameof(SelectedAgent));
            }
        }

        public List<string> ProtocolTypeList
        {
            get
            {
                List<string> protocolTypeList = new List<string>();
                if (_protocolDOs != null)
                {
                    _protocolDOs.ForEach(item => protocolTypeList.Add(item.ProtocolType));
                }
                else
                {
                    if (!IsAddOperation)
                    {
                        protocolTypeList.Add(_deviceDO.ProtocolType);
                    }
                }

                // Lite Fire add: protocol is derived from channel count; do not mutate selection in getter.
                if (!_liteFireChannelMode)
                {
                    if (_deviceDO != null)
                    {
                        SelectedProtocolType = protocolTypeList.FirstOrDefault(item => item.Equals(_deviceDO.ProtocolType));
                    }
                    else
                    {
                        SelectedProtocolType = protocolTypeList.FirstOrDefault();
                    }
                }

                return protocolTypeList;
            }
        }

        public string SelectedProtocolType
        {
            get => _selectedProtocolType;
            set
            {
                if (_selectedProtocolType == value)
                {
                    return;
                }

                _selectedProtocolType = value;
                OnPropertyChanged();

                if (!_liteFireChannelMode)
                {
                    LoadSubDevice();
                }
            }
        }

        /// <summary>
        /// Lite: hide monitor-point protocol; S/D is decided by channel count.
        /// </summary>
        public Visibility ProtocolSelectionVisibility => Visibility.Collapsed;

        public bool IsLiteFireAdd => _liteFireChannelMode;

        public Visibility AddChannelButtonVisibility
        {
            get => _addChannelButtonVisibility;
            set
            {
                _addChannelButtonVisibility = value;
                OnPropertyChanged();
            }
        }

        public bool AddChannelButtonEnabled
        {
            get => _addChannelButtonEnabled;
            set
            {
                _addChannelButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SubDeviceViewModel> SubDeviceList
        {
            get => _subDeviceVMs;
            set
            {
                _subDeviceVMs = value;
                OnPropertyChanged(nameof(SubDeviceList));
            }
        }

        public SubDeviceViewModel SelectedSubDevice { get; set; }

        public bool IsAddOperation => _deviceDO == null;

        public bool CanEditChannel => _protocolDOs != null;

        public SubDeviceViewModel SensorDeviceViewModel { get; set; }

        public Visibility DeleteChannelBtnVisibility
        {
            get => _deleteChannelBtnVisibility;
            set
            {
                _deleteChannelBtnVisibility = value;
                OnPropertyChanged();
            }
        }

        public bool IsSaveEnabled
        {
            get => _isSaveEnabled;
            set
            {
                _isSaveEnabled = value;
                OnPropertyChanged();
            }
        }

        #region onvif discovery

        public bool IsShowDiscovery
        {
            get => _showDiscovery;
            set
            {
                _showDiscovery = value;
                OnPropertyChanged(nameof(DiscoveryVisibility));
            }
        }

        public Visibility DiscoveryVisibility { get => _showDiscovery ? Visibility.Visible : Visibility.Collapsed; }

        public DiscoveryCameraViewModel SelectedDiscoveryCamera { get; set; }

        public ObservableCollection<DiscoveryCameraViewModel> DiscoveryCameraList
        {
            get => _discoveryCameraVMs;
            set
            {
                _discoveryCameraVMs = value;
                OnPropertyChanged();
            }
        }

        internal void Try2UpdateSubDevice()
        {
            SubDeviceViewModel targetSubDevice = SubDeviceList.FirstOrDefault(item => item.CurrentSubDeviceTypeItem.DeviceType == SelectedDiscoveryCamera.DeviceType);
            if (targetSubDevice != null)
            {
                targetSubDevice.CopyFrom(SelectedDiscoveryCamera);
                if (_liteFireChannelMode)
                {
                    SyncLiteFireProtocolFromChannels();
                }
            }
        }

        #endregion

        #endregion Proterties

        #region Methods

        private async void UpdateAgentConfig()
        {
            await UpdateAgentConfigAsync().ConfigureAwait(true);
        }

        public async Task UpdateAgentConfigAsync()
        {
            if (_selectedAgentVM == null)
            {
                return;
            }

            AgentConfig configDOs = await AgentManager.Instance.GetAgentConfig($"http://{_selectedAgentVM.Address}");
            if (configDOs?.MetaData != null)
            {
                LoadServerProtocolList(configDOs.MetaData.ProtocolList);
            }
        }

        private void LoadServerProtocolList(List<AgentProtocol> protocolDOs)
        {
            _protocolDOs = protocolDOs;
            _liteFireChannelMode = IsAddOperation
                && _selectedAgentVM != null
                && _selectedAgentVM.Type == ServerType.FireMonitorAgent;

            OnPropertyChanged(nameof(ProtocolTypeList));
            OnPropertyChanged(nameof(CanEditChannel));
            OnPropertyChanged(nameof(ProtocolSelectionVisibility));
            OnPropertyChanged(nameof(IsLiteFireAdd));

            if (_liteFireChannelMode)
            {
                InitLiteFireAddChannels();
                return;
            }

            if (_protocolDOs != null && _protocolDOs.Count > 0)
            {
                // Trigger LoadSubDevice via SelectedProtocolType setter.
                if (_deviceDO != null && !string.IsNullOrWhiteSpace(_deviceDO.ProtocolType))
                {
                    SelectedProtocolType = _protocolDOs
                        .FirstOrDefault(item => string.Equals(item.ProtocolType, _deviceDO.ProtocolType, StringComparison.OrdinalIgnoreCase))
                        ?.ProtocolType ?? _protocolDOs[0].ProtocolType;
                }
                else
                {
                    SelectedProtocolType = _protocolDOs[0].ProtocolType;
                }
            }
        }

        /// <summary>
        /// Add Fire: always show IR + visible-light rows.
        /// Protocol S/D is decided on save by whether the visible-light channel has an IP.
        /// </summary>
        private void InitLiteFireAddChannels()
        {
            MAX_SUBDEVICE_COUNT = MAX_LITE_FIRE_CHANNEL_COUNT;
            SubDeviceList?.Clear();
            SensorDeviceViewModel = null;

            AgentProtocol protocolD = FindProtocol(ProtocolFireMonitorD);
            Channel irChannel = protocolD?.ChannelList?.FirstOrDefault(c =>
                string.Equals(c.Type, DeviceType.IRCamera.ToString(), StringComparison.OrdinalIgnoreCase));
            Channel dcChannel = protocolD?.ChannelList?.FirstOrDefault(c =>
                string.Equals(c.Type, DeviceType.DCCamera.ToString(), StringComparison.OrdinalIgnoreCase));

            // Fallback to S IR definition if D metadata is incomplete.
            if (irChannel == null)
            {
                AgentProtocol protocolS = FindProtocol(ProtocolFireMonitorS);
                irChannel = protocolS?.ChannelList?.FirstOrDefault(c =>
                    string.Equals(c.Type, DeviceType.IRCamera.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (irChannel == null)
            {
                AddChannelButtonVisibility = Visibility.Collapsed;
                DeleteChannelBtnVisibility = Visibility.Collapsed;
                IsSaveEnabled = false;
                return;
            }

            SubDeviceList.Add(CreateChannelViewModel(irChannel));
            if (dcChannel != null)
            {
                SubDeviceViewModel dcVm = CreateChannelViewModel(dcChannel);
                dcVm.ChannelID = 1;
                SubDeviceList.Add(dcVm);
            }

            SyncLiteFireProtocolFromChannels();

            AddChannelButtonVisibility = Visibility.Collapsed;
            DeleteChannelBtnVisibility = Visibility.Visible;
        }

        /// <summary>
        /// Visible-light channel counts as dual (FireMonitor_D) only when it has an IP.
        /// </summary>
        private static bool IsChannelIpFilled(SubDeviceViewModel channel)
        {
            return channel != null && !string.IsNullOrWhiteSpace(channel.IP);
        }

        private bool IsFireMonitorAgentSelected()
        {
            return _selectedAgentVM != null && _selectedAgentVM.Type == ServerType.FireMonitorAgent;
        }

        private SubDeviceViewModel GetLiteVisibleLightChannel()
        {
            return SubDeviceList?.FirstOrDefault(x =>
                x.CurrentSubDeviceTypeItem?.DeviceType == DeviceType.DCCamera);
        }

        /// <summary>
        /// Resolve FireMonitor_S / FireMonitor_D from whether visible-light has IP.
        /// Returns the protocol string to persist (does not trust ComboBox selection).
        /// </summary>
        private string ResolveFireMonitorProtocolFromChannels()
        {
            bool dual = IsChannelIpFilled(GetLiteVisibleLightChannel());
            string protocolType = dual ? ProtocolFireMonitorD : ProtocolFireMonitorS;
            if (!string.Equals(_selectedProtocolType, protocolType, StringComparison.Ordinal))
            {
                _selectedProtocolType = protocolType;
                OnPropertyChanged(nameof(SelectedProtocolType));
            }
            return protocolType;
        }

        public void SyncLiteFireProtocolFromChannels()
        {
            if (!IsFireMonitorAgentSelected())
            {
                return;
            }

            ResolveFireMonitorProtocolFromChannels();

            // Keep channel ids aligned with FireMonitor_S/D schema (0=IR, 1=DC).
            if (SubDeviceList != null)
            {
                for (int i = 0; i < SubDeviceList.Count; i++)
                {
                    SubDeviceList[i].ChannelID = i;
                }
            }

            if (_liteFireChannelMode)
            {
                RefreshLiteFireChannelUi();
            }
        }

        public bool CanDeleteLiteFireChannel(SubDeviceViewModel subDevice)
        {
            if (!_liteFireChannelMode || subDevice == null || SubDeviceList == null || SubDeviceList.Count <= 1)
            {
                return false;
            }

            // Only allow removing the visible-light (second) channel.
            return subDevice.CurrentSubDeviceTypeItem?.DeviceType == DeviceType.DCCamera;
        }

        private void RefreshLiteFireChannelUi()
        {
            AddChannelButtonEnabled = false;
            AddChannelButtonVisibility = Visibility.Collapsed;
            IsSaveEnabled = SubDeviceList != null && SubDeviceList.Count > 0;
        }

        private AgentProtocol FindProtocol(string protocolType)
        {
            return _protocolDOs?.FirstOrDefault(p =>
                string.Equals(p.ProtocolType, protocolType, StringComparison.OrdinalIgnoreCase));
        }

        private SubDeviceViewModel CreateChannelViewModel(Channel channel)
        {
            SubDeviceViewModel subDeviceVM = new SubDeviceViewModel();
            if (channel.Type.Equals(DeviceType.IRCamera.ToString()))
            {
                subDeviceVM.CurrentSubDeviceTypeItem = subDeviceVM.DeviceTypes.First(t => t.DeviceType == DeviceType.IRCamera);
                subDeviceVM.CurrentSubDeviceTypeItem.DeviceTypeDescript = I18n.Properties.Resources.IRCamera;
                subDeviceVM.Protocol = channel.SupportedProtocolTypeList.FirstOrDefault();
                if (subDeviceVM.Protocol == IRCameraType.SVSDK_NET.ToString())
                {
                    subDeviceVM.Port = Constants.SV_DEFAULT_PORT;
                }
                else if (subDeviceVM.Protocol == IRCameraType.CZF_ONVIF_IR_NET.ToString())
                {
                    subDeviceVM.Port = Constants.CZF_ONVIF_IR_DEFAULT_PORT;
                }
                else if (subDeviceVM.Protocol == IRCameraType.IRTEKSDK2_NET.ToString())
                {
                    subDeviceVM.Port = Constants.IRTEKSDK2_DEFAULT_PORT;
                }
                else
                {
                    subDeviceVM.Port = Constants.LANTERN_DEFAULT_PORT;
                }
            }
            else if (channel.Type.Equals(DeviceType.DCCamera.ToString()))
            {
                subDeviceVM.CurrentSubDeviceTypeItem = subDeviceVM.DeviceTypes.First(t => t.DeviceType == DeviceType.DCCamera);
                subDeviceVM.CurrentSubDeviceTypeItem.DeviceTypeDescript = I18n.Properties.Resources.DCCamera;
                subDeviceVM.Protocol = channel.SupportedProtocolTypeList.FirstOrDefault();
                subDeviceVM.Port = Constants.ONVIF_DC_DEFAULT_PORT;
            }

            subDeviceVM.SupportedProtocolTypeList = channel.SupportedProtocolTypeList != null
                ? new List<string>(channel.SupportedProtocolTypeList)
                : new List<string>();
            subDeviceVM.ChannelID = channel.ID;
            return subDeviceVM;
        }

        private async void LoadSubDevice()
        {
            if (_liteFireChannelMode)
            {
                return;
            }

            SubDeviceList?.Clear();
            AddChannelButtonVisibility = Visibility.Collapsed;
            DeleteChannelBtnVisibility = Visibility.Collapsed;
            if (_protocolDOs == null)
            {
                if (!IsAddOperation)
                {
                    _deviceDO?.DeviceAddress.ForEach(x => SubDeviceList.Add(new SubDeviceViewModel(x)));
                }

                return;
            }

            AgentProtocol protocolDO = _protocolDOs?.FirstOrDefault(item => item.ProtocolType == _selectedProtocolType);
            SensorDeviceViewModel = null;

            if (_deviceDO != null)
            {
                if (string.Equals(_deviceDO.DefaultAgent().Type, ServerType.EnvironmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    MAX_SUBDEVICE_COUNT = MAX_SENSOR_COUNT;
                    AddChannelButtonVisibility = Visibility.Visible;
                    SensorDeviceViewModel = new SubDeviceViewModel();
                    DeleteChannelBtnVisibility = Visibility.Visible;
                    if (protocolDO?.ChannelList != null)
                    {
                        foreach (Channel channel in protocolDO.ChannelList)
                        {
                            SensorDeviceViewModel.SupportedProtocolTypeList = new List<string>(channel.SupportedProtocolTypeList);
                        }
                    }
                }

                _deviceDO?.DeviceAddress?.ForEach(deviceAddressDO =>
                {
                    SubDeviceViewModel subDeviceVM = null;

                    subDeviceVM = new SubDeviceViewModel(deviceAddressDO);
                    if (protocolDO?.ChannelList != null)
                    {
                        Channel channel = protocolDO.ChannelList.FirstOrDefault(it => subDeviceVM.CurrentSubDeviceTypeItem.DeviceType.ToString().Contains(it.Type));
                        if (channel != null)
                        {
                            subDeviceVM.SupportedProtocolTypeList = new List<string>(channel.SupportedProtocolTypeList);
                        }
                    }
                    SubDeviceList.Add(subDeviceVM);
                });
            }
            else
            {
                List<SubDeviceViewModel> templateList = new List<SubDeviceViewModel>();

                if (protocolDO?.ChannelList != null)
                {
                    foreach (Channel channel in protocolDO.ChannelList)
                    {
                        SubDeviceViewModel subDeviceVM = new SubDeviceViewModel();
                        if (channel.Type.Equals(DeviceType.IRCamera.ToString()))
                        {
                            subDeviceVM.CurrentSubDeviceTypeItem.DeviceType = DeviceType.IRCamera;
                            subDeviceVM.CurrentSubDeviceTypeItem.DeviceTypeDescript = I18n.Properties.Resources.IRCamera;
                            subDeviceVM.Protocol = channel.SupportedProtocolTypeList.FirstOrDefault();
                            if (subDeviceVM.Protocol == IRCameraType.SVSDK_NET.ToString())
                            {
                                subDeviceVM.Port = Constants.SV_DEFAULT_PORT;
                            }
                            else if (subDeviceVM.Protocol == IRCameraType.CZF_ONVIF_IR_NET.ToString())
                            {
                                subDeviceVM.Port = Constants.CZF_ONVIF_IR_DEFAULT_PORT;
                            }
                            else if (subDeviceVM.Protocol == IRCameraType.IRTEKSDK2_NET.ToString())
                            {
                                subDeviceVM.Port = Constants.IRTEKSDK2_DEFAULT_PORT;
                            }
                            else
                            {
                                subDeviceVM.Port = Constants.LANTERN_DEFAULT_PORT;
                            }
                        }
                        else if (string.Equals(DeviceType.Sensor.ToString(), channel.Type, StringComparison.InvariantCultureIgnoreCase))
                        {
                            MAX_SUBDEVICE_COUNT = MAX_SENSOR_COUNT;
                            AddChannelButtonVisibility = Visibility.Visible;
                            DeleteChannelBtnVisibility = Visibility.Visible;
                            SensorDeviceViewModel = new SubDeviceViewModel();
                            SensorDeviceViewModel.SupportedProtocolTypeList = new List<string>(channel.SupportedProtocolTypeList);
                            subDeviceVM.CurrentSubDeviceTypeItem.DeviceType = DeviceType.Sensor;
                            subDeviceVM.CurrentSubDeviceTypeItem.DeviceTypeDescript = "";
                            break;
                        }
                        else if (channel.Type.Equals(DeviceType.DCCamera.ToString()))
                        {
                            subDeviceVM.CurrentSubDeviceTypeItem.DeviceType = DeviceType.DCCamera;
                            subDeviceVM.CurrentSubDeviceTypeItem.DeviceTypeDescript = I18n.Properties.Resources.DCCamera;
                            subDeviceVM.Protocol = channel.SupportedProtocolTypeList.FirstOrDefault();
                            subDeviceVM.Port = Constants.ONVIF_DC_DEFAULT_PORT;
                        }
                        else if (channel.Type.Equals(DeviceType.ACCamera.ToString()))
                        {
                            subDeviceVM.CurrentSubDeviceTypeItem.DeviceType = DeviceType.ACCamera;
                            subDeviceVM.CurrentSubDeviceTypeItem.DeviceTypeDescript = I18n.Properties.Resources.ACCamera;
                            subDeviceVM.Protocol = channel.SupportedProtocolTypeList.FirstOrDefault();
                            subDeviceVM.Port = Constants.EDGE_BOX_DEFAULT_PORT;
                        }
                        subDeviceVM.SupportedProtocolTypeList = new List<string>(channel.SupportedProtocolTypeList);
                        subDeviceVM.ChannelID = channel.ID;

                        templateList.Add(subDeviceVM);
                    }
                }

                templateList.ForEach(it =>
                {
                    DeviceAddressDataObject deviceAddress = _deviceDO?.DeviceAddress?.FirstOrDefault(x => x.Type.Equals(it.CurrentSubDeviceTypeItem.DeviceType.ToString()));
                    if (deviceAddress != null)
                    {
                        SubDeviceViewModel subDeviceVM = new SubDeviceViewModel(deviceAddress);
                        subDeviceVM.SupportedProtocolTypeList = it.SupportedProtocolTypeList;
                        if (SensorDeviceViewModel != null)
                        {
                            subDeviceVM.CopyFrom(SensorDeviceViewModel);
                        }
                        SubDeviceList.Add(subDeviceVM);
                    }
                    else
                    {
                        if (SensorDeviceViewModel != null)
                        {
                            it.CopyFrom(SensorDeviceViewModel);
                        }
                        SubDeviceList.Add(it);
                    }
                });
            }

            AddChannelButtonEnabled = SubDeviceList.Count < MAX_SUBDEVICE_COUNT;
            IsSaveEnabled = SubDeviceList.Count > 0;
            if (_deviceDO != null)
            {
                await LoadSubDeviceStatus();
            }
        }

        public async Task LoadSubDeviceStatus()
        {
            DeviceParamDataObject deviceParamDO = await DeviceManager.Instance.GetDeviceParam(_deviceDO);
            if (deviceParamDO == null)
            {
                return;
            }

            foreach (SubDeviceViewModel subDevice in SubDeviceList)
            {
                DeviceModelDataObject deviceModel = deviceParamDO.DeviceModel?.FirstOrDefault(x => x.ChannelID == subDevice.ChannelID);

                if (deviceModel == null)
                {
                    continue;
                }

                subDevice.LoadStatus(deviceModel);
            }
        }

        public void DiscoverNewCamera(List<CameraDiscoveryDO> discoveryDOs)
        {
            foreach (CameraDiscoveryDO discoveryDO in discoveryDOs)
            {
                _discoveryCameraVMs.Add(new DiscoveryCameraViewModel(discoveryDO));
            }
        }

        internal DeviceDO ConvertToDataObject()
        {
            List<SubDeviceViewModel> channelsToSave = SubDeviceList?.ToList() ?? new List<SubDeviceViewModel>();
            string protocolType = _selectedProtocolType;

            if (IsFireMonitorAgentSelected())
            {
                // Always derive S/D from visible-light IP (ignore hidden ComboBox selection).
                protocolType = ResolveFireMonitorProtocolFromChannels();
                channelsToSave = channelsToSave.Where(IsChannelIpFilled).ToList();
                for (int i = 0; i < channelsToSave.Count; i++)
                {
                    channelsToSave[i].ChannelID = i;
                }
            }

            DeviceDO deviceDO = new DeviceDO
            {
                ID = _deviceDO == null ? string.Empty : _deviceDO.ID,
                Name = _name,
                ProtocolType = protocolType,
                DeviceAddress = new List<DeviceAddressDataObject>(channelsToSave.Select(it => it.ConvertToDataObject())),
                AgentAddress = new List<ServerDO> { _selectedAgentVM.RawData }
            };
            if (_selectedStoreVM.RawData != null)
            {
                deviceDO.AgentAddress.Add(_selectedStoreVM.RawData);
            }

            return deviceDO;
        }

        #endregion
    }

    public class AgentViewModel
    {
        public AgentViewModel()
        {
            Address = I18n.Properties.Resources.None;
        }

        public AgentViewModel(ServerDO serverDO)
        {
            ID = serverDO.ID;
            Enum.TryParse(serverDO.Type, out ServerType serverType);
            Type = serverType;
            IP = serverDO.IP;
            Port = serverDO.Port;
            RawData = serverDO;
            Address = $"{IP}:{Port}";
        }

        public int ID { get; set; }

        public ServerType Type { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public string Address { get; private set; }

        public ServerDO RawData { get; set; }
    }

    public class SubDeviceViewModel : BaseViewModel
    {
        #region Fields

        private SubDeviceTypeItemViewModel _currentSubDeviceTypeItem;
        private string _protocol;
        private string _ip;
        private int _port = Constants.LANTERN_DEFAULT_PORT;
        private string _mode;
        private string _sn;
        private string _fireware;
        private string _username;
        private string _password;
        private string _paramID;
        private string _channelDesc;
        private List<string> _supportedProtocolTypeList;

        #endregion Fields

        #region Constructors

        public SubDeviceViewModel()
        {
            DeviceTypes = new List<SubDeviceTypeItemViewModel>
            {
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.IRCamera, DeviceTypeDescript =I18n.Properties.Resources.IRCamera},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.DCCamera, DeviceTypeDescript =I18n.Properties.Resources.DCCamera},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.EnvTempSensor, DeviceTypeDescript =I18n.Properties.Resources.EnvTempSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.EnvHumSensor, DeviceTypeDescript =I18n.Properties.Resources.EnvHumSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.SSensor, DeviceTypeDescript =I18n.Properties.Resources.SSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.VSensor, DeviceTypeDescript =I18n.Properties.Resources.VSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.ASensor, DeviceTypeDescript =I18n.Properties.Resources.ASensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.TempSensor, DeviceTypeDescript =I18n.Properties.Resources.TempSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.NoiseSensor, DeviceTypeDescript =I18n.Properties.Resources.NoiseSensor },
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.TcSensor, DeviceTypeDescript =I18n.Properties.Resources.TcSensor },
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.ACCamera, DeviceTypeDescript =I18n.Properties.Resources.ACCamera }
            };
            CurrentSubDeviceTypeItem = DeviceTypes[0];
        }

        public SubDeviceViewModel(DeviceAddressDataObject model)
        {
            DeviceTypes = new List<SubDeviceTypeItemViewModel>
            {
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.IRCamera, DeviceTypeDescript =I18n.Properties.Resources.IRCamera},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.DCCamera, DeviceTypeDescript =I18n.Properties.Resources.DCCamera},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.EnvTempSensor, DeviceTypeDescript =I18n.Properties.Resources.EnvTempSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.EnvHumSensor, DeviceTypeDescript =I18n.Properties.Resources.EnvHumSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.SSensor, DeviceTypeDescript =I18n.Properties.Resources.SSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.VSensor, DeviceTypeDescript =I18n.Properties.Resources.VSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.ASensor, DeviceTypeDescript =I18n.Properties.Resources.ASensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.TempSensor, DeviceTypeDescript =I18n.Properties.Resources.TempSensor},
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.NoiseSensor, DeviceTypeDescript =I18n.Properties.Resources.NoiseSensor },
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.TcSensor, DeviceTypeDescript =I18n.Properties.Resources.TcSensor },
                new SubDeviceTypeItemViewModel{ DeviceType = DeviceType.ACCamera, DeviceTypeDescript =I18n.Properties.Resources.ACCamera }
            };

            if (string.Equals(model.Type, DeviceType.IRCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[0];
            }
            else if (string.Equals(model.Type, DeviceType.DCCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[1];
            }
            else if (string.Equals(model.Type, DeviceType.EnvTempSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[2];
            }
            else if (string.Equals(model.Type, DeviceType.EnvHumSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[3];
            }
            else if (string.Equals(model.Type, DeviceType.SSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[4];
            }
            else if (string.Equals(model.Type, DeviceType.VSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[5];
            }
            else if (string.Equals(model.Type, DeviceType.ASensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[6];
            }
            else if (string.Equals(model.Type, DeviceType.TempSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[7];
            }
            else if (string.Equals(model.Type, DeviceType.NoiseSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[8];
            }
            else if (string.Equals(model.Type, DeviceType.TcSensor.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[9];
            }
            else if (string.Equals(model.Type, DeviceType.ACCamera.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                CurrentSubDeviceTypeItem = DeviceTypes[10];
            }
            Init(model);
        }

        #endregion Constructors

        #region Methods

        private void Init(DeviceAddressDataObject model)
        {
            _protocol = model.ProtocolType;
            _ip = model.IP;
            _port = model.Port;
            _mode = model.Model;
            _sn = model.SN;
            _fireware = model.FirewareVersion;
            _username = model.Username;
            _password = model.Password;
            _paramID = model.ParamID;
            ChannelID = model.ChannelID;
            ChannelDescription = CurrentSubDeviceTypeItem.DeviceTypeDescript;
        }

        public void CopyFrom(SubDeviceViewModel model)
        {
            CurrentSubDeviceTypeItem = DeviceTypes.FirstOrDefault(it => it.DeviceType == model.CurrentSubDeviceTypeItem.DeviceType);
            Protocol = model._protocol;
            IP = model._ip;
            Port = model._port;
            Mode = model._mode;
            Sn = model._sn;
            Fireware = model._fireware;
            Username = model._username;
            Password = model._password;
            ChannelID = model.ChannelID;
            ParamID = model.ParamID;
            SupportedProtocolTypeList = model.SupportedProtocolTypeList;
            ChannelDescription = model.CurrentSubDeviceTypeItem.DeviceTypeDescript;
        }

        public void CopyFrom(DiscoveryCameraViewModel discoveryCamera)
        {
            IP = discoveryCamera.IP;
            Port = discoveryCamera.Port;
            Mode = discoveryCamera.Model;
            switch (discoveryCamera.DeviceType)
            {
                case DeviceType.IRCamera:
                    CurrentSubDeviceTypeItem = new SubDeviceTypeItemViewModel { DeviceType = DeviceType.IRCamera, DeviceTypeDescript = I18n.Properties.Resources.IRCamera };
                    break;
                case DeviceType.DCCamera:
                    CurrentSubDeviceTypeItem = new SubDeviceTypeItemViewModel { DeviceType = DeviceType.DCCamera, DeviceTypeDescript = I18n.Properties.Resources.DCCamera };
                    break;
                case DeviceType.ASensor:
                    CurrentSubDeviceTypeItem = new SubDeviceTypeItemViewModel { DeviceType = DeviceType.DCCamera, DeviceTypeDescript = I18n.Properties.Resources.DCCamera };
                    break;
            }
        }

        public DeviceAddressDataObject ConvertToDataObject()
        {
            if (string.IsNullOrWhiteSpace(ParamID))
            {
                ParamID = "0";
            }
            return new DeviceAddressDataObject
            {
                ChannelID = ChannelID,
                Type = CurrentSubDeviceTypeItem.DeviceType.ToString(),
                ProtocolType = Protocol,
                IP = IP,
                Port = Port,
                Username = Username,
                Password = Password,
                ParamID = ParamID
            };
        }

        internal void LoadStatus(DeviceModelDataObject status)
        {
            Mode = status.Model;
            Sn = status.SeriesNO;
            Fireware = status.FirmwareVersion;
        }

        #endregion Methods

        #region Properties

        public List<SubDeviceTypeItemViewModel> DeviceTypes { get; set; }
        public int ChannelID { get; set; }

        public SubDeviceTypeItemViewModel CurrentSubDeviceTypeItem
        {
            get => _currentSubDeviceTypeItem;
            set
            {
                _currentSubDeviceTypeItem = value;
                OnPropertyChanged();
            }
        }

        public List<string> SupportedProtocolTypeList
        {
            get => _supportedProtocolTypeList;
            set
            {
                _supportedProtocolTypeList = value;
                OnPropertyChanged();
            }
        }

        public string Protocol
        {
            get => _protocol;
            set
            {
                _protocol = value;
                if (string.Equals(value, IRCameraType.IRTEKSDK_NET.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    _port = Constants.LANTERN_DEFAULT_PORT;
                }
                else if (string.Equals(value, IRCameraType.IRTEKSDK2_NET.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    _port = Constants.IRTEKSDK2_DEFAULT_PORT;
                }
                else if (string.Equals(value, IRCameraType.SVSDK_NET.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    _port = Constants.SV_DEFAULT_PORT;
                }
                else if (string.Equals(value, IRCameraType.CZF_ONVIF_IR_NET.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    _port = Constants.CZF_ONVIF_IR_DEFAULT_PORT;
                }
                else
                {
                    _port = Constants.ONVIF_DC_DEFAULT_PORT;
                }

                OnPropertyChanged(nameof(Port));
                OnPropertyChanged(nameof(Protocol));
            }
        }

        public string IP
        {
            get => _ip;
            set
            {
                _ip = value;
                OnPropertyChanged(nameof(IP));
            }
        }

        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        public string Mode
        {
            get => _mode?.Split(',')[0];
            set
            {
                _mode = value;
                OnPropertyChanged(nameof(Mode));
            }
        }

        public string Sn
        {
            get => _sn;
            set
            {
                _sn = value;
                OnPropertyChanged(nameof(Sn));
            }
        }

        public string Fireware
        {
            get => _fireware;
            set
            {
                _fireware = value;
                OnPropertyChanged(nameof(Fireware));
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public string ParamID
        {
            get => _paramID;
            set
            {
                _paramID = value;
                OnPropertyChanged();
            }
        }

        public string ChannelDescription
        {
            get => _channelDesc;
            set
            {
                _channelDesc = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties
    }

    public class DiscoveryCameraViewModel : BaseViewModel
    {
        private string _ip;
        private int _port;
        private string _model;
        private string _name;

        public DiscoveryCameraViewModel(CameraDiscoveryDO discoveryDO)
        {
            _ip = discoveryDO.Address.ToString();
            _model = discoveryDO.Model;
            _name = discoveryDO.Name;
            switch (discoveryDO.Model)
            {
                case Constants.ONVIF_MODEL_IR_LANTERN:
                    _port = Constants.LANTERN_DEFAULT_PORT;
                    DeviceType = DeviceType.IRCamera;
                    break;
                case Constants.ONVIF_MODEL_IR_SV:
                    _port = Constants.SV_DEFAULT_PORT;
                    DeviceType = DeviceType.IRCamera;
                    break;
                case Constants.ONVIF_MODEL_IR_CZF:
                    _port = Constants.CZF_ONVIF_IR_DEFAULT_PORT;
                    DeviceType = DeviceType.IRCamera;
                    break;
                default:
                    _port = discoveryDO.Port;
                    DeviceType = DeviceType.DCCamera;
                    break;
            }
        }

        public string IP { get => _ip; set { _ip = value; OnPropertyChanged(); } }

        public int Port { get => _port; set { _port = value; OnPropertyChanged(); } }

        public string Model { get => _model; set { _model = value; OnPropertyChanged(); } }

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        public DeviceType DeviceType { get; set; }
    }

    public class SubDeviceTypeItemViewModel
    {
        public DeviceType DeviceType { get; set; }
        public string DeviceTypeDescript { get; set; }
    }
}