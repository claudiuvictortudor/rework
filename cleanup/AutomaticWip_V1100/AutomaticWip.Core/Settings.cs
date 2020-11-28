using Dapper;
using log4net;
using Microsoft.Win32.SafeHandles;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

namespace AutomaticWip.Core
{
    public static partial class Settings
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        internal sealed class Map : Attribute
        {
            /// <summary>
            /// The real value.
            /// </summary>
            public string Value { get; set; }
        }

        /// <summary>
        /// Mapper object for properties in this class.
        /// </summary>
        readonly static PropertyMapper _mapper;

        /// <summary>
        /// Logger object
        /// </summary>
        readonly static ILog _log;

        /// <summary>
        /// Allocate the enums.
        /// </summary>
        static Settings()
        {
            _mapper = new PropertyMapper();
            _mapper.Allocate<Property>(typeof(Map));
            _log = LogManager.GetLogger(typeof(Settings));
        }

        /// <summary>
        /// Credentials for impersonation.
        /// </summary>
        public sealed class Credentials
        {
            internal string User;
            internal string Domain;
            internal string Password;

            public Credentials(Updater updater)
            {
                User = updater.GetCachedValue(Property.User);
                Domain = updater.GetCachedValue(Property.Domain);
                Password = updater.GetCachedValue(Property.Password);
            }
        }

        /// <summary>
        /// Gets the value of the property.
        /// </summary>
        /// <param name="enum">The value of the enum.</param>
        /// <returns>The value of <see cref="Map"/></returns>
        public static string GetAlias(this Enum @enum)
            => _mapper.Get<string, Map>(@enum, m => m.Value);

