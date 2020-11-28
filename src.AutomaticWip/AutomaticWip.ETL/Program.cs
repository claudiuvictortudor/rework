using AutomaticWip.ETL.Business;
using System;
using Topshelf;

namespace AutomaticWip.ETL
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var exitCode = HostFactory.Run(instance =>
            {
                instance.Service<Business.Worker>(serviceConfig =>
                {
                    serviceConfig.ConstructUsing(_ => new Business.Worker());
                    serviceConfig.WhenStarted(aw => aw.OnStart());
                    serviceConfig.WhenStopped(aw => aw.OnStop());
                });

                instance.RunAsLocalService();
                instance.SetStartTimeout(TimeSpan.FromSeconds(30));
                instance.SetStopTimeout(TimeSpan.FromSeconds(30));
                instance.SetServiceName(Business.Worker.Name);
                instance.SetDisplayName(Business.Worker.DisplayName);
                instance.SetDescription(Business.Worker.Description);
                instance.StartAutomaticallyDelayed();
                instance.EnableServiceRecovery(recovery =>
                {
                    recovery.RestartService(0);
                });
            });

            Environment.ExitCode = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
        }
    }
}
