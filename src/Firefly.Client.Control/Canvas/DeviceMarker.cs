// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace Firefly.Client.Control
{
    public class DeviceMarker
    {
        public DeviceMarker(MarkerType markerType, CanvasType canvasType, List<Point> pointList, string name)
        {
            MarkerType = markerType;
            CanvasType = canvasType;
            PointList = pointList;
            Name = name;
        }

        public MarkerType MarkerType { get; set; }

        public CanvasType CanvasType { get; set; }

        public List<Point> PointList { get; set; }

        public string Name { get; set; }
    }
}
