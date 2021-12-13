// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

using log4net;

using Firefly.Client.Common;
using Amur.Common.Extension;
using System.Threading;

namespace Firefly.Client.Service
{
    public class TrackingService
    {
        #region Instance

        private static Lazy<TrackingService> _lazyService = new Lazy<TrackingService>(() => new TrackingService());

        private TrackingService()
        {
        }

        public static TrackingService Instance => _lazyService.Value;

        #endregion Instance

        private object _lockObj = new object();

        private ILog _logger = LogManager.GetLogger(typeof(TrackingService));

        private string _currentView;
        private string _lastView;

        public void In(Type type)
        {
            lock (_lockObj)
            {
                _logger.EventTracking($"Goto {type.Name}");
                _currentView = type.Name;
            }
        }

        public void Close(Type type)
        {
            lock (_lockObj)
            {
                _logger.EventTracking($"Close {type.Name}");
            }
        }

        public void Out(Type type)
        {
            lock (_lockObj)
            {
                _logger.EventTracking($"Leave {type.Name}");
            }
        }

        public void HasAError(Type type)
        {
            _logger.EventTracking($"Has a Error: {type.Name}");
        }

        public void Start()
        {
            Thread thread = new Thread(DoStart);
            thread.IsBackground = true;
            thread.Start();
        }

        private void DoStart()
        {
            while (true)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(_currentView))
                    {
                        lock (_lockObj)
                        {
                            if (!string.IsNullOrWhiteSpace(_currentView))
                            {
                                if (string.Equals(_lastView, _currentView))
                                {
                                    continue;
                                }
                                _lastView = _currentView;
                                _logger.EventTracking($"Currently {_currentView}");
                            }
                        }
                    }
                }
                finally
                {
                    Thread.Sleep(Constants.TRACKING_TIME);
                }
            }
        }
    }
}