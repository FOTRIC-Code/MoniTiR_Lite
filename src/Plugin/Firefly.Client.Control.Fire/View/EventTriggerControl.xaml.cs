// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using Newtonsoft.Json;

using Firefly.Client.Service;

namespace Firefly.Client.Control.Fire
{
    /// <summary>
    /// Interaction logic for EventTriggerControl.xaml
    /// </summary>
    public partial class EventTriggerControl : TabItem
    {
        private EventTriggerViewModel eventTriggerVM;

        public EventTriggerControl()
        {
            InitializeComponent();
            eventTriggerVM = new EventTriggerViewModel();
            DataContext = eventTriggerVM;
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            eventTriggerVM.Load(FireAgentDeviceManager.Instance.LoadEventTriggerDataObject(), FireAgentDeviceManager.Instance.GetDeviceParamDO());
        }

        public bool IsConfigChanged()
        {
            EventTriggerDataObject oldValue = FireAgentDeviceManager.Instance.LoadEventTriggerDataObject();
            EventTriggerDataObject newValue = eventTriggerVM.ConvertToDataObject();
            return JsonConvert.SerializeObject(oldValue) != JsonConvert.SerializeObject(newValue);
        }

        private async void btSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                EventTriggerDataObject eventTriggerDataObject = eventTriggerVM.ConvertToDataObject();
                ApiResult<bool> result = await FireAgentDeviceManager.Instance.UpdateDeviceConfig(new FireAgentDataObject { EventTrigger = eventTriggerDataObject });
                if (result.IsSuccess)
                {
                    FireAgentDeviceManager.Instance.UpdateEventTriggerDataObject(eventTriggerDataObject);
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else if (result.IsForbidden)
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), ErrorCodeTranslator.Translate(result.Code), I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
                else
                {
                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        public EventTriggerDataObject GetEventTriggerDataObject()
        {
            return eventTriggerVM.ConvertToDataObject();
        }
    }
}
