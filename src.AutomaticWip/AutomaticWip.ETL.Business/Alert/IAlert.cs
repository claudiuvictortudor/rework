namespace AutomaticWip.ETL.Business.Alert
{
    public interface IAlert
    {
        /// <summary>
        /// Sends an alert
        /// </summary>
        /// <param name="alertType">Alert type</param>
        /// <param name="template">Message template</param>
        /// <param name="args">Arguments</param>
        void Send(AlertType alertType, string template, params object[] args);
    }
}
