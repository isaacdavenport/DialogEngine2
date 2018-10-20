using Microsoft.Practices.Unity;
using Prism.Modularity;

namespace DialogGenerator.Utilities
{
    public class UtilitiesModule:IModule
    {
        private IUnityContainer mContainer;

        public UtilitiesModule(IUnityContainer container)
        {
            mContainer = container;
        }
        public void Initialize()
        {
            mContainer.RegisterType<IMP3Player, MP3Player>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<IMessageDialogService, MessageDialogService>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<IUserLogger, UserLogger>(new ContainerControlledLifetimeManager());
        }
    }
}
