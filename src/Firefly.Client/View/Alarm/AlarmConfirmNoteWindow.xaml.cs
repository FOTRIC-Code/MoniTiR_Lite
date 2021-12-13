// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cursors = System.Windows.Input.Cursors;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public partial class AlarmConfirmNoteWindow : Window
    {
        #region field

        private int _alarmID;
        private List<AlarmDO> _alarmDOs;

        #endregion

        #region constructor

        public AlarmConfirmNoteWindow(int alarmID, List<AlarmDO> alarmDOs)
        {
            InitializeComponent();
            _alarmID = alarmID;
            _alarmDOs = alarmDOs;
        }

        #endregion

        #region method

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
                bool isEffectiveNote = Regex.IsMatch(tbNote.Text.TrimEnd(), Constants.NOTE_REG);
                if (!isEffectiveNote)
                {
                    IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.InvalidNote, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OKCancel);
                }
                else
                {
                    AlarmDO alarmDO = _alarmDOs.FirstOrDefault(item => item.ID == _alarmID);
                    string deviceID = alarmDO.DeviceID;
                    List<AlarmConfirmerDataObject> alarmConfirmerDOs = new List<AlarmConfirmerDataObject>();
                    if (cbConfirmConfig.IsChecked == true)
                    {
                        foreach (AlarmDO alarmDOItem in _alarmDOs.Where(item => item.DeviceID == deviceID && !item.IsConfirmed))
                        {
                            alarmConfirmerDOs.Add(new AlarmConfirmerDataObject(alarmDOItem.ID, true, tbNote.Text.TrimEnd()));
                        }
                    }
                    else
                    {
                        alarmConfirmerDOs.Add(new AlarmConfirmerDataObject(alarmDO.ID, true, tbNote.Text.TrimEnd()));
                    }
                    if (await ConfirmAlarm(alarmConfirmerDOs))
                    {
                        DialogResult = true;
                    }
                    else
                    {
                        IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OKCancel);
                    }
                }
            }
            finally
            {
                this.IsEnabled = true;
                Mouse.OverrideCursor = null;
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async Task<bool> ConfirmAlarm(List<AlarmConfirmerDataObject> alarmConfirmerDOs)
        {
            List<Task<bool>> updateAlarmTask = new List<Task<bool>>();
            foreach (AlarmConfirmerDataObject alarmConfirmerDO in alarmConfirmerDOs)
            {
                updateAlarmTask.Add(AlarmManager.Instance.ConfirmAlarm(alarmConfirmerDO));
            }
            await Task.WhenAll(updateAlarmTask);

            return updateAlarmTask.All(item => item.Result);
        }

        #endregion
    }
}