using System.IO;

namespace Panteon.Host.Infrastructure
{
    internal class WatchEventArgs
    {
        public string Directory { get; set; }
        public string FullPath { get; set; }
        public WatcherChangeTypes ChangeType { get; set; }
    }
}