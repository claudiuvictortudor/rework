using AutomaticWip.Modules.Excel;
using System;
using static AutomaticWip.Contracts.DataTableExtensions;

namespace AutomaticWip.Modules.Stocks.Models
{
    [DataModel]
    public sealed class Stock
    {
        /// <summary>
        /// Time.Now - Created in days
        /// </summary>
        [ExcelSettings(Column = 'A', Size = 100, Format = ExcelFieldType.Integer)]
        [MapTo(Value = "Age (in days)")]
        public int AGE_DAYS { get; set; } = 0;

        /// <summary>
        /// When this container was created in MES
        /// </summary>
        [ExcelSettings(Column = 'B', Size = 150, Format = ExcelFieldType.DateTime)]
        [MapTo(Value = "CRAETED")]
        public DateTime CREATED { get; set; }

        /// <summary>
        /// The material referenced by this good receipt.
        /// </summary>
        [ExcelSettings(Column = 'C', Size = 100)]
        [MapTo(Value = "MATERIAL")]
        public string MATERIAL { get; set; } = "N/A";

        /// <summary>
        /// The project attached on material.
        /// </summary>
        [ExcelSettings(Column = 'D', Size = 100)]
        [MapTo(Value = "PROJECT")]
        public string PROJECT { get; set; } = "N/A";

        /// <summary>
        /// The description of the project
        /// </summary>
        [ExcelSettings(Column = 'E', Size = 100)]
        [MapTo(Value = "DESCRIPTION")]
        public string DESCRIPTION { get; set; } = "N/A";

        /// <summary>
        /// The division where this stock is used
        /// </summary>
        [ExcelSettings(Column = 'F', Size = 20)]
        [MapTo(Value = "DIVISION")]
        public string DIVISION { get; set; } = "N/A";

        /// <summary>
        /// FE/BE
        /// </summary>
        [ExcelSettings(Column = 'G', Size = 20)]
        [MapTo(Value = "ZONE")]
        public string ZONE { get; set; } = "N/A";

        /// <summary>
        /// Preassy, Assy, etc
        /// </summary>
        [ExcelSettings(Column = 'H', Size = 20)]
        [MapTo(Value = "AREA")]
        public string AREA { get; set; } = "N/A";

        /// <summary>
        /// Identifier of the part
        /// </summary>
        [ExcelSettings(Column = 'I', Size = 100)]
        [MapTo(Value = "PART ID")]
        public string PART_ID { get; set; } = "N/A";

        /// <summary>
        /// Identifier type of the part
        /// </summary>
        [ExcelSettings(Column = 'J', Size = 100)]
        [MapTo(Value = "PART ID TYPE")]
        public string PART_ID_TYPE { get; set; } = "N/A";

        /// <summary>
        /// Quantity of the part
        /// </summary>
        [ExcelSettings(Column = 'K', Size = 50, Format = ExcelFieldType.Decimal)]
        [MapTo(Value = "QUANTITY")]
        public double QUANTITY { get; set; } = 0;

        /// <summary>
        /// When this unit was synchronised with the source server
        /// </summary>
        [MapTo(Value = "SYNC AT")]
        [ExcelSettings(Format = ExcelFieldType.DateTime, Column = 'L', Size = 20)]
        public DateTime SYNCHRONISATION_TIME { get; set; }
    }
}
