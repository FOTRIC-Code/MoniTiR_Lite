// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

using Firefly.Client.Service;
using Firefly.Client.Service.LiteDb;

namespace Firefly.Client
{
    public partial class IRFileViewerWindow : Window
    {
        private IRFileViewerViewModel _viewModel;
        private List<List<IRFileViewerFileInfo>> _imagesGroups;
        private int _currentIndex;

        public IRFileViewerWindow(List<List<ImageInfo>> imagesGroups, int currentIndex) : this(new List<List<IRFileViewerFileInfo>>(), currentIndex)
        {
            foreach (List<ImageInfo> imageInfos in imagesGroups)
            {
                List<IRFileViewerFileInfo> fileInfos = new List<IRFileViewerFileInfo>();
                foreach (ImageInfo imageInfo in imageInfos)
                {
                    fileInfos.Add(new IRFileViewerFileInfo(imageInfo));
                }
                _imagesGroups.Add(fileInfos);
            }
        }

        public IRFileViewerWindow(List<List<IRFileViewerFileInfo>> imagesGroups, int currentIndex)
        {
            InitializeComponent();
            _imagesGroups = imagesGroups;
            _currentIndex = currentIndex;
            _viewModel = new IRFileViewerViewModel();
            DataContext = _viewModel;
            this.Loaded += IRFileViewerWindow_Loaded;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            CloseFile();
        }

        private void IRFileViewerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.IsPrevGroupEnabled = _currentIndex > 0;
            _viewModel.IsNextGroupEnabled = _currentIndex < _imagesGroups.Count - 1;
            LoadImage();
        }

        private void lbImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is IRFileViewerItemViewModel itemViewModel)
            {
                OpenFile(itemViewModel);
            }
        }

        private void btnPrevGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
            }

            _viewModel.IsPrevGroupEnabled = _currentIndex > 0;
            _viewModel.IsNextGroupEnabled = _currentIndex < _imagesGroups.Count - 1;
            LoadImage();
        }

        private void btnNextGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _imagesGroups.Count - 1)
            {
                _currentIndex++;
            }

            _viewModel.IsPrevGroupEnabled = _currentIndex > 0;
            _viewModel.IsNextGroupEnabled = _currentIndex < _imagesGroups.Count - 1;
            LoadImage();
        }

        private async void LoadImage()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                CloseFile();
                _viewModel.ClearImage();
                List<IRFileViewerFileInfo> images = _imagesGroups[_currentIndex];
                foreach (IRFileViewerFileInfo image in images)
                {
                    string filePath = string.Empty;
                    if (image.IsLocal)
                    {
                        filePath = image.FilePath;
                    }
                    else
                    {
                        FileDataObject fileDO = await IRTekHttpClient.DownlodFileAsync(image.URL);
                        if (fileDO.HasFile)
                        {
                            string dirPath = Path.GetTempPath();

                            if (!Directory.Exists(dirPath))
                            {
                                Directory.CreateDirectory(dirPath);
                            }
                            filePath = Path.Combine(dirPath, fileDO.FileName);
                            fileDO.Save(filePath);
                        }
                    }
                    _viewModel.ImageList.Add(new IRFileViewerItemViewModel(filePath, image.FileType));
                }
                _viewModel.SelectedItem = _viewModel.ImageList.FirstOrDefault();
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private void OpenFile(IRFileViewerItemViewModel itemViewModel)
        {
            if (itemViewModel.FileType == FileType.IRTekFile_IR || itemViewModel.FileType == FileType.None)
            {
                uniFileControl.Visibility = Visibility.Collapsed;
                irtekFileControl.Visibility = Visibility.Visible;
                uniIRFileControl.Visibility = Visibility.Collapsed;

                if (!irtekFileControl.OpenFile(itemViewModel.FileName))
                {
                    uniFileControl.Visibility = Visibility.Visible;
                    irtekFileControl.Visibility = Visibility.Collapsed;
                    uniIRFileControl.Visibility = Visibility.Collapsed;
                    uniFileControl.OpenFile(itemViewModel.FileName);
                };
            }
            else if (itemViewModel.FileType == FileType.UNIFile_IR)
            {
                uniFileControl.Visibility = Visibility.Collapsed;
                irtekFileControl.Visibility = Visibility.Collapsed;
                uniIRFileControl.Visibility = Visibility.Visible;

                uniIRFileControl.OpenFile(itemViewModel.FileName);
            }
            else if (itemViewModel.FileType == FileType.DCFile)
            {
                uniFileControl.Visibility = Visibility.Visible;
                irtekFileControl.Visibility = Visibility.Collapsed;
                uniIRFileControl.Visibility = Visibility.Collapsed;

                uniFileControl.OpenFile(itemViewModel.FileName);
            }
        }

        private void CloseFile()
        {
            uniFileControl.Visibility = Visibility.Collapsed;
            irtekFileControl.Visibility = Visibility.Collapsed;
            uniIRFileControl.Visibility = Visibility.Collapsed;

            irtekFileControl.CloseFile();
            uniIRFileControl.CloseFile();
            uniFileControl.CloseFile();
        }
    }

    public class IRFileViewerFileInfo
    {
        public IRFileViewerFileInfo(int channelID, string filePath, FileType fileType)
        {
            IsLocal = true;
            ChannelID = channelID;
            FilePath = filePath;
            FileType = fileType;
        }

        public IRFileViewerFileInfo(ImageInfo imageInfo)
        {
            ChannelID = imageInfo.ChannelID;
            FileType = imageInfo.FileType;
            URL = imageInfo.URL;

            if (LocalImagePath.TryGetExistingLocalPath(imageInfo.URL, out string localPath))
            {
                IsLocal = true;
                FilePath = localPath;
            }
            else
            {
                IsLocal = false;
            }
        }

        public bool IsLocal { get; set; }

        public int ChannelID { get; set; }

        public string URL { get; set; }

        public FileType FileType { get; set; }

        public string FilePath { get; set; }
    }
}
