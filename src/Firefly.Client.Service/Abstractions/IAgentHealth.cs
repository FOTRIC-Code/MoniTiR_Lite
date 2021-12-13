// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// Fire agent health / config (replaces /api/service/connection/test and /api/service/config).
    /// </summary>
    public interface IAgentHealth
    {
        Task<AgentConfig> GetConfigAsync(string serverAddr);

        Task<bool> PingAsync(string serverAddr);
    }
}
