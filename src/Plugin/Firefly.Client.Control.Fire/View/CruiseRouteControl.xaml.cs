// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for CruiseRouteControl.xaml
    /// </summary>
    public partial class CruiseRouteControl : TabItem
    {
        #region Fields

        private CruiseRouteViewModel cruiseRouteVM;
        private DevicePresetDataObject presetListDataObject;
        private TaskScheduleDataObject taskScheduleDataObject;

        #endregion

        #region Constructors

        public CruiseRouteControl()
        {
            InitializeComponent();
            cruiseRouteVM = new CruiseRouteViewModel();
            DataContext = cruiseRouteVM;
        }

        internal void LoginDevice()
        {
            irClient.IsPaused = false;
            dcClient.IsPaused = false;
        }

        internal void LogoutDevice()
        {
            irClient.IsPaused = true;
            dcClient.IsPaused = true;
        }

        #endregion

        #region Methods

        private void Load()
        {
            presetListDataObject = FireAgentDeviceManager.Instance.LoadPresetListDataObject();
            cruiseRouteVM.Load(FireAgentDeviceManager.Instance.LoadCruiseRouteDataObject());
            taskScheduleDataObject = FireAgentDeviceManager.Instance.LoadTaskScheduleDataObject();
        }

        public CruiseRouteDataObject GetCruiseRouteDataObject()
        {
            return cruiseRouteVM.ConvertToDataObject();
        }

        internal bool IsConfigChanged()
        {
            CruiseRouteDataObject oldValue = FireAgentDeviceManager.Instance.LoadCruiseRouteDataObject();
            CruiseRouteDataObject newValue = cruiseRouteVM.ConvertToDataObject();
            return JsonConvert.SerializeObject(oldValue) != JsonConvert.SerializeObject(newValue);
        }

        protected async override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);

            try
            {
                Cursor = Cursors.Wait;
                Load();
                DeviceDO deviceDO = FireAgentDeviceManager.Instance.GetDeviceDO();
                DeviceStreamDO streamDO = await DeviceManager.Instance.GetStream(deviceDO, StreamType.MainStream);
                cruiseRouteVM.SetRTSPUrl(streamDO);
                LoginDevice();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        protected override void OnUnselected(RoutedEventArgs e)
        {
            base.OnUnselected(e);

            try
            {
                Cursor = Cursors.Wait;
                cruiseRouteVM.CleanRTSPUrl();
                LogoutDevice();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void btnAddRoute_Click(object sender, RoutedEventArgs e)
        {
            if (cruiseRouteVM.Items.Count >= 10)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.CruiseRouteCountLessThanTen, I18n.Properties.Resources.Tip,
                    MessageBoxButton.None);
                return;
            }

            CruiseRouteItemViewModel viewModel = new CruiseRouteItemViewModel();
            int tempID = cruiseRouteVM.Items.Any() ? cruiseRouteVM.Items.Max(x => x.ID) + 1 : 1;
            viewModel.Name = $"{I18n.Properties.Resources.CruiseRoute}{tempID}";

            CruiseRouteEditWindow window = new CruiseRouteEditWindow(cruiseRouteVM, presetListDataObject, viewModel);
            window.Owner = Window.GetWindow(this);
            bool? result = window.ShowDialog();
            if (result == true)
            {
                viewModel.ID = tempID;
                cruiseRouteVM.Items.Add(viewModel);
            }
        }

        private void cbtnCruiseDelete_Click(object sender, RoutedEventArgs e)
        {
            if (cruiseRouteVM?.SelectedItem == null)
            {
                return;
            }

            if (IRtekMessageBox.ShowDialog(Window.GetWindow(this), string.Format(I18n.Properties.Resources.IsDelete, cruiseRouteVM.SelectedItem.Name),
                    I18n.Properties.Resources.Delete, MessageBoxButton.OKCancel) != true)
            {
                return;
            }

            if (taskScheduleDataObject.TaskDayList.Any(taskDay => taskDay.TaskTimeList.Any(taskTime => taskTime.CruiseRouteID == cruiseRouteVM.SelectedItem.ID)))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.DeleteCruiseRouteNotify, cruiseRouteVM.SelectedItem.Name);
                return;
            }

            cruiseRouteVM.Items.Remove(cruiseRouteVM.SelectedItem);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckData())
            {
                return;
            }

            SaveDataToServer();
        }

        private async void SaveDataToServer()
        {
            try
            {
                Cursor = Cursors.Wait;
                CruiseRouteDataObject cruiseRouteDataObject = cruiseRouteVM.ConvertToDataObject();
                ApiResult<bool> result = await FireAgentDeviceManager.Instance.UpdateDeviceConfig(new FireAgentDataObject {CruiseRouteConfig = cruiseRouteDataObject});
                if (result.IsSuccess)
                {
                    FireAgentDeviceManager.Instance.UpdateCruiseRouteDataObject(cruiseRouteDataObject);
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

        private bool CheckData()
        {
            List<IGrouping<string, CruiseRouteItemViewModel>> nameGroup = cruiseRouteVM.Items.GroupBy(x => x.Name).ToList();
            if (nameGroup.Any(x => x.Count() > 1))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.RepeatedNameError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return false;
            }

            if (cruiseRouteVM.Items.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.EmptyNameError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        private async void cbtnCruisePlay_Click(object sender, RoutedEventArgs e)
        {
            if (cruiseRouteVM?.SelectedItem != null)
            {
                ApiResult<bool> apiResult = await DeviceManager.Instance.SetPTZCruise(FireAgentDeviceManager.Instance.GetDeviceDO(),
                    new PTZCruiseDataObject()
                    {
                        Command = PTZCruiseCommand.RUN,
                        CruiseID = cruiseRouteVM.SelectedItem.ID
                    });

                if (!apiResult.IsSuccess)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.FixMonitoringNoCruise, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
        }

        private async void cbtnCruisePause_Click(object sender, RoutedEventArgs e)
        {
            if (cruiseRouteVM?.SelectedItem != null)
            {
                ApiResult<bool> apiResult = await DeviceManager.Instance.SetPTZCruise(FireAgentDeviceManager.Instance.GetDeviceDO(),
                    new PTZCruiseDataObject()
                    {
                        Command = PTZCruiseCommand.STOP,
                        CruiseID = cruiseRouteVM.SelectedItem.ID
                    });

                if (!apiResult.IsSuccess)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.FixMonitoringNoCruise, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
        }

        private void cbtnCruiseEdit_Click(object sender, RoutedEventArgs e)
        {
            if (cruiseRouteVM.SelectedItem != null)
            {
                CruiseRouteItemViewModel itemViewModel = cruiseRouteVM.SelectedItem.Clone();
                CruiseRouteEditWindow window = new CruiseRouteEditWindow(cruiseRouteVM, presetListDataObject, itemViewModel);
                window.Owner = Window.GetWindow(this);

                bool? result = window.ShowDialog();
                if (result == true)
                {
                    cruiseRouteVM.SelectedItem.CopyFrom(itemViewModel);
                }
            }
        }
        #endregion
    }
}
