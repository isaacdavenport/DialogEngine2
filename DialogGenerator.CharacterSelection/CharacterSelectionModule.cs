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
            mContainer.RegisterType<ICharacterSelection, ArenaCharacterSelection>(SelectionMode.ArenaModel.ToString());

            Func<SelectionMode, ICharacterSelection> _selectionFactory = (_selectionType) =>
            mContainer.Resolve<ICharacterSelection>(_selectionType.ToString());

            var _selectionFactoryInstance = new CharacterSelectionFactory(_selectionFactory);
            mContainer.RegisterInstance<ICharacterSelectionFactory>(_selectionFactoryInstance);

        }
    }
}
