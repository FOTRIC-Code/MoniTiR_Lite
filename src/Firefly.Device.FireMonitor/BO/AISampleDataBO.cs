// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

using OWB.DataFrame;

namespace Firefly.Device.FireMonitor
{
    internal class AISampleDataBO
    {
        internal AISampleDataBO(OWBIRFrame irFrame, OWBDCFrame dcFrame, List<OWBMarker> markerList, long timestamp)
        {
            IRFrame = irFrame;
            DCFrame = dcFrame;
            MarkerList = markerList;
            Timestamp = timestamp;
        }

        internal OWBIRFrame IRFrame { get; private set; }

        internal OWBDCFrame DCFrame { get; private set; }

        internal List<OWBMarker> MarkerList { get; private set; }

        internal long Timestamp { get; private set; }
    }
}
