// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.ComponentModel.Composition;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    [Export(typeof(IDeviceConfigurer))]
    public class DeviceConfigurer : IDeviceConfigurer
    {
        public ServerType GetServerType()
        {
            return ServerType.FireMonitorAgent;
        }

        public void Open(DeviceDO deviceDO, Window owner)
        {
            new MainWindow(deviceDO, owner).ShowDialog();
        }
    }
}
