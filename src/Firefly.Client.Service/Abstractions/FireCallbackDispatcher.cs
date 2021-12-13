// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;

using log4net;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// Bounded-queue callback dispatcher. Engine threads only Enqueue;
    /// consumer callbacks optionally marshal via captured SynchronizationContext (UI).
    /// </summary>
    public sealed class FireCallbackDispatcher : IFireCallbackDispatcher, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FireCallbackDispatcher));
        private const int MaxQueue = 2048;

        private readonly object _sync = new object();
        private readonly Queue<Action> _queue = new Queue<Action>();
        private readonly List<Action<int, string>> _eventHandlers = new List<Action<int, string>>();
        private readonly List<Action<AlarmDO>> _alarmHandlers = new List<Action<AlarmDO>>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Thread _worker;
        private long _dropped;
        private SynchronizationContext _syncContext;

        public FireCallbackDispatcher()
        {
            _worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "FireCallbackDispatcher",
            };
            _worker.Start();
        }

        public long DroppedCount => Interlocked.Read(ref _dropped);

        public void SetSynchronizationContext(SynchronizationContext context)
        {
            _syncContext = context;
        }

        public IDisposable SubscribeEvents(Action<int, string> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (_sync)
            {
                _eventHandlers.Add(handler);
            }

            return new Unsubscriber(() =>
            {
                lock (_sync)
                {
                    _eventHandlers.Remove(handler);
                }
            });
        }

        public IDisposable SubscribeAlarms(Action<AlarmDO> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (_sync)
            {
                _alarmHandlers.Add(handler);
            }

            return new Unsubscriber(() =>
            {
                lock (_sync)
                {
                    _alarmHandlers.Remove(handler);
                }
            });
        }

        public void PublishEvent(int serverId, string payloadJson)
        {
            Enqueue(() =>
            {
                Action<int, string>[] handlers;
                lock (_sync)
                {
                    handlers = _eventHandlers.ToArray();
                }

                foreach (var h in handlers)
                {
                    try
                    {
                        InvokeOnContext(() => h(serverId, payloadJson));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Event handler failed.", ex);
                    }
                }
            });
        }

        public void PublishAlarm(AlarmDO alarm)
        {
            Enqueue(() =>
            {
                Action<AlarmDO>[] handlers;
                lock (_sync)
                {
                    handlers = _alarmHandlers.ToArray();
                }

                foreach (var h in handlers)
                {
                    try
                    {
                        InvokeOnContext(() => h(alarm));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Alarm handler failed.", ex);
                    }
                }
            });
        }

        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _worker.Join(2000);
            }
            catch
            {
            }

            _cts.Dispose();
        }

        private void Enqueue(Action work)
        {
            lock (_sync)
            {
                if (_queue.Count >= MaxQueue)
                {
                    Interlocked.Increment(ref _dropped);
                    if (_queue.Count > 0)
                    {
                        _queue.Dequeue();
                    }
                }

                _queue.Enqueue(work);
                Monitor.Pulse(_sync);
            }
        }

        private void WorkerLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                Action work = null;
                lock (_sync)
                {
                    while (_queue.Count == 0 && !_cts.IsCancellationRequested)
                    {
                        Monitor.Wait(_sync, 500);
                    }

                    if (_queue.Count > 0)
                    {
                        work = _queue.Dequeue();
                    }
                }

                if (work == null)
                {
                    continue;
                }

                try
                {
                    work();
                }
                catch (Exception ex)
                {
                    Logger.Error("Dispatcher work failed.", ex);
                }
            }
        }

        private void InvokeOnContext(Action action)
        {
            SynchronizationContext ctx = _syncContext;
            if (ctx != null)
            {
                ctx.Post(_ =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("UI-marshaled callback failed.", ex);
                    }
                }, null);
            }
            else
            {
                action();
            }
        }

        private sealed class Unsubscriber : IDisposable
        {
            private Action _dispose;

            public Unsubscriber(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Action d = Interlocked.Exchange(ref _dispose, null);
                d?.Invoke();
            }
        }
    }
}
