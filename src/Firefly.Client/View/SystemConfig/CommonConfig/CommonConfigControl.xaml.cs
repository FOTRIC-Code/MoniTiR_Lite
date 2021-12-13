// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Controls;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for DatabasesConfigControl.xaml
    /// </summary>
    public partial class CommonConfigControl : TabItem
    {
        public CommonConfigControl()
        {
            ViewModel = new CommonConfigViewModel();
            DataContext = this;
            InitializeComponent();
        }

        public CommonConfigViewModel ViewModel { get; }

        private void CommonConfigControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.LoadData();
        }

        private async void btnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (int.TryParse(ViewModel.SpeechCount, out int speechCount) == false)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ContainInvalidValueError, I18n.Properties.Resources.Tip);
                return;
            }
            if (int.TryParse(ViewModel.SoundCount, out int soundCount) == false)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ContainInvalidValueError, I18n.Properties.Resources.Tip);
                return;
            }

            if (speechCount <= 0 || soundCount <= 0)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ContainInvalidValueError, I18n.Properties.Resources.Tip);
                return;
            }

            if (!int.TryParse(ViewModel.DataRetentionTime, out int dataRetentionTime))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.RetentionTimeMustGreaterThan0, I18n.Properties.Resources.Tip);
                return;
            }
            if (!int.TryParse(ViewModel.AlarmRetentionTime, out int alarmRetentionTime))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.RetentionTimeMustGreaterThan0, I18n.Properties.Resources.Tip);
                return;
            }
            if (!int.TryParse(ViewModel.LogRetentionTime, out int logRetentionTime))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.RetentionTimeMustGreaterThan0, I18n.Properties.Resources.Tip);
                return;
            }
            if (dataRetentionTime <= 0 || alarmRetentionTime <= 0 || logRetentionTime <= 0)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.RetentionTimeMustGreaterThan0, I18n.Properties.Resources.Tip);
                return;
            }

            CommonConfigDO commonConfigDO = new CommonConfigDO();
            commonConfigDO.DataRetentionTime = dataRetentionTime;
            commonConfigDO.AlarmRetentionTime = alarmRetentionTime;
            commonConfigDO.LogRetentionTime = logRetentionTime;
            commonConfigDO.SpeechCount = speechCount;
            commonConfigDO.SoundCount = soundCount;

            bool result = await SystemConfigManager.Instance.UpdateCommonConfig(commonConfigDO);
            if (!result)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip);
                ViewModel.LoadData();
                return;
            }
            else
            {
                AlarmCenterPreference.Instance.SpeechCount = speechCount;
                AlarmCenterPreference.Instance.SoundCount = soundCount;
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OK);
            }
        }
    }
}