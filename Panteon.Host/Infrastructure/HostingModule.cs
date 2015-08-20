using Autofac;
using Panteon.Host.Interface;

namespace Panteon.Host.Infrastructure
{
    internal class HostingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //builder.RegisterModule<NLogModule>();

            builder.Register(c => PanteonEngine.Instance).As<IPanteonEngine>().SingleInstance();

            base.Load(builder);
        }
    }
}