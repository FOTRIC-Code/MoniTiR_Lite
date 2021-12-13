// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows.Controls;
using System.Collections.Generic;
using Firefly.Client.Control;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for AlarmPage.xaml
    /// </summary>
    public partial class AlarmPage : Page, IPage
    {
        public AlarmPage()
        {
            InitializeComponent();
        }

        public void OnClosed()
        {
        }

        public void SearchAlarm(List<long> searchTimestampList)
        {
            tabAlarm.SelectedIndex = 0;
            if (realTimeAlarm.DataContext is RealTimeAlarmsViewModel viewModel)
            {
                viewModel.SearchTimestampList = searchTimestampList;
            }
        }
    }
}
