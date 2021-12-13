using System;
using System.Windows;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for DeviceDetailMonitorWindow.xaml
    /// </summary>
    public partial class DeviceDetailMonitorWindow : Window
    {
        private DeviceItemViewModel _viewModel;
        public DeviceDetailMonitorWindow(DeviceItemViewModel viewModel)
        {
            try
            {
                _viewModel = viewModel;
                _viewModel.UpdateChart();
                DataContext = _viewModel;
                InitializeComponent();
            }
            catch
            {
                if (_viewModel != null)
                {
                    _viewModel.PlotDataModel = null;
                }
            }
        }

        private void DeviceDetailMonitorWindow_OnClosed(object sender, EventArgs e)
        {
            DataContext = null;
            _viewModel.PlotDataModel = null;
        }
    }
}
