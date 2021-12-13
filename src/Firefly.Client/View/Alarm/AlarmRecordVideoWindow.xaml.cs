// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Controls;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for AlarmRecordVideoWindow.xaml
    /// </summary>
    public partial class AlarmRecordVideoWindow : Window
    {
        public AlarmRecordVideoWindow()
        {
            InitializeComponent();
        }
        
        private void btnPlayVideo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaceHolderViewModel placeHolderViewModel && placeHolderViewModel.IsEnable && this.DataContext is AlarmItemViewModel alarmItemViewModel)
            {
                alarmItemViewModel.ReplaceToRecordVideoViewModel(placeHolderViewModel);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (this.DataContext is AlarmItemViewModel alarmItemViewModel)
            {
                alarmItemViewModel.ClearRecordVideo();
            }
            this.DataContext = null;
        }
    }
}
