// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for AlarmRuleEditControl.xaml
    /// </summary>
    public partial class AlarmRuleEditWindow : Window
    {
        private readonly AlarmRuleViewModel _alarmRuleViewModel;
        private AlarmRuleItemViewModel _viewModel;

        public AlarmRuleEditWindow(AlarmRuleViewModel alarmRuleViewModel, AlarmRuleItemViewModel viewModel)
        {
            InitializeComponent();
            _alarmRuleViewModel = alarmRuleViewModel;
            this.DataContext = viewModel;
            _viewModel = viewModel;
            PART_Title.Title = viewModel.ID > 0 ? I18n.Properties.Resources.Edit : I18n.Properties.Resources.Add;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(twiName.Text))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.EmptyNameError, I18n.Properties.Resources.Tip, MessageBoxButton.None);
                return;
            }

            if (_alarmRuleViewModel.Items.Any(it => string.Equals(it.Name, _viewModel.Name) && it.ID != _viewModel.ID))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.RepeatedNameError, I18n.Properties.Resources.Tip, MessageBoxButton.None);
                return;
            }

            if (_viewModel.Alert1Temp < -100 || _viewModel.Alert1Temp > 1000)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.VETemperature, I18n.Properties.Resources.Tip);
                return;
            }

            if (_viewModel.Alert2Temp < -100 || _viewModel.Alert2Temp > 1000)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.VETemperature, I18n.Properties.Resources.Tip);
                return;
            }

            if (_viewModel.AlarmTemp < -100 || _viewModel.AlarmTemp > 1000)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.VETemperature, I18n.Properties.Resources.Tip);
                return;
            }

            this.DialogResult = true;
            Close();
        }
    }
}
