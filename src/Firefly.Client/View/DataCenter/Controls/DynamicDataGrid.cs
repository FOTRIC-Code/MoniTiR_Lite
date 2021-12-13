// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;
using System.Collections;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Firefly.Client
{
    public class DynamicColumnHeaderModel
    {
        public string Header { get; set; }

        public List<string> SubHeaders { get; set; }
    }

    public class DynamicDataGrid : DataGrid
    {
        static DynamicDataGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DynamicDataGrid), new FrameworkPropertyMetadata(typeof(DynamicDataGrid)));
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            if (ItemsSource is List<DataViewModel> dataSource && dataSource?.Count > 0)
            {
                while (this.Columns.Count > 3)
                {
                    this.Columns.RemoveAt(2);
                }
                var columnHeaderStyle = FindResource("DynamicColumnDataHeaderStyle") as Style;

                DataViewModel rowVM = dataSource.First();
                this.Columns.Insert(2, new DataGridTemplateColumn
                {
                    Header = rowVM.DataTitle,
                    HeaderStyle = FindResource("DynamicColumnNameHeaderStyle") as Style,
                    CellTemplate = (DataTemplate)XamlReader.Parse(GetTitleTemplateXaml("{Binding Name}"))
                });

                List<string> dynamicTypeList = new List<string>();
                int insertColumn = 3;

                for (int i = 0; i < dataSource.Count; i++)
                {
                    rowVM = dataSource[i];
                    if (rowVM.DynamicData is IRDataViewModel)
                    {
                        IRDataViewModel dynamicData = rowVM.DynamicData as IRDataViewModel;
                        if (dynamicTypeList.Contains($"{nameof(IRDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeList.Add($"{nameof(IRDataViewModel)}_{dynamicData.Name}");
                        DynamicColumnHeaderModel factorHeader = new DynamicColumnHeaderModel
                        {
                            Header = dynamicData.Name,
                            SubHeaders = new List<string> { I18n.Properties.Resources.MaxTemperatureWithSymbol, I18n.Properties.Resources.MinTemperatureWithSymbol }
                        };
                        List<string> bindingList = new List<string>();
                        bindingList.Add("{Binding " + $"DynamicData.MaxTemp" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        bindingList.Add("{Binding " + $"DynamicData.MinTemp" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        if (dynamicData.AvgTemp != null)
                        {
                            bindingList.Add("{Binding " + $"DynamicData.AvgTemp" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                            factorHeader.SubHeaders.Insert(2, I18n.Properties.Resources.AvgTemperatureWithSymbol);
                        }

                        if (dynamicData.HasAlarm)
                        {
                            factorHeader.SubHeaders.Add(I18n.Properties.Resources.AlarmLevel);
                            bindingList.Add("{Binding " + $"DynamicData.Alarm" + "}");
                        }
                        var factorCellTemplateStr = GetCellTemplateXaml(dynamicData.Name, bindingList);
                        var templateColumn = new DataGridTemplateColumn
                        {
                            Header = factorHeader,
                            HeaderStyle = columnHeaderStyle,
                            CellTemplate = (DataTemplate)XamlReader.Parse(factorCellTemplateStr)
                        };
                        this.Columns.Insert(insertColumn, templateColumn);
                        insertColumn++;
                    }
                    else if (rowVM.DynamicData is ENVDataViewModel)
                    {
                        ENVDataViewModel dynamicData = rowVM.DynamicData as ENVDataViewModel;
                        if (dynamicTypeList.Contains($"{nameof(ENVDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeList.Add($"{nameof(ENVDataViewModel)}_{dynamicData.Name}");

                        DynamicColumnHeaderModel factorHeader = new DynamicColumnHeaderModel
                        {
                            Header = dynamicData.Name,
                            SubHeaders = new List<string>()
                        };
                        List<string> bindingList = new List<string>();
                        if (dynamicData.HasTemp)
                        {
                            factorHeader.SubHeaders.Add(I18n.Properties.Resources.TemperatureWithSymbol);
                            bindingList.Add("{Binding " + $"DynamicData.Temp" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        }
                        if (dynamicData.HasRH)
                        {
                            factorHeader.SubHeaders.Add(I18n.Properties.Resources.RHWithSymbol);
                            bindingList.Add("{Binding " + $"DynamicData.RH" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        }
                        if (dynamicData.HasAlarm)
                        {
                            factorHeader.SubHeaders.Add(I18n.Properties.Resources.AlarmLevel);
                            bindingList.Add("{Binding " + $"DynamicData.Alarm" + "}");
                        }
                        var factorCellTemplateStr = GetCellTemplateXaml(dynamicData.Name, bindingList);
                        var templateColumn = new DataGridTemplateColumn
                        {
                            Header = factorHeader,
                            HeaderStyle = columnHeaderStyle,
                            CellTemplate = (DataTemplate)XamlReader.Parse(factorCellTemplateStr)
                        };
                        this.Columns.Insert(insertColumn, templateColumn);
                        insertColumn++;
                    }
                    else if (rowVM.DynamicData is VIBRDataViewModel)
                    {
                        VIBRDataViewModel dynamicData = rowVM.DynamicData as VIBRDataViewModel;
                        if (dynamicTypeList.Contains($"{nameof(VIBRDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeList.Add($"{nameof(VIBRDataViewModel)}_{dynamicData.Name}");
                        DynamicColumnHeaderModel factorHeader = new DynamicColumnHeaderModel
                        {
                            Header = dynamicData.Name,
                            SubHeaders = new List<string> { I18n.Properties.Resources.VelocityWithSymbol, I18n.Properties.Resources.AccelerationWithSymbol, I18n.Properties.Resources.TemperatureWithSymbol, I18n.Properties.Resources.VelocityAlarmLevel, I18n.Properties.Resources.AccelerationAlarmLevel, I18n.Properties.Resources.TemperatureAlarmLevel }
                        };
                        List<string> bindingList = new List<string>();
                        bindingList.Add("{Binding " + $"DynamicData.V" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        bindingList.Add("{Binding " + $"DynamicData.A" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        bindingList.Add("{Binding " + $"DynamicData.Temp" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        bindingList.Add("{Binding " + $"DynamicData.VAlarm" + "}");
                        bindingList.Add("{Binding " + $"DynamicData.AAlarm" + "}");
                        bindingList.Add("{Binding " + $"DynamicData.TempAlarm" + "}");
                        var factorCellTemplateStr = GetCellTemplateXaml(dynamicData.Name, bindingList);
                        var templateColumn = new DataGridTemplateColumn
                        {
                            Header = factorHeader,
                            HeaderStyle = columnHeaderStyle,
                            CellTemplate = (DataTemplate)XamlReader.Parse(factorCellTemplateStr)
                        };
                        this.Columns.Insert(insertColumn, templateColumn);
                        insertColumn++;
                    }
                    else if (rowVM.DynamicData is VIBRSingleDataViewModel)
                    {
                        VIBRSingleDataViewModel dynamicData = rowVM.DynamicData as VIBRSingleDataViewModel;
                        if (dynamicTypeList.Contains($"{nameof(VIBRSingleDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeList.Add($"{nameof(VIBRSingleDataViewModel)}_{dynamicData.Name}");
                        DynamicColumnHeaderModel factorHeader = new DynamicColumnHeaderModel
                        {
                            Header = dynamicData.Name,
                            SubHeaders = new List<string> { dynamicData.LegendNameWithSymbol }
                        };
                        List<string> bindingList = new List<string>();
                        bindingList.Add("{Binding " + $"DynamicData.Value" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        if (dynamicData.HasAlarm)
                        {
                            factorHeader.SubHeaders.Add(dynamicData.AlarmLevelDesc);
                            bindingList.Add("{Binding " + $"DynamicData.Alarm" + "}");
                        }
                        var factorCellTemplateStr = GetCellTemplateXaml(dynamicData.Name, bindingList);
                        var templateColumn = new DataGridTemplateColumn
                        {
                            Header = factorHeader,
                            HeaderStyle = columnHeaderStyle,
                            CellTemplate = (DataTemplate)XamlReader.Parse(factorCellTemplateStr)
                        };
                        this.Columns.Insert(insertColumn, templateColumn);
                        insertColumn++;
                    }
                    else if (rowVM.DynamicData is NoiseDataViewModel)
                    {
                        NoiseDataViewModel dynamicData = rowVM.DynamicData as NoiseDataViewModel;
                        if (dynamicTypeList.Contains($"{nameof(NoiseDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeList.Add($"{nameof(NoiseDataViewModel)}_{dynamicData.Name}");
                        DynamicColumnHeaderModel factorHeader = new DynamicColumnHeaderModel
                        {
                            Header = dynamicData.Name,
                            SubHeaders = new List<string> { I18n.Properties.Resources.NoiseWithSymbol }
                        };
                        List<string> bindingList = new List<string>();
                        bindingList.Add("{Binding " + $"DynamicData.Spl" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        if (dynamicData.HasAlarm)
                        {
                            factorHeader.SubHeaders.Add(I18n.Properties.Resources.AlarmLevel);
                            bindingList.Add("{Binding " + $"DynamicData.Alarm" + "}");
                        }
                        var factorCellTemplateStr = GetCellTemplateXaml(dynamicData.Name, bindingList);
                        var templateColumn = new DataGridTemplateColumn
                        {
                            Header = factorHeader,
                            HeaderStyle = columnHeaderStyle,
                            CellTemplate = (DataTemplate)XamlReader.Parse(factorCellTemplateStr)
                        };
                        this.Columns.Insert(insertColumn, templateColumn);
                        insertColumn++;
                    }
                    else if (rowVM.DynamicData is TcDataViewModel)
                    {
                        TcDataViewModel dynamicData = rowVM.DynamicData as TcDataViewModel;
                        if (dynamicTypeList.Contains($"{nameof(TcDataViewModel)}_{dynamicData.Name}"))
                        {
                            continue;
                        }
                        dynamicTypeList.Add($"{nameof(TcDataViewModel)}_{dynamicData.Name}");
                        DynamicColumnHeaderModel factorHeader = new DynamicColumnHeaderModel
                        {
                            Header = dynamicData.Name,
                            SubHeaders = new List<string> { I18n.Properties.Resources.TemperatureWithSymbol, I18n.Properties.Resources.AlarmLevel }
                        };
                        List<string> bindingList = new List<string>();
                        bindingList.Add("{Binding " + $"DynamicData.Temp" + ", StringFormat={}{0:F1}, FallbackValue='-'}");
                        bindingList.Add("{Binding " + $"DynamicData.Alarm" + "}");
                        var factorCellTemplateStr = GetCellTemplateXaml(dynamicData.Name, bindingList);
                        var templateColumn = new DataGridTemplateColumn
                        {
                            Header = factorHeader,
                            HeaderStyle = columnHeaderStyle,
                            CellTemplate = (DataTemplate)XamlReader.Parse(factorCellTemplateStr)
                        };
                        this.Columns.Insert(insertColumn, templateColumn);
                        insertColumn++;
                    }
                }
            }
        }

        private static string GetTitleTemplateXaml(string dataTitle)
        {
            var templateStr = "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">";
            templateStr += "<Grid><Grid.ColumnDefinitions><ColumnDefinition Width=\"*\" /><ColumnDefinition Width=\"Auto\" /></Grid.ColumnDefinitions>";
            templateStr += "<TextBlock Margin=\"{DynamicResource NormalMargin}\"" + " Text=\"" + dataTitle + "\" VerticalAlignment=\"Center\" HorizontalAlignment=\"Center\" />";
            templateStr += "</Grid></DataTemplate>";
            return templateStr;
        }

        public static string GetCellTemplateXaml(string propertyName, List<string> cellBindingList)
        {
            var templateStr = "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:lang=\"clr-namespace:Firefly.Client.I18n.Properties;assembly=Firefly.Client.I18n\">";
            templateStr += "<Grid><Grid.ColumnDefinitions><ColumnDefinition Width=\"*\" /><ColumnDefinition Width=\"Auto\" /></Grid.ColumnDefinitions>";
            templateStr += $"<UniformGrid x:Name=\"cellGird\" Rows=\"1\" Columns=\"{cellBindingList.Count}\" Visibility=\"Hidden\">";
            cellBindingList.ForEach(a =>
            {
                templateStr += "<TextBlock Margin=\"{DynamicResource NormalMargin}\" Text=\"" + a + "\" TextAlignment=\"Center\" VerticalAlignment=\"Center\" HorizontalAlignment=\"Center\" />";
            });
            templateStr += "</UniformGrid>";
            templateStr += "</Grid>";
            templateStr += "<DataTemplate.Triggers><DataTrigger Binding=\"{Binding DynamicData.Name}\" Value=\"" + propertyName + "\"><Setter TargetName=\"cellGird\" Property=\"Visibility\" Value=\"Visible\" /></DataTrigger></DataTemplate.Triggers>";
            templateStr += "</DataTemplate>";
            return templateStr;
        }
    }
}