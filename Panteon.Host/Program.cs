using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Panteon.Host.Infrastructure;
using Panteon.Host.Interface;
using Topshelf;

namespace Panteon.Host
{
    public class Program
    {
        private static AppDomain _domain;

        [STAThread]
        public static void Main(string[] args)
        {
            var cachePath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\"), "ShadowCopyCache");

            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            //This creates a ShadowCopy of the MEF DLL's (and any other DLL's in the ShadowCopyDirectories)
            var setup = new AppDomainSetup
            {
                CachePath = cachePath,
                ShadowCopyFiles = "true",
                ShadowCopyDirectories = Config.JobsFolderName
            };

            _domain = AppDomain.CreateDomain("Host_AppDomain", AppDomain.CurrentDomain.Evidence, setup);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            HostFactory.Run(configurator =>
            {
                configurator.Service<IPanteonEngine>(s =>
                {
                    s.ConstructUsing(settings => PanteonEngine.Instance);
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc =>
                    {
                        tc.Stop(true);
                        AppDomain.Unload(_domain);
                    });
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
