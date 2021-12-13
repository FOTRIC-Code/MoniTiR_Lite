// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for AlarmScheduleControl.xaml
    /// </summary>
    public partial class AlarmScheduleControl : TabItem
    {
        #region Fields

        private AlarmScheduleViewModel alarmScheduleVM;
        private List<DayGrid> dayGrids;

        #endregion

        #region Contructors
        public AlarmScheduleControl()
        {
            InitializeComponent();
            alarmScheduleVM = new AlarmScheduleViewModel();
            DataContext = alarmScheduleVM;

            dayGrids = new List<DayGrid>();
            dayGrids.Add(dpvMonday);
            dayGrids.Add(dpvTuesday);
            dayGrids.Add(dpvWednesday);
            dayGrids.Add(dpvThursday);
            dayGrids.Add(dpvFriday);
            dayGrids.Add(dpvSaturday);
            dayGrids.Add(dpvSunday);
        }
        #endregion

        #region Methods

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            Load();
        }

        private void Load()
        {
            alarmScheduleVM.Load(FireAgentDeviceManager.Instance.LoadAlarmScheduleDataObject());
            foreach (DayGrid dayGrid in dayGrids)
            {
                dayGrid.Load();
            }
        }

        private List<Duration> ConvertToDurations()
        {
            List<Duration> durations = dayGrids.Select(item => item.ConvertToDuration()).ToList();
            if (durations.All(item => item.DurationItems.Count == 0))
            {
                durations = new List<Duration>();
            }
            return durations;
        }

        internal bool IsConfigChanged()
        {
            List<Duration> durations = ConvertToDurations();
            AlarmScheduleDataObject oldValue = FireAgentDeviceManager.Instance.LoadAlarmScheduleDataObject();
            if (oldValue.AlarmDayList.All(item => item.AlarmTimeList.Count == 0))
            {
                oldValue.AlarmDayList = new List<AlarmDay>();
            }
            AlarmScheduleDataObject newValue = alarmScheduleVM.ConvertToDataObject(durations);
            return JsonConvert.SerializeObject(oldValue) != JsonConvert.SerializeObject(newValue);
        }

        public AlarmScheduleDataObject GetAlarmScheduleDataObject()
        {
            List<Duration> durations = ConvertToDurations();
            return alarmScheduleVM.ConvertToDataObject(durations);
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                ClearSelectedDruations();
                PART_day_override.Visibility = Visibility.Hidden;

                List<Duration> durations = ConvertToDurations();
                AlarmScheduleDataObject alarmScheduleDataObject = alarmScheduleVM.ConvertToDataObject(durations);
                ApiResult<bool> result = await FireAgentDeviceManager.Instance.UpdateDeviceConfig(new FireAgentDataObject { AlarmSchedule = alarmScheduleDataObject });
                if (result.IsSuccess)
                {
                    FireAgentDeviceManager.Instance.UpdateAlarmScheduleDataObject(alarmScheduleDataObject);
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else if (result.IsForbidden)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), ErrorCodeTranslator.Translate(result.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void PART_day_override_SaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                OverrideRoutedEventArgs args = e as OverrideRoutedEventArgs;

                DayGrid dayGrid = dayGrids.Where(item => item.Source.DayOfWeek == args.FormDayOfWeek).FirstOrDefault();
                if (dayGrids != null)
                {
                    Duration formDuration = dayGrid.ConvertToDuration();
                    foreach (DayOfWeek dayOfWork in args.ToDayOfWeeks)
                    {
                        DayGrid destinationDayGrid = dayGrids.Where(item => item.Source.DayOfWeek == dayOfWork).FirstOrDefault();
                        destinationDayGrid?.OverrideDayGridItem(formDuration.DurationItems);
                    }
                }
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private void DayGrid_OverrideButtonClick(object sender, RoutedEventArgs e)
        {
            ClearSelectedDruations();

            DayGrid dayGrid = sender as DayGrid;
            if (dayGrid != null)
            {
                Point point = dayGrid.TranslatePoint(new Point(0, 0), sp_day_of_week);
                double left = sp_day_of_week.ActualWidth - PART_day_override.ActualWidth - dayGrid.GetOverrideButtonActualWidth();
                double top = sp_day_of_week.ActualHeight - point.Y;
                Thickness thickness = new Thickness
                {
                    Top = -top,
                    Left = left,
                    Right = 0,
                    Bottom = 0,
                };
                PART_day_override.Margin = thickness;
                DayOfWeek dayOfWeek = dayGrid.Source.DayOfWeek;
                PART_day_override.SetFromDayOfWeek(dayOfWeek);
                PART_day_override.ClearChecked();
                PART_day_override.Visibility = Visibility.Visible;
            }
        }

        private void DayGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PART_day_override.Visibility = Visibility.Hidden;

            var currentDayGrid = (DayGrid)sender;
            foreach (DayGrid dayGrid in dayGrids.Where(item => item != currentDayGrid))
            {
                dayGrid.ClearSelectedItemStatus();
            }
        }

        private void btn_delete_all_Click(object sender, RoutedEventArgs e)
        {
            PART_day_override.Visibility = Visibility.Hidden;
            foreach (DayGrid dayGrid in dayGrids)
            {
                dayGrid.DeleteAll();
            }
        }

        private void btn_delete_Click(object sender, RoutedEventArgs e)
        {
            PART_day_override.Visibility = Visibility.Hidden;
            foreach (DayGrid dayGrid in dayGrids)
            {
                dayGrid.DeleteSelectedItem();
            }
        }

        private void ClearSelectedDruations()
        {
            foreach (DayGrid dayGrid in dayGrids)
            {
                dayGrid.ClearSelectedItemStatus();
            }
        }

        private void DayGrid_ItemStatusChanged(object sender, RoutedEventArgs e)
        {
            alarmScheduleVM.HasSelectedItem = dayGrids.Any(item => item.HasSelectedItem);
            alarmScheduleVM.HasItem = dayGrids.Any(item => item.HasItem);
        }
        #endregion
    }
}
