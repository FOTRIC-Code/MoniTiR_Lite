// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows.Media;
using System.Collections.ObjectModel;

namespace Firefly.Client.Control
{
    public class IRtekFileViewModel : BaseViewModel
    {
        #region field

        private ObservableCollection<ImageItemViewModel> _imageItemVMs;

        #endregion

        #region constructor

        public IRtekFileViewModel()
        {
            _imageItemVMs = new ObservableCollection<ImageItemViewModel>();
        }

        #endregion

        #region property

        public ObservableCollection<ImageItemViewModel> ImageItemVMs
        {
            get => _imageItemVMs;
            set
            {
                _imageItemVMs = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region method

        public void Clear()
        {
            ImageItemVMs.Clear();
        }

        public void AddImageItem(ImageItemViewModel imageItemVM)
        {
            ImageItemVMs.Add(imageItemVM);
        }

        #endregion
    }

    public class ImageItemViewModel : BaseViewModel
    {
        #region field

        private string _title;
        private ImageSource _source;

        #endregion

        #region constructor

        public ImageItemViewModel(string title, ImageSource imageSource)
        {
            _title = title;
            _source = imageSource;
        }

        #endregion

        #region property

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public ImageSource Source
        {
            get => _source;
            set
            {
                _source = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}
