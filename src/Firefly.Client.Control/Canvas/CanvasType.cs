// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

namespace Firefly.Client.Control
{
    public enum CanvasMode
    {
        Analysis,
        Mapping,
    }

    public enum CanvasType
    {
        // 屏蔽区域
        Shield = 0,

        // 测温对象
        Marker = 1
    }

    public enum MarkerType
    {
        Spot = 1,
        Line = 2,
        Region = 3
    }

    public enum ImageRenderMode
    {
        Fit, // 适应
        Fill, // 填充画面
    }
}