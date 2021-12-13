// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class IRFileViewerViewModel : BaseViewModel
    {
        private IRFileViewerItemViewModel _selectedItem;
        private bool _isPrevGroupEnabled;
        private bool _isNextGroupEnabled;

        public IRFileViewerViewModel()
        {
            ImageList = new ObservableCollection<IRFileViewerItemViewModel>();
        }

        public ObservableCollection<IRFileViewerItemViewModel> ImageList { get; set; }

        public IRFileViewerItemViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged();
                    HasImage = _selectedItem != null;
                    OnPropertyChanged(nameof(HasImage));
                }
            }
        }

        public bool HasImage { get; set; }

        public bool IsPrevGroupEnabled
        {
            get => _isPrevGroupEnabled;
            set
            {
                _isPrevGroupEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsNextGroupEnabled
        {
            get => _isNextGroupEnabled;
            set
            {
                _isNextGroupEnabled = value;
                OnPropertyChanged();
            }
        }

        public void ClearImage()
        {
            ImageList?.Clear();
            SelectedItem = null;
        }
    }

    public class IRFileViewerItemViewModel : BaseViewModel
    {
        private string _fileName;
        private FileType _fileType;
        private ImageSource _imageSource;

        public IRFileViewerItemViewModel(string fileName, FileType fileType)
        {
            FileName = fileName;
            _fileType = fileType;
        }

        public FileType FileType => _fileType;

        public string ImageTitle { get; set; }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;

                if(File.Exists(value))
                {
                    ImageSource = new BitmapImage(new Uri(_fileName));
                }
                OnPropertyChanged();
            }
        }

        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }
    }

    public enum FileImageType
    {
        None,
        ThermalImage,
        VisualImage,
        DefectImage
    }
}