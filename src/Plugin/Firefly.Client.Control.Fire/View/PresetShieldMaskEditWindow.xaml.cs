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
    /// Interaction logic for PresetShieldMaskEditWindow.xaml
    /// </summary>
    public partial class PresetShieldMaskEditWindow : Window
    {
        private List<ShieldMask> _shieldMasks;
        private ShieldMask _shieldMaskVM;

        public PresetShieldMaskEditWindow(ShieldMask shieldMask, List<ShieldMask> shieldMasks)
        {
            InitializeComponent();
            Init(shieldMask, shieldMasks);

            _shieldMaskVM.Title = I18n.Properties.Resources.Edit;
        }

        public PresetShieldMaskEditWindow(DeviceMarker deviceMarker, List<ShieldMask> shieldMasks)
        {
            InitializeComponent();
            Init(new ShieldMask(deviceMarker), shieldMasks);
            _shieldMaskVM.Title = I18n.Properties.Resources.Add;
        }

        private void Init(ShieldMask shieldMask, List<ShieldMask> shieldMasks)
        {
            RawShieldMask = shieldMask;
            _shieldMasks = shieldMasks;
            _shieldMaskVM = new ShieldMask(shieldMask);

            DataContext = _shieldMaskVM;
        }

        public ShieldMask RawShieldMask { get; private set; }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_shieldMaskVM.Name))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.EmptyNameError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            if (_shieldMasks.Any(x => x != RawShieldMask && x.Name == _shieldMaskVM.Name))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.RepeatedNameError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            if (!NameHelper.IsNameCompliance(_shieldMaskVM.Name))
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.VELongName, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            RawShieldMask.CopyDataObject(_shieldMaskVM);

            DialogResult = true;
        }
    }
}
