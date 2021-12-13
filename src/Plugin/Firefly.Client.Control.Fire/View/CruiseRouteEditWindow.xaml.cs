// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public partial class CruiseRouteEditWindow : Window
    {
        private CruiseRouteItemViewModel _viewModel;
        private CruiseRouteViewModel _cruiseRouteVM;
        private DevicePresetDataObject _presetListDataObject;

        public CruiseRouteEditWindow(CruiseRouteViewModel cruiseRouteVM, DevicePresetDataObject presetListDataObject, CruiseRouteItemViewModel viewModel)
        {
            InitializeComponent();
            _cruiseRouteVM = cruiseRouteVM;
            this._presetListDataObject = presetListDataObject;
            this._viewModel = viewModel;
            this.DataContext = viewModel;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CheckData())
            {
                DialogResult = true;
                Close();
            }
        }

        private bool CheckData()
        {
            if (_cruiseRouteVM.Items.Any(it => it.Name == _viewModel.Name && it.ID != _viewModel.ID))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.RepeatedNameError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_viewModel.Name))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.EmptyNameError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        private void cbtnAddPreset_Click(object sender, RoutedEventArgs e)
        {
            CruisePresetViewModel cruisePresetVM = new CruisePresetViewModel();
            CruiseRouteEditPresetWindow window = new CruiseRouteEditPresetWindow(cruisePresetVM, _presetListDataObject);
            window.Owner = Window.GetWindow(this);
            if (window.ShowDialog() == true)
            {
                _viewModel.Presets.Add(cruisePresetVM);
                FlushMoveButtonStatus();
            }
        }

        private void FlushMoveButtonStatus()
        {
            if (!_viewModel.Presets.Any())
            {
                return;
            }
            foreach (CruisePresetViewModel preset in _viewModel.Presets)
            {
                preset.CanDown = true;
                preset.CanUp = true;
            }
            _viewModel.Presets.FirstOrDefault().CanUp = false;
            _viewModel.Presets.LastOrDefault().CanDown = false;
        }

        private void cbtMarkerEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgPresets.SelectedItem is CruisePresetViewModel selectedItem)
            {
                CruisePresetViewModel cruisePresetVM = selectedItem.Clone();
                CruiseRouteEditPresetWindow window = new CruiseRouteEditPresetWindow(cruisePresetVM, _presetListDataObject);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    selectedItem.Copy(cruisePresetVM);
                }
            }
        }

        private void cbtnPresetDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgPresets.SelectedItem is CruisePresetViewModel selectedItem)
            {
                bool? result = IRtekMessageBox.ShowDialog(Window.GetWindow(this), string.Format(I18n.Properties.Resources.IsDelete, selectedItem.Name),
                    I18n.Properties.Resources.Delete,
                    MessageBoxButton.OKCancel);
                if (result == true)
                {
                    _viewModel.Presets.Remove(selectedItem);
                    FlushMoveButtonStatus();
                }
            }
        }

        private void cbtnPresetMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int moveTargetIndex = dgPresets.SelectedIndex - 1;
            if (dgPresets.SelectedIndex > 0 && moveTargetIndex >= 0)
            {
                _viewModel.Presets.Move(dgPresets.SelectedIndex, moveTargetIndex);
                FlushMoveButtonStatus();
            }
        }

        private void cbtnPresetMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int moveTargetIndex = dgPresets.SelectedIndex + 1;
            if (dgPresets.SelectedIndex < _viewModel.Presets.Count - 1&& moveTargetIndex <= _viewModel.Presets.Count())
            {
                _viewModel.Presets.Move(dgPresets.SelectedIndex, moveTargetIndex);
                FlushMoveButtonStatus();
            }
        }
    }
}
