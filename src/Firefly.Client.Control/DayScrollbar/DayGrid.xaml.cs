// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Windows;
using System.Collections;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Firefly.Client.Control
{
    public partial class DayGrid : Grid
    {
        private const double DAY_GRID_ITEM_MIN_VALUE = 0;

        #region filed
        private double _mouseStart;
        private double _rawStart;
        private double _rawEnd;
        private bool _isDragging;
        private List<DayGridItem> _dayGridItems;
        private DayGridItem _editingItem;
        #endregion

        #region RoutedEvent
        public static readonly RoutedEvent OverrideButtonClickEvent = EventManager.RegisterRoutedEvent("OverrideButton", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DayGridItem));
        public event RoutedEventHandler OverrideButtonClick { add { AddHandler(OverrideButtonClickEvent, value); } remove { RemoveHandler(OverrideButtonClickEvent, value); } }

        public static readonly RoutedEvent ItemStatusChangedEvent = EventManager.RegisterRoutedEvent("ItemStatusChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DayGridItem));
        public event RoutedEventHandler ItemStatusChanged { add { AddHandler(ItemStatusChangedEvent, value); } remove { RemoveHandler(ItemStatusChangedEvent, value); } }
        #endregion

        #region constructor
        public DayGrid()
        {
            InitializeComponent();
            _dayGridItems = new List<DayGridItem>();
        }
        #endregion

        #region property
        public bool HasSelectedItem
        {
            get => _dayGridItems.Any(item => item.IsSelected);
        }

        public bool HasItem
        {
            get => _dayGridItems.Count > 0;
        }
        #endregion

        #region Dependency
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source",
            typeof(DayGridViewModel),
            typeof(DayGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public DayGridViewModel Source
        {
            get { return (DayGridViewModel)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public int MaxDurationCount
        {
            get { return (int)GetValue(MaxDurationCountProperty); }
            set { SetValue(MaxDurationCountProperty, value); }
        }

        public static readonly DependencyProperty MaxDurationCountProperty =
            DependencyProperty.Register("MaxDurationCount", typeof(int), typeof(DayGrid), new PropertyMetadata(0));
        #endregion

        #region private method
        private void root_Loaded(object sender, RoutedEventArgs e)
        {
            Load();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            foreach (DayGridItem dayGridItem in _dayGridItems)
            {
                if (!PART_Content.Children.Contains(dayGridItem))
                {
                    PART_Content.Children.Add(dayGridItem);
                }
                dayGridItem.InvalidateVisual();
            }

            RoutedEventArgs args = new RoutedEventArgs(ItemStatusChangedEvent, this);
            RaiseEvent(args);
        }

        private void btn_override_Click(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args = new RoutedEventArgs(OverrideButtonClickEvent, this);
            RaiseEvent(args);
        }

        private void PART_Content_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _editingItem != null)
            {
                Point point = e.GetPosition(PART_Content);
                int delta = (int)(point.X - _mouseStart);
                _editingItem.DayGridItemMove(delta, _rawStart, _rawEnd);
                InvalidateVisual();
            }
        }

        private void PART_Content_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging)
            {
                PART_Content.CaptureMouse();
                Point point = e.GetPosition(PART_Content);

                bool isOutBounds = point.Y > PART_Content.ActualHeight || point.Y < 0;
                if (isOutBounds)
                {
                    PART_Content.ReleaseMouseCapture();
                }
                else
                {
                    _mouseStart = point.X;
                    bool clickInItem = false;
                    foreach (DayGridItem dayGridItem in _dayGridItems)
                    {
                        Point topLeft = dayGridItem.TranslatePoint(new Point(0, 0), PART_Content);
                        Rect bound = new Rect(topLeft.X, topLeft.Y, dayGridItem.ActualWidth, dayGridItem.ActualHeight);
                        if (bound.Contains(point))
                        {
                            _editingItem = dayGridItem;
                            clickInItem = true;
                            _editingItem.SetStatus(ItemStatus.Focused, true);
                        }
                        else
                        {
                            dayGridItem.SetStatus(ItemStatus.None, false);
                        }
                    }
                    if (!clickInItem)
                    {
                        if (_dayGridItems.Count >= MaxDurationCount)
                        {
                            IRtekMessageBox.ShowDialog(Window.GetWindow(this), string.Format(I18n.Properties.Resources.CannotExceedMaxDuration, MaxDurationCount), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                            PART_Content.ReleaseMouseCapture();
                            return;
                        }
                        DayGridItemViewModel dayGridItemVM = new DayGridItemViewModel(DAY_GRID_ITEM_MIN_VALUE, PART_Content.ActualWidth, _mouseStart, _mouseStart, ItemStatus.RightMove, PART_Content.ActualWidth, Source.ComboBoxItemsSource, Source.ItemLabel, Source.DurationTypeViewVisibility);
                        DayGridItem dayGridItem = new DayGridItem(dayGridItemVM)
                        {
                            Height = PART_Content.ActualHeight,
                        };
                        dayGridItem.SetStatus(ItemStatus.RightMove, true);
                        dayGridItem.LeftDragStart += EditingItem_LeftDragStart;
                        dayGridItem.RightDragStart += EditingItem_RightDragStart;
                        dayGridItem.Delete += EditingItem_Delete;
                        _editingItem = dayGridItem;
                        _dayGridItems.Add(dayGridItem);
                    }
                    _rawStart = _editingItem.Start;
                    _rawEnd = _editingItem.End;
                    _isDragging = true;
                    RefreshEditingItemLimitValue();
                    InvalidateVisual();
                }
            }
        }

        private void PART_Content_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _editingItem != null)
            {
                if (_editingItem.End - _editingItem.Start < _editingItem.MinWidth)
                {
                    _editingItem.SetStatus(ItemStatus.None, false);
                    _dayGridItems.Remove(_editingItem);
                    PART_Content.Children.Remove(_editingItem);
                }
                else
                {
                    _editingItem.RefreshStatus();
                }
                _isDragging = false;
                PART_Content.ReleaseMouseCapture();
                InvalidateVisual();
            }
            Cursor = Cursors.Arrow;
        }

        private void EditingItem_RightDragStart(object sender, RoutedEventArgs e)
        {
            if (!_isDragging)
            {
                _editingItem?.SetStatus(ItemStatus.None, false);
                PART_Content.CaptureMouse();

                _editingItem = sender as DayGridItem;
                _mouseStart = _editingItem.End;
                _rawStart = _editingItem.Start;
                _rawEnd = _editingItem.End;
                _editingItem.SetStatus(ItemStatus.RightMove, true);
                _isDragging = true;
                RefreshEditingItemLimitValue();
                Cursor = Cursors.SizeWE;
                InvalidateVisual();
            }
        }

        private void EditingItem_LeftDragStart(object sender, RoutedEventArgs e)
        {
            if (!_isDragging)
            {
                _editingItem?.SetStatus(ItemStatus.None, false);
                PART_Content.CaptureMouse();

                _editingItem = sender as DayGridItem;
                _mouseStart = _editingItem.Start;
                _rawStart = _editingItem.Start;
                _rawEnd = _editingItem.End;
                _editingItem.SetStatus(ItemStatus.LeftMove, true);
                _isDragging = true;
                RefreshEditingItemLimitValue();
                Cursor = Cursors.SizeWE;
                InvalidateVisual();
            }
        }

        private void EditingItem_Delete(object sender, RoutedEventArgs e)
        {
            DeleteSelectedItem();
        }

        private void PART_Content_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Source != null && _editingItem != null)
            {
                ClearSelectedItemStatus();
            }
        }

        private void RefreshEditingItemLimitValue()
        {
            _dayGridItems.Sort(new DayItemCompare());
            int index = _dayGridItems.IndexOf(_editingItem);

            if (_dayGridItems.Count == 1)
            {
                _editingItem.SetLimitValues(DAY_GRID_ITEM_MIN_VALUE, PART_Content.ActualWidth);
            }
            else if (index == 0)
            {
                DayGridItem nextItem = _dayGridItems[1];
                _editingItem.SetLimitValues(DAY_GRID_ITEM_MIN_VALUE, nextItem.Start);
            }
            else if (index == _dayGridItems.Count - 1)
            {
                DayGridItem lastItem = _dayGridItems[_dayGridItems.Count - 2];
                _editingItem.SetLimitValues(lastItem.End, PART_Content.ActualWidth);
            }
            else
            {
                DayGridItem lastItem = _dayGridItems[index - 1];
                DayGridItem nextItem = _dayGridItems[index + 1];
                _editingItem.SetLimitValues(lastItem.End, nextItem.Start);
            }
        }

        private void AddDurationItem(List<DurationItem> durationItems)
        {
            foreach (DurationItem durationItem in durationItems)
            {
                DayGridItemViewModel dayGridItemVM = new DayGridItemViewModel(DAY_GRID_ITEM_MIN_VALUE, PART_Content.ActualWidth, durationItem.DisplayStartValue, durationItem.DisplayEndValue, PART_Content.ActualWidth, durationItem.SelectedType, Source.ComboBoxItemsSource, Source.ItemLabel, Source.DurationTypeViewVisibility);
                DayGridItem dayGridItem = new DayGridItem(dayGridItemVM)
                {
                    Height = PART_Content.ActualHeight,
                };
                dayGridItem.LeftDragStart += EditingItem_LeftDragStart;
                dayGridItem.RightDragStart += EditingItem_RightDragStart;
                dayGridItem.Delete += EditingItem_Delete;
                _dayGridItems.Add(dayGridItem);
            }
        }
        #endregion

        #region public method
        public void Load()
        {
            if (Source != null)
            {
                DeleteAll();
                AddDurationItem(Source.DurationItems);
                InvalidateVisual();
            }
        }

        public void OverrideDayGridItem(List<DurationItem> durationItems)
        {
            DeleteAll();
            AddDurationItem(durationItems);
            InvalidateVisual();
        }

        public void DeleteAll()
        {
            _dayGridItems.Clear();
            _editingItem = null;
            PART_Content.Children.Clear();
            InvalidateVisual();
        }

        public double GetOverrideButtonActualWidth()
        {
            return btn_override.ActualWidth;
        }

        public void DeleteSelectedItem()
        {
            DayGridItem dayGridItem = _dayGridItems.FirstOrDefault(item => item.IsSelected);
            if (dayGridItem != null)
            {
                PART_Content.Children.Remove(dayGridItem);
                _dayGridItems.Remove(dayGridItem);
                InvalidateVisual();
            }
        }

        public void ClearSelectedItemStatus()
        {
            if (Source != null && _editingItem != null)
            {
                _editingItem.SetStatus(ItemStatus.None, false);
                _editingItem = null;
                InvalidateVisual();
            }
        }

        public Duration ConvertToDuration()
        {
            List<DurationItem> dayDurationItems = new List<DurationItem>();
            foreach (DayGridItem dayGridItem in _dayGridItems)
            {
                dayDurationItems.Add(dayGridItem.ConvertToDuration());
            }
            return new Duration(Source.DayOfWeek, dayDurationItems);
        }
        #endregion
    }

    public class DayGridViewModel : BaseViewModel
    {
        #region filed
        private string _label = string.Empty;
        private List<DurationItem> _durationItems;
        private IEnumerable _comboBoxItemsSource;
        private DayOfWeek _dayOfWeek;
        private string _itemLabel;
        private Visibility _durationTypeViewVisibility;
        #endregion

        #region constructor
        public DayGridViewModel()
        {
            _comboBoxItemsSource = new List<string>();
            _durationItems = new List<DurationItem>();
            _durationTypeViewVisibility = Visibility.Visible;
        }
        #endregion

        #region property
        public string Label
        {
            get
            {
                switch (_dayOfWeek)
                {
                    case DayOfWeek.Monday:
                        _label = I18n.Properties.Resources.Monday;
                        break;
                    case DayOfWeek.Tuesday:
                        _label = I18n.Properties.Resources.Tuesday;
                        break;
                    case DayOfWeek.Wednesday:
                        _label = I18n.Properties.Resources.Wednesday;
                        break;
                    case DayOfWeek.Thursday:
                        _label = I18n.Properties.Resources.Thursday;
                        break;
                    case DayOfWeek.Friday:
                        _label = I18n.Properties.Resources.Friday;
                        break;
                    case DayOfWeek.Saturday:
                        _label = I18n.Properties.Resources.Saturday;
                        break;
                    case DayOfWeek.Sunday:
                        _label = I18n.Properties.Resources.Sunday;
                        break;
                    case DayOfWeek.None:
                    default:
                        break;
                }
                return _label;
            }
        }

        public IEnumerable ComboBoxItemsSource
        {
            get => _comboBoxItemsSource;
            set { _comboBoxItemsSource = value; OnPropertyChanged(nameof(ComboBoxItemsSource)); }
        }

        public DayOfWeek DayOfWeek
        {
            get { return _dayOfWeek; }
            set { _dayOfWeek = value; OnPropertyChanged(nameof(DayOfWeek)); }
        }

        public List<DurationItem> DurationItems
        {
            get => _durationItems;
            set
            {
                _durationItems = value; OnPropertyChanged(nameof(DurationItems));
            }
        }

        public string ItemLabel
        {
            get { return _itemLabel; }
            set { _itemLabel = value; OnPropertyChanged(nameof(ItemLabel)); }
        }

        public Visibility DurationTypeViewVisibility
        {
            get => _durationTypeViewVisibility;
            set
            {
                _durationTypeViewVisibility = value;
                OnPropertyChanged();
            }
        }
        #endregion
    }

    internal class DayItemCompare : IComparer<DayGridItem>
    {
        public int Compare(DayGridItem x, DayGridItem y)
        {
            if (x.Start - y.Start == 0.0d)
            {
                return 0;
            }
            else if (x.Start - y.Start > 0.0d)
            {
                return 1;
            }
            else if (x.Start - y.Start < 0.0d)
            {
                return -1;
            }

            return 0;
        }
    }
}