// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Media;
using System.Collections;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;
using System.IO;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Threading;

namespace Firefly.Client
{
    public class RealTimeAlarmsViewModel : BaseViewModel
    {
        #region Fields

        private AlarmItemViewModel _selectedAlarm;
        private bool _detailViewVisibility = true;
        private bool _alarmBarVisibility = false;
        private bool _enableSound = true;
        private bool _enableSpeech = true;
        private ICollectionView _alarmFilterView;
        private List<string> _filterStrList;
        private string _searchText;
        private bool _needPopup;
        private List<long> _searchTimestampList;
        private ObservableCollection<AlarmFilterViewModel> _alarmFilterViewModels;
        private long _soundPlayFlag = 0;

        #endregion Fields

        #region Constructors

        public RealTimeAlarmsViewModel()
        {
            Items = new ObservableCollection<AlarmItemViewModel>();

            _alarmFilterView = CollectionViewSource.GetDefaultView(Items);
            _alarmFilterView.CurrentChanged += AlarmFilterView_CurrentChanged;

            _filterStrList = new List<string>();
            _alarmFilterViewModels = new ObservableCollection<AlarmFilterViewModel>();

            _alarmFilterViewModels.Add(new AlarmFilterViewModel
            {
                Filter = "",
            });
            _needPopup = AlarmCenterPreference.Instance.NeedPop;
            _enableSound = AlarmCenterPreference.Instance.EnableSound;
            _enableSpeech = AlarmCenterPreference.Instance.EnableSpeech;
            AutoPlayLiveVideo = AlarmCenterPreference.Instance.AutoPlayLiveVideo;
            _detailViewVisibility = AlarmCenterPreference.Instance.DetailViewVisibility;
        }

        #endregion Constructors

        #region Properties

        public ObservableCollection<AlarmItemViewModel> Items { get; set; }

        public AlarmItemViewModel SelectedAlarm
        {
            get => _selectedAlarm;
            set
            {
                _selectedAlarm?.ClearLiveVideo();
                _selectedAlarm = value;
                if (DetailViewVisibility)
                {
                    _selectedAlarm?.UpdateLiveVideoViewModels(AutoPlayLiveVideo);
                }
                OnPropertyChanged(nameof(SelectedAlarm));
            }
        }

        public bool EnableSound
        {
            get => _enableSound;
            set
            {
                _enableSound = value;
                OnPropertyChanged();
            }
        }

        public bool EnableSpeech
        {
            get => _enableSpeech;
            set
            {
                _enableSpeech = value;
                OnPropertyChanged();
            }
        }

        public bool DetailViewVisibility
        {
            get => _detailViewVisibility;
            set
            {
                _detailViewVisibility = value;
                OnPropertyChanged(nameof(DetailViewVisibility));
            }
        }

        public bool AlarmBarVisibility
        {
            get => _alarmBarVisibility;
            set
            {
                _alarmBarVisibility = value;
                OnPropertyChanged();
            }
        }

        public AlarmItemViewModel NewestAlarmItem => Items != null && Items.Count > 0 ? Items[0] : null;

        public bool AutoPlayLiveVideo { get; set; }

        public ObservableCollection<AlarmFilterViewModel> Filters
        {
            get => _alarmFilterViewModels;
            set
            {
                _alarmFilterViewModels = value;
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                bool needFilter = _searchText != value;
                _searchText = value;

                if (string.IsNullOrWhiteSpace(_searchText) && _searchTimestampList?.Count > 0)
                {
                    _searchTimestampList.Clear();
                }
                OnPropertyChanged();
                if (needFilter)
                {
                    FilterAlarmList();
                }
            }
        }

        public bool NeedPopup
        {
            get => _needPopup;
            set
            {
                _needPopup = value;
                OnPropertyChanged();
            }
        }

