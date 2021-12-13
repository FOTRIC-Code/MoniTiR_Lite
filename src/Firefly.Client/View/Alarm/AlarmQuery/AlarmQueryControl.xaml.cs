// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public partial class AlarmQueryControl : TabItem
    {
        #region Constructors

        public AlarmQueryControl()
        {
            ViewModel = new AlarmQueryViewModel();
            DataContext = this;
            InitializeComponent();
            ViewModel.ShowErrorMessageDialogCallback = ShowErrorMessageDialogCallback;
        }

        #endregion Constructors

        public AlarmQueryViewModel ViewModel { get; }

        #region Methods

        private void ShowErrorMessageDialogCallback(string msg)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), msg, I18n.Properties.Resources.Tip);
            }
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            try
            {
                Cursor = Cursors.Wait;
                ViewModel.AlarmItems.Clear();
                AlarmOutputColum.Visibility = Visibility.Collapsed;
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        protected override void OnUnselected(RoutedEventArgs e)
        {
            base.OnUnselected(e);
            ViewModel.SelectedAlarm = null;
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                await LoadData();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private async Task LoadData()
        {
            PaginationData<List<AlarmDO>> paginationAlarmDOs =
                await AlarmManager.Instance.GetAlarmList(new TimePageInfo()
                {
                    StartTime = ViewModel.BeginTime.ToUniversalTime(),
                    EndTime = ViewModel.EndTime.ToUniversalTime(),
                    AlarmConfirmationStatus = ViewModel.SelectedAlarmStatusOption.Value,
                    AlarmTypes = ViewModel.GetSelectedAlarmTypeStr(),
                    PageSize = Constants.PAGE_SIZE_INFINITAS
                });

            ViewModel.LoadAlarmData(paginationAlarmDOs.Data);
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            bool? result = CSVWriter.OuputData(ViewModel.ConvertSelectedItemsToCSV());
            if (result == null)
            {
                return;
            }
            IRtekMessageBox.ShowDialog(Window.GetWindow(this), result == true ? I18n.Properties.Resources.ExportSuccess : I18n.Properties.Resources.ExportFailed, I18n.Properties.Resources.Tip);
        }

        private void btnExportAll_Click(object sender, RoutedEventArgs e)
        {
            bool? result = CSVWriter.OuputData(ViewModel.ConvertAllToCSV());
            if (result == null)
            {
                return;
            }
            IRtekMessageBox.ShowDialog(Window.GetWindow(this), result == true ? I18n.Properties.Resources.ExportSuccess : I18n.Properties.Resources.ExportFailed, I18n.Properties.Resources.Tip);
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (dgAlarm.SelectedItem is AlarmItemViewModel item)
            {
                AlarmConfirmNoteWindow window = new AlarmConfirmNoteWindow(item.ID, ViewModel.GetAlarmDOs())
                {
                    Owner = Window.GetWindow(this)
                };
                window.DataContext = ViewModel.SelectedAlarm;
                if (window.ShowDialog() == true)
                {
                    _ = LoadData();
                }
            }
        }

        private void cbSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                foreach (AlarmItemViewModel alarmQueryItemViewModel in ViewModel.AlarmItems)
                {
                    alarmQueryItemViewModel.IsSelected = checkBox.IsChecked ?? false;
                }

                ViewModel.FlushExportButton();
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

        private void btnExpand_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DetailViewVisibility = !ViewModel.DetailViewVisibility;
        }

        private void btnPlayLiveVideo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaceHolderViewModel viewModel && viewModel.IsEnable)
            {
                ViewModel.SelectedAlarm?.ReplaceToVideoViewModel(viewModel);
            }
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            AlarmRecordVideoWindow window = new AlarmRecordVideoWindow();
            window.Owner = Window.GetWindow(this);
            ViewModel.SelectedAlarm.UpdateRecordVideoViewModels();
            window.DataContext = ViewModel.SelectedAlarm;
            window.ShowDialog();
        }
    }
}