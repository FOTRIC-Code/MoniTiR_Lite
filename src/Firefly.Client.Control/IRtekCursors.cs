// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Windows.Input;

namespace Firefly.Client.Control
{
    public class IRtekCursors
    {
        private static Cursor ellipse;
        private static Cursor polygon;
        private static Cursor line;
        private static Cursor polyline;
        private static Cursor rectangle;
        private static Cursor spot;
        private static Cursor spotSizeAll;
        private static Cursor delta;

        static IRtekCursors()
        {

            ellipse = new Cursor(new MemoryStream(Properties.Resources.Ellipse));
            polygon = new Cursor(new MemoryStream(Properties.Resources.Polygon));
            line = new Cursor(new MemoryStream(Properties.Resources.Line));
            polyline = new Cursor(new MemoryStream(Properties.Resources.Polyline));
            rectangle = new Cursor(new MemoryStream(Properties.Resources.Rectangle));
            spot = new Cursor(new MemoryStream(Properties.Resources.Spot));
            spotSizeAll = new Cursor(new MemoryStream(Properties.Resources.SpotSizeAll));
            delta = new Cursor(new MemoryStream(Properties.Resources.Delta));
        }

        public static Cursor Ellipse { get => ellipse; }

        public static Cursor Polygon { get => polygon; }

        public static Cursor Line { get => line; }

        public static Cursor Polyline { get => polyline; }

        public static Cursor Rectangle { get => rectangle; }

        public static Cursor Spot { get => spot; }

        public static Cursor SpotSizeAll { get => spotSizeAll; }

        public static Cursor Delta { get => delta; }
    }
}
