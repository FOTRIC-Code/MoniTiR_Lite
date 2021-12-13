// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;

using log4net;

using Firefly.Client.Service;
using Firefly.Client.Service.Abstractions;
using Firefly.Client.Service.LiteDb;

namespace Firefly.Client
{
    /// <summary>
    /// Boots local Lite runtime: in-proc Store + in-proc Fire (no child agent processes).
    /// </summary>
    internal static class LiteAgentBootstrap
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LiteAgentBootstrap));
        private static bool _done;

        public const string LocalIp = LiteLocalAgents.LocalIp;
        public const int StorePort = LiteLocalAgents.StorePort;
        public const int FirePort = LiteLocalAgents.FirePort;

        public static async Task EnsureLocalAgentsAsync()
        {
            if (_done)
            {
                return;
            }

            try
            {
                LiteDatabase.EnsureInitialized();

                await StoreRuntimeServices.StartAsync().ConfigureAwait(false);
                await FireRuntimeServices.StartAsync().ConfigureAwait(false);

                if (!FireRuntimeServices.DeviceRuntime.IsReady)
                {
                    Logger.Warn("In-proc Fire runtime not ready after start; will retry later.");
                    return;
                }

                if (!StoreRuntimeServices.Runtime.IsReady)
                {
                    Logger.Warn("In-proc Store runtime not ready after start; will retry later.");
                    return;
                }

                await ServerManager.Instance.GetServerDOsAsync().ConfigureAwait(false);
                _done = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Lite agent bootstrap failed.", ex);
            }
        }

        private static bool IsLocalIp(string ip)
        {
            return string.Equals(ip, LocalIp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ip, "localhost", StringComparison.OrdinalIgnoreCase);
        }

        public static ServerDO GetLocalStoreAgent()
        {
            return ServerManager.Instance.GetCachedServers()
                ?.FirstOrDefault(s =>
                    string.Equals(s.Type, ServerType.StoreAgent.ToString(), StringComparison.OrdinalIgnoreCase)
                    && IsLocalIp(s.IP)
                    && s.Port == StorePort)
                ?? LiteAgentRepository.Get(LiteLocalAgentIds.Store)
                ?? LiteLocalAgents.CreateStore();
        }
    }
}
