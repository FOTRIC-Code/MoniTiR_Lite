// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Common;
using Firefly.Client.Service.LiteDb;
using log4net;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Firefly.Client.Service
{
    public class ServerManager
    {
        #region Fields

        private static ServerManager serverManager = new ServerManager();

        public static ServerManager Instance => serverManager;

        private ConcurrentDictionary<int, ServerDO> _serverDODict;

        private ConcurrentDictionary<int, HubConnectionBO> _serverHubConnectionDict;

        #endregion Fields

        public string ServerManagerURL { get; set; }

        private bool _signalRCheckFlag = false;
        private long _isChecking = 0;
        private DateTime _lastCheckTime = DateTime.MinValue;
        private ILog _logger = LogManager.GetLogger(typeof(ServerManager));
        private static ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        #region Constructors

        private ServerManager()
        {
            Thread thread = new Thread(new ThreadStart(DoStart));
            thread.Start();
            _serverDODict = new ConcurrentDictionary<int, ServerDO>();
            _serverHubConnectionDict = new ConcurrentDictionary<int, HubConnectionBO>();
        }

        #endregion Constructors

        #region Methods

        public void Start()
        {
            _manualResetEvent.Set();
        }

        public async Task Stop()
        {
            foreach (KeyValuePair<int, HubConnectionBO> keyValuePair in _serverHubConnectionDict)
            {
                if (keyValuePair.Value.HubConnection != null)
                {
                    await keyValuePair.Value.HubConnection.StopAsync();
                }
            }
            _serverDODict.Clear();
            _serverHubConnectionDict.Clear();
            _manualResetEvent.Reset();
        }

        private async void DoStart()
        {
            while (!_signalRCheckFlag)
            {
                _manualResetEvent.WaitOne();
                try
                {
                    if (DateTime.Now - _lastCheckTime < new TimeSpan(0, 0, Constants.SIGNALR_SERVER_CHECK_DELAY))
                    {
                        continue;
                    }

                    _lastCheckTime = DateTime.Now;

                    //更新服务列表
                    await InternalGetServerDOs();
                    //更新signalR
                    InternalCheckConnectionTask();
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
                finally
                {
                    await Task.Delay(Constants.SIGNALR_CONNECTED_RETRY_DELAY);
                }
            }
        }

        public HubConnectionBO TryGetOrAddHubConnectionBO(int serverID)
        {
            if (!_serverHubConnectionDict.TryGetValue(serverID, out HubConnectionBO hubConnectionBO))
            {
                hubConnectionBO = new HubConnectionBO();
                hubConnectionBO.SubscribedDeviceIDs = new List<string>();
                hubConnectionBO.ServerDO = new ServerDO
                {
                    ID = serverID
                };
                _serverHubConnectionDict.TryAdd(serverID, hubConnectionBO);

                InvalidServerStatus();
            }

            return hubConnectionBO;
        }

        public bool TryRemoveConnectionBO(int serverID) => _serverHubConnectionDict.TryRemove(serverID, out _);

        public void InvalidServerStatus()
        {
            _lastCheckTime = DateTime.MinValue;
        }

        private void InternalCheckConnectionTask()
        {
            List<int> needRemovedItems = _serverHubConnectionDict.Where(it => it.Value.ServerDO == null).Select(it => it.Key).ToList();
            needRemovedItems?.ForEach(key =>
            {
                _serverHubConnectionDict.TryRemove(key, out _);
            });

            List<ServerDO> serverDOs = GetCachedServers();
            if (serverDOs == null)
            {
                return;
            }

            if (serverDOs.Count == 0)
            {
                _serverHubConnectionDict.Clear();
            }

            foreach (KeyValuePair<int, HubConnectionBO> keyValuePair in _serverHubConnectionDict)
            {
                ServerDO serverDO = serverDOs.FirstOrDefault(it => it.ID == keyValuePair.Key);
                if (serverDO == null)
                {
                    keyValuePair.Value.ServerDO = null;
                    continue;
                }

                keyValuePair.Value.ServerDO = serverDO;

                // Fire in-proc: skip SignalR (callbacks via IFireCallbackDispatcher).
                if (Firefly.Client.Service.Abstractions.FireRuntimeServices.Mode
                        == Firefly.Client.Service.Abstractions.FireHostMode.InProcess
                    && string.Equals(serverDO.Type, ServerType.FireMonitorAgent.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Store in-proc: no HTTP/SignalR listener on 9070.
                if (string.Equals(serverDO.Type, ServerType.StoreAgent.ToString(), StringComparison.OrdinalIgnoreCase)
                    && Firefly.Client.Service.Abstractions.StoreRuntimeServices.Runtime != null
                    && Firefly.Client.Service.Abstractions.StoreRuntimeServices.Runtime.IsReady)
                {
                    continue;
                }

                if (keyValuePair.Value.HubConnection == null)
                {
                    HubConnection hub = new HubConnectionBuilder().WithUrl($"{serverDO.GetServerAddress()}{Constants.SIGNALR_ROUTER}").WithAutomaticReconnect(new RetryPolicy()).Build();
                    hub.On<string>(Constants.SIGNALR_METHOD_NAME, keyValuePair.Value.EventNotifyHandler);
                    hub.Reconnected += Hub_Reconnected;
                    hub.Reconnecting += Hub_Reconnecting;
                    keyValuePair.Value.HubConnection = hub;
                }

                if (keyValuePair.Value.HubConnection.State == HubConnectionState.Disconnected)
                {
                    lock (keyValuePair.Value.HubConnection)
                    {
                        _ = keyValuePair.Value.HubConnection.StartAsync();
                        int retryCount = Constants.SIGNALR_CONNECTED_RETRY_COUNT;
                        while (retryCount-- > 0)
                        {
                            if (keyValuePair.Value.HubConnection.State == HubConnectionState.Connected)
                            {
                                break;
                            }

                            Thread.Sleep(Constants.SIGNALR_CONNECTED_RETRY_DELAY);
                        }
                    }
                }

                _logger.Info($"Init Server: {keyValuePair.Value.ServerDO.GetServerAddress()} SignalR State: {keyValuePair.Value.HubConnection.State}");
            }
        }

        private Task Hub_Reconnecting(Exception arg)
        {
            foreach (KeyValuePair<int, HubConnectionBO> keyValuePair in _serverHubConnectionDict)
            {
                if (keyValuePair.Value.HubConnection == null)
                {
                    keyValuePair.Value.SubscribedDeviceIDs?.Clear();
                }
                if (keyValuePair.Value.HubConnection != null && keyValuePair.Value.HubConnection.State == HubConnectionState.Disconnected)
                {
                    keyValuePair.Value.SubscribedDeviceIDs?.Clear();
                }
            }
            return Task.CompletedTask;
        }

        private Task Hub_Reconnected(string arg)
        {
            DeviceManager.Instance.InvalidDeviceState();
            return Task.CompletedTask;
        }

        #endregion Methods

        #region Api

        private async Task<List<ServerDO>> InternalGetServerDOs()
        {
            LiteDatabase.EnsureInitialized();
            List<ServerDO> servers = LiteAgentRepository.GetAll();
            foreach (ServerDO server in servers)
            {
                await RefreshAgentRuntimeAsync(server).ConfigureAwait(false);
                // Status only — no agent table.
                LiteAgentRepository.Upsert(server);
            }

            _serverDODict.Clear();
            servers.ForEach(it => _serverDODict.TryAdd(it.ID, it));
            return _serverDODict.Values.ToList();
        }

        private async Task RefreshAgentRuntimeAsync(ServerDO server)
        {
            try
            {
                bool isFire = string.Equals(server.Type, ServerType.FireMonitorAgent.ToString(), StringComparison.OrdinalIgnoreCase);
                bool isStore = string.Equals(server.Type, ServerType.StoreAgent.ToString(), StringComparison.OrdinalIgnoreCase);
                if (isFire && Firefly.Client.Service.Abstractions.FireRuntimeServices.Mode
                    == Firefly.Client.Service.Abstractions.FireHostMode.InProcess)
                {
                    bool ok = await Firefly.Client.Service.Abstractions.FireRuntimeServices.AgentHealth
                        .PingAsync(server.GetServerAddress()).ConfigureAwait(false);
                    server.Status = ok ? 1 : 0;
                    return;
                }

                if (isStore)
                {
                    bool ok = Firefly.Client.Service.Abstractions.StoreRuntimeServices.Runtime != null
                        && Firefly.Client.Service.Abstractions.StoreRuntimeServices.Runtime.IsReady;
                    server.Status = ok ? 1 : 0;
                    return;
                }

                // Fast fail when the process is not listening.
                if (!IsTcpOpen(server.IP, server.Port, 200))
                {
                    server.Status = 0;
                    return;
                }

                ApiResult<bool> ping = await IRTekHttpClient.SendAsyncWithNoAuth<bool>(
                    HttpMethod.Get,
                    $"{server.GetServerAddress()}/api/service/connection/test");
                server.Status = ping != null && ping.IsSuccess ? 1 : 0;
            }
            catch
            {
                server.Status = 0;
            }
        }

        private static bool IsTcpOpen(string host, int port, int timeoutMs)
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    IAsyncResult result = client.BeginConnect(host, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
                    if (!success)
                    {
                        return false;
                    }

                    client.EndConnect(result);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public List<ServerDO> GetCachedServers()
        {
            return _serverDODict.Values.ToList();
        }

        public async Task<List<ServerDO>> GetServerDOsAsync()
        {
            List<ServerDO> serverDOs = await InternalGetServerDOs();
            return serverDOs;
        }

        public ServerDO Get(int id)
        {
            _serverDODict.TryGetValue(id, out ServerDO serverDO);
            return serverDO;
        }

        public Task<ApiResult<bool>> UpdateAgent(ServerDO serverDO)
        {
            LiteAgentRepository.Upsert(serverDO);
            _serverDODict.AddOrUpdate(serverDO.ID, serverDO, (k, v) => serverDO);
            return Task.FromResult(new ApiResult<bool>(true));
        }

        public Task<bool> DeleteAgent(int agentID)
        {
            bool ok = LiteAgentRepository.Delete(agentID);
            _serverDODict.TryRemove(agentID, out _);
            return Task.FromResult(ok);
        }

        public Task<ApiResult<int>> RegistAgent(ServerDO serverDO)
        {
            if (serverDO.ID <= 0)
            {
                if (string.Equals(serverDO.Type, ServerType.FireMonitorAgent.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    serverDO.ID = LiteLocalAgentIds.Fire;
                }
                else if (string.Equals(serverDO.Type, ServerType.StoreAgent.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    serverDO.ID = LiteLocalAgentIds.Store;
                }
            }

            // Fixed local agents only — refresh in-memory status.
            LiteAgentRepository.Upsert(serverDO);
            _serverDODict.AddOrUpdate(serverDO.ID, serverDO, (k, v) => serverDO);
            return Task.FromResult(new ApiResult<int>(serverDO.ID));
        }

        #endregion Api
    }

    internal class RetryPolicy : IRetryPolicy
    {
        /// <summary>
        /// If passed to <see cref="M:Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilderExtensions.WithAutomaticReconnect(Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder,Microsoft.AspNetCore.SignalR.Client.IRetryPolicy)" />,
        /// this will be called after the transport loses a connection to determine if and for how long to wait before the next reconnect attempt.
        /// </summary>
        /// <param name="retryContext">
        /// Information related to the next possible reconnect attempt including the number of consecutive failed retries so far, time spent
        /// reconnecting so far and the error that lead to this reconnect attempt.
        /// </param>
        /// <returns>
        /// A <see cref="T:System.TimeSpan" /> representing the amount of time to wait from now before starting the next reconnect attempt.
        /// <see langword="null" /> tells the client to stop retrying and close.
        /// </returns>
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            long count = retryContext.PreviousRetryCount / 50;
            if (count < 1)
            {
                return new TimeSpan(0, 0, 1);
            }
            else if (count < 5)
            {
                return new TimeSpan(0, 0, 30);
            }
            else
            {
                return new TimeSpan(0, 1, 0);
            }
        }
    }
}