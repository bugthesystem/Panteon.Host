using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Reflection;

namespace Panteon.Host.Infrastructure
{
    public sealed class CatalogConfigurator
    {
        private readonly object _lockObject = new object();
        private readonly AggregateCatalog _currentAggregateCatalog = new AggregateCatalog();
        private CompositionContainer _container;


        public CatalogConfigurator AddCatalogs(params ComposablePartCatalog[] catalogs)
        {
            foreach (var catalog in catalogs)
            {
                _currentAggregateCatalog.Catalogs.Add(catalog);
            }
            return this;
        }
        public CatalogConfigurator AddCatalogs(IEnumerable<ComposablePartCatalog> catalogs)
        {
            if (catalogs != null)
            {
                foreach (var catalog in catalogs)
                {
                    _currentAggregateCatalog.Catalogs.Add(catalog);
                }
            }

            return this;
        }
        public CatalogConfigurator AddAssembly(Assembly assembly)
        {
            return AddCatalogs(new AssemblyCatalog(assembly));
        }
        public CatalogConfigurator AddAssembly(string codeBase)
        {
            return AddCatalogs(new AssemblyCatalog(codeBase));
        }
        public CatalogConfigurator AddDirectory(string path)
        {
            return AddCatalogs(new DirectoryCatalog(path));
        }

        public CatalogConfigurator AddNestedDirectory(string path)
        {
            string[] directories = Directory.GetDirectories(path);

            foreach (var directoryPath in directories)
            {
                AddCatalogs(new DirectoryCatalog(directoryPath));
            }

            return this;
        }


        public CatalogConfigurator AddDirectory(string path, string searchPattern)
        {
            return AddCatalogs(new DirectoryCatalog(path, searchPattern));
        }
        public CatalogConfigurator AddTypes(params Type[] types)
        {
            return AddCatalogs(new TypeCatalog(types));
        }
        public CatalogConfigurator AddTypes(IEnumerable<Type> types)
        {
            return AddCatalogs(new TypeCatalog(types));
        }

        public CompositionContainer BuildContainer()
        {
            return BuildContainer(false);
        }
        public CompositionContainer BuildContainer(bool rebuildContainer)
        {
            lock (_lockObject)
            {
                if (rebuildContainer || (_container == null))
                {
                    _container = new CompositionContainer(_currentAggregateCatalog);
                }
            }

            return _container;
        }
    }
}