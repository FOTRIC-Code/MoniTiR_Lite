// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;

using Firefly.Client.Control;
using Firefly.Client.Service;
using MessageBoxButton = Firefly.Client.Control.MessageBoxButton;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for StoreServerConfigWindow.xaml
    /// </summary>
    public partial class StoreServerConfigWindow : Window
    {
        private StoreServerConfigViewModel _viewModel;

        public StoreServerConfigWindow(string storeIP, int storePort)
        {
            InitializeComponent();
            _viewModel = new StoreServerConfigViewModel();
            _viewModel.StoreAddress = $"http://{storeIP}:{storePort}";
            this.DataContext = _viewModel;
        }

        #region Methods

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                _ = Refresh();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private async Task Refresh()
        {
            List<NVRDataObject> nvrDOList = await StoreManager.Instance.GetNVRList(_viewModel.StoreAddress);
            List<NVRChannelDataObject> nvrChannelDOList = await StoreManager.Instance.GetChannelList(_viewModel.StoreAddress);
            _viewModel.LoadDataObject(nvrDOList, nvrChannelDOList);
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem != null && IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.IsDelete, I18n.Properties.Resources.Tip, MessageBoxButton.OKCancel) == true)
            {
                try
                {
                    Cursor = Cursors.Wait;
                    await StoreManager.Instance.DeleteNVR(_viewModel.StoreAddress, _viewModel.SelectedItem.ID);
                    await Refresh();
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem != null)
            {
                NVREditWindow window = new NVREditWindow(_viewModel,_viewModel.SelectedItem.Clone())
                {
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                }; 
                if (window.ShowDialog() == true)
                {
                    try
                    {
                        Cursor = Cursors.Wait;
                        await Refresh();
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }
            }
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            NVREditWindow window = new NVREditWindow(_viewModel,new StoreServerConfigItemViewModel())
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            if (window.ShowDialog() == true)
            {
                try
                {
                    Cursor = Cursors.Wait;
                    await Refresh();
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                await Refresh();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        #endregion

    }
}
