// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;
using MessageBoxButton = Firefly.Client.Control.MessageBoxButton;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for NVREditWindow.xaml
    /// </summary>
    public partial class NVREditWindow : Window
    {
        private StoreServerConfigViewModel _storeServerConfigVM;
        private StoreServerConfigItemViewModel _viewModel;

        public NVREditWindow(StoreServerConfigViewModel storeServerConfigVM, StoreServerConfigItemViewModel viewModel)
        {
            InitializeComponent();
            _storeServerConfigVM = storeServerConfigVM;
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        #region Methods

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!NameHelper.IsNameCompliance(tbName.Text))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.InvalidName, I18n.Properties.Resources.Tip);
                return;
            }

            if (string.IsNullOrWhiteSpace(tbIP.Text) || !Regex.IsMatch(tbIP.Text, Constants.IP_REG))
            {
                IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.InvalidIP, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(tbPort.Text))
            {
                IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.InvalidPort, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            if (_storeServerConfigVM.Items.Any(item => tbName.Text.Equals(item.Name) && _viewModel.ID != item.ID))
            {
                IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.NameExist, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }
            
            if (_storeServerConfigVM.Items.Any(item => tbIP.Text.Equals(item.IP) && tbPort.Text.Equals(item.Port.ToString()) && _viewModel.ID != item.ID))
            {
                IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.ServerExist, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            bool result = false;
            if (_viewModel.ID > 0)
            {
                result = await StoreManager.Instance.UpdateNVR(_storeServerConfigVM.StoreAddress, _viewModel.GetDataObject());
            }
            else
            {
                result = await StoreManager.Instance.AddNVR(_storeServerConfigVM.StoreAddress, _viewModel.GetDataObject());
            }

            if (result)
            {
                DialogResult = true;
            }
            else
            {
                IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
            }
        }
        
        #endregion
    }
}
