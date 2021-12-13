// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
    public class AlarmScheduleViewModel : BaseViewModel
    {
        #region Fields

        private bool _enabled;
        private int _restoreTime;
        private bool _hasItem;
        private bool _hasSelectedItem;
        private ObservableCollection<DayGridViewModel> _items;
        private DayGridViewModel _mondayDataSource;
        private DayGridViewModel _tuesdayDataSource;
        private DayGridViewModel _wednesdayDataSource;
        private DayGridViewModel _thursdayDataSource;
        private DayGridViewModel _fridayDataSource;
        private DayGridViewModel _saturdayDataSource;
        private DayGridViewModel _sundayDataSource;

        #endregion

        #region Constructors

        public AlarmScheduleViewModel()
        {
            PauseTypeList = new List<AlarmTypeItemViewModel>
            {
                new AlarmTypeItemViewModel(AlarmType.None),
                new AlarmTypeItemViewModel(AlarmType.Alert1),
                new AlarmTypeItemViewModel(AlarmType.Alert2),
                new AlarmTypeItemViewModel(AlarmType.Alarm)
            };

            _mondayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Monday, ComboBoxItemsSource = PauseTypeList, ItemLabel = I18n.Properties.Resources.PauseCruise };
            _tuesdayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Tuesday, ComboBoxItemsSource = PauseTypeList, ItemLabel = I18n.Properties.Resources.PauseCruise };
            _wednesdayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Wednesday, ComboBoxItemsSource = PauseTypeList, ItemLabel = I18n.Properties.Resources.PauseCruise };
            _thursdayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Thursday, ComboBoxItemsSource = PauseTypeList, ItemLabel = I18n.Properties.Resources.PauseCruise };
            _fridayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Friday, ComboBoxItemsSource = PauseTypeList, ItemLabel = I18n.Properties.Resources.PauseCruise };
            _saturdayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Saturday, ComboBoxItemsSource = PauseTypeList, ItemLabel = I18n.Properties.Resources.PauseCruise };
            _sundayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Sunday, ComboBoxItemsSource = PauseTypeList, ItemLabel = I18n.Properties.Resources.PauseCruise };

            _items = new ObservableCollection<DayGridViewModel>();
            _items.Add(_mondayDataSource);
            _items.Add(_tuesdayDataSource);
            _items.Add(_wednesdayDataSource);
            _items.Add(_thursdayDataSource);
            _items.Add(_fridayDataSource);
            _items.Add(_saturdayDataSource);
            _items.Add(_sundayDataSource);
        }

        #endregion

        #region Properties

        public DayGridViewModel MondayDataSource
        {
            get => _mondayDataSource;
            set
            {
                _mondayDataSource = value;
                OnPropertyChanged(nameof(MondayDataSource));
            }
        }

        public DayGridViewModel TuesdayDataSource
        {
            get => _tuesdayDataSource;
            set
            {
                _tuesdayDataSource = value;
                OnPropertyChanged(nameof(TuesdayDataSource));
            }
        }

        public DayGridViewModel WednesdayDataSource
        {
            get => _wednesdayDataSource;
            set
            {
                _wednesdayDataSource = value;
                OnPropertyChanged(nameof(WednesdayDataSource));
            }
        }

        public DayGridViewModel ThursdayDataSource
        {
            get => _thursdayDataSource;
            set
            {
                _thursdayDataSource = value;
                OnPropertyChanged(nameof(ThursdayDataSource));
            }
        }

        public DayGridViewModel FridayDataSource
        {
            get => _fridayDataSource;
            set
            {
                _fridayDataSource = value;
                OnPropertyChanged(nameof(FridayDataSource));
            }
        }

        public DayGridViewModel SaturdayDataSource
        {
            get => _saturdayDataSource;
            set
            {
                _saturdayDataSource = value;
                OnPropertyChanged(nameof(SaturdayDataSource));
            }
        }

        public DayGridViewModel SundayDataSource
        {
            get => _sundayDataSource;
            set
            {
                _sundayDataSource = value;
                OnPropertyChanged(nameof(SundayDataSource));
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged(nameof(Enabled));
                OnPropertyChanged(nameof(HasItem));
                OnPropertyChanged(nameof(HasSelectedItem));
            }
        }

        public bool HasItem
        {
            get => _hasItem && _enabled;
            set
            {
                _hasItem = value;
                OnPropertyChanged(nameof(HasItem));
            }
        }

        public bool HasSelectedItem
        {
            get => _hasSelectedItem && _enabled;
            set
            {
                _hasSelectedItem = value;
                OnPropertyChanged(nameof(HasSelectedItem));
            }
        }

        public int RestoreTime
        {
            get => _restoreTime;
            set
            {
                _restoreTime = value;
                OnPropertyChanged(nameof(RestoreTime));
            }
        }

        public ObservableCollection<DayGridViewModel> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        public List<AlarmTypeItemViewModel> PauseTypeList { get; set; }

        #endregion

        #region Methods

        public void Load(AlarmScheduleDataObject alarmScheduleDO)
        {
            Enabled = alarmScheduleDO.Enabled;

            foreach (DayGridViewModel dayGridVM in _items)
            {
                dayGridVM.DurationItems.Clear();
                AlarmDay alarmDay = alarmScheduleDO.AlarmDayList.FirstOrDefault(item => (DayOfWeek)item.DayOfWeek == dayGridVM.DayOfWeek);
                if (alarmDay != null)
                {
                    foreach (AlarmTime alarmTime in alarmDay.AlarmTimeList)
                    {
                        AlarmTypeItemViewModel alarmTypeViewModel = PauseTypeList.FirstOrDefault(it => it.PauseType == alarmTime.PauseType);
                        DurationItem durationItem = new DurationItem(alarmTime.From, alarmTime.To, alarmTypeViewModel ?? PauseTypeList[0]);
                        dayGridVM.DurationItems.Add(durationItem);
                    }
                }
            }
        }

        public AlarmScheduleDataObject ConvertToDataObject(List<Duration> durations)
        {
            List<AlarmDay> alarmDayList = new List<AlarmDay>();

            foreach (Duration duration in durations)
            {
                List<AlarmTime> alarmTimeList = new List<AlarmTime>();
                List<DurationItem> durationItems = duration.DurationItems;
                foreach (DurationItem durationItem in durationItems)
                {
                    AlarmType pauseType = AlarmType.None;
                    if (durationItem.SelectedType is AlarmTypeItemViewModel)
                    {
                        pauseType = ((AlarmTypeItemViewModel)durationItem.SelectedType).PauseType;
                    }

                    alarmTimeList.Add(new AlarmTime
                    {
                        From = durationItem.StartValue,
                        To = durationItem.EndValue,
                        PauseType = pauseType
                    });
                }
                alarmDayList.Add(new AlarmDay
                {
                    DayOfWeek = (int)duration.DayOfWeek,
                    AlarmTimeList = alarmTimeList
                });
            }

            return new AlarmScheduleDataObject
            {
                Enabled = Enabled,
                AlarmDayList = alarmDayList
            };
        }
        #endregion
    }

    public class AlarmTypeItemViewModel : BaseViewModel
    {
        #region Fields

        private string name;

        #endregion

        #region Properties

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public AlarmType PauseType { get; set; }

        #endregion

        #region Constructors

        public AlarmTypeItemViewModel(AlarmType pauseType)
        {
            PauseType = pauseType;

            if (pauseType == AlarmType.None)
            {
                Name = I18n.Properties.Resources.None;
            }

            if (pauseType == AlarmType.Alert1)
            {
                Name = I18n.Properties.Resources.Alarm1Output;
            }

            if (pauseType == AlarmType.Alert2)
            {
                Name = I18n.Properties.Resources.Alarm2Output;
            }

            if (pauseType == AlarmType.Alarm)
            {
                Name = I18n.Properties.Resources.Alarm;
            }
        }

        #endregion
    }
}
