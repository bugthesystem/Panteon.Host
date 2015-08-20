using System;
using Panteon.Host.Infrastructure;

namespace Panteon.Host.Interface
{
    internal interface IFileSystemWatcher
    {
        void WathcTaks(string path);
        Action<WatchEventArgs> OnChanged { get; set; }
    }
}