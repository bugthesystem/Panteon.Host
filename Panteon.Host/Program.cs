using System;
using System.Linq;
using System.Reflection;
using Panteon.Host.Interface;
using Topshelf;

namespace Panteon.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            HostFactory.Run(configurator =>
            {
                configurator.Service<IPanteonEngine>(s =>
                {
                    s.ConstructUsing(settings => PanteonEngine.Instance);
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop(true));
                });

                configurator.BeforeInstall(settings => { });
                configurator.AfterInstall(settings => { });
                configurator.UseNLog();

                configurator.EnableServiceRecovery(recovery =>
                {
                    recovery.RestartService(1);
                    recovery.OnCrashOnly();
                });

                configurator.RunAsNetworkService();
                configurator.StartAutomatically();
                configurator.EnableShutdown();

                configurator.SetDescription("Panteon Scheduler Host");
                configurator.SetDisplayName("Panteon Scheduler Host ");
                configurator.SetServiceName("Panteon.Host");
            });
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            return loadedAssemblies.FirstOrDefault(asm => asm.FullName.Contains(args.Name));
        }
    }
}
