using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.NLog;
using Microsoft.Owin.Hosting;
using Panteon.Host.API;
using Panteon.Host.Infrastructure;
using Panteon.Host.Interface;
using Panteon.Sdk;

namespace Panteon.Host
{
    internal class PanteonEngine : IPanteonEngine
    {
        [ImportMany(typeof(ITaskExports), AllowRecomposition = true)]
        internal ITaskExports[] Exports { get; set; }

        private static readonly Lazy<IPanteonEngine> Lazy = new Lazy<IPanteonEngine>(() => new PanteonEngine(), true);
        public static IPanteonEngine Instance => Lazy.Value;

        private CompositionContainer _compositionContainer;
        private string TasksFolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.TasksFolderName);

        private IContainer _systemContainer;
        private ContainerBuilder _syatemContainerBuilder;
        private IDisposable _webApplication;

        private ILogger _logger;
        private IFileSystemWatcher _fileSystemWatcher;

        private ConcurrentDictionary<string, TaskModel> _taskModelDictionary;

        protected PanteonEngine()
        {
            Init();
        }

        private void Init()
        {
            _taskModelDictionary = new ConcurrentDictionary<string, TaskModel>();

            _compositionContainer = new CatalogConfigurator()
               .AddAssembly(Assembly.GetExecutingAssembly())
               .AddNestedDirectory(Constants.TasksFolderName)
               .BuildContainer();

            _compositionContainer.ComposeParts(this);

            InitTasksRegistry();

            _syatemContainerBuilder = new ContainerBuilder();
            _syatemContainerBuilder.RegisterModule<CoreModule>();
            _syatemContainerBuilder.RegisterModule<HostingModule>();
            _systemContainer = _syatemContainerBuilder.Build();

            _fileSystemWatcher = new TasksWatcher { OnChanged = OnChanged };
            _fileSystemWatcher.WathcTaks(TasksFolderPath);

            Task.Run(() => StartApi());
        }

        private void OnChanged(WatchEventArgs e)
        {
            var catalog = (AggregateCatalog)_compositionContainer.Catalog;
            var folders = Directory.GetDirectories(TasksFolderPath, "*", SearchOption.AllDirectories);

            foreach (string folder in folders)
            {
                Func<ComposablePartCatalog, bool> predicate = composablePartCatalog =>
                {
                    var catalog1 = composablePartCatalog as DirectoryCatalog;

                    string path = GetPathName(folder);

                    return catalog1 != null && (catalog1.Path == path || catalog1.Path == folder);
                };

                if (catalog.Catalogs.Count(predicate) > 0)
                    continue;

                var directoryCatalog = new DirectoryCatalog(folder);
                catalog.Catalogs.Add(directoryCatalog);
            }

            InitTasksRegistry();

            Console.WriteLine($"File: {e.FullPath}  {e.ChangeType}");
            _logger.Info($"File: {e.FullPath}  {e.ChangeType}");
        }

        private string GetPathName(string folder)
        {
            return $"{Constants.TasksFolderName}\\{Path.GetFileName(folder)}";
        }

        public IEnumerable<IPanteonTask> GetTasks()
        {
            IEnumerable<IPanteonTask> tasks = _taskModelDictionary.Select(pair => pair.Value.Task);

            return tasks;
        }

        public void Start()
        {
            _logger = _systemContainer.Resolve<ILogger>();

            try
            {
                var panteonTasks = GetTasks();

                foreach (IPanteonTask task in panteonTasks)
                {
                    task.Bootstrap();
                }
            }
            catch (Exception exception)
            {
                _logger.Error("An error occurred while starting Panteon Host.", exception);
            }
        }

        private void InitTasksRegistry()
        {
            Dictionary<string, TaskModel> taskModels =
                Exports.ToDictionary(exports => exports.GetType().Assembly.FullName,
                    exports =>
                    {
                        var taskContainer = CreateTaskContainer(exports);

                        var panteonTask = taskContainer.Resolve<IPanteonTask>();

                        return new TaskModel
                        {
                            Task = panteonTask,
                            Container = taskContainer
                        };
                    });

            if (_taskModelDictionary.Any())
            {
                foreach (KeyValuePair<string, TaskModel> pair in taskModels)
                {
                    if (!_taskModelDictionary.ContainsKey(pair.Key))
                    {
                        TaskModel taskModel = _taskModelDictionary.AddOrUpdate(pair.Key, pair.Value,
                            (s, model) =>
                                pair.Value.Container.GetHashCode() != model.Container.GetHashCode() ? pair.Value : model);

                        taskModel?.Task?.Bootstrap();
                    }
                    else
                    {
                        //TODO,: UPDATE or REMOVE
                        //TaskModel taskModel;

                        //if (_taskModelDictionary.TryRemove(pair.Key, out taskModel))
                        //{
                        //    StopTask(taskModel.Task.Name);
                        //}
                    }
                }
            }
            else
            {
                _taskModelDictionary = new ConcurrentDictionary<string, TaskModel>(taskModels);
            }
        }

        private IContainer CreateTaskContainer(ITaskExports exports)
        {
            exports.Builder.RegisterModule<HostingModule>();

            return exports.Builder.Build();
        }

        public void Stop(bool immediate)
        {
            _webApplication.Dispose();

            if (immediate)
            {
                foreach (IPanteonTask task in GetTasks())
                {
                    task.Stop();
                }
            }
        }

        public bool StartTask(string name)
        {
            var task = GetTasks().SingleOrDefault(t => t.Name == name);

            return task != null && task.Bootstrap();
        }

        public bool UpdateTask(string name, string scheduleExpression)
        {
            throw new NotImplementedException();
        }

        public bool StopTask(string name)
        {
            var task = GetTasks().SingleOrDefault(t => t.Name == name);

            return task != null && task.Stop();
        }

        private void StartApi()
        {
            try
            {
                _webApplication = WebApp.Start<Startup>("http://localhost:5002");
            }
            catch (Exception exception)
            {
                _logger.Error("An error occurred while starting Panteon API", exception);
            }
        }
    }
}