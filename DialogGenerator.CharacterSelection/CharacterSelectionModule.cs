using Microsoft.Practices.Unity;
using Prism.Modularity;

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
            var _arenaCharacterSelection = mContainer.Resolve<ArenaCharacterSelection>();
            var _selectionFactoryInstance = new CharacterSelectionFactory(_arenaCharacterSelection);
            mContainer.RegisterInstance<ICharacterSelectionFactory>(_selectionFactoryInstance);
        }
    }
}
