// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;

namespace Firefly.Client.Service
{
    public class HubConnectionBO
    {
        private HubConnectionState _state;

        public ServerDO ServerDO { get; set; }
        public HubConnection HubConnection { get; set; }
        public Action<int, string> EventNotifyCallback { get; set; }
        public List<string> SubscribedDeviceIDs { get; set; }

        public bool IsConnectionStateChanged()
        {
            if (HubConnection.State == _state)
            {
                return false;
            }

            _state = HubConnection.State;
            return true;
        }

        public void EventNotifyHandler(string message)
        {
            if (ServerDO != null)
            {
                EventNotifyCallback?.Invoke(ServerDO.ID, message);
            }
        }
    }
}