        /// <summary>
        /// Calculates the max width of all fields in the table
        /// </summary>
        /// <param name="data">Data collection</param>
        static double GetMaxWidth(this DataTable data)
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
                _log.Warn("Error while calculating max width for the datatable", e);
            }

            return local;
        }

        /// <summary>
        /// Create the report content.
        /// </summary>
        /// <param name="data">Data to populate the report.</param>
        /// <param name="secure">Password.</param>
        /// <returns>Data as byte[]</returns>
        public static byte[] CreateContent(DataTable data, string secure)
        {
            using (var excel = new ExcelPackage())
            {
                var sheet = excel.Workbook.Worksheets.Add("Data");
                PrepareSheet(sheet, data, secure);
                return excel.GetAsByteArray();
            }
        }

        /// <summary>
        /// Create and save the report.
        /// </summary>
        public static void Report(Updater updater)
        {
            try
            {
                Compile.Against<ArgumentNullException>(updater is null, "Updater is null.");
                Compile.Against(updater.Disposed, "Updater is disposed.");

                _log.Info("A new report is beeing generated ..");
                var query = updater.GetCachedValue(Property.QueryMaterials);
                var data = GetMaterials("Materials", query);
                var content = CreateContent(data, updater.GetCachedValue(Property.FileProtection));
                var credentials = new Credentials(updater);
                var fileName = "";
                var timestamp = DateTime.Now;
                Impersonate.RunAs(credentials, () => 
                {
                    fileName = timestamp.AsFileName(updater.GetCachedValue(Property.ExportDir));
                    using (var stream = new MemoryStream(content))
                    {
                        using (var writer = new StreamWriter(fileName))
                        {
                            stream.CopyTo(writer.BaseStream);
                            writer.Flush();
                        }
                    }
                });

                _log.InfoFormat("Report has been created successfully at: '{0}'", fileName);
                SaveReport(content, timestamp);
            }
            catch (Exception e)
            {
                _log.Error(nameof(Settings.Report), e);
            }
        }

        /// <summary>
        /// Save the report in database
        /// </summary>
        /// <param name="data">Data formated as <see cref="byte[]"/>.</param>
        /// <param name="timestamp">When happened.</param>
        public static void SaveReport(byte[] data, DateTime timestamp)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("ext", "xlsx");
                parameters.Add("file", data, DbType.Binary);
                parameters.Add("timestamp", timestamp, DbType.DateTime);

                using (var conn = new SqlConnection(Property.TMAS275A.GetAlias()))
                {
                    conn.Execute(sql: Property.SP_SAVE_REPORT.GetAlias(), param: parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception e)
            {
                _log.Error("Error while saving the report to db", e);
            }
        }

        // Test
        public static byte[] ReadReport()
        {
            try
            {
                var query = "Select OutputValue from [dbo].[Reports] Where ReportKey = 1";
                using (var conn = new SqlConnection(Property.TMAS275A.GetAlias()))
                {
                    return conn.Query<byte[]>(sql: query,  commandType: CommandType.Text).First();
                }
            }
            catch (Exception e)
            {
                _log.Error(nameof(ReadReport), e);
                return null;
            }
        }

        /// <summary>
        /// Protect the worksheet.
        /// </summary>
        static void Protect(ExcelSheetProtection protection, string secured)
        {
            Compile.Against(string.IsNullOrWhiteSpace(secured), "Invalid protection password.");
            protection.AllowSort = true;
            protection.AllowAutoFilter = true;
            protection.AllowSelectLockedCells = true;
            protection.AllowSelectUnlockedCells = true;
            protection.SetPassword(secured);
        }

        /// <summary>
        /// Prepare the worksheet to be exported.
        /// </summary>
        /// <param name="sheet">Current sheet</param>
        /// <param name="data">Data which is about to be loaded.</param>
        static void PrepareSheet(ExcelWorksheet sheet, DataTable data, string secured)
        {
            sheet.TabColor = Color.Green;
            sheet.Cells.LoadFromDataTable(data, true, OfficeOpenXml.Table.TableStyles.Light18);
            sheet.Cells.AutoFitColumns(0, GetMaxWidth(data));
            Protect(sheet.Protection, secured);
        }

        /// <summary>
        /// Gets material data from database.
        /// </summary>
        /// <param name="tableName">Nameof of the table</param>
        /// <param name="query">Command text to run.</param>
        /// <returns><see cref="DataTable"/></returns>
        public static DataTable GetMaterials(string tableName, string query)
        {
            var table = new DataTable();
            if (string.IsNullOrWhiteSpace(table.TableName))
                table.TableName = string.IsNullOrWhiteSpace(tableName) ? "$data" : tableName;

            using (var conn = new OracleConnection(Property.DataWarehouse.GetAlias()))
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

        /// <summary>
        /// Gets the property value from cache.
        /// </summary>
        /// <param name="updater">The updater implementation.</param>
        /// <param name="property">Property field.</param>
        /// <returns>The value from local cache.</returns>
        static string GetCachedValue(this Updater updater, Property property)
            => updater.GetValue(property);

        /// <summary>
        /// Gets the path from time, based on settings format.
        /// </summary>
        /// <param name="time">The time to parse from.</param>
        /// <returns>A full validated path.</returns>
        static string AsFileName(this DateTime time, string baseDirectory)
        {
            Compile.Against(!Directory.Exists(baseDirectory), $"{nameof(baseDirectory)} doesn't exists or its not accessible.");
            var format = Property.PathFormat.GetAlias();
            var construct = string.Format(format,
                    baseDirectory, 
                    time.ToString("yyyy"), 
                    time.ToString("MM"), 
                    time.ToString("MMMM"), 
                    time.ToString("dd"), 
                    time.ToString("dddd"), 
                    time.ToString("HH"), 
                    time.ToString("mm")
                );

            var dir = Path.GetDirectoryName(construct);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return construct;
        }

        /// <summary>
        /// General settings used in this assembly.
        /// </summary>
        public enum Property
        {
            /// <summary>
            /// Username used to login
            /// </summary>
            [Map(Value = "login/user")]
            User,

            /// <summary>
            /// Password for the user
            /// </summary>
            [Map(Value = "login/pass")]
            Password,

            /// <summary>
            /// Domain of the user
            /// </summary>
            [Map(Value = "login/domain")]
            Domain,

            /// <summary>
            /// Query to get material's data
            /// </summary>
            [Map(Value = "query/materials")]
            QueryMaterials,

            /// <summary>
            /// Where to output the data
            /// </summary>
            [Map(Value = "file/output")]
            ExportDir,

            /// <summary>
            /// Seets a password to the worksheet.
            /// </summary>
            [Map(Value = "file/protection")]
            FileProtection,

            /// <summary>
            /// Format for the file
            /// </summary>
            [Map(Value = "{0}\\{1}\\{2}-{3}\\{4}-{5}\\{6}-{7}.xlsx")]
            PathFormat,

            /// <summary>
            /// Datawarehouse connection string
            /// </summary>
            [Map(Value = "Data Source=(DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = tmdb003.cw01.contiwan.com)(PORT = 1521))(ADDRESS = (PROTOCOL = TCP)(HOST = tmdb003.cw01.contiwan.com)(PORT = 1521)))(CONNECT_DATA = (SERVICE_NAME = MESTMDWH.cw01.contiwan.com)));User Id=MDICE_REPORTS;Password=MDICE_REPORTS;")]
            DataWarehouse,

            /// <summary>
            /// Connection string where are properties
            /// </summary>
            [Map(Value = @"Server=tmas275a\I0001;Database=AutomaticWip;User Id=sa;Password=Q!w2e3r4t5;")]
            TMAS275A,

            /// <summary>
            /// The query to run to get properties
            /// </summary>
            [Map(Value = "SELECT VariableName as propName, VariableValue as propValue FROM [dbo].[WorkerVariables] WHERE VariableName IN @list")]
            QueryUpdateProperties,

            /// <summary>
            /// Interval to update properties
            /// </summary>
            [Map(Value = "updater/pulse")]
            PropertyUpdaterPulse,

            /// <summary>
            /// Save the last file to db
            /// </summary>
            [Map(Value = "dbo.SaveReport")]
            SP_SAVE_REPORT
        }

        /// <summary>
        /// Helper class for compile statements, which allow prettier code for compile clauses
        /// </summary>
        public sealed class Compile
        {
            /// <summary>
            /// Will throw a <see cref="InvalidOperationException"/> if the assertion
            /// is true, with the specificied message.
            /// </summary>
            /// <param name="assertion">if set to <c>true</c> [assertion].</param>
            /// <param name="message">The message.</param>
            /// <example>
            /// Sample usage:
            /// <code>
            /// <![CDATA[
            /// Guard.Against(string.IsNullOrEmpty(name), "Name must have a value");
            /// ]]>
            /// </code>
            /// </example>
            public static void Against(bool assertion, string message)
            {
                if (!assertion)
                    return;
                throw new InvalidOperationException(message);
            }

            /// <summary>
            /// Will throw exception of type <typeparamref name="TException"/>
            /// with the specified message if the assertion is true
            /// </summary>
            /// <typeparam name="TException"></typeparam>
            /// <param name="assertion">if set to <c>true</c> [assertion].</param>
            /// <param name="message">The message.</param>
            /// <example>
            /// Sample usage:
            /// <code>
            /// <![CDATA[
            /// Guard.Against<ArgumentException>(string.IsNullOrEmpty(name), "Name must have a value");
            /// ]]>
            /// </code>
            /// </example>
            public static void Against<TException>(bool assertion, string message) where TException : Exception
            {
                if (!assertion)
                    return;
                throw (TException)Activator.CreateInstance(typeof(TException), message);
            }
        }

        /// <summary>
        /// Helper for login cross domains.
        /// </summary>
        public sealed class Impersonate
        {
            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            private extern static bool CloseHandle(IntPtr handle);

            [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
            public static void RunAs(Credentials credentials, Action action)
            {
                SafeTokenHandle loginToken = null;
                try
                {

                    const int LOGON32_PROVIDER_DEFAULT = 0;
                    //This parameter causes LogonUser to create a primary token.
                    const int LOGON32_LOGON_INTERACTIVE = 2;

                    // Call LogonUser to obtain a handle to an access token.
                    bool returnValue = LogonUser(credentials.User, credentials.Domain, credentials.Password,
                        LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                        out loginToken);

                    if (!returnValue)
                    {
                        int result = Marshal.GetLastWin32Error();
                        throw new Win32Exception(result);
                    }

                    using (loginToken)
                    {
                        using (var identity = new WindowsIdentity(loginToken.DangerousGetHandle()))
                        {
                            using (WindowsImpersonationContext impersonatedUser = identity.Impersonate())
                            {
                                action();
                            }
                        }
                    }

                }
                finally
                {
                    loginToken?.Dispose();
                }
            }

            sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
            {
                SafeTokenHandle()
                    : base(true)
                {
                }

                [DllImport("kernel32.dll")]
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
                [SuppressUnmanagedCodeSecurity]
                [return: MarshalAs(UnmanagedType.Bool)]
                static extern bool CloseHandle(IntPtr handle);

                protected override bool ReleaseHandle()
                {
                    return CloseHandle(handle);
                }
            }
        }
    }
}
