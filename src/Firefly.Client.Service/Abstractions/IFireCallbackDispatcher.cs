// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// Process-local alarm/device event dispatch (replaces SignalR /event and /alarm hubs).
    /// Must use bounded queue + fire-and-forget; never block device-engine threads.
    /// </summary>
    public interface IFireCallbackDispatcher
    {
        /// <summary>Subscribe a consumer for device events (Manager instance). Returns unsubscribe action.</summary>
        IDisposable SubscribeEvents(Action<int, string> handler);

        /// <summary>Subscribe a consumer for alarms. Returns unsubscribe action.</summary>
        IDisposable SubscribeAlarms(Action<AlarmDO> handler);

        /// <summary>Enqueue a device event from engine thread (non-blocking).</summary>
        void PublishEvent(int serverId, string payloadJson);

        /// <summary>Enqueue an alarm from engine thread (non-blocking).</summary>
        void PublishAlarm(AlarmDO alarm);

        /// <summary>Dropped messages when queue is full (for diagnostics).</summary>
        long DroppedCount { get; }
    }
}
