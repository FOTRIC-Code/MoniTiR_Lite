// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;

namespace Firefly.Client.Service
{
    public interface IDeviceConfigurer
    {
        ServerType GetServerType();

        void Open(DeviceDO deviceDO, Window owner);
    }
}
