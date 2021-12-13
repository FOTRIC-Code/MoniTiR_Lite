// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Firefly.Client.Common;
using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for FireServiceControl.xaml
    /// </summary>
    public partial class GeneralControl : TabItem
    {
        #region Fields
        private GeneralViewModel generalVM;
        #endregion

        #region Constructors
        public GeneralControl()
        {
            InitializeComponent();
            generalVM = new GeneralViewModel();
            DataContext = generalVM;
        }
        #endregion

        #region Methods
        private async void Load()
        {
            await FireAgentDeviceManager.Instance.GetDeviceConfig();
            generalVM.Load(FireAgentDeviceManager.Instance.LoadGeneralConfigDataObject());
        }

        public bool IsConfigChanged()
        {
            // 这里使用.Result，因为此时肯定是及时返回，不会await
            GeneralConfigDataObject oldValue = FireAgentDeviceManager.Instance.LoadGeneralConfigDataObject();
            GeneralConfigDataObject newValue = generalVM.ConvertToDataObject();
            return JsonConvert.SerializeObject(oldValue) != JsonConvert.SerializeObject(newValue);
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            Load();
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                if (!Regex.IsMatch(tbSampleInterval.Text, Constants.FLOAT_REG) || generalVM.SampleInterval < 0.1 || generalVM.SampleInterval > 3600)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.VESampleInterval, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                    return;
                }

                if (!Regex.IsMatch(tbDataRecordInterval.Text, Constants.NUMBER_REG) || generalVM.DataRecordInterval < 1 || generalVM.DataRecordInterval > 36000)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.VEDataRecordInterval, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                    return;
                }

                if (!Regex.IsMatch(tbImageRecordInterval.Text, Constants.NUMBER_REG) || generalVM.ImageRecordInterval < 1 || generalVM.ImageRecordInterval > 36000)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.VEImageRecordInterval, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                    return;
                }

                GeneralConfigDataObject generalConfigDataObject = generalVM.ConvertToDataObject();
                ApiResult<bool> result = await FireAgentDeviceManager.Instance.UpdateDeviceConfig(new FireAgentDataObject { GeneralConfig = generalConfigDataObject });
                if (result.IsSuccess)
                {
                    FireAgentDeviceManager.Instance.UpdateGeneralConfig(generalConfigDataObject);
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else if (result.IsForbidden)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), ErrorCodeTranslator.Translate(result.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        public GeneralConfigDataObject GetGeneralConfigDataObject()
        {
            return generalVM.ConvertToDataObject();
        }
        #endregion
    }
}
