using DialogGenerator.Core;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
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
            mContainer.RegisterType<ICharacterDataProvider, CharacterDataProvider>();
            mContainer.RegisterType<IDialogModelDataProvider, DialogModelDataProvider>();
            mContainer.RegisterType<IWizardDataProvider, WizardDataProvider>();

            mContainer.RegisterType<object, CreateView>(typeof(CreateView).FullName);
            mContainer.RegisterType<object, CharacterDetailView>(typeof(CharacterDetailView).FullName);
            mContainer.RegisterType<object, DialogModelsView>(typeof(DialogModelsView).FullName);
            mContainer.RegisterType<object, DialogModelDetailView>(typeof(DialogModelDetailView).FullName);
            mContainer.RegisterType<object, DialogModelsNavigationView>(typeof(DialogModelsNavigationView).FullName);
            mContainer.RegisterType<object, DialogView>(typeof(DialogView).FullName);
            mContainer.RegisterType<object, AssignCharactersToToysView>(typeof(AssignCharactersToToysView).FullName,new TransientLifetimeManager());
            mContainer.RegisterType<object, WizardView>(typeof(WizardView).FullName,new ContainerControlledLifetimeManager());
            mContainer.RegisterType<object, HomeView>(typeof(HomeView).FullName, new ContainerControlledLifetimeManager());
            mContainer.RegisterType<object, CharacterSelectionView>(typeof(CharacterSelectionView).FullName, new ContainerControlledLifetimeManager());
            mContainer.RegisterType<object, ComputerSelectsView>(typeof(ComputerSelectsView).FullName, new ContainerControlledLifetimeManager());

            mContainer.RegisterType<WizardViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<CharacterDetailViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<DialogModelsViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<DialogModelDetailViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<DialogModelsNavigationViewModel>(new ContainerControlledLifetimeManager())
                .Resolve(typeof(DialogModelsNavigationViewModel));

            //mRegionManager.RegisterViewWithRegion(Constants.MenuRegion, typeof(MenuView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(DialogView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(HomeView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(CreateView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(WizardView));            
                                   
            mContainer.RegisterType<CreateCharacterViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<ArenaViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<AssignedRadiosViewModel>(new ContainerControlledLifetimeManager());
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(CreateCharacterView));

        }
    }
}
