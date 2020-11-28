using AutomaticWip.Core;
using System;
using System.IO;
using Topshelf;

namespace AutomaticWip.Worker
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var updater = new ServiceWrapper.UpdaterImpl();
            var data = Settings.GetMaterials("data", updater.GetValue(Settings.Property.QueryMaterials));
            //var content = Settings.CreateContent(data, "$protected");
            //Settings.SaveReport(content, DateTime.Now);
            //var back = Settings.ReadReport();
            //using (var stream = new MemoryStream(back))
            //{
            //    using (var writer = new StreamWriter(@"D:\Testbyte.xlsx"))
            //    {
            //        stream.CopyTo(writer.BaseStream);
            //        writer.Flush();
            //    }
            //}

            //var prop = updater.GetValue(Core.Settings.Property.QueryMaterials);
            //Console.WriteLine(prop);
            //Settings.SaveReport("test", DateTime.Now);
            Console.WriteLine("Done");
            //Console.ReadKey();

            //var exitCode = HostFactory.Run(instance =>
            //{
            //    instance.Service<ServiceWrapper>(serviceConfig =>
            //    {
            //        serviceConfig.ConstructUsing(_ => new ServiceWrapper());
            //        serviceConfig.WhenStarted(aw => aw.Start());
            //        serviceConfig.WhenStopped(aw => aw.Stop());
            //    });

            //    instance.RunAsLocalService();
            //    instance.SetStartTimeout(TimeSpan.FromSeconds(30));
            //    instance.SetStopTimeout(TimeSpan.FromSeconds(30));
            //    instance.SetServiceName(ServiceWrapper.SERVICE_NAME);
            //    instance.SetDisplayName(ServiceWrapper.SERVICE_DYSPLAY_NAME);
            //    instance.SetDescription(ServiceWrapper.SERVICE_DESCRIPTION);
            //    instance.StartAutomaticallyDelayed();
            //    instance.EnableServiceRecovery(recovery =>
            //    {
            //        recovery.RestartService(0);
            //    });
            //});

            //Environment.ExitCode = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
        }
    }
}