        public List<long> SearchTimestampList
        {
            get => _searchTimestampList;
            set
            {
                _searchTimestampList = value;
                if (_searchTimestampList?.Count > 0)
                {
                    FilterAlarmList();
                }
            }
        }

        #endregion Properties

        #region Methods

        public void AddOrUpdateAlarm(AlarmDO alarmDO)
        {
            AlarmItemViewModel itemViewModel = Items.FirstOrDefault(it => it.ID == alarmDO.ID);
            if (itemViewModel == null)
            {
                itemViewModel = new AlarmItemViewModel();
                if (Items.Count >= AlarmManager.MAX_REALTIME_ALARM_COUNT)
                {
                    Items.RemoveAt(Items.Count - 1);
                }

                Items.Insert(0, itemViewModel);
                if (EnableSound || EnableSpeech)
                {
                    PlayAlarm(alarmDO.AlarmDescription);
                }
            }

            itemViewModel.LoadAlarmDataObject(alarmDO);

            if (SelectedAlarm == null)
            {
                SelectedAlarm = itemViewModel;
            }

            OnPropertyChanged(nameof(NewestAlarmItem));
            AlarmFilterViewModel emptyItemVM = _alarmFilterViewModels.FirstOrDefault(it => string.IsNullOrEmpty(it.Filter));
            if (emptyItemVM != null)
            {
                _alarmFilterViewModels.Remove(emptyItemVM);
            }
            if (!_alarmFilterViewModels.Any(it => it.Filter.Equals(itemViewModel.AlarmLevel)))
            {
                _alarmFilterViewModels.Add(new AlarmFilterViewModel
                {
                    Filter = itemViewModel.AlarmLevel,
                    FilterChangedCallback = FilterChangedCallback
                });
            }
            _alarmFilterView?.Refresh();
        }

        private void PlayAlarm(string description)
        {
            string tempDescription = description;
            if (Interlocked.Read(ref _soundPlayFlag) > 0)
            {
                return;
            }
            Task.Run(() =>
            {
                Interlocked.Exchange(ref _soundPlayFlag, 1);
                if (EnableSound)
                {
                    int soundCount = AlarmCenterPreference.Instance.SoundCount;
                    while (soundCount > 0)
                    {
                        using (SoundPlayer player = new SoundPlayer(Properties.Resources.warrning))
                        {
                            player.PlaySync();
                        }
                        soundCount--;
                    }
                }
                if (EnableSpeech && !string.IsNullOrWhiteSpace(tempDescription))
                {
                    int speechCount = AlarmCenterPreference.Instance.SpeechCount;
                    while (speechCount > 0)
                    {
                        using (SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer())
                        {
                            speechSynthesizer.Speak(tempDescription);
                        }
                        speechCount--;
                    }
                }
                Interlocked.Exchange(ref _soundPlayFlag, 0);
            });
        }

        private void FilterChangedCallback(bool isSelected, string filter)
        {
            if (isSelected)
            {
                _filterStrList.Add(filter);
            }
            else
            {
                _filterStrList.Remove(filter);
            }
            FilterAlarmList();
        }

