using AppVersionChecker;
using AutomaticWip.Contracts;
using AutomaticWip.ETL.Business.Alert;
using AutomaticWip.Modules.Excel;
using AutomaticWip.Modules.Materials;
using AutomaticWip.Modules.Stocks;
using AutomaticWip.Modules.Stocks.Models;
using AutomaticWip.Modules.Subsets;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutomaticWip.ETL.Business
{
    public sealed class Worker
    {
        /// <summary>
        /// Worker's name
        /// </summary>
        public const string Name = "AutomaticWip.ETL";

        /// <summary>
        /// Worker's display name
        /// </summary>
        public const string DisplayName = "Automatic WIP";

        /// <summary>
        /// Worker's description
        /// </summary>
        public const string Description = "Aggregates stocks/subsets/materials from MES OLTP to clone server";

        /// <summary>
        /// Alerter service
        /// </summary>
        readonly AlertManager _alerter;

        /// <summary>
        /// Thread synchronisation
        /// </summary>
        readonly object _lock = new object();

        /// <summary>
        /// App.config settings
        /// </summary>
        readonly IConfiguration _settings = Contracts.Settings.Default;

        /// <summary>
        /// Inner timmer to call the <see cref="OnWorking(DateTime)"/>
        /// </summary>
        Timer _timer;

        /// <summary>
        /// Logger object
        /// </summary>
        readonly ILog _log = LogManager.GetLogger(typeof(Worker));

        /// <summary>
        /// Error template for notifications
        /// </summary>
        string _errorTemplate;

        /// <summary>
        /// Initialize a new <see cref="Worker"/>
        /// </summary>
        public Worker()
        {
            _alerter = new AlertManager(_settings);
        }

        /// <summary>
        /// Worker's delegate
        /// </summary>
        /// <param name="time">Current time</param>
        public void OnWorking()
        {
            lock (_lock)
            {
                var time = DateTime.Now;
                _log.DebugFormat("Running data collection[{0}] ..", time);
                try
                {
                    // Faulty states
                    string execution = time.ToString();
                    uint fails = 0;
                    IEnumerable<Container> containers = null;
                    DataTable subsets = null;
                    var tasks = new Task[2];

                    // Stocks collection
                    tasks[0] = Task.Run(() => 
                    {
                        try
                        {
                            _log.Debug("Looking for containers in OLTP ..");
                            containers = StockManager.Q_T_MAT_CONTAINER(time.AddMonths(-1), time);
                            _log.DebugFormat("Found {0} containers!", containers.Count());
                        }
                        catch (Exception e)
                        {
                            _log.Error("Query containers", e);
                            lock (execution)
                            {
                                fails++;
                            }
                        }
                    });

                    // Subset collection
                    tasks[1] = Task.Run(() =>
                    {
                        try
                        {
                            _log.Debug("Looking for subsets in OLTP ..");
                            subsets = SubsetManager.T_WIP_SUBSET;
                            _log.DebugFormat("Found {0} subsets!", subsets.Rows.Count);
                        }
                        catch (Exception e)
                        {
                            _log.Error("Query subsets", e);
                            lock (execution)
                            {
                                fails++;
                            }
                        }
                    });

                    _log.Debug("Waiting for tasks to complete ..");
                    Task.WaitAll(tasks);

                    if (fails > 0)
                    {
                        const string error = "Faulty tasks detected: {0}. Execution is canceled!";
                        _log.ErrorFormat(error, fails);
                        _alerter.Send(AlertType.Error, error, fails);
                        return;
                    }

                    if (!UpdateDependencies())
                    {
                        _log.Error("Dependency data synchronisation failed, the transaction it will be canceled!");
                        _alerter.Send(AlertType.Error, "ETL transaction from {0} was canceled: faulty data dependency synchronisation found!", time);
                        return;
                    }

                    //Update stocks
                    var cloned = StockManager.UpdateClone(containers, time);
                    _log.InfoFormat("Updated {0} containers on clone server!", cloned);

                    var clonedSubsets = SubsetManager.Update(subsets);
                    _log.InfoFormat("Updated {0} subsets on clone server!", clonedSubsets);


                    // Prerender excel file and update container history
                    var updated = StockManager.T_CLONE_STOCK;
                    _log.DebugFormat("Rendering history file for {0} stocks", updated.Count());
                    var serialized = StockManager.Serialize(updated, StockManager.Aggregate(updated));
                    _log.DebugFormat("Saving file[{0} bytes] on server ..", serialized.Length);
                    StockManager.UpdateHistory(time, serialized);

                    _log.InfoFormat("Run ended sucessfully!");
                }
                catch (Exception e)
                {
                    _log.Error(nameof(OnWorking), e);
                    _alerter.Send(AlertType.Error, _errorTemplate, e.Message, e);
                }
            }
        }

        /// <summary>
        /// Update materisl/jobs/etc on clone server
        /// </summary>
        bool UpdateDependencies()
        {
            try
            {
                _log.Debug("Updating materials on clone server ..");
                var materials = MaterialProvider.Source;
                MaterialProvider.Synchronise(materials);
                _log.InfoFormat("{0} materials synchronised on clone server!", materials.Count());
            }
            catch (Exception e)
            {
                _log.Error("OnMateriaUpdater", e);
                return false;
            }

            try
            {
                _log.Debug("Updating process steps on clone server ..");
                var processSteps = SubsetManager.T_PROCESS_STEP;
                SubsetManager.UpdateProcessStep(processSteps);
                _log.InfoFormat("{0} process steps synchronised on clone server!", processSteps.Rows.Count);
            }
            catch (Exception e)
            {
                _log.Error("OnProcessStepUpdater", e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Starting the worker
        /// </summary>
        public void OnStart()
        {
            _log.Info("Service is starting ..");
            var template = new StringBuilder();
            template.AppendLine(">> {0}");
            template.AppendLine("Stacktrace: {1}");
            _errorTemplate = template.ToString();

            var a = AssemblyChecker;
            foreach (var item in AssemblyChecker)
            {
                if (!Sql.Synchronise(item, true))
                {
                    _log.ErrorFormat("Invalid assembly '{0}[{1}]'", Path.GetFileName(item.Assembly.Location), item.Version);
                    _alerter.Send(AlertType.Error, "Invalid assembly '{0}[{1}]'", Path.GetFileName(item.Assembly.Location), item.Version);
                    throw new NotSupportedException("Invalid assemblies!");
                }

                _log.InfoFormat("Assembly [{0}] validated!", Path.GetFileName(item.Assembly.Location));
            }

            Sql.Start();
            _timer = InitializeTimer();
            _alerter.Send(AlertType.Info, "Service '{0}' has started!", DisplayName);
        }

        /// <summary>
        /// Calculate and initialize the timer.
        /// </summary>
        Timer InitializeTimer()
        {
            var now = DateTime.Now;
            var fixedNext = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
            var wait = fixedNext - now;
            _log.DebugFormat("Waiting time until first invokation: {0}", wait);
            return new Timer(obj => OnWorking(), null, wait, TimeSpan.FromMinutes(60));
        }

        /// <summary>
        /// Assemblies to be checked
        /// </summary>
        ISettings[] AssemblyChecker
        {

            get
            {
                const string sqlServer = @"TMAS275A.cw01.contiwan.com\I0001";
                const string targetHost = "TMAS337A";
                var list = new List<ISettings>();

                // Busines
                list.Add(new AppVersionChecker.Settings(Assembly.GetExecutingAssembly(), sqlServer, 
                    "Integrated subsets synchroniser so it can run in parallel with stock synchroniser", 
                    targetHost, exception => _log.Error("", exception)));

                // Stocks
                list.Add(new AppVersionChecker.Settings(typeof(StockManager).Assembly, sqlServer,
                    "Added new excel serializer",
                    targetHost, exception => _log.Error("", exception)));

                // Subsets
                list.Add(new AppVersionChecker.Settings(typeof(SubsetManager).Assembly, sqlServer,
                    "Used DataTable as common layer between OLTP and clone server",
                    targetHost, exception => _log.Error("", exception)));

                // materials
                list.Add(new AppVersionChecker.Settings(typeof(MaterialProvider).Assembly, sqlServer,
                    "Added RAW_GROUP field",
                    targetHost, exception => _log.Error("", exception)));

                // materials
                list.Add(new AppVersionChecker.Settings(typeof(ExcelFieldType).Assembly, sqlServer,
                    "Added a base serializer class to handle EPPlus serialization",
                    targetHost, exception => _log.Error("", exception)));

                // materials
                list.Add(new AppVersionChecker.Settings(typeof(Compile).Assembly, sqlServer,
                    "New handle to cache runtime attributes",
                    targetHost, exception => _log.Error("", exception)));

                return list.ToArray();
            }
        }

        /// <summary>
        /// Stopping the worker
        /// </summary>
        public void OnStop()
        {
            Sql.End();
            _timer?.Dispose();
            _alerter.Send(AlertType.Info, "Service '{0}' has stopped!", DisplayName);
        }
    }
}
