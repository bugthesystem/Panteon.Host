using System;
using Panteon.Host.Infrastructure;

namespace Panteon.Host.Interface
{
    internal interface IFileSystemWatcher
    {
        void Watch(string path);
        Action<WatchEventArgs> OnChanged { get; set; }
    }
}