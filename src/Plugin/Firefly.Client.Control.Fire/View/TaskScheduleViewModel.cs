// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Firefly.Client.Control.Fire
{

    public class TaskScheduleViewModel : BaseViewModel
    {
        #region Fields
        private bool _enabled;
        private bool _hasItem;
        private int _restoreTime;
        private bool _hasSelectedItem;
        private DayGridViewModel _mondayDataSource;
        private DayGridViewModel _tuesdayDataSource;
        private DayGridViewModel _wednesdayDataSource;
        private DayGridViewModel _thursdayDataSource;
        private DayGridViewModel _fridayDataSource;
        private DayGridViewModel _saturdayDataSource;
        private DayGridViewModel _sundayDataSource;
        private ObservableCollection<DayGridViewModel> _items;
        private CruiseRouteViewModel cruiseRouteVM;

        #endregion

        #region Constructors

        public TaskScheduleViewModel()
        {
            _enabled = true;
            _restoreTime = 5;
            _items = new ObservableCollection<DayGridViewModel>();

            _mondayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Monday, ItemLabel = I18n.Properties.Resources.CruiseRoute };
            _tuesdayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Tuesday, ItemLabel = I18n.Properties.Resources.CruiseRoute };
            _wednesdayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Wednesday, ItemLabel = I18n.Properties.Resources.CruiseRoute };
            _thursdayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Thursday, ItemLabel = I18n.Properties.Resources.CruiseRoute };
            _fridayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Friday, ItemLabel = I18n.Properties.Resources.CruiseRoute };
            _saturdayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Saturday, ItemLabel = I18n.Properties.Resources.CruiseRoute };
            _sundayDataSource = new DayGridViewModel { DayOfWeek = DayOfWeek.Sunday, ItemLabel = I18n.Properties.Resources.CruiseRoute };

            _items.Add(_mondayDataSource);
            _items.Add(_tuesdayDataSource);
            _items.Add(_wednesdayDataSource);
            _items.Add(_thursdayDataSource);
            _items.Add(_fridayDataSource);
            _items.Add(_saturdayDataSource);
            _items.Add(_sundayDataSource);

            cruiseRouteVM = new CruiseRouteViewModel();
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

        public ObservableCollection<CruiseRouteItemViewModel> CruiseRouteItems
        {
            get => cruiseRouteVM.Items;
        }

        #endregion

        #region Methods

        public void Load(TaskScheduleDataObject taskScheduleDO, CruiseRouteDataObject cruiseRouteDO)
        {
            Enabled = taskScheduleDO.Enabled;
            RestoreTime = taskScheduleDO.RestoreTime;

            cruiseRouteVM = new CruiseRouteViewModel();
            cruiseRouteVM.Load(cruiseRouteDO);
            OnPropertyChanged(nameof(CruiseRouteItems));

            foreach (DayGridViewModel dayGridVM in _items)
            {
                dayGridVM.ComboBoxItemsSource = CruiseRouteItems;
                dayGridVM.DurationItems.Clear();
                TaskDay taskDay = taskScheduleDO.TaskDayList.FirstOrDefault(item => (DayOfWeek)item.DayOfWeek == dayGridVM.DayOfWeek);
                if (taskDay != null)
                {
                    foreach (TaskTime time in taskDay.TaskTimeList)
                    {
                        DurationItem durationItem = new DurationItem(time.From, time.To, CruiseRouteItems.FirstOrDefault(it => it.ID == time.CruiseRouteID));
                        dayGridVM.DurationItems.Add(durationItem);
                    }
                }
            }
        }

        internal TaskScheduleDataObject ConvertToDataObject(List<Duration> durations)
        {
            List<TaskDay> taskDayList = new List<TaskDay>();

            foreach (Duration duration in durations)
            {
                List<TaskTime> taskTimeList = new List<TaskTime>();
                List<DurationItem> durationItems = duration.DurationItems;
                foreach (DurationItem durationItem in durationItems)
                {
                    int cruiseRouteID = 0;
                    if (durationItem.SelectedType is CruiseRouteItemViewModel)
                    {
                        cruiseRouteID = ((CruiseRouteItemViewModel)durationItem.SelectedType).ID;
                    }

                    taskTimeList.Add(new TaskTime
                    {
                        From = durationItem.StartValue,
                        To = durationItem.EndValue,
                        CruiseRouteID = cruiseRouteID
                    });
                }
                taskDayList.Add(new TaskDay
                {
                    DayOfWeek = (int)duration.DayOfWeek,
                    TaskTimeList = taskTimeList
                });
            }

            return new TaskScheduleDataObject
            {
                Enabled = _enabled,
                TaskDayList = taskDayList,
                RestoreTime = _restoreTime
            };
        }
        #endregion
    }
}