        private void FilterAlarmList()
        {
            if (_alarmFilterView?.CanFilter == true)
            {
                _alarmFilterView.Filter = (obj) =>
                {
                    if (obj is AlarmItemViewModel alarmItemVM)
                    {
                        if (alarmItemVM.ID == 0)
                        {
                            return true;
                        }
                        bool searchResult = true;
                        if (_searchTimestampList?.Count > 0)
                        {
                            searchResult = _searchTimestampList.Any(it => it == alarmItemVM.Timestamp);

                            if (searchResult && !string.Equals(_searchText, I18n.Properties.Resources.SearchFromEquipmentMonitor))
                            {
                                _searchText = I18n.Properties.Resources.SearchFromEquipmentMonitor;
                                OnPropertyChanged(nameof(SearchText));
                            }

                            return searchResult;
                        }

                        if (!string.IsNullOrWhiteSpace(_searchText))
                        {
                            searchResult = alarmItemVM.DeviceName.ToLower().Contains(_searchText.ToLower())
                                || alarmItemVM.AlarmType.ToLower().Contains(_searchText.ToLower())
                                || alarmItemVM.AlarmRegion.ToLower().Contains(_searchText.ToLower())
                                || alarmItemVM.AlarmLevel.ToLower().Contains(_searchText.ToLower())
                                || alarmItemVM.Description.ToLower().Contains(_searchText.ToLower())
                                           || alarmItemVM.Note.ToLower().Contains(_searchText.ToLower());
                        }

                        if (_filterStrList.Count == 0)
                        {
                            return searchResult;
                        }

                        return _filterStrList.Any(it =>
                        {
                            string text = it == null ? "" : it.ToLower();
                            return searchResult && alarmItemVM.AlarmLevel.ToLower().Contains(text);
                        });
                    }

                    return false;
                };
            }
        }

        private void AlarmFilterView_CurrentChanged(object sender, EventArgs e)
        {
            if (!_alarmFilterView.Contains(SelectedAlarm))
            {
                IEnumerator enumerator = _alarmFilterView.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    SelectedAlarm = enumerator.Current as AlarmItemViewModel;
                }
            }
        }

        public void ClearFilter()
        {
            foreach (AlarmFilterViewModel viewModel in Filters)
            {
                viewModel.IsSelected = false;
            }

            SearchText = string.Empty;
        }

        public void ClearAll()
        {
            Items.Clear();
            SelectedAlarm = null;
            ClearFilter();
            Filters?.Clear();

            Filters?.Add(new AlarmFilterViewModel
            {
                Filter = "",
            });
            OnPropertyChanged(nameof(NewestAlarmItem));
            AlarmManager.Instance.ClearAll();
        }

        public async void UpdateData()
        {
            AlarmItemViewModel firstAlarmViewModel = Items.FirstOrDefault();
            AlarmItemViewModel lastAlarmViewModel = Items.LastOrDefault();

            if (firstAlarmViewModel != null && lastAlarmViewModel != null)
            {
                PaginationData<List<AlarmDO>> paginationAlarmDOs =
                    await AlarmManager.Instance.GetAlarmList(new TimePageInfo()
                    {
                        StartTime = lastAlarmViewModel.AlarmTime.ToUniversalTime(),
                        EndTime = firstAlarmViewModel.AlarmTime.ToUniversalTime(),
                        PageSize = Constants.PAGE_SIZE_100,
                        Page = 1
                    });

                foreach (AlarmItemViewModel itemViewModel in Items)
                {
                    AlarmDO alarmDO = paginationAlarmDOs.Data?.FirstOrDefault(it => it.ID == itemViewModel.ID);
                    if (alarmDO != null)
                    {
                        itemViewModel.IsConfirmed = alarmDO.IsConfirmed;
                        itemViewModel.Note = alarmDO.Note;
                    }
                }
            }
        }

        public List<AlarmDO> GetAlarmDOs()
        {
            return Items.Select(item => item.AlarmDO).ToList();
        }

        public void SavePreference()
        {
            AlarmCenterPreference.Instance.NeedPop = _needPopup;
            AlarmCenterPreference.Instance.EnableSound = _enableSound;
            AlarmCenterPreference.Instance.EnableSpeech = _enableSpeech;
            AlarmCenterPreference.Instance.AutoPlayLiveVideo = AutoPlayLiveVideo;
            AlarmCenterPreference.Instance.DetailViewVisibility = _detailViewVisibility;
            AlarmCenterPreference.Instance.Save();
        }

        #endregion Methods
    }

    public class AlarmFilterViewModel : BaseViewModel
    {
        private bool _isSelected;
        public string Filter { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
                FilterChangedCallback?.Invoke(_isSelected, Filter);
            }
        }

        public Action<bool, string> FilterChangedCallback { get; set; }
    }
}