// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Controls.Primitives;

namespace Firefly.Client.Control
{
    public partial class DayTickBar : TickBar
    {
        public int FontSize
        {
            get { return (int)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(int), typeof(DayTickBar), new PropertyMetadata(8));

        public int TickLength
        {
            get { return (int)GetValue(TickLengthProperty); }
            set { SetValue(TickLengthProperty, value); }
        }

        public static readonly DependencyProperty TickLengthProperty =
            DependencyProperty.Register("LineLength", typeof(int), typeof(DayTickBar), new PropertyMetadata(10));

        /// <summary>
        /// 文字显示间距 每隔多少个刻度显示文字
        /// </summary>
        public int TextDisplayInterval
        {
            get { return (int)GetValue(TextDisplayIntervalProperty); }
            set { SetValue(TextDisplayIntervalProperty, value); }
        }

        public static readonly DependencyProperty TextDisplayIntervalProperty =
            DependencyProperty.Register("TheTextDisplayInterval", typeof(int), typeof(DayTickBar), new PropertyMetadata(2));

        protected override void OnRender(DrawingContext dc)
        {
            Size size = new Size(ActualWidth, ActualHeight);
            double panWidth = 1.0d;
            double offset = 2 * panWidth;
            Point startPoint = new Point(0d, 0d);
            Pen pen = new Pen(Fill, panWidth);
            int lastCount = 1;
            double logicalToPhysical = (size.Width - offset) / Maximum;
            double multiples = Maximum / (Ticks.Count - lastCount);
            double lineStartPointY = ActualHeight;
            double lineEndPointY = lineStartPointY - TickLength;
            int textHeight = 4;
            if (Ticks != null)
            {
                for (int i = 0; i < Ticks.Count; i++)
                {
                    double adjustedTick = Ticks[i] - Minimum;
                    double lineX = adjustedTick * logicalToPhysical * multiples + startPoint.X;
                    dc.DrawLine(pen,
                        new Point(lineX, startPoint.Y + lineStartPointY),
                        new Point(lineX, startPoint.Y + lineEndPointY));

                    if (i % TextDisplayInterval == 0)
                    {
                        string ticksStr = Ticks[i].ToString();
                        FormattedText formattedText = new FormattedText(ticksStr, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), FontSize, Fill);
                        double halfTextWidth = formattedText.Width * 0.5;
                        double textX = lineX - halfTextWidth;
                        dc.DrawText(formattedText, new Point(textX, startPoint.Y + textHeight));
                    }
                }
            }
        }
    }
}