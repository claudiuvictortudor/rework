using log4net.Config;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("AutomaticWip.Worker")]
[assembly: AssemblyDescription("Automatic Wip service")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyCompany("Continental AG")]
[assembly: AssemblyProduct("AutomaticWip.Worker")]
[assembly: AssemblyCopyright("Copyright © 2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("629f83c5-51af-44e1-ad0e-5c8ab0a41c22")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
