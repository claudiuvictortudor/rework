using OfficeOpenXml;

namespace AutomaticWip.Modules.Excel
{
    public interface IExcelColumnFormat
    {
        /// <summary>
        /// Formats a given range
        /// </summary>
        /// <param name="range">The range of cells</param>
        /// <param name="column">Letter of the column</param>
        /// <param name="head">First row</param>
        /// <param name="tail">Last row</param>
        void Format(ExcelRange range, char column, uint head, uint tail);
    }
}
