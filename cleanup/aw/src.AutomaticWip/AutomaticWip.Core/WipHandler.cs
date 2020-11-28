using AutomaticWip.Core.Models;
using Dapper;
using log4net;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;

namespace AutomaticWip.Core
{
    /// <summary>
    /// db handler
    /// </summary>
    public sealed class WipHandler
    {
        /// <summary>
        /// Domain settings
        /// </summary>
        readonly Settings _settings;

        /// <summary>
        /// Logger object
        /// </summary>
        readonly ILog _log;

        /// <summary>
        /// Thread safe
        /// </summary>
        readonly object _lock = new object();

        /// <summary>
        /// Initialize a new <see cref="WipHandler"/>
        /// </summary>
        public WipHandler(Settings settings)
        {
            _log = LogManager.GetLogger(typeof(WipHandler));
            _settings = settings;
        }

        /// <summary>
        /// Execute the query
        /// </summary>
        public IEnumerable<MATERIAL_DATA> Load()
        {
            lock (_lock)
            {
                var query = _settings.Get(Settings.Variables.QueryMaterials) as string;
                using (var conn = new OracleConnection(Settings.WIP_ONLINE))
                {
                    return conn.Query<MATERIAL_DATA>(sql: query, commandType: System.Data.CommandType.Text);
                }
            }
        }

        /// <summary>
        /// Gets all the data needed for partnumbers.
        /// </summary>
        /// <param name="table">Container for data.</param>
        /// <returns>The container with data.</returns>
        public DataTable GetMaterials(DataTable table = null, string tableName = "")
        {
            lock (_lock)
            {
                if (table is null)
                    table = new DataTable();

                if (string.IsNullOrWhiteSpace(table.TableName))
                    table.TableName = string.IsNullOrWhiteSpace(tableName) ? "$data" : tableName;

                var query = _settings.Get(Settings.Variables.QueryMaterials) as string;
                using (var conn = new OracleConnection(Settings.WIP_ONLINE))
                {
                    conn.Open();
                    using (var cmd = new OracleCommand())
                    {
                        cmd.CommandText = query;
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;
                        using (var adapter = new OracleDataAdapter(cmd))
                        {
                            adapter.Fill(table);
                        }
                    }
                }

                return table;
            }
        }
    }
}
