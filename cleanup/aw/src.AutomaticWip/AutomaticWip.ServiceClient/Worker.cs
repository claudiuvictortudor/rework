using AutomaticWip.Core;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace AutomaticWip.ServiceClient
{
    internal sealed class Worker
    {
        /// <summary>
        /// Trigger obj
        /// </summary>
        Timer _trigger;

        /// <summary>
        /// <see cref="FileManager"/>
        /// </summary>
        FileManager _fileManager;

        /// <summary>
        /// <see cref="WipHandler"/>
        /// </summary>
        WipHandler _wip;

        /// <summary>
        /// Logger object
        /// </summary>
        ILog _log = LogManager.GetLogger(typeof(Worker));

        /// <summary>
        /// Settings object
        /// </summary>
        Settings _settings;

        /// <summary>
        /// Indicates if this service is started
        /// </summary>
        bool _started = false;

        /// <summary>
        /// Keeps metadata to avoid reflection.
        /// </summary>
        DataPool.IDataMapper _mapper = DataPool.GetMapper(false);

        /// <summary>
        /// Initialize a new <see cref="Worker"/>
        /// </summary>
        public Worker()
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
            _settings.Initialize((int)TimeSpan.FromMinutes(10).TotalMilliseconds);
            _fileManager = new FileManager(_settings);
            _wip = new WipHandler(_settings);
        }

        /// <summary>
        /// Execute the logic
        /// </summary>
        /// <param name="state">null</param>
        public void Execute(object state)
        {
            if (!_started)
                return;

            _log.Debug("Worker is executing ...");
            var time = DateTime.Now;
            try
            {
                var table = _wip.GetMaterials(tableName: "data");
                _fileManager.CreateExcel(time, table, out Stream data);
                var user = _settings.Get(Settings.Variables.User) as string;
                var pass = _settings.Get(Settings.Variables.Pass) as string;
                var domain = _settings.Get(Settings.Variables.Domain) as string;
                Logon.Impersonate(domain, user, pass, () =>
                {
                    _fileManager.Export(time, data);
                });
            }
            catch (Exception e)
            {
                _log.Error(nameof(Execute), e);
            }
        }

        /// <summary>
        /// Starts the service
        /// </summary>
        public void Start()
        {
            _log.Debug("Service is starting ..");
            var cycle = int.Parse(_settings.Get(Settings.Variables.CycleTime) as string);
            var localTime = DateTime.Now.AddHours(1);
            var wait = (TimeSpan)(new DateTime(localTime.Year, localTime.Month, localTime.Day, localTime.Hour, 0, 0) - DateTime.Now);
            _trigger = new Timer(Execute, null, wait, TimeSpan.FromMilliseconds(cycle));
            _log.DebugFormat("Initial wait set to : {0}", wait);
            _started = true;
        }

        /// <summary>
        /// On close
        /// </summary>
        public void Stop()
        {
            _trigger?.Dispose();
            _settings?.Dispose();
            _started = false;
            GC.SuppressFinalize(this);
        }
    }
}
