// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Firefly.Client.Control
{
    public class OverrideRoutedEventArgs : RoutedEventArgs
    {
        public OverrideRoutedEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
        }

        public DayOfWeek FormDayOfWeek { get; set; }

        public List<DayOfWeek> ToDayOfWeeks { get; set; }
    }

    public partial class DayOverride : Grid
    {
        private DayOverrideViewModel _dayOverrideViewModel;
        private List<CheckBox> checkBoxes;

        public static readonly RoutedEvent SaveClickEvent = EventManager.RegisterRoutedEvent("SaveClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DayOverride));
        public event RoutedEventHandler SaveClick { add { AddHandler(SaveClickEvent, value); } remove { RemoveHandler(SaveClickEvent, value); } }

        public DayOverride()
        {
            InitializeComponent();

            checkBoxes = new List<CheckBox>();
            checkBoxes.Add(cb_monday);
            checkBoxes.Add(cb_tuesday);
            checkBoxes.Add(cb_wednesday);
            checkBoxes.Add(cb_thursday);
            checkBoxes.Add(cb_friday);
            checkBoxes.Add(cb_saturday);
            checkBoxes.Add(cb_sunday);
            _dayOverrideViewModel = new DayOverrideViewModel();
            DataContext = _dayOverrideViewModel;
        }

        public void SetFromDayOfWeek(DayOfWeek dayOfWeek)
        {
            _dayOverrideViewModel.FromDayOfWeek = dayOfWeek;
        }

        public void ClearChecked()
        {
            _dayOverrideViewModel.IsAllChecked = false;
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            OverrideRoutedEventArgs args = new OverrideRoutedEventArgs(SaveClickEvent, this);
            args.FormDayOfWeek = _dayOverrideViewModel.FromDayOfWeek;
            List<DayOfWeek> dayOfWeeks = new List<DayOfWeek>();

            foreach (CheckBox checkBox in checkBoxes)
            {
                if (checkBox.IsChecked ?? false)
                {
                    DayOfWeek dayOfWeek = (DayOfWeek)checkBox.Tag;
                    if (dayOfWeek != _dayOverrideViewModel.FromDayOfWeek)
                    {
                        dayOfWeeks.Add(dayOfWeek);
                    }
                }
            }
            args.ToDayOfWeeks = dayOfWeeks;
            RaiseEvent(args);
            Visibility = Visibility.Hidden;
        }

        private void btn_delete_Click(object sender, RoutedEventArgs e)
        {
            _dayOverrideViewModel.FromDayOfWeek = DayOfWeek.None;
            Visibility = Visibility.Hidden;
        }
    }

    public class DayOverrideViewModel : BaseViewModel
    {
        private bool _isMondayChecked;
        private bool _isTuesdayChecked;
        private bool _isWednesdayChecked;
        private bool _isThursdayChecked;
        private bool _isFridayChecked;
        private bool _isSaturdayChecked;
        private bool _isSundayChecked;

        private DayOfWeek _fromDayOfWeek;

        public DayOfWeek FromDayOfWeek
        {
            get { return _fromDayOfWeek; }
            set
            {
                _fromDayOfWeek = value;
                OnPropertyChanged(nameof(FromDayOfWeek));
                RefreshData();
            }
        }

        public bool IsAllChecked
        {
            get
            {
                return _isMondayChecked && _isTuesdayChecked && _isWednesdayChecked && _isThursdayChecked && _isFridayChecked && _isSaturdayChecked && _isSundayChecked;
            }
            set
            {
                _isMondayChecked = value;
                _isTuesdayChecked = value;
                _isWednesdayChecked = value;
                _isThursdayChecked = value;
                _isFridayChecked = value;
                _isSaturdayChecked = value;
                _isSundayChecked = value;
                RefreshData();
            }
        }

        public bool IsMondayChecked
        {
            get { return _isMondayChecked; }
            set
            {
                _isMondayChecked = value;
                OnPropertyChanged(nameof(IsMondayChecked));
                OnPropertyChanged(nameof(IsAllChecked));
            }
        }

        public bool IsTuesdayChecked
        {
            get { return _isTuesdayChecked; }
            set
            {
                _isTuesdayChecked = value;
                OnPropertyChanged(nameof(IsTuesdayChecked));
                OnPropertyChanged(nameof(IsAllChecked));
            }
        }

        public bool IsWednesdayChecked
        {
            get { return _isWednesdayChecked; }
            set
            {
                _isWednesdayChecked = value;
                OnPropertyChanged(nameof(IsWednesdayChecked));
                OnPropertyChanged(nameof(IsAllChecked));
            }
        }

        public bool IsThursdayChecked
        {
            get { return _isThursdayChecked; }
            set
            {
                _isThursdayChecked = value;
                OnPropertyChanged(nameof(IsThursdayChecked));
                OnPropertyChanged(nameof(IsAllChecked));
            }
        }

        public bool IsFridayChecked
        {
            get { return _isFridayChecked; }
            set
            {
                _isFridayChecked = value;
                OnPropertyChanged(nameof(IsFridayChecked));
                OnPropertyChanged(nameof(IsAllChecked));
            }
        }

        public bool IsSaturdayChecked
        {
            get { return _isSaturdayChecked; }
            set
            {
                _isSaturdayChecked = value;
                OnPropertyChanged(nameof(IsSaturdayChecked));
                OnPropertyChanged(nameof(IsAllChecked));
            }
        }

        public bool IsSundayChecked
        {
            get { return _isSundayChecked; }
            set
            {
                _isSundayChecked = value;
                OnPropertyChanged(nameof(IsSundayChecked));
                OnPropertyChanged(nameof(IsAllChecked));
            }
        }

        private void RefreshData()
        {
            ///被复制的复选框Checked状态始终为true
            switch (_fromDayOfWeek)
            {
                case DayOfWeek.Monday:
                    _isMondayChecked = true;
                    break;
                case DayOfWeek.Tuesday:
                    _isTuesdayChecked = true;
                    break;
                case DayOfWeek.Wednesday:
                    _isWednesdayChecked = true;
                    break;
                case DayOfWeek.Thursday:
                    IsThursdayChecked = true;
                    break;
                case DayOfWeek.Friday:
                    _isFridayChecked = true;
                    break;
                case DayOfWeek.Saturday:
                    _isSaturdayChecked = true;
                    break;
                case DayOfWeek.Sunday:
                    IsSundayChecked = true;
                    break;
            }
            OnPropertyChanged(nameof(IsAllChecked));
            OnPropertyChanged(nameof(IsMondayChecked));
            OnPropertyChanged(nameof(IsTuesdayChecked));
            OnPropertyChanged(nameof(IsWednesdayChecked));
            OnPropertyChanged(nameof(IsThursdayChecked));
            OnPropertyChanged(nameof(IsFridayChecked));
            OnPropertyChanged(nameof(IsSaturdayChecked));
            OnPropertyChanged(nameof(IsSundayChecked));
        }
    }
}