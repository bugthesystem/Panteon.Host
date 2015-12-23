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
    internal class PanteonEngine : MarshalByRefObject, IPanteonEngine
    {
        [ImportMany(typeof(ITaskExports), AllowRecomposition = true)]
        internal ITaskExports[] Exports { get; set; }

        private static readonly Lazy<IPanteonEngine> LazyEngineInstance = new Lazy<IPanteonEngine>(EngineFactory(), true);

        private static Func<IPanteonEngine> EngineFactory()
        {
            return () => (PanteonEngine)AppDomain.CurrentDomain.CreateInstanceAndUnwrap(typeof(PanteonEngine).Assembly.FullName, typeof(PanteonEngine).FullName);
        }

        public static IPanteonEngine Instance => LazyEngineInstance.Value;

        private CompositionContainer _compositionContainer;

        private string TasksFolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.JobsFolderName);

        private IContainer _appContainer;
        private ContainerBuilder _appContainerBuilder;
        private IDisposable _webApplication;


        private ILogger _logger;
        private IFileSystemWatcher _fileSystemWatcher;

        private ConcurrentDictionary<string, JobModel> _jobModelRegistry;

        public PanteonEngine()
        {
            Bootstrap();
        }

        private void Bootstrap()
        {
            _jobModelRegistry = new ConcurrentDictionary<string, JobModel>();

            _compositionContainer = new CatalogConfigurator()
               .AddAssembly(Assembly.GetExecutingAssembly())
               .AddNestedDirectory(Config.JobsFolderName)
               .BuildContainer();


            _compositionContainer.ComposeParts(this);

            InitTasksRegistry();

            _appContainerBuilder = new ContainerBuilder();
            _appContainerBuilder.RegisterModule<WorkerModule>();
            _appContainerBuilder.RegisterModule<HostingModule>();
            _appContainer = _appContainerBuilder.Build();

             //TODO: make onchanged to an event
            _fileSystemWatcher = new JobsWatcher { OnChanged = OnChanged };
            _fileSystemWatcher.Watch(TasksFolderPath);

            _logger = _appContainer.Resolve<ILogger>();

            _logger.Info("[START] PanteonEngine");

            Task.Run(() => MountApi());
        }

        private void OnChanged(WatchEventArgs e)
        {
            var catalog = (AggregateCatalog)_compositionContainer.Catalog;
            var folders = Directory.GetDirectories(TasksFolderPath, "*", SearchOption.AllDirectories);

            foreach (string f in folders)
            {
                var folderPath = f;
                Func<ComposablePartCatalog, bool> catalogPredicate = composablePartCatalog =>
                {
                    var directoryCatalog = composablePartCatalog as DirectoryCatalog;

                    string path = GetPathName(folderPath);

                    return directoryCatalog != null && (directoryCatalog.Path == path || directoryCatalog.Path == folderPath);
                };

                if (catalog.Catalogs.Count(catalogPredicate) > 0)
                    continue;

                catalog.Catalogs.Add(new DirectoryCatalog(folderPath));
            }

            InitTasksRegistry();

            Console.WriteLine("File: {0}  {1}", e.FullPath, e.ChangeType);

            _logger.Info($"[ON_CHANGED] File: {e.FullPath}  {e.ChangeType}");
        }

        private string GetPathName(string folder)
        {
            return $"{Config.JobsFolderName}\\{Path.GetFileName(folder)}";
        }

        public IEnumerable<IPanteonWorker> GetTasks()
        {
            IEnumerable<IPanteonWorker> tasks = _jobModelRegistry.Select(pair => pair.Value.Task);

            return tasks;
        }

        public void Start()
        {
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
                _logger.Error("[ERROR] An error occurred while starting Panteon Host.", exception);
            }
        }

        private void InitTasksRegistry()
        {
            Dictionary<string, JobModel> taskModels =
                Exports.ToDictionary(exports => exports.GetType().Assembly.FullName,
                    exports =>
                    {
                        var taskContainer = CreateTaskContainer(exports);

                        var panteonTask = taskContainer.Resolve<IPanteonWorker>();

                        return new JobModel
                        {
                            Task = panteonTask,
                            Container = taskContainer
                        };
                    });

            if (_jobModelRegistry.Any())
            {
                foreach (KeyValuePair<string, JobModel> pair in taskModels)
                {
                    if (!_jobModelRegistry.ContainsKey(pair.Key))
                    {
                        JobModel jobModel = _jobModelRegistry.AddOrUpdate(pair.Key, pair.Value,
                            (s, model) =>
                                /*TODO: change detection*/
                                pair.Value.Container.GetHashCode() != model.Container.GetHashCode() ? pair.Value : model);

                        jobModel?.Task?.Init(autoRun: true);
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
                _jobModelRegistry = new ConcurrentDictionary<string, JobModel>(taskModels);
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

        private void MountApi()
        {
            try
            {
                _webApplication = WebApp.Start<Startup>(Config.ApiStartUrl);
                _logger.Info("[START] Panteon REST API");
            }
            catch (Exception exception)
            {
                _logger.Error("[ERROR] An error occurred while starting Panteon API", exception);
            }
        }
    }
}
