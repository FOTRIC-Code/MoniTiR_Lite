// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

using log4net;

namespace Firefly.Client
{
    /// <summary>
    /// Legacy host for child-process Lite agents. Store and Fire both run in-process now;
    /// this type remains as a no-op so existing App.xaml call sites keep compiling.
    /// </summary>
    internal static class LiteServerHost
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LiteServerHost));

        public static Task EnsureStartedAsync(TimeSpan timeout)
        {
            Logger.Info("LiteServerHost: no child agents to start (Store+Fire in-process).");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Store runs in-process inside MoniTiRLite.exe (no 9070 child process).
        /// </summary>
        public static Task EnsureStoreStartedAsync(TimeSpan timeout)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Nothing to stop — Store/Fire are in-process.
        /// </summary>
        public static void StopOwned()
        {
        }
    }
}
