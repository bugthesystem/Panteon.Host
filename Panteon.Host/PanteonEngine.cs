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

        private static readonly Lazy<IPanteonEngine> LazyEngineInstance = new Lazy<IPanteonEngine>(() => new PanteonEngine(), true);

        public static IPanteonEngine Instance
        {
            get { return LazyEngineInstance.Value; }
        }

        private CompositionContainer _compositionContainer;
        private string TasksFolderPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.TasksFolderName); }
        }

        private IContainer _appContainer;
        private ContainerBuilder _appContainerBuilder;
        private IDisposable _webApplication;

        private ILogger _logger;
        private IFileSystemWatcher _fileSystemWatcher;

        private ConcurrentDictionary<string, TaskModel> _taskModelDictionary;

        protected PanteonEngine()
        {
            Bootstrap();
        }

        private void Bootstrap()
        {
            _taskModelDictionary = new ConcurrentDictionary<string, TaskModel>();

            _compositionContainer = new CatalogConfigurator()
               .AddAssembly(Assembly.GetExecutingAssembly())
               .AddNestedDirectory(Constants.TasksFolderName)
               .BuildContainer();

            _compositionContainer.ComposeParts(this);

            InitTasksRegistry();

            _appContainerBuilder = new ContainerBuilder();
            _appContainerBuilder.RegisterModule<WorkerModule>();
            _appContainerBuilder.RegisterModule<HostingModule>();
            _appContainer = _appContainerBuilder.Build();

            _fileSystemWatcher = new TasksWatcher { OnChanged = OnChanged };
            _fileSystemWatcher.Watch(TasksFolderPath);

            Task.Run(() => StartApi());
        }

        private void OnChanged(WatchEventArgs e)
        {
            var catalog = (AggregateCatalog)_compositionContainer.Catalog;
            var folders = Directory.GetDirectories(TasksFolderPath, "*", SearchOption.AllDirectories);

            foreach (string f in folders)
            {
                var folderPath = f;
                Func<ComposablePartCatalog, bool> predicate = composablePartCatalog =>
                {
                    var directoryCatalog = composablePartCatalog as DirectoryCatalog;

                    string path = GetPathName(folderPath);

                    return directoryCatalog != null && (directoryCatalog.Path == path || directoryCatalog.Path == folderPath);
                };

                if (catalog.Catalogs.Count(predicate) > 0)
                    continue;

                catalog.Catalogs.Add(new DirectoryCatalog(folderPath));
            }

            InitTasksRegistry();

            Console.WriteLine("File: {0}  {1}", e.FullPath, e.ChangeType);

            _logger.Info(string.Format("File: {0}  {1}", e.FullPath, e.ChangeType));
        }

        private string GetPathName(string folder)
        {
            return string.Format("{0}\\{1}", Constants.TasksFolderName, Path.GetFileName(folder));
        }

        public IEnumerable<IPanteonWorker> GetTasks()
        {
            IEnumerable<IPanteonWorker> tasks = _taskModelDictionary.Select(pair => pair.Value.Task);

            return tasks;
        }

        public void Start()
        {
            _logger = _appContainer.Resolve<ILogger>();

            try
            {
                var panteonTasks = GetTasks();

                foreach (IPanteonWorker task in panteonTasks)
                {
                    task.Init(autoRun: true);
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

                        var panteonTask = taskContainer.Resolve<IPanteonWorker>();

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
                                /*TODO: change detection*/
                                pair.Value.Container.GetHashCode() != model.Container.GetHashCode() ? pair.Value : model);

                        if (taskModel != null && taskModel.Task != null)
                            taskModel.Task.Init(autoRun: true);
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
                foreach (IPanteonWorker task in GetTasks())
                {
                    task.Stop();
                }
            }
        }

        public bool StartTask(string name, DateTimeOffset lastKnownEvent = default(DateTimeOffset))
        {
            var task = GetTasks().SingleOrDefault(t => t.Name == name);

            return task != null && task.Start(lastKnownEvent);
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
                _webApplication = WebApp.Start<Startup>(Constants.ApiStartUrl);
            }
            catch (Exception exception)
            {
                _logger.Error("An error occurred while starting Panteon API", exception);
            }
        }
    }
}
