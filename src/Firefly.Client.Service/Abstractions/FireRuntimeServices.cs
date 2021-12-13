// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;

using log4net;

namespace Firefly.Client.Service.Abstractions
{
    public enum FireHostMode
    {
        /// <summary>Run Fire runtime inside Client process.</summary>
        InProcess = 1,
    }

    /// <summary>
    /// Resolves Fire runtime. Phase 3: in-process only (no 9080 child process).
    /// </summary>
    public static class FireRuntimeServices
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FireRuntimeServices));
        private static readonly object Sync = new object();

        private static IAgentHealth _agentHealth;
        private static IFireDeviceRuntime _deviceRuntime;
        private static IFireCallbackDispatcher _callbackDispatcher;
        private static bool _started;

        public static FireHostMode Mode => FireHostMode.InProcess;

        public static IAgentHealth AgentHealth
        {
            get
            {
                EnsureCreated();
                return _agentHealth;
            }
        }

        public static IFireDeviceRuntime DeviceRuntime
        {
            get
            {
                EnsureCreated();
                return _deviceRuntime;
            }
        }

        public static IFireCallbackDispatcher CallbackDispatcher
        {
            get
            {
                EnsureCreated();
                return _callbackDispatcher;
            }
        }

        public static void ConfigureFromAppSettings()
        {
            Logger.Info("Fire host mode: InProcess");
        }

        public static async Task StartAsync()
        {
            EnsureCreated();
            if (_started)
            {
                return;
            }

            await _deviceRuntime.StartAsync().ConfigureAwait(false);
            _started = true;
        }

        public static async Task StopAsync()
        {
            if (!_started)
            {
                return;
            }

            try
            {
                await _deviceRuntime.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn("Fire runtime stop failed: " + ex.Message);
            }

            (_callbackDispatcher as IDisposable)?.Dispose();
            _started = false;
        }

        public static void CaptureUiSynchronizationContext()
        {
            EnsureCreated();
            SynchronizationContext ctx = SynchronizationContext.Current;
            if (ctx != null && _callbackDispatcher is FireCallbackDispatcher d)
            {
                d.SetSynchronizationContext(ctx);
            }
        }

        private static void EnsureCreated()
        {
            if (_agentHealth != null)
            {
                return;
            }

            lock (Sync)
            {
                if (_agentHealth != null)
                {
                    return;
                }

                var dispatcher = new FireCallbackDispatcher();
                _callbackDispatcher = dispatcher;
                _agentHealth = new InProcAgentHealth();
                _deviceRuntime = new InProcFireDeviceRuntime(dispatcher);
            }
        }
    }
}
