using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Helper;
using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model.Enum;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using System;

namespace DialogGenerator.CharacterSelection
{
    public class CharacterSelectionModule : IModule
    {
        private IUnityContainer mContainer;

        public CharacterSelectionModule(IUnityContainer container)
        {
            mContainer = container;
        }

        public void Initialize()
        {
            mContainer.RegisterType<IBLEDataProvider, WinBLEWatcherDataProvider>(BLEDataProviderType.WinBLEWatcher.ToString());

            Func<BLEDataProviderType, IBLEDataProvider> _dataProviderFactory = (_providerType) =>
                 mContainer.Resolve<IBLEDataProvider>(_providerType.ToString());
            var _dataProviderfactoryInstance = new BLEDataProviderFactory(_dataProviderFactory);
            mContainer.RegisterInstance<IBLEDataProviderFactory>(_dataProviderfactoryInstance);

            mContainer.RegisterType<ICharacterSelection, BLESelectionService>(SelectionMode.SerialSelectionMode.ToString());
            mContainer.RegisterType<ICharacterSelection, ArenaCharacterSelection>(SelectionMode.ArenaModel.ToString());

            Func<SelectionMode, ICharacterSelection> _selectionFactory = (_selectionType) =>
            mContainer.Resolve<ICharacterSelection>(_selectionType.ToString());

            var _selectionFactoryInstance = new CharacterSelectionFactory(_selectionFactory);
            mContainer.RegisterInstance<ICharacterSelectionFactory>(_selectionFactoryInstance);

            ParseMessageHelper.Logger = mContainer.Resolve<ILogger>();
            ParseMessageHelper.CharacterRepository = mContainer.Resolve<ICharacterRepository>();
        }
    }
}
