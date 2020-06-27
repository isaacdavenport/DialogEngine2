using DialogGenerator.Core;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Regions;
using System;

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
            mContainer.RegisterType<object, DialogView>(typeof(DialogView).FullName);
            mContainer.RegisterType<object, WizardView>(typeof(WizardView).FullName,new ContainerControlledLifetimeManager());

            mContainer.RegisterType<WizardViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<CharacterDetailViewModel>(new ContainerControlledLifetimeManager());

            //mRegionManager.RegisterViewWithRegion(Constants.MenuRegion, typeof(MenuView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(DialogView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(CreateView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(WizardView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(CharacterDialogLinesView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(CustomDialogCreatorView));
            mRegionManager.RegisterViewWithRegion(Constants.ContentRegion, typeof(CreateCharacterView));

            mContainer.RegisterType<CreateCharacterViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<ArenaViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<AssignedRadiosViewModel>(new ContainerControlledLifetimeManager());            
            mContainer.RegisterType<CharacterDialogLinesViewModel>(new ContainerControlledLifetimeManager());
            mContainer.RegisterType<CustomDialogCreatorViewModel>(new ContainerControlledLifetimeManager());
        }
    }
}
