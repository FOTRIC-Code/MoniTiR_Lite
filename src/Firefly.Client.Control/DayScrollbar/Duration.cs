// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace Firefly.Client.Control
{
    public class Duration
    {
        private DayOfWeek _dayOfWeek;
        private List<DurationItem> _durationItems;

        public Duration(DayOfWeek dayOfWeek, List<DurationItem> durationItems)
        {
            _dayOfWeek = dayOfWeek;
            _durationItems = durationItems;
        }

        public DayOfWeek DayOfWeek
        {
            get => _dayOfWeek;
            set { _dayOfWeek = value; }
        }
        public List<DurationItem> DurationItems
        {
            get => _durationItems;
            set { _durationItems = value; }
        }
    }

    public class DurationItem
    {
        private const decimal ONE_HOUR_SIXTY_VALUE = 60;
        private const decimal ONE_HOUR_HUNDREDS_VALUE = 100;
        //100进制与60进制，表示一个小时的进制差
        private const decimal ONE_HOUR_DIFFRTENCE = 40;
        private int _startValue;
        private int _endValue;
        private object _selectedType;

        public DurationItem(int startValue, int endValue, object selectedType)
        {
            _startValue = startValue;
            _endValue = endValue;
            _selectedType = selectedType;
        }

        public int DisplayStartValue
        {
            get { return ConvertToDisplayValue(_startValue); }
        }
        public int DisplayEndValue
        {
            get { return ConvertToDisplayValue(_endValue); }
        }
        public int StartValue
        {
            get { return _startValue; }
            set
            {
                _startValue = value;
            }
        }
        public int EndValue
        {
            get { return _endValue; }
            set
            {
                _endValue = value;
            }
        }
        public object SelectedType
        {
            get { return _selectedType; }
            set
            {
                _selectedType = value;
            }
        }

        public static int ConvertToValue(int displayValue)
        {
            return (int)(Math.Floor(displayValue / ONE_HOUR_SIXTY_VALUE) * ONE_HOUR_DIFFRTENCE + displayValue);
        }

        public static int ConvertToDisplayValue(int value)
        {
            return (int)(value - Math.Floor(value / ONE_HOUR_HUNDREDS_VALUE) * ONE_HOUR_DIFFRTENCE);
        }
    }

    public enum DayOfWeek
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
        Sunday = 7
    }
}
