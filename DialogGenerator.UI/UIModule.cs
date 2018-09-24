using DialogGenerator.Infrastructure;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Views.Services;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Regions;

namespace DialogGenerator.UI
{
    public class UIModule : IModule
    {
        private IUnityContainer mContainer;
        private IRegionManager mRegionManager;

        public UIModule(IUnityContainer _unityContainer,IRegionManager _regionManager)
        {
            mContainer = _unityContainer;
            mRegionManager = _regionManager;
        }
        public void Initialize()
        {
            mContainer.RegisterType<IMessageDialogService,MessageDialogService>();
            mContainer.RegisterType<ICharacterDataProvider, CharacterDataProvider>();
            mContainer.RegisterType<IDialogModelDataProvider, DialogModelDataProvider>();
            mContainer.RegisterType<IWizardDataProvider, WizardDataProvider>();

            mContainer.RegisterType<object, CharactersNavigationView>(typeof(CharactersNavigationView).FullName);
            mContainer.RegisterType<object, CharacterDetailView>(typeof(CharacterDetailView).FullName);
            mContainer.RegisterType<object, DialogModelDetailView>(typeof(DialogModelDetailView).FullName);
            mContainer.RegisterType<object, DialogModelsNavigationView>(typeof(DialogModelsNavigationView).FullName);
            mContainer.RegisterType<object, DialogView>(typeof(DialogView).FullName);
            mContainer.RegisterType<object, AssignCharactersToDollsView>(typeof(AssignCharactersToDollsView).FullName);
            mContainer.RegisterType<object, WizardView>(typeof(WizardView).FullName,new ContainerControlledLifetimeManager());

            mContainer.RegisterType<WizardViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<CharacterDetailViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<DialogModelDetailViewModel>(new ContainerControlledLifetimeManager());

            mRegionManager.RegisterViewWithRegion(RegionNames.MenuRegion, typeof(MenuView));
            mRegionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(DialogView));
            mRegionManager.RegisterViewWithRegion(RegionNames.NavigationRegion, typeof(CharactersNavigationView));
        }
    }
}
