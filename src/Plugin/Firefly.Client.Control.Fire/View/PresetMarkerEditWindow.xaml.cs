// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

using Firefly.Client.Common;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for EditMarkerWindow.xaml
    /// </summary>
    public partial class PresetMarkerEditWindow : Window
    {
        private List<Marker> _markers;
        private Marker _markerVM;

        public PresetMarkerEditWindow(Marker marker, List<Marker> markers)
        {
            InitializeComponent();
            Init(marker, markers);
            _markerVM.Title = I18n.Properties.Resources.Edit;
        }

        public PresetMarkerEditWindow(DeviceMarker deviceMarker, List<Marker> markers)
        {
            InitializeComponent();
            Init(new Marker(deviceMarker), markers);
            _markerVM.Title = I18n.Properties.Resources.Add;
        }

        private void Init(Marker marker, List<Marker> markers)
        {
            _markers = markers;
            RawMarker = marker;
            _markerVM = new Marker(marker);

            DataContext = _markerVM;
        }

        public Marker RawMarker { get; private set; }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_markerVM.Name))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.EmptyNameError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            if (_markers.Any(x => x != RawMarker && x.Name == _markerVM.Name))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.RepeatedNameError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            if (!NameHelper.IsNameCompliance(_markerVM.Name))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.VELongName, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            RawMarker.CopyDataObject(_markerVM);

            DialogResult = true;
        }
    }
}
