using Dapper;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace AutomaticWip.Core
{
    public static partial class Settings
    {
        /// <summary>
        /// Keeps track on settings.
        /// </summary>
        public abstract class Updater
        {
            /// <summary>
            /// Disposal flag
            /// </summary>
            bool _disposed;

            /// <summary>
            /// logger object
            /// </summary>
            protected readonly ILog _log = LogManager.GetLogger(typeof(Updater));

            /// <summary>
            /// Since this will be used in a worker, a thread sync has to be assured.
            /// </summary>
            readonly object _lock = new object();

            /// <summary>
            /// Maps enum values.
            /// </summary>
            readonly IDictionary<Property, string> _map = new Dictionary<Property, string>();

            /// <summary>
            /// The values of the properties;
            /// </summary>
            readonly IDictionary<string, string> _values = new Dictionary<string, string>();

            /// <summary>
            /// Max cycle time.
            /// </summary>
            readonly TimeSpan _pulse;

            /// <summary>
            /// The internal timer
            /// </summary>
            readonly Timer _timer;

            /// <summary>
            /// The connection string for db.
            /// </summary>
            readonly string _connection;

            /// <summary>
            /// The query to run to update properties;
            /// </summary>
            readonly string _queryProperties;

            /// <summary>
            /// Initialize a new updater based on a list of properties.
            /// </summary>
            /// <param name="properties">The properties to be checked.</param>
            protected Updater(params Property[] properties)
            {
                Compile.Against<ArgumentNullException>(properties?.Any() != true, $"{nameof(properties)} cannot be null, empty.");
                foreach (var item in properties)
                {
                    _map[item] = item.GetAlias();
                }

                foreach (var item in _map)
                {
                    _values[item.Value] = "null";
                }

                _connection = Property.TMAS275A.GetAlias();
                _queryProperties = Property.QueryUpdateProperties.GetAlias();

                _log.Debug("Updater is initializing ..");

                // First run to avoid any checks.
                UpdateWrapper(false);

                var pulse = long.Parse(GetValue(Property.PropertyUpdaterPulse));
                _pulse = TimeSpan.FromMilliseconds(pulse);
                _timer = new Timer(Update, null, TimeSpan.Zero, _pulse);
            }

            /// <summary>
            /// Disposal flag.
            /// </summary>
            public bool Disposed
            {
                get
                {
                    lock (_lock)
                    {
                        return _disposed;
                    }
                }
            }

            /// <summary>
            /// Gets the property from cache.
            /// </summary>
            /// <param name="property">Property value.</param>
            /// <returns>The property value from cache.</returns>
            public string GetValue(Property property)
            {
                lock (_lock)
                {
                    Compile.Against(!_map.ContainsKey(property), "Invalid property.");
                    return _values[_map[property]];
                }
            }

            /// <summary>
            /// Updates the cache.
            /// </summary>
            void Update(object obj)
            {
                lock (_lock)
                {
                    if (_disposed)
                        return;

                    UpdateWrapper(true);
                }
            }

            /// <summary>
            /// The logic of this updater.
            /// </summary>
            void UpdateWrapper(bool notify)
            {
                try
                {
                    if(notify)
                        _log.Debug("Checking for updated properties ..");

                    var props = _map.Values;
                    var reverseMap = new Dictionary<string, Property>();
                    foreach (var item in _map)
                    {
                        reverseMap[item.Value] = item.Key;
                    }

                    // Defaults
                    IEnumerable<(string propName, string propValue)> result = new List<(string propName, string propValue)>();

                    var parameters = new DynamicParameters();
                    parameters.Add(name: "list", value: props);
                    using (var cnn = new SqlConnection(_connection))
                    {
                        result = cnn.Query<(string propName, string propValue)>(sql: _queryProperties, parameters, commandType: System.Data.CommandType.Text, commandTimeout: 10);
                    }

                    if(result.Count() < 1)
                    {
                        _log.Warn("Updater didn't found any properties in database.");
                        return;
                    }

                    foreach (var (propName, propValue) in result)
                    {
                        var cached = _values[propName];
                        if(!cached.Equals(propValue, StringComparison.Ordinal))
                        {
                            if(notify && OnUpdated(reverseMap[propName], cached, propValue))
                            {
                                _values[propName] = propValue;
                                _log.DebugFormat("New value is updated in cache for: {0}", propName);
                            }
                            else
                            {
                                _values[propName] = propValue;
                                _log.DebugFormat("New value is updated in cache for: {0}", propName);
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    _log.Error(nameof(Settings.Updater.Update), e);
                }
            }

            /// <summary>
            /// Fired when a property has a changed value.
            /// </summary>
            /// <param name="property">Property value.</param>
            /// <param name="currentValue">The value from local cache.</param>
            /// <param name="newValue">The new value.</param>
            /// <returns>True if new value can be set in local cache.</returns>
            protected abstract bool OnUpdated(Property property, string currentValue, string newValue);

            /// <summary>
            /// Called on dispose.
            /// </summary>
            protected abstract void OnDispose();

            /// <summary>
            /// Release the timer, clear cache, etc.
            /// </summary>
            public void Dispose()
            {
                lock (_lock)
                {
                    if (_disposed)
                        return;

                    OnDispose();
                    _timer.Dispose();
                    _map.Clear();
                    _values.Clear();
                    _disposed = true;
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}
