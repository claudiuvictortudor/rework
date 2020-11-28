using AutomaticWip.Contracts;
using AutomaticWip.Modules.Excel;
using AutomaticWip.Modules.Stocks.ExcelSerializers;
using AutomaticWip.Modules.Stocks.Models;
using Dapper;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AutomaticWip.Modules.Stocks
{
    public static class StockManager
    {
        /// <summary>
        /// Get raw stocks from source server
        /// </summary>
        const string GET_STOCKS = "SELECT MATERIAL, UNIT_ID, UNIT_ID_TYPE, QUANTITY, DIFF_DAYS, DIFF_HOURS, DIFF_MINUTES, CREATED FROM TABLE(REP_AUTOMATIC_WIP.GET_STOCK_DETAILS(:begin, :end))";

        /// <summary>
        /// Get serialized file from history table on clone server
        /// </summary>
        const string HIS_FILE = "SELECT COMPRESSED_DATA FROM T_MAT_CONTAINER_HIS WHERE GENERATED_AT = @time";

        /// <summary>
        /// Excel serializer for stock aggregations
        /// </summary>
        static readonly ExcelSerializer<StockAggregation> AggregateSerializer = new AggregateSerializer();

        /// <summary>
        /// Excel serializer for stocks
        /// </summary>
        static readonly ExcelSerializer<Stock> StockSerializer = new StockSerializer();

        /// <summary>
        /// Serialize the whole collection as byte[](xcel format)
        /// </summary>
        /// <param name="stocks">Collection of stocks</param>
        /// <param name="stockAggregations">Collection of aggregations</param>
        /// <returns>Serialized file</returns>
        public static byte[] Serialize(IEnumerable<Stock> stocks, IEnumerable<StockAggregation> stockAggregations)
        {
            using (var package = new ExcelPackage())
            {
                StockSerializer.Serialize(package.Workbook, stocks);
                AggregateSerializer.Serialize(package.Workbook, stockAggregations);
                return package.GetAsByteArray();
            }
        }

        /// <summary>
        /// Serialize the whole collection of stocks as byte[](xcel format)
        /// </summary>
        /// <param name="stocks">Collection of stocks</param>
        /// <returns>Serialized file</returns>
        public static byte[] Serialize(IEnumerable<Stock> stocks)
        {
            using (var package = new ExcelPackage())
            {
                StockSerializer.Serialize(package.Workbook, stocks);
                return package.GetAsByteArray();
            }
        }

        /// <summary>
        /// Serialize the whole collection of aggregations as byte[](xcel format)
        /// </summary>
        /// <param name="stockAggregations">Collection of stock aggregations</param>
        /// <returns>Serialized file</returns>
        public static byte[] Serialize(IEnumerable<StockAggregation> stockAggregations)
        {
            using (var package = new ExcelPackage())
            {
                AggregateSerializer.Serialize(package.Workbook, stockAggregations);
                return package.GetAsByteArray();
            }
        }

        /// <summary>
        /// Request a file from history
        /// </summary>
        /// <param name="synchronisationTime">Identifier for the report</param>
        /// <returns>The serialized content</returns>
        public static byte[] RequestFile(DateTime synchronisationTime)
        {
            var p = new DynamicParameters();
            p.Add("@time", synchronisationTime, DbType.DateTime);
            using (var cnn = new SqlConnection(Settings.TMAS275A))
            {
                return cnn.Query<byte[]>(sql: HIS_FILE, param: p, commandType: CommandType.Text)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Query containers from source server
        /// </summary>
        /// <param name="begin">Begin range</param>
        /// <param name="end">End range</param>
        /// <returns>Found containers</returns>
        public static IEnumerable<Container> Q_T_MAT_CONTAINER(DateTime begin, DateTime end)
        {
            // Check for invalid range
            Compile.Against<InvalidOperationException>(end <= begin, $"End range({end}) must be greater than begin({begin})!");

            var p = new DynamicParameters();
            p.Add(":begin", begin, DbType.DateTime);
            p.Add(":end", end, DbType.DateTime);

            using (var cnn = new OracleConnection(Settings.MES_OLAP))
            {
                return cnn.Query<Container>(sql: GET_STOCKS, param: p, commandType: CommandType.Text);
            }
        }

        /// <summary>
        /// Update the file in clone history server
        /// </summary>
        /// <param name="synchronised">When this file was synchronised</param>
        /// <param name="content">The content of the file</param>
        public static void UpdateHistory(DateTime synchronised, byte[] content)
        {
            var p = new DynamicParameters();
            p.Add("@sync", synchronised, DbType.DateTime);
            p.Add("@data", content, DbType.Binary);
            p.Add("@by", StockSerializer.Encrypter);
            p.Add("@size", content.Length, DbType.Int64);

            using (var cnn = new SqlConnection(Settings.TMAS275A))
            {
                cnn.Execute(sql: "dbo.UpdateContainerHistory", param: p, commandType: CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// Update containers on clone server
        /// </summary>
        /// <param name="containers">Collection of <see cref="Container"/></param>
        /// <param name="synchTime">When these containers have been found</param>
        /// <returns>Rows affected</returns>
        public static int UpdateClone(IEnumerable<Container> containers, DateTime synchTime)
        {
            // If sql columns are aphabetically ordered, this is no needed
            var table = new DataTable("rows");
            table.Columns.Add("AGE_DAYS", typeof(int));
            table.Columns.Add("AGE_HOURS", typeof(int));
            table.Columns.Add("AGE_MINUTES", typeof(int));
            table.Columns.Add("CREATED", typeof(DateTime));
            table.Columns.Add("MATERIAL", typeof(string));
            table.Columns.Add("PART_ID", typeof(string));
            table.Columns.Add("PART_ID_TYPE", typeof(string));
            table.Columns.Add("QUANTITY", typeof(double));
            table.Columns.Add("SYNCHRONISATION_TIME", typeof(DateTime));

            // aaaarrsqqqqqqqlll!!!
            foreach (var item in containers)
                item.SYNCHRONISATION_TIME = synchTime;

            var param = new DynamicParameters();
            param.Add("@rows", containers.AsTable(table).AsTableValuedParameter());

            //containers.AsTable(table).AsTableValuedParameter() 
            //  -> Injects a collection of models to an already defined datatable(to match the one from sql) and convert it as UDT
            //  -> This should be used only with big collections to avoid mid-interupts/incomplete executions/etc

            using (var cnn = new SqlConnection(Settings.TMAS275A))
            {
                return cnn.Execute("dbo.UpdateContainers", param: param, commandType: CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// Stocks from clone server
        /// </summary>
        public static IEnumerable<Stock> T_CLONE_STOCK
        {
            get
            {
                using (var cnn = new SqlConnection(Settings.TMAS275A))
                {
                    return cnn.Query<Stock>("dbo.GetStocks", commandType: CommandType.StoredProcedure);
                }
            }
        }

        /// <summary>
        /// Aggregate the stocks
        /// </summary>
        /// <param name="stocks">Stocks from clone server</param>
        /// <returns>Collection of aggregate stocks</returns>
        public static IEnumerable<StockAggregation> Aggregate(IEnumerable<Stock> stocks)
        {
            var result = new List<StockAggregation>();
            foreach (var item in stocks.GroupBy(stock => stock.MATERIAL))
            {
                var aggregation = Activator.CreateInstance<StockAggregation>();
                var common = item.First();
                aggregation.Area = common.AREA;
                aggregation.Description = common.DESCRIPTION;
                aggregation.Division = common.DIVISION;
                aggregation.Material = common.MATERIAL;
                aggregation.Project = common.PROJECT;
                aggregation.Zone = common.ZONE;
                aggregation.SmallerOrEqualOneDay = (int)item.Where(s => s.AGE_DAYS < 2).Select(s => s.QUANTITY).Sum();
                aggregation.BetweenTwoAndFiveDays = (int)item.Where(s => s.AGE_DAYS >= 2 && s.AGE_DAYS <= 5).Select(s => s.QUANTITY).Sum();
                aggregation.MoreThanFiveDays = (int)item.Where(s => s.AGE_DAYS > 5).Select(s => s.QUANTITY).Sum();

                result.Add(aggregation);
            }

            return result;
        }
    }
}
