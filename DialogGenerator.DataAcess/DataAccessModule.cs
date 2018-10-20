using Microsoft.Practices.Unity;
using Prism.Modularity;

namespace DialogGenerator.DataAccess
{
    public class DataAccessModule:IModule
    {
        private IUnityContainer mContainer;
        public DataAccessModule(IUnityContainer container)
        {
            mContainer = container;
        }
        public void Initialize()
        {
            mContainer.RegisterType<IDialogDataRepository, DialogDataRepository>();
            mContainer.RegisterType<ICharacterRepository,CharacterRepository>();
            mContainer.RegisterType<IDialogModelRepository,DialogModelRepository>();
            mContainer.RegisterType<IWizardRepository,WizardRepository>();
        }
    }
}
