using AutomaticWip.Contracts;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static AutomaticWip.Contracts.DataTableExtensions;
using static AutomaticWip.Contracts.TypeMap;

namespace AutomaticWip.Modules.Excel
{
    /// <summary>
    /// Base class for performin serialization with epp
    /// </summary>
    public abstract class ExcelSerializer<T>
        where T:class
    {
        /// <summary>
        /// Type mapper
        /// </summary>
        protected static readonly ITypeMap TypeMap = CreateMap(typeof(ExcelFieldType));

        /// <summary>
        /// Encrypter's name
        /// </summary>
        public readonly string Encrypter = "EPPlus.dll [4.5.3.3]";

        /// <summary>
        /// Models fields settings
        /// </summary>
        protected readonly IDictionary<string, ExcelSettings> Settings = new Dictionary<string, ExcelSettings>();

        /// <summary>
        /// The name of current excel worksheet
        /// </summary>
        protected abstract string Name { get; }

        /// <summary>
        /// If null/empty it will not secure the worksheet
        /// </summary>
        protected abstract string Security { get; }

        /// <summary>
        /// Serialize the collection
        /// </summary>
        /// <param name="models">Input data</param>
        /// <returns></returns>
        public ExcelWorksheet Serialize(ExcelWorkbook workbook, IEnumerable<T> models)
        {
            // Placeholder
            var table = new DataTable();

            // Order is important for later on, when formating takes place
            var columns = Settings.OrderBy(p => p.Value.Column);

            // Placeholder
            var worksheet = workbook.Worksheets.Add(Name);

            foreach (var item in columns)
            {
                var name = TypeMap.Resolve<T, MapTo>(item.Key).Value;
                switch (item.Value.Format)
                {
                    case ExcelFieldType.String:
                        table.Columns.Add(name, typeof(string));
                        break;
                    case ExcelFieldType.Integer:
                        table.Columns.Add(name, typeof(int));
                        break;
                    case ExcelFieldType.DateTime:
                        table.Columns.Add(name, typeof(DateTime));
                        break;
                    case ExcelFieldType.Decimal:
                        table.Columns.Add(name, typeof(double));
                        break;
                    default:
                        break;
                }
            }

            worksheet.Cells.LoadFromDataTable(models.AsTable(table), true, OfficeOpenXml.Table.TableStyles.Light18);
            Format(worksheet, (uint)models.Count());

            if (!string.IsNullOrWhiteSpace(Security))
                SecureWith(worksheet, Security);

            return worksheet;
        }

        /// <summary>
        /// Gets the format from a type
        /// </summary>
        static bool GetFormat(ExcelFieldType type, out string format)
        {
            format = "";
            if (type == ExcelFieldType.String)
                return false;

            format = TypeMap.Resolve<ExcelFieldFormat, string, ExcelFieldType>(type, flag => flag.Format);
            return true;
        }

        /// <summary>
        /// Formats a collection of cells as a specific format.
        /// </summary>
        /// <param name="cells">The range of cells.</param>
        /// <param name="type"><see cref="ExcelFieldType"/></param>
        static void FormatDataType(ExcelRange cells, ExcelFieldType type)
        {
            if (GetFormat(type, out string format))
                cells.Style.Numberformat.Format = format;
        }

        /// <summary>
        /// Format the data
        /// </summary>
        /// <param name="excelWorksheet">Current worksheet</param>
        void Format(ExcelWorksheet excelWorksheet, uint lenght)
        {
            foreach (var item in Settings)
            {
                var range = excelWorksheet.Cells[$"{item.Value.Column}:{item.Value.Column}"];
                FormatDataType(range, item.Value.Format);
                OnFormat(item.Value.Column, range, 2, lenght);
                range.AutoFitColumns(item.Key.Length + 1, item.Value.Size);
            }
        }

        /// <summary>
        /// Full column range of cells
        /// </summary>
        /// <param name="column">Column letter</param>
        /// <param name="cells">Cells range</param>
        /// <param name="head">First cell</param>
        /// <param name="tail">Last cell</param>
        protected abstract void OnFormat(char column, ExcelRange cells, uint head, uint tail);

        /// <summary>
        /// Protects a worksheet with a password.
        /// </summary>
        /// <param name="worksheet">Current worksheet.</param>
        /// <param name="secured">The password to protect</param>
        void SecureWith(ExcelWorksheet worksheet, string secured)
        {
            var protection = worksheet.Protection;
            protection.AllowSort = true;
            protection.AllowAutoFilter = true;
            protection.AllowSelectLockedCells = true;
            protection.AllowSelectUnlockedCells = true;
            protection.SetPassword(secured);
        }
    }
}
