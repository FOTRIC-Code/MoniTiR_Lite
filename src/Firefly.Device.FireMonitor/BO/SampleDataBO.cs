// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using OWB.DataFrame;

namespace Firefly.Device.FireMonitor
{
    internal class SampleDataBO
    {
        internal SampleDataBO(OWBIRFrame irFrame, OWBDCFrame dcFrame, List<OWBMarker> markers, long timestamp)
        {
            IRFrame = irFrame;
            DCFrame = dcFrame;
            Markers = markers;
            Timestamp = timestamp;
        }

        internal OWBIRFrame IRFrame { get; private set; }

        internal OWBDCFrame DCFrame { get; private set; }

        internal List<OWBMarker> Markers { get; private set; }

        internal long Timestamp { get; private set; }
    }
}
