// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;
using System.Collections.Generic;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public partial class CruiseRouteEditPresetWindow : Window
    {
        private CruisePresetViewModel cruisePresetVM;
        public List<PresetInfoViewModel> PresetList { get; set; }

        public PresetInfoViewModel SelectedPreset { get; set; }

        public CruiseRouteEditPresetWindow(CruisePresetViewModel cruisePresetDO, DevicePresetDataObject presetListDataObject)
        {
            InitializeComponent();

            PresetList = presetListDataObject.PresetList.Select(x => new PresetInfoViewModel
            {
                ID = x.ID,
                Name = I18n.Properties.Resources.Preset + x.ID
            }).ToList();
            SelectedPreset = string.IsNullOrWhiteSpace(cruisePresetDO.Name) ? PresetList.FirstOrDefault() : PresetList.FirstOrDefault(x => x.ID == cruisePresetDO.ID);

            cruisePresetVM = cruisePresetDO;
            DataContext = cruisePresetDO;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPreset == null)
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.PresetEmpeyError, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            if (cruisePresetVM.Speed > 10 || cruisePresetVM.Speed < 0)
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.VESpeed, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            if (cruisePresetVM.WaitTime > 600 || cruisePresetVM.WaitTime < 1)
            {
                IRtekMessageBox.ShowDialog(this, I18n.Properties.Resources.VEWaitime, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                return;
            }

            cruisePresetVM.ID = SelectedPreset.ID;
            cruisePresetVM.Name = SelectedPreset.Name;

            DialogResult = true;
            Close();
        }
    }

    public class PresetInfoViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
