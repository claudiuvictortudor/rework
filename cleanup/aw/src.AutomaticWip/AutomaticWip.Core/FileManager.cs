using log4net;
using OfficeOpenXml;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;

namespace AutomaticWip.Core
{
    /// <summary>
    /// Handles all file management
    /// </summary>
    public sealed class FileManager
    {

        /// <summary>
        /// Logger object
        /// </summary>
        readonly ILog _log;

        /// <summary>
        /// Domain settings
        /// </summary>
        readonly Settings _settings;

        /// <summary>
        /// Creates a new <see cref="FileManager"/>
        /// </summary>
        public FileManager(Settings settings)
        {
            _log = LogManager.GetLogger(typeof(FileManager));
            _settings = settings;
        }

        /// <summary>
        /// Resolves a directory from a path.
        /// </summary>
        /// <param name="time">Time from which to create the directory.</param>
        /// <param name="path">The path to the directory.</param>
        /// <returns>True of dir exists.</returns>
        bool GetDir(DateTime time, out string path)
        {
            path = "";
            try
            {
                var baseDir = _settings.Get(Settings.Variables.ExportPath) as string;
                path = $"{baseDir}\\{time:yyyy}\\{time:MM}-{time:MMMM}\\{time:dd}-{time:dddd}";
                var normalize = Path.GetFullPath(path);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _log.DebugFormat("Directory created: {0}", path);
                }
                else
                    _log.DebugFormat("Directory exists at: {0}", path);

                return true;
            }
            catch (Exception e)
            {
                _log.Error($"{nameof(GetDir)}({path})", e);
                return false;
            }
        }

        /// <summary>
        /// Gets the full path to the file.
        /// </summary>
        /// <param name="time">Time from which to create the file.</param>
        /// <param name="stream">The stream to the file.</param>
        /// <returns>True if the file exists.</returns>
        bool CreateFile(DateTime time, out Stream stream)
        {
            stream = null;
            if (!GetDir(time, out string dir))
                return false;
            else
            {
                var file = $"{dir}\\{time:HH}-{time:mm}.xlsx";
                try
                {
                    var local = new StreamWriter(path: file, append: false);
                    stream = local.BaseStream;
                    _log.DebugFormat("Stream created to the file: {0}", file);
                    return true;
                }
                catch (Exception e)
                {
                    _log.Error($"{nameof(CreateFile)}({time})", e);
                    return false;
                }
            }
        }

        /// <summary>
        /// Create the excel stream
        /// </summary>
        public void CreateExcel(DateTime time, DataTable data, out Stream created)
        {
            if (data?.Rows?.Count < 1)
                throw new NotSupportedException("Data has less than 1 row to export.");

            using (var excel = new ExcelPackage())
            {
                var sheet = excel.Workbook.Worksheets.Add("Data");
                PrepareSheet(sheet, data);
                created = new MemoryStream(excel.GetAsByteArray());
            }
        }

        /// <summary>
        /// Exports the file.
        /// </summary>
        /// <param name="input"></param>
        public void Export(DateTime time, Stream input)
        {
            if (CreateFile(time, out Stream created))
            {
                try
                {
                    input.CopyTo(created);
                }
                catch (Exception e)
                {
                    _log.Error(e);
                }
                finally
                {
                    input.Dispose();
                    created.Dispose();
                }
            }
        }

        /// <summary>
        /// Prepare the worksheet to be exported.
        /// </summary>
        /// <param name="sheet">Current sheet</param>
        /// <param name="data">Data which is about to be loaded.</param>
        void PrepareSheet(ExcelWorksheet sheet, DataTable data)
        {
            sheet.TabColor = Color.Green;
            sheet.Cells.LoadFromDataTable(data, true, OfficeOpenXml.Table.TableStyles.Light18);
            //AddDescriptions(sheet.Cells["A1"], "This is the description of the product");
            //AddDescriptions(sheet.Cells["B1"], "This is the product name");
            //AddDescriptions(sheet.Cells["C1"], "This is the Project/Group to which the product belongs");
            //AddDescriptions(sheet.Cells["D1"], "This is the actual quantity in MaMa for selected product");
            //FormatColums(sheet, data);
            sheet.Cells.AutoFitColumns(0, GetMaxWidth(data));
            Protect(sheet.Protection);
        }

        /// <summary>
        /// Resolves any formatting for the sheet
        /// </summary>
        void FormatColums(ExcelWorksheet sheet, DataTable data)
        {
            // Format this column to group each 3 digits and sperates each group by ","
            sheet.Cells["D:D"].Style.Numberformat.Format = "#,##0";

            // Perform validation on each row
            for (var i = 0; i < data.Rows.Count; i++)
            {
                if (data.Rows[i]["QUANTITY"].ToString() == "0")
                {
                    sheet.Cells[$"D{i + /* row[0] = excel collections starts from 1, row[1] = header */ 2}"].Style.Font.Color.SetColor(Color.Red);
                }
            }
        }

        /// <summary>
        /// Calculates the max width of all fields in the table
        /// </summary>
        /// <param name="data">Data collection</param>
        double GetMaxWidth(DataTable data)
        {
            var local = 50;

            try
            {
                local = data.Rows.OfType<DataRow>()
                    .Select(r => r.ItemArray.ToList().Select(o => (o.ToString()).Length).Max())
                    .Max();
            }
            catch (Exception e)
            {
                _log.Warn($"{nameof(GetMaxWidth)}(fallbackValue: {local})", e);
            }

            return local;
        }

        /// <summary>
        /// Security protection
        /// </summary>
        void Protect(ExcelSheetProtection protection)
        {
            var pass = _settings.Get(Settings.Variables.FileProtection) as string;
            protection.AllowSort = true;
            protection.AllowAutoFilter = true;
            protection.AllowSelectLockedCells = true;
            protection.AllowSelectUnlockedCells = true;
            protection.SetPassword(pass);
        }
    }
}
