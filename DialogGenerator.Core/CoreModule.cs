using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Modularity;

namespace DialogGenerator.Core
{
    public class CoreModule:IModule
    {
        private IUnityContainer mContainer;

        public CoreModule(IUnityContainer container)
        {
            mContainer = container;
        }

        public void Initialize()
        {
            mContainer.RegisterType<ILogger,Logger>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<IEventAggregator, EventAggregator>();
        }
    }
}
