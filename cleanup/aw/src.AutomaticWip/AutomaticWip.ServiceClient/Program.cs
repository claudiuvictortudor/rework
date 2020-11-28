using System;
using Topshelf;

namespace AutomaticWip.ServiceClient
{
    internal static class Program
    {


        static void Main(string[] args)
        {
            //var test = new Test();
            //Thread.Sleep(5000);
            //test.TestCase();
            //try
            //{
            //    //map = new CustomShareMap(@"\\cw01.contiwan.com\root\loc\tma1\did94002\140_LOG\WIP", @"cw01\uidu620z", "Winter2020");
            //    Logon.Impersonate("cw01", "uidu620z", "Winter2020", () =>
            //    {
            //        var dir = Directory.EnumerateDirectories(@"\\cw01.contiwan.com\root\loc\tma1\did94002\140_LOG\WIP");
            //        foreach (var item in dir)
            //        {
            //            Console.WriteLine(item);
            //        }
            //    });
            //    Console.WriteLine("Success");
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}
            //finally
            //{
            //    Console.ReadLine();
            //}

            //Console.WriteLine("Done");
            //Console.ReadLine();


            var exitCode = HostFactory.Run(instance =>
            {
                instance.Service<Worker>(s =>
                {
                    s.ConstructUsing(_ => new Worker());
                    s.WhenStarted(aw => aw.Start());
                    s.WhenStopped(aw => aw.Stop());
                });

                instance.RunAsLocalService();
                instance.SetStartTimeout(TimeSpan.FromSeconds(30));
                instance.SetStopTimeout(TimeSpan.FromSeconds(30));
                instance.SetServiceName("AutomaticWip");
                instance.SetDisplayName("Automatic WIP");
                instance.SetDescription("Creates reports for each SEMI Material.");
            });

            Environment.ExitCode = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
        }
    }
}
