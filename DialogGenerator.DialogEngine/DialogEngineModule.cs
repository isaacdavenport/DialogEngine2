using Microsoft.Practices.Unity;
using Prism.Modularity;

namespace DialogGenerator.DialogEngine
{
    public class DialogEngineModule:IModule
    {
        private IUnityContainer mContainer;

        public DialogEngineModule(IUnityContainer container)
        {
            mContainer = container;
        }

        public void Initialize()
        {
            mContainer.RegisterType<IDialogEngine, DialogEngine>( new ContainerControlledLifetimeManager());
        }
    }
}
