// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Drawing;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace Firefly.Client
{
    public static class ChartHelper
    {
        public const float DEFAULT_MAX_TEMPERATURE = 100f;
        public const float DEFAULT_MIN_TEMPERATURE = 0f;

        private static readonly string[] _defaultColors = new string[] { "#00FF00", "#0000FF", "#FF0000", "#01FFFE", "#FFA6FE", "#FFDB66", "#006401", "#010067", "#95003A", "#007DB5", "#FF00F6", "#FFEEE8", "#774D00", "#90FB92", "#0076FF", "#D5FF00", "#FF937E", "#6A826C", "#FF029D", "#FE8900", "#7A4782", "#7E2DD2", "#85A900", "#FF0056", "#A42400", "#00AE7E", "#683D3B", "#BDC6FF", "#263400", "#BDD393", "#00B917", "#9E008E", "#001544", "#C28C9F", "#FF74A3", "#01D0FF", "#004754", "#E56FFE", "#788231", "#0E4CA1", "#91D0CB", "#BE9970", "#968AE8", "#BB8800", "#43002C", "#DEFF74", "#00FFC6", "#FFE502", "#620E00", "#008F9C", "#98FF52", "#7544B1", "#B500FF", "#00FF78", "#FF6E41", "#005F39", "#6B6882", "#5FAD4E", "#A75740", "#A5FFD2", "#FFB167", "#009BFF", "#E85EBE" };

        public static OxyColor GetColor(int index)
        {
            Color color = ColorTranslator.FromHtml(_defaultColors[index]);
            return OxyColor.FromRgb(color.R, color.G, color.B);
        }

        public static LineSeries SetSeriesStyle(this LineSeries series, string title, string groupName, OxyColor color)
        {
            series.Title = title;
            series.SeriesGroupName = groupName;
            series.Color = color;
            series.StrokeThickness = 1;
            series.MarkerSize = 3;
            series.CanTrackerInterpolatePoints = false;
            series.MarkerStroke = OxyColors.DarkGreen;
            series.MarkerType = OxyPlot.MarkerType.Circle;
            return series;
        }

        public static Legend SetLegendStyle(this Legend legend)
        {
            legend.LegendBackground = OxyColors.Transparent;
            legend.LegendTextColor = OxyColors.White;
            legend.SeriesInvisibleTextColor = OxyColors.Gray;
            legend.LegendTitleColor = OxyColors.White;
            legend.LegendPlacement = LegendPlacement.Outside;
            legend.LegendPosition = LegendPosition.TopRight;
            legend.LegendOrientation = LegendOrientation.Horizontal;
            return legend;
        }

        public static PlotModel InitChartModel()
        {
            OxyColor color = OxyColor.FromArgb(255, 210, 217, 234);
            PlotModel chartModel = new PlotModel
            {
                PlotAreaBackground = OxyColor.FromArgb(255, 32, 39, 54),
                TextColor = color,
                PlotAreaBorderThickness = new OxyThickness(1, 0, 0, 1),
            };

            chartModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                IsZoomEnabled = false,
                TicklineColor = color,
                MinorIntervalType = DateTimeIntervalType.Milliseconds,
                Title = I18n.Properties.Resources.Time,
                StringFormat = "yyyy-MM-dd\nHH:mm:ss",
                TitlePosition = 0.9
            });
            chartModel.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Left,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                IsZoomEnabled = false,
                IntervalLength = 20,
                MajorGridlineColor = OxyColor.FromAColor(40, color),
                MajorGridlineStyle = LineStyle.Solid,
                TicklineColor = OxyColors.Black,
                Title = I18n.Properties.Resources.DataValue,
                TitlePosition = 0.9,
            });
            chartModel.Legends.Add(new Legend().SetLegendStyle());

            return chartModel;
        }

        public static LinearAxis CreateYAxis()
        {
            return new LinearAxis()
            {
                Position = AxisPosition.Left,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                IsZoomEnabled = false,
                IntervalLength = 20,
                MajorGridlineColor = OxyColor.FromAColor(40, OxyColor.FromArgb(255, 210, 217, 234)),
                MajorGridlineStyle = LineStyle.Solid,
                TicklineColor = OxyColors.Black,
                Title = I18n.Properties.Resources.DataValue,
                TitlePosition = 0.9,
            };
        }
    }
}