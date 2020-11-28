using AutomaticWip.Core;
using log4net;
using System;
using System.Threading;

namespace AutomaticWip.Worker
{
    internal sealed class ServiceWrapper
    {
        internal const string SERVICE_NAME = "AutomaticWip";
        internal const string SERVICE_DYSPLAY_NAME = "Automatic Wip";
        internal const string SERVICE_DESCRIPTION = "Creates reports for SEMI Materials.";

        /// <summary>
        /// Logger object
        /// </summary>
        readonly ILog _log = LogManager.GetLogger(typeof(ServiceWrapper));

        /// <summary>
        /// The updater for settings.
        /// </summary>
        readonly Settings.Updater _updater;

        /// <summary>
        /// Internal timer
        /// </summary>
        Timer _timer;

        /// <summary>
        /// Interval to trigger.
        /// </summary>
        readonly TimeSpan _pulse;

        /// <summary>
        /// Worker flag
        /// </summary>
        bool _isWorking;

        /// <summary>
        /// locker
        /// </summary>
        readonly object _lock = new object();

        /// <summary>
        /// Initialize a new <see cref="ServiceWrapper"/>
        /// </summary>
        public ServiceWrapper()
        {
            _log.Debug("Service is initializing ..");
            _updater = new UpdaterImpl();
            _pulse = TimeSpan.FromMinutes(60);
            _timer = InitializeTimer();
        }

        /// <summary>
        /// Calculate and initialize the timer.
        /// </summary>
        /// <returns><see cref="Timer"/></returns>
        Timer InitializeTimer()
        {
            var now = DateTime.Now;
            var fixedNext = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
            var wait = fixedNext - now;
            _log.DebugFormat("Waiting time until first invokation: {0}", wait);
            return new Timer(Action, null, wait, _pulse);
        }

        /// <summary>
        /// Wrapper 
        /// </summary>
        /// <param name="obj">NULL</param>
        void Action(object obj)
        {
            lock (_lock)
            {
                if (!_isWorking)
                    return;

                Settings.Report(_updater);
            }
        }

        /// <summary>
        /// Starts the action.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (_isWorking)
                {
                    
                    _isWorking = false;
                    _log.Debug("Service has stopped!");
                }
            }
        }

        /// <summary>
        /// Stops the action.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (!_isWorking)
                {
                    _isWorking = true;
                    _log.Debug("Service has started!");
                }
            }
        }

        /// <summary>
        /// Impletementation of <see cref="Settings.Updater"/>
        /// </summary>
        internal sealed class UpdaterImpl : Settings.Updater
        {
            public UpdaterImpl()
                :base(
                        Settings.Property.Domain, 
                        Settings.Property.ExportDir, 
                        Settings.Property.Password, 
                        Settings.Property.QueryMaterials, 
                        Settings.Property.User,
                        Settings.Property.FileProtection,
                        Settings.Property.PropertyUpdaterPulse
                     )
            {
            }

            protected override void OnDispose()
            {
                _log.DebugFormat("{0} is stopping ..", nameof(Settings.Updater));
            }

            /// <summary>
            /// Restarts the service on critical changes.
            /// </summary>
            /// <param name="property">The property which was updated.</param>
            /// <param name="currentValue">The value from cache.</param>
            /// <param name="newValue">The new value.</param>
            /// <returns>True if value can be saved in local cache.</returns>
            protected override bool OnUpdated(Settings.Property property, string currentValue, string newValue)
            {
                switch (property)
                {
                    case Settings.Property.User:
                        _log.DebugFormat("Service is restarting due to a critical update on property '{0}'", property);
                        Environment.Exit(1);
                        return false;
                    case Settings.Property.Password:
                        _log.DebugFormat("Service is restarting due to a critical update on property '{0}'", property);
                        Environment.Exit(1);
                        return false;
                    case Settings.Property.Domain:
                        _log.DebugFormat("Service is restarting due to a critical update on property '{0}'", property);
                        Environment.Exit(1);
                        return false;
                    case Settings.Property.QueryMaterials:
                        _log.DebugFormat("A new updated has been found for property: {0}", property);
                        return true;
                    case Settings.Property.ExportDir:
                        _log.DebugFormat("A new updated has been found for property: {0}", property);
                        return true;
                    case Settings.Property.FileProtection:
                        _log.DebugFormat("A new updated has been found for property: {0}", property);
                        return true;
                    case Settings.Property.PathFormat:
                        _log.DebugFormat("A new updated has been found for property: {0}", property);
                        return true;
                    case Settings.Property.QueryUpdateProperties:
                        _log.DebugFormat("Service is restarting due to a critical update on property '{0}'", property);
                        Environment.Exit(1);
                        return false;
                    case Settings.Property.PropertyUpdaterPulse:
                        _log.DebugFormat("Service is restarting due to a critical update on property '{0}'", property);
                        Environment.Exit(1);
                        return false;
                }

                return true;
            }
        }
    }
}
