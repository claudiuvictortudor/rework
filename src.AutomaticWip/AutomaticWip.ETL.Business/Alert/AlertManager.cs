using AutomaticWip.Contracts;
using log4net;
using System;
using System.Collections.Generic;

namespace AutomaticWip.ETL.Business.Alert
{
    public sealed class AlertManager
    {
        /// <summary>
        /// Alerter cache
        /// </summary>
        readonly HashSet<IAlert> Alerters = new HashSet<IAlert>();

        /// <summary>
        /// Logger object
        /// </summary>
        readonly ILog log = LogManager.GetLogger(typeof(AlertManager));

        /// <summary>
        /// Initialize a new <see cref="AlertManager"/>
        /// </summary>
        public AlertManager(IConfiguration settings)
        {
            Alerters.Add(new EventLogAlert());
            Alerters.Add(new MailAlert(settings));
        }

        /// <summary>
        /// Sends the notification to all alerters
        /// </summary>
        /// <param name="alertType"><see cref="AlertType"/></param>
        /// <param name="template">Message template</param>
        /// <param name="args">Arguments</param>
        public void Send(AlertType alertType, string template, params object[] args)
        {
            lock (Alerters)
            {
                foreach (var item in Alerters)
                {
                    try
                    {
                        item.Send(alertType, template, args);
                    }
                    catch (Exception e)
                    {
                        log.Error(nameof(Send), e);
                    }
                }
            }
        }
    }
}
