// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Collections;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Firefly.Client.Control
{
    public partial class DayGridItem : DockPanel
    {
        private DayGridItemViewModel _dayGridItemVM;

        public static readonly RoutedEvent LeftDragStartEvent = EventManager.RegisterRoutedEvent("LeftDragStart", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DayGridItem));
        public static readonly RoutedEvent RightDragStartEvent = EventManager.RegisterRoutedEvent("RightDragStart", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DayGridItem));
        public static readonly RoutedEvent DeleteEvent = EventManager.RegisterRoutedEvent("DeleteEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DayGridItem));

        public event RoutedEventHandler LeftDragStart { add { AddHandler(LeftDragStartEvent, value); } remove { RemoveHandler(LeftDragStartEvent, value); } }
        public event RoutedEventHandler RightDragStart { add { AddHandler(RightDragStartEvent, value); } remove { RemoveHandler(RightDragStartEvent, value); } }
        public event RoutedEventHandler Delete { add { AddHandler(DeleteEvent, value); } remove { RemoveHandler(DeleteEvent, value); } }

        public DayGridItem(DayGridItemViewModel dayGridItemVM)
        {
            InitializeComponent();
            PART_set_value.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(PlacePopup);
            _dayGridItemVM = dayGridItemVM;
            DataContext = _dayGridItemVM;
            if (dayGridItemVM.ComboBoxItemsSource != null && dayGridItemVM.SelectedType == null)
            {
                cmb_duration_type.SelectedIndex = 0;
            }
        }

        public double Start { get => _dayGridItemVM.Start; }

        public double End { get => _dayGridItemVM.End; }

        public new double MinWidth { get => _dayGridItemVM.MinWidth; }

        public bool IsSelected { get => _dayGridItemVM.IsSelected; }

        protected override void OnRender(DrawingContext dc)
        {
            double width = _dayGridItemVM.End - _dayGridItemVM.Start;
            if (width >= 0)
            {
                base.OnRender(dc);
                Margin = new Thickness(_dayGridItemVM.Start, 0, 0, 0);
                Width = width;
                _dayGridItemVM.RefreshTime();
            }
        }

        public void DayGridItemMove(int delta, double rawStart, double rawEnd)
        {
            if (_dayGridItemVM.ItemStatus == ItemStatus.Focused || _dayGridItemVM.ItemStatus == ItemStatus.Moving)
            {
                _dayGridItemVM.ItemStatus = ItemStatus.Moving;
                _dayGridItemVM.SetPosition(delta + rawStart, delta + rawEnd);
            }
            else if (_dayGridItemVM.ItemStatus == ItemStatus.LeftMove)
            {
                _dayGridItemVM.Start = delta + rawStart;
            }
            else if (_dayGridItemVM.ItemStatus == ItemStatus.RightMove)
            {
                _dayGridItemVM.End = delta + rawEnd;
            }
        }

        public void RefreshStatus()
        {
            if (_dayGridItemVM.ItemStatus == ItemStatus.Moving
               || _dayGridItemVM.ItemStatus == ItemStatus.LeftMove
               || _dayGridItemVM.ItemStatus == ItemStatus.RightMove
               || _dayGridItemVM.ItemStatus == ItemStatus.Focused
               )
            {
                SetStatus(ItemStatus.Focused, true);
            }
            else
            {
                SetStatus(ItemStatus.None, false);
            }
        }

        public void SetStatus(ItemStatus itemStatus, bool isSelected)
        {
            _dayGridItemVM.ItemStatus = itemStatus;
            _dayGridItemVM.IsSelected = isSelected;
        }

        public void SetLimitValues(double min, double max)
        {
            _dayGridItemVM.Min = min;
            _dayGridItemVM.Max = max;
        }

        public DurationItem ConvertToDuration()
        {
            int startValue = DurationItem.ConvertToValue(_dayGridItemVM.StartValue);
            int endValue = DurationItem.ConvertToValue(_dayGridItemVM.EndValue);
            return new DurationItem(startValue, endValue, _dayGridItemVM.SelectedType);
        }

        private void PART_left_DragStarted(object sender, DragStartedEventArgs e)
        {
            RoutedEventArgs args = new RoutedEventArgs(LeftDragStartEvent, this);
            RaiseEvent(args);
        }

        private void PART_right_DragStarted(object sender, DragStartedEventArgs e)
        {
            RoutedEventArgs args = new RoutedEventArgs(RightDragStartEvent, this);
            RaiseEvent(args);
        }

        private void btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            SetStatus(ItemStatus.None, true);
            RoutedEventArgs args = new RoutedEventArgs(DeleteEvent, this);
            RaiseEvent(args);
        }

        private void btn_Close_Click(object sender, RoutedEventArgs e)
        {
            SetStatus(_dayGridItemVM.ItemStatus = ItemStatus.None, true);
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            if (_dayGridItemVM.SelectedType == null)
            {
                IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.PleaseAssociate + _dayGridItemVM.Label, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
            }
            else
            {
                double start = _dayGridItemVM.ConvertToStartPixel();
                double end = _dayGridItemVM.ConvertToEndPixel();
                if (_dayGridItemVM.IsValid(start, end, out string errorMessage))
                {
                    _dayGridItemVM.SetPosition(start, end);
                    SetStatus(_dayGridItemVM.ItemStatus = ItemStatus.None, true);
                    _dayGridItemVM.RefreshTime();
                    InvalidateVisual();
                }
                else
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), errorMessage, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
        }

        private CustomPopupPlacement[] PlacePopup(Size popupSize, Size targetSize, Point offset)
        {
            CustomPopupPlacement placement1 = new CustomPopupPlacement(new Point((targetSize.Width - popupSize.Width) / 2, -(targetSize.Height + popupSize.Height)), PopupPrimaryAxis.Vertical);
            CustomPopupPlacement[] ttplaces = new CustomPopupPlacement[]
            {
                placement1
            };
            return ttplaces;
        }
    }

    public class DayGridItemViewModel : BaseViewModel
    {
        #region const
        private const int ONE_DAY_MINUTES = 1440;
        private const int ONE_HOUR_MINUTES = 60;
        private const int ONE_DAY = 1;
        private const int ONE_DAY_HOUR = 24;
        private const int MIN_VALUE = 0;
        private const int MIN_MINITES = 15;
        #endregion

        #region filed
        private double _min;
        private double _max;
        private double _start;
        private double _end;
        private string _editingStartHours;
        private string _editingStartMinutes;
        private string _editingEndHours;
        private string _editingEndMinutes;
        private bool _isSelected;
        private double _pixelsPerMinute;
        private double _parentWidth;
        private ItemStatus _itemStatus;
        private IEnumerable _comboBoxItemsSource;
        private object _selectedType;
        private string _label;
        private Visibility _durationTypeViewVisibility;
        #endregion

        #region constructor
        public DayGridItemViewModel(double min, double max, double start, double end, ItemStatus itemStatus, double parentWidth, IEnumerable comboBoxItemsSource, string label, Visibility durationTypeViewVisibility)
        {
            _min = min;
            _max = max;
            _start = start;
            _itemStatus = itemStatus;
            _end = end;
            _parentWidth = parentWidth;
            _pixelsPerMinute = parentWidth / ONE_DAY_MINUTES;
            _comboBoxItemsSource = comboBoxItemsSource;
            _label = label;
            _durationTypeViewVisibility = durationTypeViewVisibility;
        }

        public DayGridItemViewModel(double min, double max, int startValue, int endValue, double parentWidth, object selectedType, IEnumerable comboBoxItemsSource, string label, Visibility durationTypeViewVisibility)
        {
            _min = min;
            _max = max;
            _parentWidth = parentWidth;
            _pixelsPerMinute = parentWidth / ONE_DAY_MINUTES;
            _start = ConvertToPixel(startValue);
            _end = ConvertToPixel(endValue);
            _itemStatus = ItemStatus.None;
            _selectedType = selectedType;
            _comboBoxItemsSource = comboBoxItemsSource;
            _label = label;
            _durationTypeViewVisibility = durationTypeViewVisibility;
        }
        #endregion

        #region property
        public double Start
        {
            get => _start;
            set
            {
                if (value < _min)
                {
                    _start = _min;
                }
                else if (value >= _end)
                {
                    _start = _end - _pixelsPerMinute;
                }
                else
                {
                    _start = value;
                }
            }
        }

        public double End
        {
            get => _end;
            set
            {
                if (value > _max)
                {
                    _end = _max;
                }
                else if (value <= _start)
                {
                    _end = _start + _pixelsPerMinute;
                }
                else
                {
                    _end = value;
                }
            }
        }

        public double Min { get => _min; set { _min = value; } }

        public double Max { get => _max; set { _max = value; } }

        public double MinWidth { get => _pixelsPerMinute * MIN_MINITES; }

        public int StartValue
        {
            get => ConvertToValue(_start);
            set
            {
                _start = ConvertToPixel(value);
            }
        }

        public int EndValue
        {
            get => ConvertToValue(_end);
            set
            {
                _end = ConvertToPixel(value);
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public string StartTime
        {
            get
            {
                TimeSpan timeSpan = TimeSpan.FromMinutes(ConvertToValue(_start));
                int startHours = timeSpan.Hours;
                if (timeSpan.Days == ONE_DAY)
                {
                    startHours = ONE_DAY_HOUR;
                }
                return $"{startHours.ToString("D2")}:{timeSpan.Minutes.ToString("D2")}";
            }
        }

        public string EndTime
        {
            get
            {
                TimeSpan timeSpan = TimeSpan.FromMinutes(ConvertToValue(_end));
                int endHours = timeSpan.Hours;
                if (timeSpan.Days == ONE_DAY)
                {
                    endHours = ONE_DAY_HOUR;
                }
                return $"{endHours.ToString("D2")}:{timeSpan.Minutes.ToString("D2")}";
            }
        }

        public string EditingStartHours
        {
            get
            {
                return _editingStartHours;
            }
            set
            {
                int editingStartHours;
                if (int.TryParse(value, out editingStartHours) || string.IsNullOrWhiteSpace(value))
                {
                    _editingStartHours = value;
                }
                OnPropertyChanged(nameof(EditingStartHours));
            }
        }

        public string EditingStartMinutes
        {
            get
            {
                return _editingStartMinutes;
            }
            set
            {
                int editingStartMinutes;
                if (int.TryParse(value, out editingStartMinutes) || string.IsNullOrWhiteSpace(value))
                {
                    _editingStartMinutes = value;
                }
                OnPropertyChanged(nameof(EditingStartMinutes));
            }
        }

        public string EditingEndHours
        {
            get
            {
                return _editingEndHours;
            }
            set
            {
                int editingEndHours;
                if (int.TryParse(value, out editingEndHours) || string.IsNullOrWhiteSpace(value))
                {
                    _editingEndHours = value;
                }
                OnPropertyChanged(nameof(EditingEndHours));
            }
        }

        public string EditingEndMinutes
        {
            get
            {
                return _editingEndMinutes;
            }
            set
            {
                int editingEndMinutes;
                if (int.TryParse(value, out editingEndMinutes) || string.IsNullOrWhiteSpace(value))
                {
                    _editingEndMinutes = value;
                }
                OnPropertyChanged(nameof(EditingEndMinutes));
            }
        }

        public IEnumerable ComboBoxItemsSource
        {
            get => _comboBoxItemsSource;
            set
            {
                _comboBoxItemsSource = value;
                OnPropertyChanged(nameof(ComboBoxItemsSource));
            }
        }

        public object SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(nameof(SelectedType)); }
        }

        public ItemStatus ItemStatus
        {
            get => _itemStatus;
            set
            {
                _itemStatus = value; OnPropertyChanged(nameof(ItemStatus));
            }
        }

        public string Label
        {
            get => _label;
            set
            {
                _label = value; OnPropertyChanged(nameof(Label));
            }
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

        #region method
        private double ConvertToPixel(int value)
        {
            return _pixelsPerMinute * value;
        }

        private int ConvertToValue(double pixel)
        {
            return (int)(pixel / _pixelsPerMinute);
        }

        internal void SetPosition(double start, double end)
        {
            start = CalculateRound(start);
            end = CalculateRound(end);
            if (start >= Min && end <= Max)
            {
                Start = start;
                End = end;
            }
            else if (start < Min)
            {
                Start = Min;
                End = end + (Min - start);
            }
            else if (end > Max)
            {
                End = Max;
                Start = start - (end - Max);
            }
        }

        private double CalculateRound(double value)
        {
            double valueDecimal = value - Math.Truncate(value);
            return (int)(valueDecimal / _pixelsPerMinute) * _pixelsPerMinute + Math.Truncate(value);
        }

        public void RefreshTime()
        {
            TimeSpan editingStartTime = TimeSpan.FromMinutes(ConvertToValue(_start));
            _editingStartHours = (editingStartTime.Days * ONE_DAY_HOUR + editingStartTime.Hours).ToString("D2");
            _editingStartMinutes = editingStartTime.Minutes.ToString("D2");

            TimeSpan editingEndTime = TimeSpan.FromMinutes(ConvertToValue(_end));
            _editingEndHours = (editingEndTime.Days * ONE_DAY_HOUR + editingEndTime.Hours).ToString("D2");
            _editingEndMinutes = editingEndTime.Minutes.ToString("D2");

            OnPropertyChanged(nameof(EditingEndHours));
            OnPropertyChanged(nameof(EditingEndMinutes));
            OnPropertyChanged(nameof(EditingStartHours));
            OnPropertyChanged(nameof(EditingStartMinutes));
            OnPropertyChanged(nameof(EndTime));
            OnPropertyChanged(nameof(StartTime));
            OnPropertyChanged(nameof(EndTime));
            OnPropertyChanged(nameof(Start));
            OnPropertyChanged(nameof(End));
        }

        public bool IsValid(double start, double end, out string errorMessage)
        {
            bool isValid = true;
            errorMessage = string.Empty;
            int minutesDefference = ConvertToValue(end) - ConvertToValue(start);
            if (start < MIN_VALUE || start > _parentWidth || end < MIN_VALUE || end > _parentWidth)
            {
                errorMessage = I18n.Properties.Resources.TimeWorng;
                isValid = false;
            }
            else if (minutesDefference <= 0)
            {
                errorMessage = I18n.Properties.Resources.StartGreaterThanEnd;
                isValid = false;
            }
            else if (minutesDefference < MIN_MINITES)
            {
                errorMessage = I18n.Properties.Resources.NotLessThanTime + MIN_MINITES + I18n.Properties.Resources.Minutes;
                isValid = false;
            }
            else if (start < _min || start > _max || end < _min || end > _max)
            {
                errorMessage = I18n.Properties.Resources.NotRepeatOfTimePeriods;
                isValid = false;
            }
            return isValid;
        }

        public double ConvertToStartPixel()
        {
            int editingStartValue = int.Parse(_editingStartHours) * ONE_HOUR_MINUTES + int.Parse(_editingStartMinutes);
            return ConvertToPixel(editingStartValue);
        }

        public double ConvertToEndPixel()
        {
            int editingEndValue = int.Parse(_editingEndHours) * ONE_HOUR_MINUTES + int.Parse(_editingEndMinutes);
            return ConvertToPixel(editingEndValue);
        }
        #endregion
    }

    public enum ItemStatus
    {
        None = 0,
        Hover = 1,
        Focused = 2,
        LeftMove = 3,
        RightMove = 4,
        Moving = 5
    }
}
