// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

using log4net;

using Firefly.Client.Service.LiteDb;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// Resolves Store runtime. Lite: in-process only (no 9070 child process).
    /// </summary>
    public static class StoreRuntimeServices
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StoreRuntimeServices));
        private static readonly object Sync = new object();
        private static IStoreRuntime _runtime;
        private static bool _started;

        public static IStoreRuntime Runtime
        {
            get
            {
                EnsureCreated();
                return _runtime;
            }
        }

        public static async Task StartAsync()
        {
            EnsureCreated();
            if (_started)
            {
                return;
            }

            await _runtime.StartAsync().ConfigureAwait(false);
            _started = true;
        }

        public static async Task StopAsync()
        {
            if (!_started || _runtime == null)
            {
                return;
            }

            await _runtime.StopAsync().ConfigureAwait(false);
            _started = false;
        }

        private static void EnsureCreated()
        {
            if (_runtime != null)
            {
                return;
            }

            lock (Sync)
            {
                if (_runtime != null)
                {
                    return;
                }

                StoreSqlite.EnsureNvrSchema();
                _runtime = new InProcStoreRuntime();
                Logger.Info("Store runtime: InProcess");
            }
        }
    }
}
