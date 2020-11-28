using System.Diagnostics;

namespace AutomaticWip.ETL.Business.Alert
{
    internal sealed class EventLogAlert : IAlert
    {
        public void Send(AlertType alertType, string template, params object[] args)
        {
            var message = string.Format(template, args);
            EventLog.WriteEntry(".NET Runtime", message, Convert(alertType), 1000);
        }

        EventLogEntryType Convert(AlertType alertType)
        {
            switch (alertType)
            {
                case AlertType.Info:
                    return EventLogEntryType.Information;
                case AlertType.Warn:
                    return EventLogEntryType.Warning;
                case AlertType.Error:
                    return EventLogEntryType.Error;
            }

            return EventLogEntryType.FailureAudit;
        }
    }
}
