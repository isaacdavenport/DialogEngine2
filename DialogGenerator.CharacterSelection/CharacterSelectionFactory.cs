using DialogGenerator.Model.Enum;
using System;

namespace DialogGenerator.CharacterSelection
{
    public class CharacterSelectionFactory : ICharacterSelectionFactory
    {
        private readonly Func<SelectionMode, ICharacterSelection> mfactoryFactory;

        public CharacterSelectionFactory(Func<SelectionMode, ICharacterSelection> _factoryFactory)
        {
            this.mfactoryFactory = _factoryFactory;
        }

        public ICharacterSelection Create(SelectionMode _selectionMode)
        {
            return mfactoryFactory(_selectionMode);
        }
    }
}
