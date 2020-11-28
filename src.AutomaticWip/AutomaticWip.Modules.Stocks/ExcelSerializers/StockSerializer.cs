using AutomaticWip.Modules.Excel;
using AutomaticWip.Modules.Stocks.Models;
using OfficeOpenXml;

namespace AutomaticWip.Modules.Stocks.ExcelSerializers
{
    public sealed class StockSerializer : ExcelSerializer<Stock>
    {
        protected override string Name 
            => "RAW";

        protected override string Security 
            => "RAW";

        public StockSerializer()
            :base()
        {
            TypeMap.Register<Stock>();

            Settings[nameof(Stock.AGE_DAYS)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.AGE_DAYS));
            Settings[nameof(Stock.AREA)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.AREA));
            Settings[nameof(Stock.CREATED)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.CREATED));
            Settings[nameof(Stock.DESCRIPTION)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.DESCRIPTION));
            Settings[nameof(Stock.DIVISION)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.DIVISION));
            Settings[nameof(Stock.MATERIAL)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.MATERIAL));
            Settings[nameof(Stock.PART_ID)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.PART_ID));
            Settings[nameof(Stock.PART_ID_TYPE)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.PART_ID_TYPE));
            Settings[nameof(Stock.PROJECT)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.PROJECT));
            Settings[nameof(Stock.QUANTITY)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.QUANTITY));
            Settings[nameof(Stock.ZONE)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.ZONE));
            Settings[nameof(Stock.SYNCHRONISATION_TIME)] = TypeMap.Resolve<Stock, ExcelSettings>(nameof(Stock.SYNCHRONISATION_TIME));
        }

        protected override void OnFormat(char column, ExcelRange cells, uint head, uint tail)
        {
        }
    }
}
