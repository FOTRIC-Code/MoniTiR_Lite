// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

using Firefly.Client.Control;

namespace Firefly.Client.Control.Fire
{
    public partial class ExportDialog : Window
    {
        #region Constructors

        private ExportDialog(Window owner, string text)
        {
            InitializeComponent();
            this.Owner = owner;
            tbExport.Text = text;
        }

        #endregion

        #region Methods

        public static bool? ShowDialog(Window owner, string text)
        {
            ExportDialog messageBox = new ExportDialog(owner, text);

            return messageBox.ShowDialog();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                CheckPathExists = true,
                Filter = I18n.Properties.Resources.ExportFileFilter
            };
            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                string fileName = saveFileDialog.FileName;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }

                using (FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.SetLength(0);

                    byte[] content = Encoding.UTF8.GetBytes(tbExport.Text);
                    fileStream.Write(content, 0, content.Length);
                    fileStream.Flush();
                }

                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.ExportSuccess, I18n.Properties.Resources.Export, MessageBoxButton.OK);
            }
        }

        #endregion
    }
}
