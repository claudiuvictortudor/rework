using AutomaticWip.Contracts;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AutomaticWip.Modules.Materials
{
    public sealed class MaterialProvider
    {
        /// <summary>
        /// Source data from MAMA.T_MAT_DEF
        /// </summary>
        public static IEnumerable<Material> Source
        {
            get
            {
                using (var conn = new OracleConnection(Settings.MES_OLTP))
                {
                    return conn.Query<Material>(sql: "SELECT PART_NUMBER, PART_NAME, PART_GROUP FROM MAMA.T_MAT_DEF WHERE PART_TYPE <> 'RAW'", commandType: CommandType.Text);
                }
            }
        }

        /// <summary>
        /// Split the group on div/zone/area/proj
        /// </summary>
        /// <param name="value">The value to split</param>
        /// <param name="childs">The sub values of this <see cref="value"/></param>
        /// <returns>True if has childs</returns>
        static bool Split(string value, out string[] fields)
        {
            fields = Array.Empty<string>();
            if (value?.Contains("_") is true)
            {
                fields = value.Split('_');
                if (fields.Length == 4)
                    return true;
                else
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Synchronise materials in clone server
        /// </summary>
        public static void Synchronise(IEnumerable<Material> source)
        {
            var table = new DataTable("rows");
            table.Columns.Add("MATERIAL_NUMBER", typeof(string));
            table.Columns.Add("MATERIAL_GROUP", typeof(string));
            table.Columns.Add("DIVISION_NAME", typeof(string));
            table.Columns.Add("ZONE_NAME", typeof(string));
            table.Columns.Add("AREA_NAME", typeof(string));
            table.Columns.Add("MATERIAL_DESCRIPTON", typeof(string));
            table.Columns.Add("RAW_GROUP", typeof(string));

            foreach (var item in source)
            {
                if (Split(item.PART_GROUP, out string[] values))
                    table.Rows.Add(item.PART_NUMBER, values[3], values[0], values[1], values[2], item.PART_NAME, item.PART_GROUP);
                else
                    table.Rows.Add(item.PART_NUMBER, item.PART_GROUP, "N/A", "N/A", "N/A", item.PART_NAME, item.PART_GROUP);
            }

            var p = new DynamicParameters();
            p.Add("@rows", table.AsTableValuedParameter());
            using (var conn = new SqlConnection(Settings.TMAS275A))
            {
                conn.Execute(sql: "dbo.UpdateMaterials", param: p, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
