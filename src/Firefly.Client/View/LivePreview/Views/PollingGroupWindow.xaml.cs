// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;

using Firefly.Client.Common;
using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for PollingGroupWindow.xaml
    /// </summary>
    public partial class PollingGroupWindow : Window
    {

        public PollingGroupWindow(PollingGroupViewModel viewModel = null)
        {
            if (viewModel == null)
            {
                ViewModel = new PollingGroupViewModel();
                ViewModel.IsAdd = true;
            }
            else
            {
                ViewModel = viewModel;
                ViewModel.IsAdd = false;
            }
            DataContext = this;
            InitializeComponent();
        }

        public PollingGroupViewModel ViewModel { get; }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadData();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (!NameHelper.IsNameCompliance(ViewModel.Name))
            {
                IRtekMessageBox.ShowDialog(GetWindow(this), I18n.Properties.Resources.InvalidName, I18n.Properties.Resources.Tip);
                return;
            }

            this.DialogResult = true;
        }
    }
}