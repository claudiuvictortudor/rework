using AutomaticWip.Modules.Excel;
using static AutomaticWip.Contracts.DataTableExtensions;

namespace AutomaticWip.Modules.Stocks.Models
{
    [DataModel]
    public sealed class StockAggregation
    {
        /// <summary>
        /// The Part number
        /// </summary>
        [ExcelSettings(Column = 'A', Size = 100)]
        [MapTo(Value = "MATERIAL")]
        public string Material { get; set; }

        /// <summary>
        /// The description of this material
        /// </summary>
        [ExcelSettings(Column = 'B', Size = 100)]
        [MapTo(Value = "DESCRIPTION")]
        public string Description { get; set; }

        /// <summary>
        /// The name of the group of <see cref="Material"/>
        /// </summary>
        [ExcelSettings(Column = 'C', Size = 100)]
        [MapTo(Value = "PROJECT")]
        public string Project { get; set; }

        /// <summary>
        /// The division of this Material
        /// </summary>
        [ExcelSettings(Column = 'D', Size = 20)]
        [MapTo(Value = "DIVISION")]
        public string Division { get; set; }

        /// <summary>
        /// The name of the zone of this material
        /// </summary>
        [ExcelSettings(Column = 'E', Size = 20)]
        [MapTo(Value = "ZONE")]
        public string Zone { get; set; }

        /// <summary>
        /// The area of this material
        /// </summary>
        [ExcelSettings(Column = 'F', Size = 50)]
        [MapTo(Value = "AREA")]
        public string Area { get; set; }

        /// <summary>
        /// Quantity of units in the last 24 hours
        /// </summary>
        [MapTo(Value = "< 2 DAYS")]
        [ExcelSettings(Column = 'G', Size = 50, Format = ExcelFieldType.Integer)]
        public int SmallerOrEqualOneDay { get; set; }

        /// <summary>
        /// Quantity between 2 and 5 days
        /// </summary>
        [MapTo(Value = ">= 2 DAYS & <= 5 DAYS")]
        [ExcelSettings(Column = 'H', Size = 50, Format = ExcelFieldType.Integer)]
        public int BetweenTwoAndFiveDays { get; set; }

        /// <summary>
        /// Quantity more than 5 days
        /// </summary>
        [MapTo(Value = "> 5 DAYS")]
        [ExcelSettings(Column = 'I', Size = 50, Format = ExcelFieldType.Integer)]
        public int MoreThanFiveDays { get; set; }
    }
}
