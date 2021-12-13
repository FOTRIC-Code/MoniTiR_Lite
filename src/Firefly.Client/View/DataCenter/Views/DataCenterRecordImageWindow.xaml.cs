// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Net;
using System.Linq;
using System.Windows;
using System.Drawing;
using System.Threading;
using System.Windows.Media;
using System.IO.Compression;
using System.ComponentModel;
using System.Collections.Generic;

using Firefly.Client.Common;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for DataCenterRecordImageWindow.xaml
    /// </summary>
    public partial class DataCenterRecordImageWindow : Window
    {
        private DataCenterRecordImageWindowViewModel _viewModel;

        public DataCenterRecordImageWindow(string imageUrl)
        {
            InitializeComponent();
            _viewModel = new DataCenterRecordImageWindowViewModel()
            {
                ImageList = new List<AlarmImageViewModel>()
            };

            using (WebClient webClient = new WebClient())
            {
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        byte[] result = webClient.DownloadData(imageUrl);
                        using (MemoryStream ms = new MemoryStream(result))
                        {
                            using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Read, true))
                            {
                                foreach (ZipArchiveEntry entry in zip.Entries)
                                {
                                    using (Stream imageStream = entry.Open())
                                    {
                                        Bitmap image = new Bitmap(imageStream);
                                        //image.Save("D:\\Source\\test.jpg");
                                        image.Dispose();
                                    }
                                }
                            }
                        }
                        break;
                    }
                    catch
                    {
                        retryCount++;
                        Thread.Sleep(Constants.TIMEOUT_REQUEST_TEST);
                        if (retryCount > Constants.DOWNLOAD_IMAGE_RETRY_COUNT)
                        {
                            break;
                        }
                    }
                }
            }

            DataContext = _viewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            
            if(_viewModel != null)
            {
                _viewModel.ImageList?.ForEach(item =>
                {
                    item.Image = null;
                });
                _viewModel.ImageList?.Clear();
                _viewModel.ImageList = null;
                _viewModel = null;
            }
        }
    }

    public class DataCenterRecordImageWindowViewModel
    {
        public List<AlarmImageViewModel> ImageList { get; set; }

        public bool HasImage => ImageList?.Any() ?? false;
    }

    public class AlarmImageViewModel
    {
        public ImageSource Image { get; set; }
    }
}
