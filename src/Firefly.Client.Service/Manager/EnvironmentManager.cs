// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Service.DataObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firefly.Client.Service
{
    public class EnvironmentManager
    {
        private static EnvironmentManager environmentManager = new EnvironmentManager();

        public static EnvironmentManager Instance => environmentManager;

        private EnvironmentManager() { }

        public async Task<List<EnvironmentDO>> Get(string address)
        {
            List<EnvironmentDO> environmentDOs = await Task.Run(() =>
            {
                return new List<EnvironmentDO>();
            });

            return environmentDOs;
        }
    }
}
