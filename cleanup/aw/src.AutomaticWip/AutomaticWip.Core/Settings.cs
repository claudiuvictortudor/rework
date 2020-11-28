using Dapper;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace AutomaticWip.Core
{
    /// <summary>
    /// db settings
    /// </summary>
    public sealed class Settings : IDisposable
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        internal sealed class Map : Attribute
        {
            /// <summary>
            /// The value from database.
            /// </summary>
            public string Value { get; set; }
        }

        /// <summary>
        /// Variables used by the worker.
        /// </summary>
        public enum Variables
        {
            /// <summary>
            /// Username used to login
            /// </summary>
            [Map(Value = "login/user")]
            User = 0,

            /// <summary>
            /// Password for the user
            /// </summary>
            [Map(Value = "login/pass")]
            Pass = 1,

            /// <summary>
            /// Domain of the user
            /// </summary>
            [Map(Value = "login/domain")]
            Domain = 2,

            /// <summary>
            /// Query to get material;s data
            /// </summary>
            [Map(Value = "query/materials")]
            QueryMaterials = 3,

            /// <summary>
            /// Where to output the data
            /// </summary>
            [Map(Value = "file/output")]
            ExportPath = 4,

            /// <summary>
            /// Interval to run
            /// </summary>
            [Map(Value = "pulse/interval")]
            CycleTime = 5,

            /// <summary>
            /// Seets a password to the worksheet.
            /// </summary>
            [Map(Value = "file/protection")]
            FileProtection = 6
        }

        /// <summary>
        /// Helper model
        /// </summary>
        sealed class SettingValue
        {
            /// <summary>
            /// Object value
            /// </summary>
            internal object Value;

            /// <summary>
            /// Object type
            /// </summary>
            internal Type Type;
        }

        /// <summary>
        /// Disposal flag
        /// </summary>
        bool _disposed = false;

        /// <summary>
        /// Initialization flag
        /// </summary>
        bool _initialized = false;

        /// <summary>
        /// Thread safe
        /// </summary>
        readonly object _lock = new object();

        /// <summary>
        /// Connectionstring to the WIP Online db.
        /// </summary>
        internal const string WIP_ONLINE = "Data Source=(DESCRIPTION =(ADDRESS =(PROTOCOL = TCP)(HOST = tmdb008.TM.RO.CONTI.DE)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)(SERVICE_NAME = REPORTING.TM.RO.CONTI.DE)));User Id=MESREAD;Password=MESREAD;";

        /// <summary>
        /// Not avaible
        /// </summary>
        internal const string NOT_AVAIBLE = "N/A";

        /// <summary>
        /// Connection string to the database
        /// </summary>
        readonly string AUTOMATIC_WIP_SETTINGS = @"Server=tmas275a\I0001;Database=AutomaticWip;User Id=sa;Password=Q!w2e3r4t5;";

        /// <summary>
        /// Gets the value for a setting
        /// </summary>
        const string QuerySetting = "SELECT VariableValue, VariableType FROM [dbo].[WorkerVariables] WHERE VariableName = @name";

        /// <summary>
        /// Cache
        /// </summary>
        readonly IDictionary<string, SettingValue> _parameters = new Dictionary<string, SettingValue>();

        /// <summary>
        /// <see cref="Timer"/>
        /// </summary>
        Timer _timer;

        /// <summary>
        /// Keeps metadata to avoid reflection.
        /// </summary>
        readonly DataPool.IDataMapper _mapper = DataPool.GetMapper(false);


        /// <summary>
        /// Logger object
        /// </summary>
        ILog _log;

        /// <summary>
        /// Initialize a new settings
        /// </summary>
        public Settings(ILog log, params Variables[] properties)
        {
            _log = log;
            _mapper.Allocate<Variables>(typeof(Map));

            if (properties?.Any() != true) // No point on continue here
                throw new ArgumentNullException(nameof(properties));

            // prepare defaults
            foreach (var item in properties)
            {
                var local = _mapper.Get<string, Map>(item, var => var.Value);
                _parameters[local] = new SettingValue();
            }

            // Ensure this has values.
            ActionUpdater(null);

            // Info
            _log?.DebugFormat("Settings to watch for: {0}", string.Join(", ", properties));
            _initialized = true;
        }

        /// <summary>
        /// Initialize the scheduler.
        /// </summary>
        public void Initialize(int interval)
        {
            lock (_lock)
            {
                if (!_initialized)
                    throw new NotSupportedException("Initialization was not done correctly.");

                if (_disposed)
                    return;

                var cycle = TimeSpan.FromMilliseconds(interval);
                _timer = new Timer(ActionUpdater, null, cycle, cycle);
                _disposed = false;
                _log?.DebugFormat("Updating interval is set at: {0}", cycle);
            }
        }

        /// <summary>
        /// Gets a property value from cache.
        /// </summary>
        /// <param name="variable">Name of the property.</param>
        /// <returns>Value of the object</returns>
        public object Get(Variables variable)
        {
            lock (_lock)
            {
                var local = _mapper.Get<string, Map>(variable, var => var.Value);
                if (!_parameters.ContainsKey(local))
                    throw new ArgumentException("Invalid property name!", nameof(variable));

                if (!_initialized)
                    throw new NotSupportedException("Initialization was not done correctly.");

                var obj = _parameters[local];
                if (obj.Value != null)
                {
                    return obj.Value;
                }

                return null;
            }
        }

        /// <summary>
        /// Wrapper to be executed by the <see cref="Timer"/>
        /// </summary>
        /// <param name="obj">null</param>
        void ActionUpdater(object obj)
        {
            lock (_lock)
            {
                foreach (var item in _parameters)
                    UpdateInternalCache(item.Key);
            }
        }

        /// <summary>
        /// Updates the cache
        /// </summary>
        /// <param name="property"></param>
        void UpdateInternalCache(string property)
        {
            (object value, string type) local;
            try
            {
                _log?.DebugFormat("Updating cache for: {0}", property);
                var parameters = new DynamicParameters();
                parameters.Add("name", property);
                using (var conn = new SqlConnection(AUTOMATIC_WIP_SETTINGS))
                {
                    local = conn.Query<(object value, string type)>(sql: QuerySetting, param: parameters, commandType: System.Data.CommandType.Text)
                        .FirstOrDefault();
                }

                if (local.value != null)
                {
                    var type = Type.GetType(local.type, true, true);
                    var localEntry = _parameters[property];
                    localEntry.Type = type;
                    localEntry.Value = local.value;
                }
            }
            catch (Exception e)
            {
                _log?.ErrorFormat("Error while updating cache for: {0}", property);
                _log?.Error(nameof(UpdateInternalCache), e);
            }
        }

        /// <summary>
        /// Release the timer.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _timer?.Dispose();
                foreach (var item in _parameters)
                {
                    item.Value.Type = null;
                    item.Value.Value = null;
                }
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
