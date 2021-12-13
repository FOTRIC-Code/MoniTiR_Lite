// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Window = System.Windows.Window;

using Firefly.Client.Service;
using Firefly.Client.Control;
using IRtekMessageBox = Firefly.Client.Control.IRtekMessageBox;
using MessageBoxButton = Firefly.Client.Control.MessageBoxButton;

namespace Firefly.Client
{
    public partial class AlarmStatusBarControl : UserControl
    {
        public const string REAL_TIME_ALARM_VIEWMODEL = "RealTimeAlarmViewModel";
        private Storyboard _warningStoryboard;
        private bool _storyFlag = false;
        private RealTimeAlarmsViewModel _realTimeAlarmsVM;
        private AlarmTipWindow _alarmTipWindow;

        public AlarmStatusBarControl()
        {
            InitializeComponent();
            AlarmManager.Instance.AddOrUpdateAlarmEvent += AddOrUpdateAlarmEvent;
            AlarmCenterPreference.Instance.Load();
            _realTimeAlarmsVM = new RealTimeAlarmsViewModel();
            LocalDataStoreSlot storeSlot = Thread.GetNamedDataSlot(REAL_TIME_ALARM_VIEWMODEL);
            if (storeSlot != null)
            {
                Thread.FreeNamedDataSlot(REAL_TIME_ALARM_VIEWMODEL);
            }
            storeSlot = Thread.AllocateNamedDataSlot(REAL_TIME_ALARM_VIEWMODEL);
            Thread.SetData(storeSlot, _realTimeAlarmsVM);
            this.DataContext = _realTimeAlarmsVM;
        }

        #region Methods

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                _realTimeAlarmsVM?.UpdateData();
            }
        }

        private void AddOrUpdateAlarmEvent(AlarmDO alarmDO)
        {
            this.Dispatcher.Invoke(() =>
            {
                DeviceDO deviceDO = DeviceManager.Instance.GetCached(alarmDO.DeviceID);
                if (deviceDO == null)
                {
                    return;
                }

                _realTimeAlarmsVM.AddOrUpdateAlarm(alarmDO);
                StartWarrning();

                if (Window.GetWindow(this) is MainWindow window)
                {
                    RoutedEventArgs args = new RoutedEventArgs
                    {
                        RoutedEvent = MainPage.NotifyBalloonTipEvent,
                        Source = new BalloonTipArgs
                        {
                            Message = alarmDO.AlarmDescription,
                            Type = BalloonTipType.Alarm
                        }
                    };
                    window.RaiseEvent(args);
                }

                if (!alarmDO.IsConfirmed)
                {
                    TryPopupWindow(alarmDO);
                }
            });
        }

        private void TryPopupWindow(AlarmDO alarmDO)
        {
            lock (this)
            {
                if (_realTimeAlarmsVM.NeedPopup && (_alarmTipWindow == null || PresentationSource.FromVisual(_alarmTipWindow) == null))
                {
                    DeviceDO deviceDO = DeviceManager.Instance.GetCached(alarmDO.DeviceID);
                    if (deviceDO != null)
                    {
                        _alarmTipWindow = new AlarmTipWindow(alarmDO, deviceDO)
                        {
                            Owner = Window.GetWindow(this),
                        };
                        _alarmTipWindow.Show();
                    }
                }
            }
        }

        private void IrcbMax_OnClick(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args = new RoutedEventArgs
            {
                RoutedEvent = MainPage.NavigatingEvent,
                Source = this.Tag
            };
            RaiseEvent(args);
        }

        private void StartWarrning()
        {
            if (_warningStoryboard == null)
            {
                _warningStoryboard = this.FindResource("NotifyWarringStoryboard") as Storyboard;
                _warningStoryboard?.Stop();
                _warningStoryboard?.Begin();
                _storyFlag = true;
            }

            if (!_storyFlag)
            {
                _warningStoryboard?.Begin();
                _storyFlag = true;
            }
        }

        private void StopWarrning()
        {
            if (_warningStoryboard == null)
            {
                _warningStoryboard = this.FindResource("NotifyWarringStoryboard") as Storyboard;
            }

            if (_storyFlag)
            {
                _warningStoryboard?.Pause();
                _storyFlag = false;
            }
        }

        private void btnExpand_Click(object sender, RoutedEventArgs e)
        {
            _realTimeAlarmsVM.AlarmBarVisibility = !_realTimeAlarmsVM.AlarmBarVisibility;
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

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            AlarmRecordVideoWindow window = new AlarmRecordVideoWindow();
            window.Owner = Window.GetWindow(this);
            _realTimeAlarmsVM.SelectedAlarm.UpdateRecordVideoViewModels();
            window.DataContext = _realTimeAlarmsVM.SelectedAlarm;
            window.ShowDialog();
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

                StopWarrning();
                _realTimeAlarmsVM?.UpdateData();
            }
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            if (IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsCleanListMsg, I18n.Properties.Resources.Tip, MessageBoxButton.OKCancel) == true)
            {
                _realTimeAlarmsVM.ClearAll();
            }
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            _storyFlag = false;
            _warningStoryboard?.Stop();
        }

        #endregion Methods
    }
}