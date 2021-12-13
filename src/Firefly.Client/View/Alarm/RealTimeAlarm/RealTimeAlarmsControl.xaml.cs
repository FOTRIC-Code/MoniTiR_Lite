// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Collections.Generic;

using Firefly.Client.Control;
using Firefly.Client.Service;
using MessageBoxButton = Firefly.Client.Control.MessageBoxButton;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for RealTimeAlarmsControl.xaml
    /// </summary>
    public partial class RealTimeAlarmsControl : TabItem
    {
        private RealTimeAlarmsViewModel _realTimeAlarmsVM;

        public RealTimeAlarmsControl()
        {
            InitializeComponent();

            _realTimeAlarmsVM = Thread.GetData(Thread.GetNamedDataSlot(AlarmStatusBarControl.REAL_TIME_ALARM_VIEWMODEL)) as RealTimeAlarmsViewModel;
            DataContext = _realTimeAlarmsVM;
        }

        #region Methods

        private void RealTimeAlarmsControl_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _realTimeAlarmsVM.SelectedAlarm = null;
            _realTimeAlarmsVM.ClearFilter();
            _realTimeAlarmsVM.SavePreference();
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            if (_realTimeAlarmsVM.Items.Count > 0)
            {
                _realTimeAlarmsVM.SelectedAlarm = _realTimeAlarmsVM.Items[0];
            }

            AlarmOutputColum.Visibility = Visibility.Collapsed;
            _realTimeAlarmsVM?.UpdateData();
        }

        protected override void OnUnselected(RoutedEventArgs e)
        {
            base.OnUnselected(e);
            _realTimeAlarmsVM.SelectedAlarm = null;
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            if (IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsCleanListMsg, I18n.Properties.Resources.Tip, MessageBoxButton.OKCancel) == true)
            {
                _realTimeAlarmsVM.ClearAll();
            }
        }

        private void btnSound_Click(object sender, RoutedEventArgs e)
        {
            if (_realTimeAlarmsVM.EnableSound
                && IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsCloseSound, I18n.Properties.Resources.Tip, MessageBoxButton.OKCancel) == true)
            {
                _realTimeAlarmsVM.EnableSound = false;
            }
            else if (!_realTimeAlarmsVM.EnableSound
                     && IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsOpenSound, I18n.Properties.Resources.Tip, MessageBoxButton.OKCancel) == true)
            {
                _realTimeAlarmsVM.EnableSound = true;
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_realTimeAlarmsVM.SelectedAlarm != null)
            {
                AlarmConfirmNoteWindow window = new AlarmConfirmNoteWindow(_realTimeAlarmsVM.SelectedAlarm.ID, _realTimeAlarmsVM.GetAlarmDOs())
                {
                    Owner = Window.GetWindow(this)
                };
                window.DataContext = _realTimeAlarmsVM.SelectedAlarm;
                window.ShowDialog();
                _realTimeAlarmsVM?.UpdateData();
            }
        }

        private void btnExpand_Click(object sender, RoutedEventArgs e)
        {
            _realTimeAlarmsVM.DetailViewVisibility = !_realTimeAlarmsVM.DetailViewVisibility;
            if (_realTimeAlarmsVM.DetailViewVisibility)
            {
                _realTimeAlarmsVM.SelectedAlarm?.UpdateLiveVideoViewModels(_realTimeAlarmsVM.AutoPlayLiveVideo);
            }
            else
            {
                _realTimeAlarmsVM.SelectedAlarm?.ClearLiveVideo();
            }
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            AlarmRecordVideoWindow window = new AlarmRecordVideoWindow();
            window.Owner = Window.GetWindow(this);
            _realTimeAlarmsVM.SelectedAlarm.UpdateRecordVideoViewModels();
            window.DataContext = _realTimeAlarmsVM.SelectedAlarm;
            window.ShowDialog();
        }

        private void btnPlayLiveVideo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaceHolderViewModel viewModel && viewModel.IsEnable)
            {
                _realTimeAlarmsVM.SelectedAlarm?.ReplaceToVideoViewModel(viewModel);
            }
        }

        private void btnNeedPopup_Click(object sender, RoutedEventArgs e)
        {
            _realTimeAlarmsVM.NeedPopup = !_realTimeAlarmsVM.NeedPopup;
        }

        private void btnSpeech_Click(object sender, RoutedEventArgs e)
        {
            if (_realTimeAlarmsVM.EnableSpeech
                && IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsCloseSpeech, I18n.Properties.Resources.Tip, MessageBoxButton.OKCancel) == true)
            {
                _realTimeAlarmsVM.EnableSpeech = false;
            }
            else if (!_realTimeAlarmsVM.EnableSpeech
                     && IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsOpenSpeech, I18n.Properties.Resources.Tip, MessageBoxButton.OKCancel) == true)
            {
                _realTimeAlarmsVM.EnableSpeech = true;
            }
        }

        private void btnAnalyzIR_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PlaceHolderViewModel itemVM)
            {
                List<List<Service.ImageInfo>> imagesGroups = new List<List<Service.ImageInfo>>();
                int currentIndex = 0;
                imagesGroups.Add(new List<Service.ImageInfo>()
                {
                    new ImageInfo
                    {
                        ChannelID = 0,
                        FileType = FileType.IRTekFile_IR,
                        URL = itemVM.Url
                    }
                });
                new IRFileViewerWindow(imagesGroups, currentIndex) { Owner = Window.GetWindow(this) }.ShowDialog();
            }
        }

        #endregion Methods
    }
}