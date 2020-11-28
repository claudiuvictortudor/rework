using AutomaticWip.Contracts;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace AutomaticWip.ETL.Business.Alert
{
    internal sealed class MailAlert : IAlert
    {
        /// <summary>
        /// Loggger object
        /// </summary>
        readonly ILog log = LogManager.GetLogger(typeof(MailAlert));

        /// <summary>
        /// Cache attachment resolvers
        /// </summary>
        readonly IDictionary<string, Func<Attachment>> AttachmentFactory = new Dictionary<string, Func<Attachment>>();

        /// <summary>
        /// Listeners of this alerter
        /// </summary>
        readonly HashSet<MailAddress> listeners = new HashSet<MailAddress>();

        /// <summary>
        /// Server address
        /// </summary>
        readonly string Smtp;

        /// <summary>
        /// Communication port
        /// </summary>
        readonly int Port;

        /// <summary>
        /// Sender identity
        /// </summary>
        readonly MailAddress Sender;

        /// <summary>
        /// Initialize a new <see cref="MailAlert"/>
        /// </summary>
        public MailAlert(IConfiguration settings)
        {
            try
            {
                Sender = new MailAddress($"AutomaticWip@continental-corportation.com", "Automatic Wip");

                Compile.Against(!settings.Get("Alert", "server", out Smtp), "Cannot get alert server's address!");
                Compile.Against(!settings.Get("Alert", "port", out Port), "Cannot get alert server's port!");
                Compile.Against(!settings.Get("Alert", "listeners", out string[] values) || values?.Length < 1, "There are no listeners for alert!");

                foreach (var item in values)
                    listeners.Add(new MailAddress(item, item.Split('.')[0]));

                if (settings.Get("Alert", "attchments", out string[] attach) && attach.Length > 1)
                {
                    foreach (var item in attach)
                    {
                        switch (item)
                        {
                            case "log4net": // This has to be dynamic because the file's location will change every day
                                AttachmentFactory["log4net"] = () =>
                                {
                                    try
                                    {
                                        var file = ((Hierarchy)LogManager
                                            .GetRepository())
                                            .Root.Appenders.OfType<RollingFileAppender>()
                                            .FirstOrDefault()?.File;

                                        Compile.Against<InvalidOperationException>(string.IsNullOrWhiteSpace(file), "Cannot get log file!");

                                        var content = new StringBuilder();
                                        var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                        using (var reader = new StreamReader(stream))
                                        {
                                            while (!reader.EndOfStream)
                                            {
                                                content.AppendLine(reader.ReadLine());
                                            }
                                        }

                                        return Attachment.CreateAttachmentFromString(content.ToString(), "log4net.log");
                                    }
                                    catch (Exception e)
                                    {
                                        log.Error("Reading log4net file", e);
                                        return Attachment.CreateAttachmentFromString("log4net:null", "log4net.log");
                                    }
                                };
                                break;
                            case "app.config":
                                AttachmentFactory["app.config"] = () => new Attachment(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"{nameof(MailAlert)}.Initialize(..)", e);
            }
        }

        /// <summary>
        /// Sends the notification
        /// </summary>
        public void Send(AlertType alertType, string template, params object[] args)
        {
            try
            {
                if (listeners.Count > 0)
                {
                    using (var client = new SmtpClient(Smtp))
                    using (var mail = new MailMessage())
                    {
                        foreach (var item in listeners)
                            mail.To.Add(item);

                        if (alertType > AlertType.Info)
                        {
                            foreach (var item in AttachmentFactory)
                                mail.Attachments.Add(item.Value.Invoke());
                        }

                        mail.Subject = alertType.ToString();
                        mail.Body = string.Format(template, args);
                        mail.From = Sender;
                        mail.Priority = MailPriority.High;
                        client.Send(mail);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(nameof(Send), e);
            }
        }
    }
}
