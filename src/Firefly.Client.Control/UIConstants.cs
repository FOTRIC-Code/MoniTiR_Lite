// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;

namespace Firefly.Client.Control
{
    public static class UIConstants
    {
        // After max window, top, bottom, left, right will be fill 7 or 8 pixel, so max window height and width need add 16 pixel.
        public static double MAX_WINDOW_WIDTH = SystemParameters.WorkArea.Width + 16;

        public static double MAX_WINDOW_HEIGHT = SystemParameters.WorkArea.Height + 16;

        public static ImageRenderMode RenderMode { get; set; }
    }
}