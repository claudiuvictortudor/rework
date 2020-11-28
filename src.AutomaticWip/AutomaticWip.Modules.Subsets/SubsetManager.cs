using AutomaticWip.Contracts;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Data.SqlClient;

namespace AutomaticWip.Modules.Subsets
{
    public static class SubsetManager
    {
        /// <summary>
        /// Gets the process step info from oltp MES server
        /// </summary>
        const string GET_PROCESS_STEP_INFO = "SELECT DISTINCT p.JOB, p.PROCESS_STEP, p.DESCRIPTION_SHORT, j.PRODUCT_DEFINITION FROM T_WIP_PROCESS_STEP p JOIN T_WIP_JOB j ON p.JOB = j.JOB WHERE j.JOB_TYPE = 'LOIPLO'";

        /// <summary>
        /// Get all subsets from a given interval
        /// </summary>
        const string GET_SUBSET_DATA = "SELECT * FROM TABLE(REP_AUTOMATIC_WIP.GET_AUTOMATIC_WIP_SUBSETS(TO_DATE(:since, 'YYYY-MM-DD HH24:MI:SS')))";

        /// <summary>
        /// Source process steps 
        /// </summary>
        public static DataTable T_PROCESS_STEP
        {
            get
            {
                var oltp = new DataTable();
                using (var conn = new OracleConnection(Settings.MES_OLTP))
                {
                    conn.Open();
                    using (var cmd = new OracleCommand())
                    {
                        cmd.CommandText = GET_PROCESS_STEP_INFO;
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;
                        using (var adapter = new OracleDataAdapter(cmd))
                        {
                            adapter.Fill(oltp);
                        }
                    }
                }

                return oltp;
            }
        }

        /// <summary>
        /// Update process step info in clone server
        /// </summary>
        public static void UpdateProcessStep(DataTable oltp)
        {
            var p = new DynamicParameters();
            p.Add("@rows", oltp.AsTableValuedParameter());

            // Update the clone server
            using (var conn = new SqlConnection(Settings.TMAS275A))
            {
                conn.Execute(sql: "dbo.UpdateWipInfo", param: p, commandType: CommandType.StoredProcedure, commandTimeout: 60);
            }
        }

        /// <summary>
        /// Source subsets
        /// </summary>
        public static DataTable T_WIP_SUBSET
        {
            get
            {
                var oltp = new DataTable();
                using (var conn = new OracleConnection(Settings.MES_OLAP))
                {
                    conn.Open();
                    using (var cmd = new OracleCommand())
                    {
                        cmd.CommandText = GET_SUBSET_DATA;
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add(new OracleParameter
                        {
                            OracleDbType = OracleDbType.Varchar2,
                            ParameterName = "since",
                            Value = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss")
                        });

                        using (var adapter = new OracleDataAdapter(cmd))
                        {
                            adapter.Fill(oltp);
                        }
                    }
                }

                return oltp;
            }
        }

        /// <summary>
        /// Update the subsets in clone server
        /// </summary>
        public static int Update(DataTable subsets)
        {
            var p = new DynamicParameters();
            p.Add("@rows", subsets.AsTableValuedParameter());

            // Update the clone server
            using (var conn = new SqlConnection(Settings.TMAS275A))
            {
                return conn.Execute(sql: "dbo.UpdateSubsets", param: p, commandType: CommandType.StoredProcedure, commandTimeout: 60);
            }
        }
    }
}
