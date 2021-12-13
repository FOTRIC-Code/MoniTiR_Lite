// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Firefly.Client.Service;
using Firefly.Client.Service.Abstractions;

namespace Firefly.Client.Control.Fire
{
    internal class FireAgentDeviceManager
    {
        private static FireAgentDeviceManager fireAgentDM = new FireAgentDeviceManager();

        private FireAgentDataObject _fireAgentDO;

        private DeviceDO _deviceDO;
        private DeviceParamDataObject _deviceParamDO;

        public static FireAgentDeviceManager Instance => fireAgentDM;

        private FireAgentDeviceManager() { }

        public void SetDeviceDO(DeviceDO deviceDO)
        {
            _deviceDO = deviceDO;
        }

        public void SetDeviceParamDO(DeviceParamDataObject deviceParamDO)
        {
            _deviceParamDO = deviceParamDO;
        }

        public DeviceDO GetDeviceDO()
        {
            return _deviceDO;
        }

        public DeviceParamDataObject GetDeviceParamDO()
        {
            return _deviceParamDO;
        }

        #region requests to agent server

        public async Task<FireAgentDataObject> GetDeviceConfig()
        {
            _fireAgentDO = await DeviceManager.Instance.GetDeviceConfig<FireAgentDataObject>(_deviceDO)
                ?? new FireAgentDataObject();
            return _fireAgentDO;
        }

        public async Task<ApiResult<bool>> UpdateDeviceConfig(FireAgentDataObject fireAgentDO)
        {
            if (UseInProcFire)
            {
                return ToApiResult(await FireRuntime.UpdateDeviceConfigAsync(_deviceDO, fireAgentDO).ConfigureAwait(false));
            }

            return await IRTekHttpClient.SendAsync<bool>(HttpMethod.Put, $"{_deviceDO.DefaultAgent().GetServerAddress()}/api/service/device/{_deviceDO.ID}/conf", fireAgentDO);
        }

        public async Task<ApiResult<bool>> OverriedDeviceConfig(DeviceDO deviceDO, DeviceOverrideDataObject deviceOverrideDataObject)
        {
            if (UseInProcFire)
            {
                return ToApiResult(await FireRuntime.OverrideDeviceConfigAsync(deviceDO, deviceOverrideDataObject).ConfigureAwait(false));
            }

            return await IRTekHttpClient.SendAsync<bool>(HttpMethod.Put, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/service/device/conf/override", deviceOverrideDataObject);
        }

        private static bool UseInProcFire =>
            FireRuntimeServices.Mode == FireHostMode.InProcess;

        private static IFireDeviceRuntime FireRuntime => FireRuntimeServices.DeviceRuntime;

        private static ApiResult<bool> ToApiResult(bool ok)
        {
            return new ApiResult<bool>(ok, ok ? ResponseCode.SUCCESS : ResponseCode.FAILED);
        }

        #endregion

        #region get local data

        internal bool LoadOSDConfigDataObject()
        {
            return _fireAgentDO?.OSDConfigEnabled ?? false;
        }

        internal ChannelConfigDataObject LoadChannelConfigDataObject()
        {
            return _fireAgentDO?.ChannelConfig ?? new ChannelConfigDataObject();
        }

        internal PTZOSDConfigDataObject LoadPTZOSDConfigDataObject()
        {
            return _fireAgentDO?.PTZOSDConfig ?? new PTZOSDConfigDataObject();
        }

        internal MarkerOSDConfigDataObject LoadMarkerOSDConfigDataObject()
        {
            return _fireAgentDO?.MarkerOSDConfig ?? new MarkerOSDConfigDataObject();
        }

        internal GeneralConfigDataObject LoadGeneralConfigDataObject()
        {
            return _fireAgentDO?.GeneralConfig ?? new GeneralConfigDataObject();
        }

        internal CruiseRouteDataObject LoadCruiseRouteDataObject()
        {
            if (_fireAgentDO?.CruiseRouteConfig != null)
            {
                _fireAgentDO.CruiseRouteConfig.CruiseRouteList.Sort((x, y) => x.ID.CompareTo(y.ID));
            }
            return _fireAgentDO?.CruiseRouteConfig ?? new CruiseRouteDataObject();
        }

        internal TaskScheduleDataObject LoadTaskScheduleDataObject()
        {
            return _fireAgentDO?.TaskSchedule ?? new TaskScheduleDataObject();
        }

        internal AlarmScheduleDataObject LoadAlarmScheduleDataObject()
        {
            return _fireAgentDO?.AlarmSchedule ?? new AlarmScheduleDataObject();
        }

        internal EventTriggerDataObject LoadEventTriggerDataObject()
        {
            return _fireAgentDO?.EventTrigger ?? new EventTriggerDataObject();
        }

        internal DevicePresetDataObject LoadPresetListDataObject()
        {
            if (_fireAgentDO?.PresetConfig != null)
            {
                if (_fireAgentDO.PresetConfig.PresetList == null)
                {
                    _fireAgentDO.PresetConfig.PresetList = new List<PresetDataObject>();
                }
                _fireAgentDO.PresetConfig.PresetList.Sort((x, y) => x.ID.CompareTo(y.ID));
            }

            return _fireAgentDO?.PresetConfig ?? new DevicePresetDataObject();
        }

        internal AlarmRuleDataObject LoadAlarmRuleDataObject()
        {
            return _fireAgentDO?.AlarmRuleConfig ?? new AlarmRuleDataObject();
        }

        private void EnsureFireAgentDO()
        {
            if (_fireAgentDO == null)
            {
                _fireAgentDO = new FireAgentDataObject();
            }
        }

        internal void UpdateOSDConfig(bool osdConfigEnabled)
        {
            EnsureFireAgentDO();
            _fireAgentDO.OSDConfigEnabled = osdConfigEnabled;
        }

        internal void UpdateChannelConfig(ChannelConfigDataObject channelConfigDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.ChannelConfig = channelConfigDataObject;
        }

        internal void UpdatePTZOSDConfig(PTZOSDConfigDataObject ptzOSDConfigDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.PTZOSDConfig = ptzOSDConfigDataObject;
        }

        internal void UpdateMarkerOSDConfig(MarkerOSDConfigDataObject markerOSDConfigDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.MarkerOSDConfig = markerOSDConfigDataObject;
        }

        internal void UpdateGeneralConfig(GeneralConfigDataObject generalConfigDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.GeneralConfig = generalConfigDataObject;
        }

        internal void UpdateAlarmScheduleDataObject(AlarmScheduleDataObject alarmScheduleDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.AlarmSchedule = alarmScheduleDataObject;
        }

        internal void UpdateCruiseRouteDataObject(CruiseRouteDataObject cruiseRouteDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.CruiseRouteConfig = cruiseRouteDataObject;
        }

        internal void UpdateEventTriggerDataObject(EventTriggerDataObject eventTriggerDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.EventTrigger = eventTriggerDataObject;
        }

        internal void UpdateTaskScheduleDataObject(TaskScheduleDataObject taskScheduleDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.TaskSchedule = taskScheduleDataObject;
        }

        internal void UpdatePresetListDataObject(DevicePresetDataObject presetListDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.PresetConfig = presetListDataObject;
        }

        internal void UpdateAlarmRuleDataObject(AlarmRuleDataObject alarmRuleDataObject)
        {
            EnsureFireAgentDO();
            _fireAgentDO.AlarmRuleConfig = alarmRuleDataObject;
        }

        internal void Dispose()
        {
            _fireAgentDO = null;
        }
        #endregion
    }
}
