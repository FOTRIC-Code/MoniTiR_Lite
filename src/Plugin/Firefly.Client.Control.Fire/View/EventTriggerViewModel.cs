// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    public class EventTriggerViewModel : BaseViewModel
    {
        #region Fields

        private ObservableCollection<EventTriggerItemViewModel> alert1Outs;
        private ObservableCollection<EventTriggerItemViewModel> alert2Outs;
        private ObservableCollection<EventTriggerItemViewModel> alarmOuts;

        #endregion

        #region Properties

        public ObservableCollection<EventTriggerItemViewModel> Alert1Outs
        {
            get => alert1Outs;
            set
            {
                alert1Outs = value;
                OnPropertyChanged(nameof(Alert1Outs));
            }
        }

        public ObservableCollection<EventTriggerItemViewModel> Alert2Outs
        {
            get => alert2Outs;
            set
            {
                alert2Outs = value;
                OnPropertyChanged(nameof(Alert2Outs));
            }
        }

        public ObservableCollection<EventTriggerItemViewModel> AlarmOuts
        {
            get => alarmOuts;
            set
            {
                alarmOuts = value;
                OnPropertyChanged(nameof(AlarmOuts));
            }
        }

        #endregion

        public EventTriggerViewModel()
        {
            alert1Outs = new ObservableCollection<EventTriggerItemViewModel>();
            alert2Outs = new ObservableCollection<EventTriggerItemViewModel>();
            alarmOuts = new ObservableCollection<EventTriggerItemViewModel>();
        }

        #region Methods

        public void Load(EventTriggerDataObject eventTriggerDO, DeviceParamDataObject deviceParamDO)
        {
            Alert1Outs.Clear();
            Alert2Outs.Clear();
            AlarmOuts.Clear();

            LoadAlertList(Alert1Outs, deviceParamDO.HardwareAbility.AlarmOutPortNum, eventTriggerDO.Alert1Out);
            LoadAlertList(Alert2Outs, deviceParamDO.HardwareAbility.AlarmOutPortNum, eventTriggerDO.Alert2Out);
            LoadAlertList(AlarmOuts, deviceParamDO.HardwareAbility.AlarmOutPortNum, eventTriggerDO.AlarmOut);
        }

        public EventTriggerDataObject ConvertToDataObject()
        {
            EventTriggerDataObject dataObject = new EventTriggerDataObject();
            dataObject.AlarmOut = alarmOuts.Where(it => it.IsChecked).Select(it => it.ChannelNum).ToList();
            dataObject.Alert1Out = alert1Outs.Where(it => it.IsChecked).Select(it => it.ChannelNum).ToList();
            dataObject.Alert2Out = alert2Outs.Where(it => it.IsChecked).Select(it => it.ChannelNum).ToList();
            return dataObject;
        }

        public string ConvertToJson()
        {
            return JsonConvert.SerializeObject(new FireAgentDataObject() { EventTrigger = ConvertToDataObject() }, Formatting.Indented);
        }

        private static void LoadAlertList(ObservableCollection<EventTriggerItemViewModel> alertCollection, int alarmOutPortNum, List<int> dataObjectList)
        {
            for (int i = 0; i < alarmOutPortNum; i++)
            {
                EventTriggerItemViewModel itemViewModel = new EventTriggerItemViewModel
                {
                    ChannelNum = i + 1,
                };
                itemViewModel.IsChecked = dataObjectList?.Any(it => it == itemViewModel.ChannelNum) == true;
                alertCollection.Add(itemViewModel);
            }
        }

        #endregion
    }

    public class EventTriggerItemViewModel : BaseViewModel
    {
        private bool _isChecked;
        private int _channelNum;

        public int ChannelNum
        {
            get => _channelNum;
            set
            {
                _channelNum = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }
        public string Name => $"{I18n.Properties.Resources.Channel}{ChannelNum}";
    }

}
