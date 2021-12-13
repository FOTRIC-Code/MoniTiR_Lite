// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Text.RegularExpressions;

using Firefly.Client.Common;
using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for SubDeviceControl.xaml
    /// </summary>
    public partial class SubDeviceDetailWindow : Window
    {
        private SubDeviceDetailViewModel _subDeviceDetailVM;

        public SubDeviceDetailWindow(SubDeviceDetailViewModel subDeviceDetailVM)
        {
            InitializeComponent();
            _subDeviceDetailVM = subDeviceDetailVM;
        }

        public SubDeviceDetailWindow(SubDeviceViewModel subDeviceVM)
        {
            InitializeComponent();
            _subDeviceDetailVM = new SubDeviceDetailViewModel(subDeviceVM);
            DataContext = _subDeviceDetailVM;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_subDeviceDetailVM.SubDeviceVM.IP) || !Regex.IsMatch(_subDeviceDetailVM.SubDeviceVM.IP, Constants.IP_REG))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.InvalidIP, I18n.Properties.Resources.SaveFailed, Control.MessageBoxButton.OK);
                return;
            }

            DialogResult = true;
        }

        private void SubDeviceDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _subDeviceDetailVM.LoadSensorChannel();
        }
    }
}
