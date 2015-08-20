using Autofac;
using Panteon.Sdk;

namespace Panteon.Host.Infrastructure
{
    public class TaskModel
    {
        public IPanteonTask Task { get; set; }
        public IContainer Container { get; set; }
    }
}