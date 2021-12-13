// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;

namespace Firefly.Client.Control
{
    public static class WindowAssist
    {
        public static readonly DependencyProperty HeightRatioProperty
            = DependencyProperty.RegisterAttached("HeightRatio", typeof(float), typeof(WindowAssist),
                new FrameworkPropertyMetadata(1.0f, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(HeightRatioChanged)));

        private static void HeightRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window window = d as Window;
            if (window != null && e.NewValue != null)
            {
                Window owner = window.Owner;
                float heightRatio = (float)e.NewValue;
                if (heightRatio <= 1 && heightRatio >= 0.1)
                {
                    double newHeight = SystemParameters.WorkArea.Height * heightRatio;
                    window.Height = Math.Min(newHeight, owner?.Height * heightRatio ?? int.MaxValue);
                }
            }
        }

        public static float GetHeightRatio(UIElement target)
        {
            return (float)target.GetValue(HeightRatioProperty);
        }

        public static void SetHeightRatio(UIElement target, float heightRadio)
        {
            target.SetValue(HeightRatioProperty, heightRadio);
        }

        public static readonly DependencyProperty WidthRatioProperty
            = DependencyProperty.RegisterAttached("WidthRatio", typeof(float), typeof(WindowAssist),
                new FrameworkPropertyMetadata(1.0f, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(WidthRatioChanged)));

        private static void WidthRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window window = d as Window;

            if (window != null && e.NewValue != null)
            {
                Window owner = window.Owner;
                float widthRatio = (float)e.NewValue;
                if (widthRatio <= 1 && widthRatio >= 0.1)
                {
                    double newWidth = SystemParameters.WorkArea.Width * widthRatio;
                    window.Width = Math.Min(newWidth, owner?.Width * widthRatio ?? int.MaxValue);
                }
            }
        }

        public static float GetWidthRatio(UIElement target)
        {
            return (float)target.GetValue(WidthRatioProperty);
        }

        public static void SetWidthRatio(UIElement target, float widthRadio)
        {
            target.SetValue(WidthRatioProperty, widthRadio);
        }
    }
}
