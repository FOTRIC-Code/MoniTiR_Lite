// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Common;
using Firefly.Client.Service.LiteDb;
using log4net;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Firefly.Client.Service
{
    public class DeviceManager
    {
        private const int MAX_DATA_EVENT_MSG_COUNT = 2000;
        private static DeviceManager deviceManager = new DeviceManager();

        public static DeviceManager Instance => deviceManager;

        public string ServerURL { get; set; }

        private ConcurrentDictionary<string, DeviceDO> _deviceDict = new ConcurrentDictionary<string, DeviceDO>();

        private bool _updateFlag = false;
        private long _isUpdating = 0;
        private long _isUpdateDeviceStatus = 0;
        private long _threadCount = 0;

        private List<EventMessageBO> _dataEventMessageList = new List<EventMessageBO>();
        private List<EventMessageBO> _defectDataEventMessageList = new List<EventMessageBO>();

        private ILog _logger = LogManager.GetLogger(typeof(DeviceManager));
        private long _startPage = Constants.PAGE_NO_DEFAULT;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private static ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private IDisposable _inProcEventSub;

        private static bool UseInProcFire =>
            Firefly.Client.Service.Abstractions.FireRuntimeServices.Mode
            == Firefly.Client.Service.Abstractions.FireHostMode.InProcess;

        private static Firefly.Client.Service.Abstractions.IFireDeviceRuntime FireRuntime =>
            Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime;

        // The HTTP route took channelID as a query param the Client never sent, so it defaulted to 0.
        private const int DEFAULT_OSD_CHANNEL = 0;

        private static ApiResult<bool> ToApiResult(bool ok)
        {
            return new ApiResult<bool>(ok, ok ? ResponseCode.SUCCESS : ResponseCode.FAILED);
        }

        private DeviceManager()
        {
            Thread thread = new Thread(new ThreadStart(DoStart));
            thread.Start();
        }

        public int Count => _deviceDict.Count;

        public void Start()
        {
            _manualResetEvent.Set();
        }

        public void Stop()
        {
            _manualResetEvent.Reset();
            _deviceDict.Clear();
            InvalidDeviceState();
        }

        private async void DoStart()
        {
            // 跟ServerManager错开一下，避免集中请求API
            Thread.Sleep(1000);
            while (!_updateFlag)
            {
                _manualResetEvent.WaitOne();
                try
                {
                    if (DateTime.Now - _lastUpdateTime < new TimeSpan(0, 0, Constants.DEVICE_STATUS_CHECK_DELAY))
                    {
                        continue;
                    }

                    _lastUpdateTime = DateTime.Now;

                    //更新设备列表
                    await InternalUpdateAllDevice();
                    //更新设备订阅信息
                    await InternalUpdateDeviceSubscribe();
                    //更新设备状态信息
                    await InternalUpdateDeviceState();
                }
                finally
                {
                    Thread.Sleep(5 * 100);
                }
            }
        }

        public void InvalidDeviceState()
        {
            _lastUpdateTime = DateTime.MinValue;
        }

        private async Task InternalUpdateDeviceSubscribe()
        {
            try
            {
                List<DeviceDO> deviceDOs = _deviceDict.Values.ToList();
                if (deviceDOs != null)
                {
                    List<Tuple<int, string>> resultList = new List<Tuple<int, string>>();
                    foreach (DeviceDO deviceDO in deviceDOs)
                    {
                        ServerType serverType = ServerType.None;
                        if (string.Equals(deviceDO.DefaultAgent().Type, ServerType.EquipmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            serverType = ServerType.EquipmentMonitorAgent;
                        }
                        else if (string.Equals(deviceDO.DefaultAgent().Type, ServerType.FireMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            serverType = ServerType.FireMonitorAgent;
                        }
                        else if (string.Equals(deviceDO.DefaultAgent().Type, ServerType.EnvironmentMonitorAgent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            serverType = ServerType.EnvironmentMonitorAgent;
                        }
                        if (serverType != ServerType.None)
                        {
                            // In-process Fire: no SignalR hub — events arrive via IFireCallbackDispatcher.
                            if (UseInProcFire && serverType == ServerType.FireMonitorAgent)
                            {
                                EnsureInProcEventSubscription();
                                HubConnectionBO fireHub = ServerManager.Instance.TryGetOrAddHubConnectionBO(deviceDO.DefaultAgent().ID);
                                if (fireHub.EventNotifyCallback == null)
                                {
                                    fireHub.EventNotifyCallback = EventNotifyHandler;
                                }

                                if (!fireHub.SubscribedDeviceIDs.Any(it => string.Equals(it, deviceDO.ID)))
                                {
                                    fireHub.SubscribedDeviceIDs.Add(deviceDO.ID);
                                }

                                resultList.Add(new Tuple<int, string>(ResponseCode.SUCCESS, ""));
                                continue;
                            }

                            HubConnectionBO hubConnectionBO = ServerManager.Instance.TryGetOrAddHubConnectionBO(deviceDO.DefaultAgent().ID);

                            if (hubConnectionBO.HubConnection == null)
                            {
                                ServerManager.Instance.InvalidServerStatus();
                                int retry = Constants.SIGNALR_CONNECTED_RETRY_COUNT;
                                while (retry-- > 0)
                                {
                                    if (hubConnectionBO.HubConnection != null)
                                    {
                                        break;
                                    }

                                    Thread.Sleep(Constants.SIGNALR_CONNECTED_RETRY_DELAY);
                                }
                            }

                            if (hubConnectionBO.HubConnection == null || hubConnectionBO.HubConnection.State != HubConnectionState.Connected)
                            {
                                continue;
                            }

                            if (hubConnectionBO.EventNotifyCallback == null)
                            {
                                hubConnectionBO.EventNotifyCallback = EventNotifyHandler;
                            }

                            if (hubConnectionBO.SubscribedDeviceIDs.Any(it => string.Equals(it, deviceDO.ID)))
                            {
                                resultList.Add(new Tuple<int, string>(ResponseCode.SUCCESS, ""));
                                continue;
                            }
                            ApiResult<bool> result = await Subscribe(hubConnectionBO.ServerDO.GetServerAddress(), serverType, deviceDO.ID, hubConnectionBO.HubConnection.ConnectionId);
                            int code = result.Code;
                            if (code == ResponseCode.EQUIPMENT_EVENT_DEVICE_NOT_EXISTED)
                            {
                                resultList.Add(new Tuple<int, string>(code, $"{deviceDO.Name} EQUIPMENT_EVENT_DEVICE_NOT_EXISTED"));
                                continue;
                            }

                            if (code == ResponseCode.EQUIPMENT_EVENT_DEVICE_OFFLINE)
                            {
                                resultList.Add(new Tuple<int, string>(code, $"{deviceDO.Name} EQUIPMENT_EVENT_DEVICE_OFFLINE"));
                                continue;
                            }

                            if (code == ResponseCode.EQUIPMENT_EVENT_SUBSCRIBE_INVALID_EVENT)
                            {
                                // todo 核对版本限制的原因
                                resultList.Add(new Tuple<int, string>(code, $"{deviceDO.Name} EQUIPMENT_EVENT_SUBSCRIBE_INVALID_EVENT，当前EventVersion不支持"));
                                continue;
                            }

                            if (code == ResponseCode.EQUIPMENT_EVENT_CONNECTION_NOT_EXISTED)
                            {
                                resultList.Add(new Tuple<int, string>(code, $"{deviceDO.Name} EQUIPMENT_EVENT_CONNECTION_NOT_EXISTED，SignalR提示连接上了，但是服务器没有这条连接"));
                                ServerManager.Instance.TryRemoveConnectionBO(deviceDO.DefaultAgent().ID);
                                continue;
                            }

                            if (code == ResponseCode.EQUIPMENT_EVENT_SUBSCRIBE_FAILED)
                            {
                                resultList.Add(new Tuple<int, string>(code, $"{deviceDO.Name} EQUIPMENT_EVENT_SUBSCRIBE_FAILED"));
                                continue;
                            }

                            if (code == ResponseCode.FIRE_EVENT_DEVICE_OFFLINE)
                            {
                                resultList.Add(new Tuple<int, string>(code, $"{deviceDO.Name} FIRE_EVENT_DEVICE_OFFLINE"));
                                continue;
                            }

                            if (code == ResponseCode.SUCCESS)
                            {
                                hubConnectionBO.SubscribedDeviceIDs.Add(deviceDO.ID);
                                resultList.Add(new Tuple<int, string>(code, ""));
                                continue;
                            }
                            resultList.Add(new Tuple<int, string>(code, $"{deviceDO.Name} {code}"));
                        }
                    }

                    int successCount = resultList.Where(it => it.Item1 == 0).Count();
                    string msg = $"Device SignalR State, Total Count: {deviceDOs.Count}, Need SignalR: {resultList.Count}, Connected Count: {successCount}";
                    List<string> msgList = resultList.Where(it => it.Item1 != 0).Select(it => it.Item2).ToList();
                    if (msgList.Count > 0)
                    {
                        msg += ",";
                        foreach (string str in msgList)
                        {
                            msg += $" [{str}]";
                        }
                    }
                    _logger.Info(msg);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
            finally
            {
                Interlocked.Exchange(ref _isUpdating, 0);
            }
        }

        private async Task InternalUpdateDeviceState()
        {
            List<ServerDO> serverDOs = await ServerManager.Instance.GetServerDOsAsync();
            List<DeviceStatusDataObject> allStatus = new List<DeviceStatusDataObject>();
            foreach (ServerDO serverDO in serverDOs)
            {
                if (serverDO.Status != (int)DeviceStatus.Online)
                {
                    continue;
                }
                List<DeviceStatusDataObject> statusList = await GetDeviceStatusList(serverDO);
                if (statusList == null)
                {
                    continue;
                }
                allStatus.AddRange(statusList);
            }
            List<DeviceDO> deviceList = GetCachedDeviceList();
            foreach (DeviceDO deviceDO in deviceList)
            {
                DeviceStatusDataObject deviceStatusDO = allStatus.FirstOrDefault(it => string.Equals(deviceDO.ID, it.ID));
                if (deviceStatusDO != null)
                {
                    deviceDO.SetDeviceStatus(deviceStatusDO);
                }
                else
                {
                    deviceDO.SetDeviceStatus(new DeviceStatusDataObject
                    {
                        Status = DeviceStatus.Offline,
                        ID = deviceDO.ID,
                    });
                }
            }
        }

        private async Task InternalUpdateAllDevice()
        {
            List<DeviceDO> tempDeviceDOList = new List<DeviceDO>();
            while (true)
            {
                try
                {
                    int page = (int)Interlocked.Read(ref _startPage);
                    PaginationData<List<DeviceDO>> paginationData = await GetDeviceList(new PaginationInfo { Page = page, PageSize = Constants.PAGE_SIZE_100 });

                    if (paginationData.Data != null)
                    {
                        List<DeviceDO> deviceDOList = paginationData.Data;
                        tempDeviceDOList.AddRange(deviceDOList);
                    }
                    if (paginationData.Data == null || paginationData.TotalCount == 0)
                    {
                        Interlocked.Exchange(ref _startPage, Constants.PAGE_NO_DEFAULT);
                        break;
                    }
                    if (paginationData.Data.Count == Constants.PAGE_SIZE_100)
                    {
                        Interlocked.Increment(ref _startPage);
                    }
                    if (paginationData.Data.Count < Constants.PAGE_SIZE_100)
                    {
                        Interlocked.Exchange(ref _startPage, Constants.PAGE_NO_DEFAULT);
                        break;
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.ToString());
                }
            }

            List<string> oldDeviceIDList = _deviceDict.Keys.ToList();
            List<string> newDeviceIDList = tempDeviceDOList.Select(it => it.ID).ToList();
            oldDeviceIDList.ForEach(oldID =>
            {
                if (newDeviceIDList.All(it => it != oldID))
                {
                    _deviceDict.TryRemove(oldID, out _);
                }
            });

            foreach (DeviceDO deviceDO in tempDeviceDOList)
            {
                if (!_deviceDict.TryAdd(deviceDO.ID, deviceDO))
                {
                    _deviceDict.TryRemove(deviceDO.ID, out DeviceDO oldDeviceDO);
                    deviceDO.SetDeviceStatus(new DeviceStatusDataObject
                    {
                        Status = oldDeviceDO.IsOnline() ? DeviceStatus.Online : DeviceStatus.Offline,
                        StatusDescription = oldDeviceDO.GetDeviceStatusDesc(),
                    });
                    _deviceDict.TryAdd(deviceDO.ID, deviceDO);
                }
            }
        }

        private void EnsureInProcEventSubscription()
        {
            if (_inProcEventSub != null)
            {
                return;
            }

            _inProcEventSub = Firefly.Client.Service.Abstractions.FireRuntimeServices.CallbackDispatcher
                .SubscribeEvents(EventNotifyHandler);
        }

        private void EventNotifyHandler(int serverID, string message)
        {
            EventMessageBO eventMessage = JsonConvert.DeserializeObject<EventMessageBO>(message);

            if (eventMessage == null)
            {
                return;
            }
            switch (eventMessage.EventType)
            {
                case EventType.Fire_DataEvent:
                case EventType.Equipment_DataEvent:
                case EventType.Environment_DataEvent:
                    Monitor.Enter(_dataEventMessageList);
                    while (_dataEventMessageList.Count >= MAX_DATA_EVENT_MSG_COUNT)
                    {
                        _dataEventMessageList.RemoveAt(0);
                    }
                    eventMessage.ServerID = serverID;
                    _dataEventMessageList.Add(eventMessage);
                    Monitor.Exit(_dataEventMessageList);
                    break;

                case EventType.Fire_AlarmEvent:
                case EventType.Equipment_AlarmEvent:
                case EventType.Environment_AlarmEvent:
                    break;

                case EventType.Equipment_AlarmInputEvent:
                    break;

                case EventType.Equipment_DeviceAlarmIOEvent:
                    break;

                case EventType.Equipment_CruiseStatusEvent:
                    break;

                case EventType.Equipment_CruisePositionEvent:
                    break;

                case EventType.Equipment_DefectEvent:
                    Monitor.Enter(_defectDataEventMessageList);
                    while (_defectDataEventMessageList.Count >= MAX_DATA_EVENT_MSG_COUNT)
                    {
                        _defectDataEventMessageList.RemoveAt(0);
                    }
                    eventMessage.ServerID = serverID;
                    _defectDataEventMessageList.Add(eventMessage);
                    Monitor.Exit(_defectDataEventMessageList);
                    break;

                case EventType.PreviewException:
                    break;

                case EventType.PreviewReconnected:
                    break;

                case EventType.Fire_CruiseStatusEvent:
                    break;

                case EventType.Fire_CruisePositionEvent:
                    break;

                case EventType.Fire_AlarmInputEvent:
                    break;

                case EventType.Fire_DeviceAlarmIOEvent:
                    break;

                case EventType.Fire_PreviewException:
                    _logger.Warn(eventMessage.Event);
                    break;

                case EventType.Fire_PreviewReconnected:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public List<EventMessageBO> GetDataEventMsgRange(EventMessageBO indexItem, int millisecondsTimeout)
        {
            if (Monitor.TryEnter(_dataEventMessageList, millisecondsTimeout))
            {
                try
                {
                    int index = 0;
                    if (indexItem != null && _dataEventMessageList.Contains(indexItem))
                    {
                        index = _dataEventMessageList.IndexOf(indexItem);
                    }

                    if (index == 0)
                    {
                        return _dataEventMessageList.ToList();
                    }

                    if (index == -1 || index + 1 >= _dataEventMessageList.Count)
                    {
                        return new List<EventMessageBO>();
                    }

                    return _dataEventMessageList.Skip(index + 1).ToList();
                }
                finally
                {
                    Monitor.Exit(_dataEventMessageList);
                }
            }

            return new List<EventMessageBO>();
        }

        public List<EventMessageBO> GetDefectDataEventMsgRange(EventMessageBO indexItem, int millisecondsTimeout)
        {
            if (Monitor.TryEnter(_defectDataEventMessageList, millisecondsTimeout))
            {
                try
                {
                    int index = 0;
                    if (indexItem != null && _defectDataEventMessageList.Contains(indexItem))
                    {
                        index = _defectDataEventMessageList.IndexOf(indexItem);
                    }

                    if (index == 0)
                    {
                        return _defectDataEventMessageList.ToList();
                    }

                    if (index == -1 || index + 1 >= _defectDataEventMessageList.Count)
                    {
                        return new List<EventMessageBO>();
                    }

                    return _defectDataEventMessageList.Skip(index + 1).ToList();
                }
                finally
                {
                    Monitor.Exit(_defectDataEventMessageList);
                }
            }

            return new List<EventMessageBO>();
        }

        public List<DeviceDO> GetCachedDeviceList(string searchText = "")
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return _deviceDict.Values.OrderBy(it => it.Name).ToList();
            }
            else
            {
                return _deviceDict.Values.Where(it => it.Name.Contains(searchText)).OrderBy(it => it.Name).ToList();
            }
        }

        public PaginationData<List<DeviceDO>> GetDeviceListWithSort(PaginationInfo pagination, bool ascStatus)
        {
            PaginationData<List<DeviceDO>> paginationData = new PaginationData<List<DeviceDO>>();
            List<DeviceDO> tempDeviceList = new List<DeviceDO>();
            if (string.IsNullOrWhiteSpace(pagination.SearchStr))
            {
                tempDeviceList = _deviceDict.Values.ToList();
            }
            else
            {
                tempDeviceList = _deviceDict.Values.Where(it => it.Name.Contains(pagination.SearchStr)).ToList();
            }
            paginationData.TotalCount = tempDeviceList.Count();
            if (ascStatus)
            {
                var onlineList = tempDeviceList.OrderBy(it => it.Name)
                    .Where(it => it.IsOnline()).ToList();
                var offlineList = tempDeviceList.OrderBy(it => it.Name)
                    .Where(it => !it.IsOnline()).ToList();
                paginationData.Data = onlineList.Concat(offlineList)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize).ToList();
            }
            else
            {
                var onlineList = tempDeviceList.OrderBy(it => it.Name)
                    .Where(it => it.IsOnline()).ToList();
                var offlineList = tempDeviceList.OrderBy(it => it.Name)
                    .Where(it => !it.IsOnline()).ToList();
                paginationData.Data = offlineList.Concat(onlineList)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize).ToList();
            }
            return paginationData;
        }

        public bool IsOnline(string id)
        {
            _deviceDict.TryGetValue(id, out DeviceDO deviceDO);
            return deviceDO?.IsOnline() ?? false;
        }

        #region local sqlite (+ temporary Fire sync)

        public Task<PaginationData<List<DeviceDO>>> GetDeviceList(PaginationInfo pagination)
        {
            var result = LiteDeviceRepository.GetByPagination(pagination);
            return Task.FromResult(new PaginationData<List<DeviceDO>>()
            {
                TotalCount = result.Total,
                Data = result.Items
            });
        }

        public DeviceDO GetCached(string id)
        {
            _deviceDict.TryGetValue(id, out DeviceDO deviceDO);
            return deviceDO;
        }

        public Task<DeviceDO> GetDevice(string id)
        {
            return Task.FromResult(LiteDeviceRepository.Get(id));
        }

        public async Task<bool> AddDevice(DeviceDO deviceDO)
        {
            EnsureDefaultAgents(deviceDO);
            if (string.IsNullOrWhiteSpace(deviceDO.ID))
            {
                deviceDO.ID = Guid.NewGuid().ToString("N");
            }

            if (UseInProcFire && IsFireDevice(deviceDO) && string.IsNullOrWhiteSpace(deviceDO.Conf))
            {
                deviceDO.Conf = LiteDb.LiteFireDeviceConfDefaults.CreateDefaultJson();
            }

            if (!LiteDeviceRepository.Upsert(deviceDO))
            {
                return false;
            }

            _deviceDict.AddOrUpdate(deviceDO.ID, deviceDO, (k, v) => deviceDO);
            await SyncDeviceToFireAsync(deviceDO, HttpMethod.Post).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> DeleteDevice(string id)
        {
            bool ok = LiteDeviceRepository.Delete(id);
            if (ok)
            {
                _deviceDict.TryRemove(id, out _);
                await SyncDeviceDeleteToFireAsync(id).ConfigureAwait(false);
            }
            return ok;
        }

        public async Task<bool> UpdateDevice(DeviceDO deviceDO)
        {
            EnsureDefaultAgents(deviceDO);
            if (!LiteDeviceRepository.Upsert(deviceDO))
            {
                return false;
            }

            if (!_deviceDict.TryAdd(deviceDO.ID, deviceDO))
            {
                _deviceDict.TryRemove(deviceDO.ID, out DeviceDO oldDeviceDO);
                deviceDO.SetDeviceStatus(new DeviceStatusDataObject
                {
                    Status = oldDeviceDO.IsOnline() ? DeviceStatus.Online : DeviceStatus.Offline,
                    StatusDescription = oldDeviceDO.GetDeviceStatusDesc(),
                });
                _deviceDict.TryAdd(deviceDO.ID, deviceDO);
            }

            await SyncDeviceToFireAsync(deviceDO, HttpMethod.Put).ConfigureAwait(false);
            return true;
        }

        public Task<List<DeviceDO>> GetDevicesConfigByAgentID(int agentID)
        {
            return Task.FromResult(LiteDeviceRepository.GetByAgentId(agentID));
        }

        private static void EnsureDefaultAgents(DeviceDO deviceDO)
        {
            if (deviceDO.AgentAddress == null || deviceDO.AgentAddress.Count == 0)
            {
                deviceDO.AgentAddress = LiteAgentRepository.GetAll();
            }
        }

        private async Task SyncDeviceToFireAsync(DeviceDO deviceDO, HttpMethod method)
        {
            try
            {
                await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                    .SyncDeviceAsync(deviceDO, method).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"Sync device to Fire error. id={deviceDO.ID}", ex);
            }
        }

        private async Task SyncDeviceDeleteToFireAsync(string id)
        {
            try
            {
                await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                    .SyncDeviceDeleteAsync(id).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"Sync delete device to Fire error. id={id}", ex);
            }
        }

        #endregion local sqlite (+ temporary Fire sync)

        #region AgentServer

        public async Task<T> GetDeviceConfig<T>(DeviceDO deviceDO)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                    .GetDeviceConfigAsync<T>(deviceDO).ConfigureAwait(false);
            }

            ApiResult<T> result = await IRTekHttpClient.SendAsync<T>(HttpMethod.Get, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/service/device/{deviceDO.ID}/conf");
            return result.Data;
        }

        // GET /api/Device/{deviceID}/DeviceParam
        public async Task<DeviceParamDataObject> GetDeviceParam(DeviceDO deviceDO)
        {
            if (deviceDO == null)
            {
                return new DeviceParamDataObject();
            }

            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                    .GetDeviceParamAsync(deviceDO).ConfigureAwait(false)
                    ?? new DeviceParamDataObject();
            }

            ApiResult<DeviceParamDataObject> result = await IRTekHttpClient.SendAsync<DeviceParamDataObject>(HttpMethod.Get, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/deviceParam");
            return result.Data;
        }

        // GET /api/Device/{deviceID}/Status
        public async Task<DeviceStatusDataObject> GetDeviceStatus(DeviceDO deviceDO)
        {
            DeviceStatusDataObject deviceStatusDO = new DeviceStatusDataObject();
            if (deviceDO == null)
            {
                return deviceStatusDO;
            }

            try
            {
                if (UseInProcFire && IsFireDevice(deviceDO))
                {
                    return await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                        .GetDeviceStatusAsync(deviceDO).ConfigureAwait(false)
                        ?? new DeviceStatusDataObject { ID = deviceDO.ID, Status = DeviceStatus.Offline };
                }

                ApiResult<DeviceStatusDataObject> result = await IRTekHttpClient.SendAsync<DeviceStatusDataObject>(HttpMethod.Get, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/status", null, Constants.TIMEOUT_REQUEST_REFRESH_DEVICE_STATUS);
                deviceStatusDO = result.Data;
            }
            catch (NetworkException)
            {
                deviceStatusDO = new DeviceStatusDataObject
                {
                    ID = deviceDO.ID,
                    Status = DeviceStatus.Offline
                };
            }
            finally
            {
            }

            return deviceStatusDO;
        }

        public async Task<List<DeviceStatusDataObject>> GetDeviceStatusList(ServerDO serverDO)
        {
            if (serverDO == null || serverDO.Status == (int)DeviceStatus.Offline)
            {
                return new List<DeviceStatusDataObject>();
            }

            if (UseInProcFire && string.Equals(serverDO.Type, ServerType.FireMonitorAgent.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                    .GetDeviceStatusListAsync(serverDO).ConfigureAwait(false)
                    ?? new List<DeviceStatusDataObject>();
            }

            // Store is in-process and has no /api/device/status HTTP endpoint.
            if (string.Equals(serverDO.Type, ServerType.StoreAgent.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return new List<DeviceStatusDataObject>();
            }

            ApiResult<List<DeviceStatusDataObject>> result = await IRTekHttpClient.SendAsync<List<DeviceStatusDataObject>>(HttpMethod.Get, $"{serverDO.GetServerAddress()}/api/device/status");
            if (result?.IsSuccess == true)
            {
                return result.Data;
            }
            return new List<DeviceStatusDataObject>();
        }

        // PUT /api/Device/{deviceID}/PTZPreset
        public async Task<ApiResult<bool>> SetPreset(DeviceDO deviceDO, PTZPresetDataObject ptzPresetDO)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return ToApiResult(await FireRuntime.SetPresetAsync(deviceDO, ptzPresetDO).ConfigureAwait(false));
            }

            return await IRTekHttpClient.SendAsync<bool>(HttpMethod.Put, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/ptzPreset", ptzPresetDO);
        }

        // PUT /api/Device/{deviceID}/PTZControl
        public async Task<ApiResult<bool>> PresetControl(DeviceDO deviceDO, PTZControlDataObject ptzControlDO)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return ToApiResult(await FireRuntime.PtzControlAsync(deviceDO, ptzControlDO).ConfigureAwait(false));
            }

            return await IRTekHttpClient.SendAsync<bool>(HttpMethod.Put, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/ptzControl", ptzControlDO);
        }

        // PUT /api/Device/{deviceID}/PTZCruise
        public async Task<ApiResult<bool>> SetPTZCruise(DeviceDO deviceDO, PTZCruiseDataObject ptzCruiseDO)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return ToApiResult(await FireRuntime.SetCruiseAsync(deviceDO, ptzCruiseDO).ConfigureAwait(false));
            }

            return await IRTekHttpClient.SendAsync<bool>(HttpMethod.Put, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/ptzCruise", ptzCruiseDO);
        }

        // PUT /api/Device/{deviceID}/RestoreCruise
        public async Task<bool> RestoreCruise(DeviceDO deviceDO)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return await FireRuntime.RestoreCruiseAsync(deviceDO).ConfigureAwait(false);
            }

            ApiResult<string> result = await IRTekHttpClient.SendAsync<string>(HttpMethod.Put, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/restoreCruise");
            return result.IsSuccess;
        }

        // GET /api/Device/{deviceID}/Conf
        public async Task<DevicePresetDataObject> GetDevicePresetListDataObject(DeviceDO deviceDO)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                DeviceConfDataObject conf = await FireRuntime.GetDeviceConfigAsync<DeviceConfDataObject>(deviceDO).ConfigureAwait(false);
                return conf?.PresetConfig ?? new DevicePresetDataObject();
            }

            ApiResult<DeviceConfDataObject> result = await IRTekHttpClient.SendAsync<DeviceConfDataObject>(HttpMethod.Get, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/service/device/{deviceDO.ID}/conf");
            return result.Data.PresetConfig;
        }

        // GET /api/service/deviceData?page={page}&pagesz={pagesz}&startTime={startTime}&endTime={endTime}&deviceID={}&presetID={}
        public async Task<PaginationData<List<DeviceDataDO>>> GetDeviceData(DeviceDataQueryParam param, DeviceDO deviceDO)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                var page = LiteFireDataRepository.GetByPagination(param);
                return new PaginationData<List<DeviceDataDO>>()
                {
                    TotalCount = page.Total,
                    Data = page.Items
                };
            }

            PaginationApiResult<List<DeviceDataDO>> result = await IRTekHttpClient.SendPaginationAsync<List<DeviceDataDO>>(HttpMethod.Get, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/service/deviceData?" + IRTekHttpClient.GetParamStr(param));
            return new PaginationData<List<DeviceDataDO>>()
            {
                TotalCount = result.Total,
                Data = result.Data
            };
        }

        // GET /api/service/deviceData?startTime={startTime}&endTime={endTime}&deviceIDs={}
        public async Task<List<DeviceDataStatisticsDO>> GetDeviceDataStatistics(DeviceDataStatisticsQueryParam param, string serverAddress)
        {
            if (UseInProcFire)
            {
                return LiteFireDataRepository.GetStatistics(param);
            }

            ApiResult<List<DeviceDataStatisticsDO>> result = await IRTekHttpClient.SendAsync<List<DeviceDataStatisticsDO>>(HttpMethod.Get, $"{serverAddress}/api/service/deviceData/statistics?" + IRTekHttpClient.GetParamStr(param));
            return result.Data ?? new List<DeviceDataStatisticsDO>();
        }

        public async Task<ApiResult<bool>> Subscribe(string serverAddress, ServerType type, string id, string connectionId)
        {
            if (UseInProcFire && type == ServerType.FireMonitorAgent)
            {
                EnsureInProcEventSubscription();
                await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                    .SubscribeAsync(serverAddress, type, id, connectionId).ConfigureAwait(false);
                return new ApiResult<bool>(true, ResponseCode.SUCCESS);
            }

            SubscriptionBO subscriptionBO = new SubscriptionBO();
            subscriptionBO.EventConsumer = connectionId;
            string server = string.Empty;
            switch (type)
            {
                case ServerType.FireMonitorAgent:
                    server = "fire";
                    subscriptionBO.EventTypes = new List<EventType>() { EventType.Fire_DataEvent, EventType.Fire_AlarmEvent, EventType.Fire_PreviewException };
                    break;

                case ServerType.EquipmentMonitorAgent:
                    server = "equipment";
                    subscriptionBO.EventTypes = new List<EventType>() { EventType.Equipment_DataEvent, EventType.Equipment_AlarmEvent };
                    break;

                case ServerType.EnvironmentMonitorAgent:
                    server = "environment";
                    subscriptionBO.EventTypes = new List<EventType>() { EventType.Environment_DataEvent, EventType.Environment_AlarmEvent };
                    break;
            }

            try
            {
                ApiResult<bool> result = await IRTekHttpClient.SendAsync<bool>(HttpMethod.Post, $"{serverAddress}/api/{server}/device/{id}/v1/event/subscribe", subscriptionBO);
                return result;
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
                return new ApiResult<bool>(false, ResponseCode.EQUIPMENT_EVENT_SUBSCRIBE_FAILED);
            }
        }

        public async Task<bool> Unsubscribe(string serverAddress, ServerType type, int id, SubscriptionBO subscriptionBO)
        {
            if (UseInProcFire && type == ServerType.FireMonitorAgent)
            {
                await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                    .UnsubscribeAsync(serverAddress, type, id.ToString(), subscriptionBO?.EventConsumer).ConfigureAwait(false);
                return true;
            }

            string server = string.Empty;
            switch (type)
            {
                case ServerType.FireMonitorAgent:
                    server = "fire";
                    break;

                case ServerType.EquipmentMonitorAgent:
                    server = "equipment";
                    break;

                case ServerType.EnvironmentMonitorAgent:
                    server = "environment";
                    break;
            }
            ApiResult<bool> result = await IRTekHttpClient.SendAsync<bool>(HttpMethod.Post, $"{serverAddress}/api/{server}/device/{id}/v1/event/unsubscribe", subscriptionBO);
            return result.Data;
        }

        public async Task<DeviceStreamDO> GetStream(DeviceDO deviceDO, StreamType streamType)
        {
            if (deviceDO == null)
            {
                return new DeviceStreamDO();
            }

            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                DeviceStreamDO stream = await Firefly.Client.Service.Abstractions.FireRuntimeServices.DeviceRuntime
                    .GetStreamAsync(deviceDO, streamType).ConfigureAwait(false)
                    ?? new DeviceStreamDO();

                // The engine reports its bind address (0.0.0.0), which is not connectable.
                // Mirror non-Lite behavior (rewrite to agent host IP) using this machine's LAN address.
                string hostIp = GetLocalIPv4();
                stream.StreamItems?.ForEach(x =>
                {
                    x.RTSPUrl = Regex.Replace(x.RTSPUrl ?? string.Empty, Constants.IPS_REG, hostIp);
                    x.WebRTCUrl = Regex.Replace(x.WebRTCUrl ?? string.Empty, Constants.IPS_REG, hostIp);
                });
                return stream;
            }

            ApiResult<DeviceStreamDO> result = await IRTekHttpClient.SendAsync<DeviceStreamDO>(HttpMethod.Get, $"{deviceDO.DefaultAgent().GetServerAddress()}" + $"/api/device/{deviceDO.ID}/stream/{(int)streamType}");
            result.Data.StreamItems.ForEach(x =>
            {
                x.RTSPUrl = Regex.Replace(x.RTSPUrl, Constants.IPS_REG, deviceDO.DefaultAgent().IP);
                x.WebRTCUrl = Regex.Replace(x.RTSPUrl, Constants.IPS_REG, deviceDO.DefaultAgent().IP);
            });
            return result.Data;
        }

        public static string GetLocalIPv4()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp))
                {
                    socket.Connect("8.8.8.8", 65530);
                    if (socket.LocalEndPoint is IPEndPoint endPoint && !IPAddress.IsLoopback(endPoint.Address))
                    {
                        return endPoint.Address.ToString();
                    }
                }
            }
            catch
            {
            }

            try
            {
                foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
            }

            return "127.0.0.1";
        }

        private static bool IsFireDevice(DeviceDO deviceDO)
        {
            if (deviceDO?.DefaultAgent() == null)
            {
                return true; // Lite local devices default to Fire runtime
            }

            return string.Equals(
                deviceDO.DefaultAgent().Type,
                ServerType.FireMonitorAgent.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> SetAlarmOut(DeviceDO deviceDO, List<AlarmOutDataObject> alarmOutDOs)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return await FireRuntime.SetAlarmOutAsync(deviceDO, alarmOutDOs).ConfigureAwait(false);
            }

            ApiResult<bool> result = await IRTekHttpClient.SendAsync<bool>(HttpMethod.Put, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/setAlarmOut", alarmOutDOs);

            return result.Data;
        }

        public async Task<bool> SetCustomerOSD(DeviceDO deviceDO, OSDCustomerDO osdCustomerDO)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return await FireRuntime.SetCustomerOsdAsync(deviceDO, DEFAULT_OSD_CHANNEL, osdCustomerDO).ConfigureAwait(false);
            }

            ApiResult<bool> result = await IRTekHttpClient.SendAsync<bool>(HttpMethod.Post, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/osdCustomer", osdCustomerDO);

            return result.Data;
        }

        public async Task<ImageInfo> CaptureImage(DeviceDO deviceDO, int channel)
        {
            if (UseInProcFire && IsFireDevice(deviceDO))
            {
                return await FireRuntime.CaptureImageAsync(deviceDO, channel).ConfigureAwait(false) ?? new ImageInfo();
            }

            ApiResult<ImageInfo> result = await IRTekHttpClient.SendAsync<ImageInfo>(HttpMethod.Get, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/device/{deviceDO.ID}/snapshot?channel={channel}");
            return result.Data ?? new ImageInfo();
        }

        public async Task<List<ImageObject>> GetFiles<ImageObject>(DeviceDO deviceDO, int dataID)
        {
            ApiResult<List<ImageObject>> result = await IRTekHttpClient.SendAsync<List<ImageObject>>(HttpMethod.Get, $"{deviceDO.DefaultAgent().GetServerAddress()}/api/service/file/fireData/{dataID}");
            return result.Data;
        }

        #endregion AgentServer
    }
}