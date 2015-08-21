using System;
using System.IO;
using System.Security.Permissions;
using Panteon.Host.Interface;

namespace Panteon.Host.Infrastructure
{
    internal sealed class TasksWatcher : IFileSystemWatcher
    {
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Watch(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = path,
                    NotifyFilter = NotifyFilters.DirectoryName,
                    Filter = "*.*"
                };

                watcher.Changed += HandleChange;
                watcher.Created += HandleChange;
                watcher.Deleted += HandleChange;
                watcher.Renamed += OnRenamed;

                watcher.EnableRaisingEvents = true;
            }
        }

        public Action<WatchEventArgs> OnChanged { get; set; }
 
        private void HandleChange(object source, FileSystemEventArgs e)
        {
            OnChanged?.Invoke(new WatchEventArgs
            {
                Directory = Path.GetDirectoryName(e.FullPath),
                FullPath = e.FullPath,
                ChangeType = e.ChangeType,
            });

            Console.WriteLine($"File: {e.FullPath}  {e.ChangeType}");
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
        }
    }
}