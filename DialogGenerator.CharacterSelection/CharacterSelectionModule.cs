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
            mContainer.RegisterType<ICharacterSelection, RandomSelectionService>();
        }
    }
}
