// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.Generic;

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public partial class AlarmRuleWindow : Window
    {
        private AlarmRuleViewModel alarmRuleViewModel;

        private List<int> deletedIDList;

        public Action<AlarmRuleItemViewModel> AlarmRuleSelectedCallback { get; set; }

        public Action<List<int>> AlarmRuleDeletedCallback { get; set; }

        public int SelectedRuleID { get; set; }

        public AlarmRuleWindow()
        {
            InitializeComponent();
            alarmRuleViewModel = new AlarmRuleViewModel();
            alarmRuleViewModel.Load(FireAgentDeviceManager.Instance.LoadAlarmRuleDataObject());
            DataContext = alarmRuleViewModel;
            deletedIDList = new List<int>();
        }

        private void AlarmRuleControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (SelectedRuleID != 0)
            {
                AlarmRuleItemViewModel selectedItem = alarmRuleViewModel.Items.FirstOrDefault(it => it.ID == SelectedRuleID);
                dgRule.SelectedItem = selectedItem;
            }
            else if(alarmRuleViewModel.Items.Count > 0)
            {
                dgRule.SelectedIndex = 0;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            AlarmRuleDataObject oldValue = FireAgentDeviceManager.Instance.LoadAlarmRuleDataObject();
            AlarmRuleDataObject newValue = alarmRuleViewModel.GetAlarmRuleDataObject();

            if (!string.Equals(JsonConvert.SerializeObject(newValue), JsonConvert.SerializeObject(oldValue)) && DialogResult == null)
            {
                if (IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsLeave, I18n.Properties.Resources.Operation, MessageBoxButton.OKCancel) == true)
                {
                    DialogResult = false;
                    return;
                }

                e.Cancel = true;
            }
        }

        private void btChoose_Click(object sender, RoutedEventArgs e)
        {
            AlarmRuleDataObject oldValue = FireAgentDeviceManager.Instance.LoadAlarmRuleDataObject();
            AlarmRuleDataObject newValue = alarmRuleViewModel.GetAlarmRuleDataObject();
            if (!string.Equals(JsonConvert.SerializeObject(newValue), JsonConvert.SerializeObject(oldValue)))
            {
                if (IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsLeave, I18n.Properties.Resources.Operation, MessageBoxButton.OKCancel) == true)
                {
                    DialogResult = false;
                    Close();
                }
            }
            else
            {
                AlarmRuleSelectedCallback?.Invoke(alarmRuleViewModel.SelectedItem);
                AlarmRuleDeletedCallback?.Invoke(deletedIDList);
                DialogResult = true;
                Close();
            }
        }

        private void cbRuleDelete_Click(object sender, RoutedEventArgs e)
        {
            bool? res = IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsDelete, I18n.Properties.Resources.Delete, MessageBoxButton.OKCancel);
            if (res != true)
            {
                /* cancel deleting */
                return;
            }

            if (dgRule.SelectedItem is AlarmRuleItemViewModel vm)
            {
                alarmRuleViewModel.Items.Remove(vm);
                if (!deletedIDList.Contains(vm.ID))
                {
                    deletedIDList.Add(vm.ID);
                }
            }
        }

        private async void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            AlarmRuleDataObject alarmRuleDO = alarmRuleViewModel.GetAlarmRuleDataObject();
            ApiResult<bool> result = await FireAgentDeviceManager.Instance.UpdateDeviceConfig(new FireAgentDataObject {AlarmRuleConfig = alarmRuleDO});
            if (result.IsSuccess)
            {
                FireAgentDeviceManager.Instance.UpdateAlarmRuleDataObject(alarmRuleDO);
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                alarmRuleViewModel.Load(alarmRuleViewModel.GetAlarmRuleDataObject());
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

        private void cbRuleEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgRule.SelectedItem is AlarmRuleItemViewModel itemViewModel)
            {
                AlarmRuleItemViewModel cloneViewModel = itemViewModel.Clone();
                AlarmRuleEditWindow editControl = new AlarmRuleEditWindow(alarmRuleViewModel, cloneViewModel);
                editControl.Owner = Window.GetWindow(this);
                bool? result = editControl.ShowDialog();
                if (result == true)
                {
                    itemViewModel.Load(cloneViewModel.GetAlarmRuleItemDataObject());
                }
            }
        }

        private void cbAddRule_Click(object sender, RoutedEventArgs e)
        {
            if (alarmRuleViewModel.Items.Count < 50)
            {
                AlarmRuleItemViewModel alarmRuleItemVM = new AlarmRuleItemViewModel();
                AlarmRuleEditWindow editControl = new AlarmRuleEditWindow(alarmRuleViewModel, alarmRuleItemVM);
                editControl.Owner = Window.GetWindow(this);

                bool? result = editControl.ShowDialog();
                if (result == true)
                {
                    alarmRuleItemVM.ID = alarmRuleViewModel.Items.Count + 1;
                    alarmRuleViewModel.Items.Add(alarmRuleItemVM);
                }
            }
            else
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.AlarmRuleFullMessage, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
            }
        }

        private bool CheckAlarmRuleItemValidFunc(AlarmRuleItemViewModel newValue)
        {
            if (alarmRuleViewModel.Items.Any(it => string.Equals(it.Name, newValue.Name) && it.ID != newValue.ID))
            {
                return false;
            }

            return true;
        }
    }
}
