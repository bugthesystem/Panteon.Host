using Autofac;
using Panteon.Sdk;

namespace Panteon.Host.Infrastructure
{
    public class JobModel
    {
        public IPanteonWorker Task { get; set; }
        public IContainer Container { get; set; }
    }
}