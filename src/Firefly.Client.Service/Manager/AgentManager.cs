// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Net.Http;
using System.Threading.Tasks;

using Firefly.Client.Service.LiteDb;

namespace Firefly.Client.Service
{
    public class AgentManager
    {
        private static AgentManager agentManager = new AgentManager();

        public static AgentManager Instance => agentManager;

        private AgentManager()
        {
            
        }

        public async Task<AgentConfig> GetAgentConfig(string serverAddr)
        {
            return await Firefly.Client.Service.Abstractions.FireRuntimeServices.AgentHealth
                .GetConfigAsync(serverAddr).ConfigureAwait(false);
        }

        public Task<AdminAgentConfig> GetAdminAgentConfig(string adminServerAddr = null)
        {
            const string defaultName = "MoniTiR Lite";
            AdminAgentConfig config = LiteSystemConfigRepository.Get<AdminAgentConfig>(LiteSystemConfigRepository.KeyAdminConfig);
            if (config == null)
            {
                config = new AdminAgentConfig
                {
                    Name = defaultName,
                    NameEN = defaultName
                };
                LiteSystemConfigRepository.SetObject(LiteSystemConfigRepository.KeyAdminConfig, config);
            }
            else if (string.Equals(config.Name, "MoniTiR", StringComparison.Ordinal)
                || string.Equals(config.NameEN, "MoniTiR", StringComparison.Ordinal))
            {
                // Migrate previous default product name to MoniTiR Lite.
                if (string.Equals(config.Name, "MoniTiR", StringComparison.Ordinal))
                {
                    config.Name = defaultName;
                }
                if (string.Equals(config.NameEN, "MoniTiR", StringComparison.Ordinal))
                {
                    config.NameEN = defaultName;
                }
                LiteSystemConfigRepository.SetObject(LiteSystemConfigRepository.KeyAdminConfig, config);
            }
            return Task.FromResult(config);
        }
    }
}