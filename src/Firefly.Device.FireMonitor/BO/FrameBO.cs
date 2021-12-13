// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using OWB.DataFrame;

namespace Firefly.Device.FireMonitor
{
    internal class FrameBO
    {
        internal FrameBO(OWBIRFrame irFrame, OWBDCFrame dcFrame, bool isSuccess)
        {
            IRFrame = irFrame;
            DCFrame = dcFrame;
            IsSuccess = isSuccess;
        }

        internal OWBIRFrame IRFrame { get; private set; }

        internal OWBDCFrame DCFrame { get; private set; }

        internal bool IsSuccess { get; private set; }
    }
}
