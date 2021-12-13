// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.ObjectModel;

using Firefly.Client.Common;

namespace Firefly.Client.Control
{
    public delegate void PageChangedEventHandler(object sender, PageChangedEventArgs args);

    public class PageChangedEventArgs : RoutedEventArgs
    {
        public PaginationViewModel PaginationViewModel { get; set; }
    }

    /// <summary>
    /// Interaction logic for Pagination.xaml
    /// </summary>
    public partial class Pagination : UserControl
    {
        private PaginationViewModel _viewModel;

        public static readonly RoutedEvent PageChangedEvent = EventManager.RegisterRoutedEvent("PageChanged", RoutingStrategy.Direct, typeof(PageChangedEventHandler), typeof(Pagination));

        public Pagination()
        {
            InitializeComponent();

            _viewModel = new PaginationViewModel();
            DataContext = _viewModel;
        }

        public PaginationViewModel PaginationViewModel { get => _viewModel; }

        public event PageChangedEventHandler PageChanged
        {
            add { AddHandler(PageChangedEvent, value); }
            remove { RemoveHandler(PageChangedEvent, value); }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentPageIndex > 1)
            {
                _viewModel.CurrentPageIndex--;

                RaisePageChangedEvent();
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentPageIndex > 1)
            {
                _viewModel.CurrentPageIndex = 1;

                RaisePageChangedEvent();
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentPageIndex < _viewModel.PageCount)
            {
                _viewModel.CurrentPageIndex++;

                RaisePageChangedEvent();
            }
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentPageIndex < _viewModel.PageCount)
            {
                _viewModel.CurrentPageIndex = _viewModel.PageCount;

                RaisePageChangedEvent();
            }
        }

        private void cmbCombox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaisePageChangedEvent();
        }

        private void tbPageIndex_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RaisePageChangedEvent();
            }
        }

        private void RaisePageChangedEvent()
        {
            PageChangedEventArgs pageChangedArgs = new PageChangedEventArgs
            {
                RoutedEvent = PageChangedEvent,
                PaginationViewModel = _viewModel
            };
            RaiseEvent(pageChangedArgs);
        }
    }

    public class PaginationViewModel : BaseViewModel
    {
        private int _totalCount;
        private int _currentPageIndex;
        private int _currentPageSize;
        private ObservableCollection<int> _pageSizeList;

        public PaginationViewModel()
        {
            _totalCount = 0;
            _currentPageIndex = 1;
            _currentPageSize = Constants.PAGE_SIZE_20;
            _pageSizeList = new ObservableCollection<int>();
            foreach (int pageSize in Constants.PAGE_SIZE_LIST)
            {
                _pageSizeList.Add(pageSize);
            }
        }

        public int PageCount => _totalCount == 0 ? 1 : (int)Math.Ceiling((float)_totalCount / _currentPageSize);

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(PageCount));
                OnPropertyChanged(nameof(CanGoFirstOrPre));
                OnPropertyChanged(nameof(CanGoLastOrNext));
            }
        }

        public int CurrentPageIndex
        {
            get => _currentPageIndex;
            set
            {
                _currentPageIndex = value;
                if (_currentPageIndex < 1)
                {
                    _currentPageIndex = 1;
                }
                if (_currentPageIndex > PageCount)
                {
                    _currentPageIndex = PageCount;
                }
                OnPropertyChanged(nameof(CurrentPageIndex));
                OnPropertyChanged(nameof(CanGoFirstOrPre));
                OnPropertyChanged(nameof(CanGoLastOrNext));
            }
        }

        public int CurrentPageSize
        {
            get => _currentPageSize;
            set
            {
                _currentPageSize = value;
                if (_currentPageIndex > PageCount)
                {
                    CurrentPageIndex = PageCount;
                }
                OnPropertyChanged(nameof(PageCount));
                OnPropertyChanged(nameof(CanGoFirstOrPre));
                OnPropertyChanged(nameof(CanGoLastOrNext));
            }
        }

        public bool CanGoFirstOrPre => CurrentPageIndex > 1;

        public bool CanGoLastOrNext => CurrentPageIndex < PageCount;

        public ObservableCollection<int> PageSizeList { get => _pageSizeList; set { _pageSizeList = value; OnPropertyChanged(nameof(PageSizeList)); } }
    }
}
