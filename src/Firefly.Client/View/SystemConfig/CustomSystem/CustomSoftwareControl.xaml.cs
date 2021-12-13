using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for CustomSoftwareControl.xaml
    /// </summary>
    public partial class CustomSoftwareControl : TabItem
    {
        public CustomSoftwareControl()
        {
            ViewModel = new CustomSoftwareViewModel();
            this.DataContext = this;
            InitializeComponent();
        }

        public CustomSoftwareViewModel ViewModel { get; }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.SoftwareIcon))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidSoftwareIcon, I18n.Properties.Resources.Warning);
                return;
            }

            if (!File.Exists(ViewModel.SoftwareIcon))
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidSoftwareIcon, I18n.Properties.Resources.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(ViewModel.SoftwareName))
            {
                string name = ViewModel.SoftwareName.Replace("\\n", "*");
                if (name.Split('*').Length > 1)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidName, I18n.Properties.Resources.Warning);
                    return;
                }

                if (ViewModel.SoftwareName.Contains("\\n"))
                {
                    ViewModel.SoftwareName = ViewModel.SoftwareName.Replace("\\n", "\r\n");
                }
            }

            if (string.IsNullOrWhiteSpace(ViewModel.SoftwareName) || !Regex.IsMatch(ViewModel.SoftwareName, Constants.SOFTWARE_NAME_REG) || ViewModel.SoftwareName.Count() > Constants.MAX_NAME_LENGTH)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.InvalidName, I18n.Properties.Resources.Warning);
                return;
            }

            CustomSoftwareDO customSoftwareDO = new CustomSoftwareDO();
            if (string.Equals(ViewModel.SoftwareIcon, SystemConfigPreference.Intance.SoftwareIconPath))
            {
                customSoftwareDO.IconPath = "";
            }
            else
            {
                ApiResult<string> result = await SystemConfigManager.Instance.UploadSoftwareIcon(ViewModel.SoftwareIcon);
                if (!result.IsSuccess)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.TipUploadFileFailed, I18n.Properties.Resources.Warning);
                    return;
                }
                customSoftwareDO.IconPath = result.Data;
            }

            customSoftwareDO.SoftwareName = ViewModel.SoftwareName;
            customSoftwareDO.I18n = AppPreference.Instance.SystemLanguage;
            customSoftwareDO.FontSize = ViewModel.FontSize;
            bool success = await SystemConfigManager.Instance.UpdateCustomSoftware(customSoftwareDO);
            if (success)
            {
                SystemConfigPreference.Intance.SoftwareIconPath = string.Empty;
                SystemConfigPreference.Intance.SoftwareDownloadIconPath = string.Empty;
                SystemConfigPreference.Intance.Save();
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip);
            }
            else
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip);
            }
        }

        private async void CustomSoftwareControl_Loaded(object sender, RoutedEventArgs e)
        {
            CustomSoftwareDO customSoftwareDO = await SystemConfigManager.Instance.GetCustomSoftware();
            ViewModel.SoftwareName = customSoftwareDO.SoftwareName;
            ViewModel.FontSize = customSoftwareDO.FontSize <= 0 ? 16 : customSoftwareDO.FontSize;
            if (!string.IsNullOrWhiteSpace(customSoftwareDO?.IconPath) && File.Exists(customSoftwareDO.IconPath))
            {
                ViewModel.SoftwareIcon = customSoftwareDO.IconPath;
                SystemConfigPreference.Intance.SoftwareIconPath = customSoftwareDO.IconPath;
                SystemConfigPreference.Intance.SoftwareDownloadIconPath = customSoftwareDO.IconPath;
                SystemConfigPreference.Intance.Save();
            }

            ViewModel.SoftwareIcon = SystemConfigPreference.Intance.SoftwareDownloadIconPath;
            ViewModel.SoftwareIconName = string.IsNullOrWhiteSpace(SystemConfigPreference.Intance.SoftwareIconPath)
                ? string.Empty
                : System.IO.Path.GetFileName(SystemConfigPreference.Intance.SoftwareIconPath);
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "File|*.png";
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (ofd.ShowDialog() == true)
            {
                string iconPath = ofd.FileName;
                if (File.Exists(iconPath))
                {
                    ViewModel.SoftwareIconName = System.IO.Path.GetFileName(iconPath);
                    ViewModel.SoftwareIcon = iconPath;
                }
            }
        }
    }
}