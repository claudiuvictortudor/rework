using AutomaticWip.Core;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace AutomaticWip.ServiceClient
{
    public class Test
    {
        readonly FileManager _fileManager;
        readonly WipHandler _wip;
        readonly ILog _log = LogManager.GetLogger(typeof(Worker));

        readonly Settings _settings;

        public Test()
        {
            var variables = new List<Settings.Variables>
            {
                Settings.Variables.CycleTime,
                Settings.Variables.Domain,
                Settings.Variables.ExportPath,
                Settings.Variables.FileProtection,
                Settings.Variables.Pass,
                Settings.Variables.QueryMaterials,
                Settings.Variables.User
            };

            _settings = new Settings(LogManager.GetLogger(typeof(Settings)), variables.ToArray());
            _settings.Initialize(5000);
            _fileManager = new FileManager(_settings);
            _wip = new WipHandler(_settings);
        }
        public void TestCase()
        {
            try
            {
                _log.DebugFormat("$service/runcycle: {0}", _settings.Get(Settings.Variables.CycleTime));
                _log.DebugFormat("$service/output: {0}", _settings.Get(Settings.Variables.ExportPath));
                _log.DebugFormat("query:mat_data: {0}", _settings.Get(Settings.Variables.QueryMaterials));
                _log.DebugFormat("$service/login/pass: {0}", _settings.Get(Settings.Variables.User));
                _log.DebugFormat("$service/login/user: {0}", _settings.Get(Settings.Variables.Pass));

                var time = DateTime.Now;
                var table = new DataTable();
                AddDefaults(table);
                var data = _wip.GetMaterials(table, tableName: "data");
                _fileManager.CreateExcel(time, data, out Stream stream);
                Logon.Impersonate(
                    _settings.Get(Settings.Variables.Domain) as string,
                    _settings.Get(Settings.Variables.User) as string,
                    _settings.Get(Settings.Variables.Pass) as string,
                    () => _fileManager.Export(time, stream));
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        void AddDefaults(DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn column in table.Columns)
                {

                }
            }
        }
    }
}
