// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Firefly.Client
{
    public partial class AmurPlotView : UserControl
    {
        private Dictionary<string, List<double>> _originYDictionary = new Dictionary<string, List<double>>();
        public ObservableCollection<PlotPointViewModel> AllDataList { get; set; }

        private List<PlotLegendViewModel> _plotLegendList;

        public PlotModel CanvasModel { get; set; }

        public AmurPlotView()
        {
            InitializeComponent();
            CanvasModel = ChartHelper.InitChartModel();
        }

        public void RefreshChart(List<PlotLegendViewModel> plotLegendList, ObservableCollection<PlotPointViewModel> plotPointList)
        {
            if (plotLegendList == null || plotPointList == null || plotLegendList.Count == 0 || plotPointList.Count == 0)
            {
                return;
            }

            if (AllDataList != null && AllDataList.Count > 0)
            {
                if(CanvasModel.Series.Count > 0)
                {
                    CanvasModel.Series.Clear();
                    List<Axis> axisList = CanvasModel.Axes.Where(it => it.Position != AxisPosition.Bottom).ToList();
                    if(axisList.Count > 0)
                    {
                        foreach (var item in axisList)
                        {
                            CanvasModel.Axes.Remove(item);
                        }
                        CanvasModel.Axes.Add(ChartHelper.CreateYAxis());
                    }
                    CanvasModel.InvalidatePlot(true);
                }
            }
            _plotLegendList = plotLegendList;
            lbLegend.ItemsSource = plotLegendList;
            AllDataList = plotPointList;
            lbLegend.SelectedIndex = 0;
        }

        #region Events

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                PlotLegendViewModel legendVM = e.AddedItems[0] as PlotLegendViewModel;
                var nameGroupList = AllDataList.GroupBy(it => it.Name).ToList();
                foreach (var nameGroup in nameGroupList)
                {
                    if (string.Equals(nameGroup.Key, legendVM.Name))
                    {
                        if (legendVM.LineSeries == null)
                        {
                            legendVM.LineSeries = new LineSeries().SetSeriesStyle(legendVM.Name, "", legendVM.Color);
                            legendVM.LineSeries.RenderInLegend = false;
                            legendVM.LineSeries.IsVisible = true;
                        }
                        legendVM.LineSeries.Points.Clear();
                        var enumerator = nameGroup.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            PlotPointViewModel plotPoint = enumerator.Current;
                            legendVM.LineSeries.Points.Add(DateTimeAxis.CreateDataPoint(plotPoint.DateTime, plotPoint.Value));
                        }
                        double max = legendVM.LineSeries.Points.Max(it => it.Y);
                        double min = legendVM.LineSeries.Points.Min(it => it.Y);
                        if (max <= min)
                        {
                            max = min + 1;
                        }

                        LinearAxis yAxis = GetYAxis(I18n.Properties.Resources.DataValue);
                        if (yAxis != null)
                        {
                            yAxis.Title = legendVM.AxisTitle;
                        }

                        yAxis = GetYAxis(legendVM.AxisTitle);
                        if (yAxis == null)
                        {
                            AxisPosition nextPosition = AxisPosition.Right;
                            List<Axis> axisList = CanvasModel.Axes.Where(it => it.Position == AxisPosition.Left || it.Position == AxisPosition.Right).ToList();
                            if (axisList.Count > 1)
                            {
                                Axis removedAxis = axisList.First();
                                CanvasModel.Axes.Remove(removedAxis);
                                axisList.Remove(removedAxis);
                                List<PlotLegendViewModel> removedPlotLegendList = _plotLegendList.Where(it => string.Equals(it.AxisTitle, removedAxis.Title)).ToList();
                                removedPlotLegendList?.ForEach(it =>
                                {
                                    if (it.LineSeries != null && CanvasModel.Series.Contains(it.LineSeries))
                                    {
                                        if (sender is ListBox listBox)
                                        {
                                            listBox.SelectedItems.Remove(it);
                                        }
                                    }
                                });
                            }
                            if (axisList.Count > 0)
                            {
                                nextPosition = axisList[0].Position == AxisPosition.Left ? AxisPosition.Right : AxisPosition.Left;
                            }

                            yAxis = ChartHelper.CreateYAxis();
                            yAxis.Title = legendVM.AxisTitle;
                            yAxis.Position = nextPosition;
                            yAxis.Maximum = max + 1;
                            yAxis.Minimum = min - 1;
                            CanvasModel.Axes.Add(yAxis);
                        }
                        legendVM.LineSeries.YAxisKey = legendVM.AxisTitle;
                        yAxis.Key = legendVM.AxisTitle;

                        CanvasModel.Series.Add(legendVM.LineSeries);
                    }
                }
                CanvasModel.ResetAllAxes();
                CanvasModel.InvalidatePlot(true);
            }

            if (e.RemovedItems.Count > 0)
            {
                PlotLegendViewModel legendVM = e.RemovedItems[0] as PlotLegendViewModel;
                var first = CanvasModel.Series.FirstOrDefault(it => string.Equals(it.Title, legendVM.Name));
                if (first != null)
                {
                    CanvasModel.Series.Remove(first);
                    if (CanvasModel.Series.Count == 1)
                    {
                        List<Axis> yAxisList = CanvasModel.Axes.Where(it => it.Position != AxisPosition.Bottom).ToList();
                        if(yAxisList.Count > 0)
                        {
                            if(!yAxisList.All(it => string.Equals(it.Title, legendVM?.AxisTitle)))
                            {
                                yAxisList.ForEach(item =>
                                {
                                    if (string.Equals(item.Title, legendVM?.AxisTitle))
                                    {
                                        CanvasModel.Axes.Remove(item);
                                    }
                                    else
                                    {
                                        item.Position = AxisPosition.Left;
                                    }
                                });
                            }
                        }
                    }
                    else if(CanvasModel.Series.Count == 0)
                    {
                        Axis firstOrDefault = CanvasModel.Axes.FirstOrDefault(it => it.Position != AxisPosition.Bottom);
                        if (firstOrDefault != null)
                        {
                            CanvasModel.Axes.Remove(firstOrDefault);
                            CanvasModel.Axes.Add(ChartHelper.CreateYAxis());
                        }
                    }
                    CanvasModel.InvalidatePlot(true);
                }
            }
        }

        private void btnHorizontalShrinkage_Click(object sender, RoutedEventArgs e)
        {
            Axis firstOrDefault = CanvasModel.Axes.FirstOrDefault(it => it is DateTimeAxis);
            if (firstOrDefault is DateTimeAxis xAxis)
            {
                if (!_originYDictionary.ContainsKey(xAxis.Title))
                {
                    _originYDictionary.Add(xAxis.Title, new List<double>
                    {
                        xAxis.DataMaximum + 0.1,
                        xAxis.DataMinimum - 0.1
                    });
                }
                double maximum = xAxis.ActualMaximum;
                double minimum = xAxis.ActualMinimum;
                DateTime maximumDateTime = DateTimeAxis.ToDateTime(xAxis.ActualMaximum);
                DateTime minimumDateTime = DateTimeAxis.ToDateTime(xAxis.ActualMinimum);

                if (maximumDateTime.Subtract(minimumDateTime).TotalHours > 5)
                {
                    maximum = DateTimeAxis.ToDouble(maximumDateTime.AddHours(1));
                    minimum = DateTimeAxis.ToDouble(minimumDateTime.AddHours(-1));
                }
                else if (maximumDateTime.Subtract(minimumDateTime).TotalMinutes > 70)
                {
                    maximum = DateTimeAxis.ToDouble(maximumDateTime.AddMinutes(30));
                    minimum = DateTimeAxis.ToDouble(minimumDateTime.AddMinutes(-30));
                }
                else if (maximumDateTime.Subtract(minimumDateTime).TotalMinutes > 5)
                {
                    maximum = DateTimeAxis.ToDouble(maximumDateTime.AddMinutes(1));
                    minimum = DateTimeAxis.ToDouble(minimumDateTime.AddMinutes(-1));
                }
                xAxis.Maximum = maximum;
                xAxis.Minimum = minimum;
                CanvasModel.InvalidatePlot(true);
            }
        }

        private void btnVerticalShrinkage_Click(object sender, RoutedEventArgs e)
        {
            if (CanvasModel.Axes.Count > 0)
            {
                foreach (Axis axis in CanvasModel.Axes)
                {
                    if (axis is DateTimeAxis)
                    {
                        continue;
                    }
                    if (!_originYDictionary.ContainsKey(axis.Title))
                    {
                        _originYDictionary.Add(axis.Title, new List<double>
                    {
                        axis.DataMaximum + 0.1,
                        axis.DataMinimum - 0.1
                    });
                    }

                    double maximum = axis.ActualMaximum;
                    double minimum = axis.ActualMinimum;

                    if (maximum - minimum > 100)
                    {
                        maximum += 10;
                        minimum -= 10;
                    }
                    else if (maximum - minimum > 10)
                    {
                        maximum += 1;
                        minimum -= 1;
                    }
                    else
                    {
                        maximum += 0.1;
                        minimum -= 0.1;
                    }
                    axis.Maximum = maximum;
                    axis.Minimum = minimum;
                }
                CanvasModel.InvalidatePlot(true);
            }
        }

        private void btnAdaptive_Click(object sender, RoutedEventArgs e)
        {
            if (CanvasModel.Axes?.Count > 0)
            {
                foreach (Axis axis in CanvasModel.Axes)
                {
                    if (_originYDictionary.TryGetValue(axis.Title, out List<double> originList))
                    {
                        axis.Maximum = originList[0];
                        axis.Minimum = originList[1];
                    }
                }
                _originYDictionary.Clear();
                CanvasModel.ResetAllAxes();
                CanvasModel.InvalidatePlot(true);
            }
        }

        private void btnHorizontalMagnification_Click(object sender, RoutedEventArgs e)
        {
            Axis firstOrDefault = CanvasModel.Axes.FirstOrDefault(it => it is DateTimeAxis);
            if (firstOrDefault is DateTimeAxis xAxis)
            {
                if (!_originYDictionary.ContainsKey(xAxis.Title))
                {
                    _originYDictionary.Add(xAxis.Title, new List<double>
                    {
                        xAxis.DataMaximum + 0.1,
                        xAxis.DataMinimum - 0.1
                    });
                }

                double maximum = xAxis.ActualMaximum;
                double minimum = xAxis.ActualMinimum;
                DateTime maximumDateTime = DateTimeAxis.ToDateTime(xAxis.ActualMaximum);
                DateTime minimumDateTime = DateTimeAxis.ToDateTime(xAxis.ActualMinimum);

                if (maximumDateTime.Subtract(minimumDateTime).TotalHours > 5)
                {
                    maximum = DateTimeAxis.ToDouble(maximumDateTime.AddHours(-1));
                    minimum = DateTimeAxis.ToDouble(minimumDateTime.AddHours(1));
                }
                else if (maximumDateTime.Subtract(minimumDateTime).TotalMinutes > 70)
                {
                    maximum = DateTimeAxis.ToDouble(maximumDateTime.AddMinutes(-30));
                    minimum = DateTimeAxis.ToDouble(minimumDateTime.AddMinutes(30));
                }
                else if (maximumDateTime.Subtract(minimumDateTime).TotalMinutes > 5)
                {
                    maximum = DateTimeAxis.ToDouble(maximumDateTime.AddMinutes(-1));
                    minimum = DateTimeAxis.ToDouble(minimumDateTime.AddMinutes(1));
                }
                xAxis.Maximum = maximum;
                xAxis.Minimum = minimum;
                CanvasModel.InvalidatePlot(true);
            }
        }

        private void btnVerticalMagnification_Click(object sender, RoutedEventArgs e)
        {
            if (CanvasModel.Axes?.Count > 0)
            {
                foreach (Axis axis in CanvasModel.Axes)
                {
                    if (axis is DateTimeAxis)
                    {
                        continue;
                    }
                    if (!_originYDictionary.ContainsKey(axis.Title))
                    {
                        _originYDictionary.Add(axis.Title, new List<double>
                    {
                        axis.DataMaximum + 0.1,
                        axis.DataMinimum - 0.1
                    });
                    }

                    double maximum = axis.ActualMaximum;
                    double minimum = axis.ActualMinimum;

                    if (maximum - minimum > 100)
                    {
                        maximum -= 10;
                        minimum += 10;
                    }
                    else if (maximum - minimum > 10)
                    {
                        maximum -= 1;
                        minimum += 1;
                    }
                    else if (maximum - minimum > 1)
                    {
                        maximum -= 0.1;
                        minimum += 0.1;
                    }
                    else if (maximum - minimum > 0.3)
                    {
                        maximum -= 0.1;
                        minimum += 0.1;
                    }
                    axis.Maximum = maximum;
                    axis.Minimum = minimum;
                }
                CanvasModel.InvalidatePlot(true);
            }
        }

        #endregion Events

        private LinearAxis GetYAxis(string title)
        {
            Axis axis = CanvasModel.Axes.FirstOrDefault(it => string.Equals(it.Title, title) && it is LinearAxis);
            if (axis != null && axis is LinearAxis linearAxis)
            {
                return linearAxis;
            }
            return null;
        }
    }

    public class PlotLegendViewModel
    {
        public string Name { get; set; }
        public string AxisTitle { get; set; }
        public SolidColorBrush Foreground { get; set; }
        public OxyColor Color { get; set; }
        public LinearAxis YAxis { get; set; }
        public LineSeries LineSeries { get; set; }
    }

    public class PlotPointViewModel
    {
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
    }
}