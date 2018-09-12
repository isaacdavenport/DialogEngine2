using Autofac;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.View;
using DialogGenerator.UI.View.Services;
using DialogGenerator.UI.ViewModel;
using DialogGenerator.Utilities;
using Prism.Events;

namespace DialogGenerator.UI.Startup
{
    public class Bootstrapper
    {
        public IContainer Bootstrap()
        {
            var builder = new ContainerBuilder();

            // register modules
            builder.RegisterModule<CoreModule>();
            builder.RegisterModule<UtilitiesModule>();
            builder.RegisterModule<DataAccessModule>();
            builder.RegisterModule<DialogEngineModule>();

            builder.RegisterType<AppInitializer>().AsSelf();
            builder.RegisterType<MessageDialogService>()
                .As<IMessageDialogService>().SingleInstance();
            builder.RegisterType<EventAggregator>()
                .As<IEventAggregator>().SingleInstance();

            builder.RegisterType<CharacterDataProvider>().
                As<ICharacterDataProvider>();
            builder.RegisterType<DialogModelDataProvider>()
                .As<IDialogModelDataProvider>();
            builder.RegisterType<DialogModelDataProvider>()
                .As<IDialogModelDataProvider>();
            builder.RegisterType<MainViewModel>().AsSelf();

            builder.RegisterType<CharactersNavigationViewModel>()
                .As<INavigationViewModel>();
            builder.RegisterType<CharacterDetailViewModel>()
                .As<ICharacterDetailViewModel>();

            builder.RegisterType<MainWindow>().AsSelf();

            return builder.Build();
        }
    }
}

