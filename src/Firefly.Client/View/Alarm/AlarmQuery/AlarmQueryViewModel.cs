// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    public class AlarmQueryViewModel : BaseViewModel
    {
        #region Fields

        private AlarmItemViewModel _selectedAlarm;
        private bool _detailViewVisibility = true;
        private List<AlarmStatusOptionViewModel> _alarmStatusOptions;
        private AlarmStatusOptionViewModel _selectedAlarmStatusOption;
        private string _displayText;
        private List<AlarmTypeOptionViewModel> _alarmTypeOptions;
        private List<AlarmTypeOptionViewModel> _selectedAlarmTypeOptions;

        #endregion Fields

        #region Constructors

        public AlarmQueryViewModel()
        {
            _selectedAlarmTypeOptions = new List<AlarmTypeOptionViewModel>();
            AlarmItems = new ObservableCollection<AlarmItemViewModel>();
            InitAlarmStatusOptions();
            InitAlarmTypeOptions();
            _selectedAlarmStatusOption = _alarmStatusOptions.FirstOrDefault();
        }

        #endregion Constructors

        #region Properties

        public DateTime BeginTime { get; set; } = DateTime.Today;

        public DateTime EndTime { get; set; } = DateTime.Now;

        public bool AutoPlayLiveVideo { get; set; }

        public ObservableCollection<AlarmItemViewModel> AlarmItems { get; set; }

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

        public string DisplayText
        {
            get => string.IsNullOrWhiteSpace(_displayText) ? I18n.Properties.Resources.AllOption : _displayText;
            set
            {
                _displayText = value;
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public List<AlarmStatusOptionViewModel> AlarmStatusOptions => _alarmStatusOptions;

        public AlarmStatusOptionViewModel SelectedAlarmStatusOption
        {
            get => _selectedAlarmStatusOption;
            set
            {
                _selectedAlarmStatusOption = value;
                OnPropertyChanged(nameof(SelectedAlarmStatusOption));
            }
        }

        public List<AlarmTypeOptionViewModel> AlarmTypeOptions => _alarmTypeOptions;

        public bool CanExportItem => AlarmItems.Any(x => x.IsSelected);

        public bool CanExportAll => AlarmItems.Any();

        public Action<string> ShowErrorMessageDialogCallback { get; set; }

        public bool DetailViewVisibility
        {
            get => _detailViewVisibility;
            set
            {
                _detailViewVisibility = value;
                OnPropertyChanged(nameof(DetailViewVisibility));
            }
        }

        #endregion Properties

        #region Public Methods

        public void LoadAlarmData(List<AlarmDO> alarmDOs)
        {
            AlarmItems.Clear();

            alarmDOs?.ForEach(it =>
            {
                AlarmItemViewModel alarmItem = new AlarmItemViewModel();
                alarmItem.LoadAlarmDataObject(it);
                AlarmItems.Add(alarmItem);
            });

            FlushExportButton();
        }

        public void FlushExportButton()
        {
            OnPropertyChanged(nameof(CanExportItem));
            OnPropertyChanged(nameof(CanExportAll));
        }

        public List<string> ConvertSelectedItemsToCSV()
        {
            return GetCSV(AlarmItems.Where(it => it.IsSelected).ToList());
        }

        public List<string> ConvertAllToCSV()
        {
            return GetCSV(AlarmItems.ToList());
        }

        public List<AlarmDO> GetAlarmDOs()
        {
            return AlarmItems.Select(item => item.AlarmDO).ToList();
        }

        public void OnCheckChanged(object sender, EventArgs e)
        {
            if (sender is AlarmTypeOptionViewModel treeComboBoxItem)
            {
                if (treeComboBoxItem.IsChecked)
                {
                    _selectedAlarmTypeOptions.Add(treeComboBoxItem);
                }
                else
                {
                    _selectedAlarmTypeOptions.Remove(treeComboBoxItem);
                }
            }
            DisplayText = GetSelectedAlarmTypeStr();
        }

        public string GetSelectedAlarmTypeStr()
        {
            return string.Join(";", _selectedAlarmTypeOptions.Where(item => item.NodeType == NodeType.AlarmTypeNode).Select(treeItem => treeItem.Value).ToList());
        }

        #endregion Methods

        #region Private Methods

        private List<string> GetCSV(List<AlarmItemViewModel> items)
        {
            List<string> csvList = new List<string>();
            csvList.Add($"{I18n.Properties.Resources.DeviceName},{I18n.Properties.Resources.Channel},{I18n.Properties.Resources.AlarmRegion},{I18n.Properties.Resources.AlarmType},{I18n.Properties.Resources.AlarmLevel},{I18n.Properties.Resources.AlarmTime},{I18n.Properties.Resources.AlarmDescription},{I18n.Properties.Resources.ConfirmState},{I18n.Properties.Resources.OutputState},{I18n.Properties.Resources.Note}");
            csvList.AddRange(items.Select(alarmItem => alarmItem.ConvertToCSVLineData()));

            return csvList;
        }

        private void InitAlarmStatusOptions()
        {
            _alarmStatusOptions = new List<AlarmStatusOptionViewModel>
            {
                new AlarmStatusOptionViewModel(I18n.Properties.Resources.AllOption,AlarmConfirmationStatus.All),
                new AlarmStatusOptionViewModel(I18n.Properties.Resources.Confirmed,AlarmConfirmationStatus.Confirmed),
                new AlarmStatusOptionViewModel(I18n.Properties.Resources.UnConfirmed,AlarmConfirmationStatus.Unconfirmed)
            };
        }

        private void InitAlarmTypeOptions()
        {
            _alarmTypeOptions = new List<AlarmTypeOptionViewModel>();

            var root = new AlarmTypeOptionViewModel(I18n.Properties.Resources.AllOption, I18n.Properties.Resources.AllOption, NodeType.RootNode, OnCheckChanged, true);

            root.AddChild(CreateAgentNode(I18n.Properties.Resources.IRAlarm, ServerType.FireMonitorAgent.ToString(),
                new List<string>
                {
                    I18n.Properties.Resources.HighestGreater,
                    I18n.Properties.Resources.HighestLess,
                    I18n.Properties.Resources.LowestGreater,
                    I18n.Properties.Resources.LowestLess,
                    I18n.Properties.Resources.AverageGreater,
                    I18n.Properties.Resources.AverageLess,
                    I18n.Properties.Resources.DiffTempGreater,
                    I18n.Properties.Resources.DiffTempLess
                }));

            _alarmTypeOptions.Add(root);
        }

        private AlarmTypeOptionViewModel CreateAgentNode(string parentName, string parentValue, List<string> childOptions)
        {
            var parent = new AlarmTypeOptionViewModel(parentName, parentValue, NodeType.AgentNode, OnCheckChanged);

            foreach (var childOption in childOptions)
            {
                parent.AddChild(new AlarmTypeOptionViewModel(childOption, childOption, NodeType.AlarmTypeNode, OnCheckChanged));
            }

            return parent;
        }

        #endregion
    }

    public class AlarmStatusOptionViewModel : BaseViewModel
    {
        private string _text;
        private AlarmConfirmationStatus _value;

        public AlarmStatusOptionViewModel(string text, AlarmConfirmationStatus value)
        {
            _text = text;
            _value = value;
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public AlarmConfirmationStatus Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public class AlarmTypeOptionViewModel : BaseViewModel
    {
        public event EventHandler OnCheckChanged;

        private bool _isChecked;
        private AlarmTypeOptionViewModel _parent;
        private string _name;
        private string _value;
        private bool _isExpanded;
        private NodeType _nodeType;
        private ObservableCollection<AlarmTypeOptionViewModel> _children;

        public AlarmTypeOptionViewModel(string name, string value, NodeType nodeType, EventHandler eventHandler = null, bool isExpanded = false) : this()
        {
            _name = name;
            _isExpanded = isExpanded;
            _value = value;
            _nodeType = nodeType;
            OnCheckChanged = eventHandler;
        }

        public AlarmTypeOptionViewModel()
        {
            _children = new ObservableCollection<AlarmTypeOptionViewModel>();
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    SetChecked(value);
                    UpdateParentCheckedState(this);
                    UpdateChildrenCheckedState(this);
                }
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(_name));
                }
            }
        }

        public string Value => _value;

        public AlarmTypeOptionViewModel Parent
        {
            get => _parent;
            set
            {
                if (_parent != value)
                {
                    _parent = value;
                    OnPropertyChanged(nameof(Parent));
                }
            }
        }

        public ObservableCollection<AlarmTypeOptionViewModel> Children => _children;

        public NodeType NodeType => _nodeType;

        public void AddChild(AlarmTypeOptionViewModel child)
        {
            child.Parent = this;
            _children.Add(child);
        }

        private void SetChecked(bool isChecked)
        {
            _isChecked = isChecked;
            OnPropertyChanged(nameof(IsChecked));
            OnCheckChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateParentCheckedState(AlarmTypeOptionViewModel treeComboBoxItem)
        {
            AlarmTypeOptionViewModel parent = treeComboBoxItem.Parent;
            if (parent == null) return;

            if (parent.Children.All(child => child.IsChecked))
            {
                if (!parent.IsChecked)
                {
                    parent.SetChecked(true);
                }
            }
            else
            {
                if (parent.IsChecked)
                {
                    parent.SetChecked(false);
                }
            }
            if (parent.Parent != null)
            {
                UpdateParentCheckedState(parent);
            }
        }

        private void UpdateChildrenCheckedState(AlarmTypeOptionViewModel treeComboBoxItem)
        {
            foreach (var chiled in treeComboBoxItem.Children)
            {
                if (chiled.Children.Count > 0)
                {
                    UpdateChildrenCheckedState(chiled);
                }
                if (chiled.IsChecked != _isChecked)
                {
                    chiled.SetChecked(_isChecked);
                }
            }
        }
    }

    public enum NodeType
    {
        RootNode = 0,
        AgentNode = 1,
        AlarmTypeNode = 2
    }
}