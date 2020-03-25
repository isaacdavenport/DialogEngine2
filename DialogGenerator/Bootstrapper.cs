using System.Windows;
using Prism.Unity;
using Microsoft.Practices.Unity;
using System;
using DialogGenerator.Core;
using Prism.Modularity;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.Views;
using DialogGenerator.UI;
using DialogGenerator.Utilities;
using DialogGenerator.CharacterSelection;
using Prism.Events;
using DialogGenerator.ViewModels;
using DialogGenerator.Handlers;

namespace DialogGenerator
{
    public class Bootstrapper: UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<Shell>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            Application.Current.MainWindow = (Window)Shell;
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
            Container.RegisterType<IEventAggregator, EventAggregator>(new ContainerControlledLifetimeManager());
            Container.RegisterType<AppInitializer>(new ContainerControlledLifetimeManager());
            Container.RegisterType<ShellViewModel>(new ContainerControlledLifetimeManager());
            Container.RegisterType<Shell>(new ContainerControlledLifetimeManager());
            Container.RegisterType<FileChangesHandler>(new ContainerControlledLifetimeManager());
            Container.RegisterType<UpdatesHandler>(new ContainerControlledLifetimeManager());
            Container.RegisterType<Random>(new ContainerControlledLifetimeManager());
            
            Random _random = new Random();
            Container.RegisterInstance<Random>(_random);
        }

        protected override void ConfigureModuleCatalog()
        {
            base.ConfigureModuleCatalog();

            Type _coreModuleType = typeof(CoreModule);
            ModuleCatalog.AddModule(new ModuleInfo()
            {
                ModuleName = _coreModuleType.Name,
                ModuleType = _coreModuleType.AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable                
            });

            Type _utilitiesModuleType = typeof(UtilitiesModule);
            ModuleCatalog.AddModule(new ModuleInfo()
            {
                ModuleName = _utilitiesModuleType.Name,
                ModuleType = _utilitiesModuleType.AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            Type _dialogEngineModuleType = typeof(DialogEngineModule);
            ModuleCatalog.AddModule(new ModuleInfo()
            {
                ModuleName = _dialogEngineModuleType.Name,
                ModuleType = _dialogEngineModuleType.AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            Type _dataAccessModuleType = typeof(DataAccessModule);
            ModuleCatalog.AddModule(new ModuleInfo()
            {
                ModuleName = _dataAccessModuleType.Name,
                ModuleType = _dataAccessModuleType.AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            Type _chSelectionModuleType = typeof(CharacterSelectionModule);
            ModuleCatalog.AddModule(new ModuleInfo()
            {
                ModuleName = _chSelectionModuleType.Name,
                ModuleType = _chSelectionModuleType.AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            Type _uiModuleType = typeof(UIModule);
            ModuleCatalog.AddModule(new ModuleInfo()
            {
                ModuleName = _uiModuleType.Name,
                ModuleType = _uiModuleType.AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
        }

    }
}

