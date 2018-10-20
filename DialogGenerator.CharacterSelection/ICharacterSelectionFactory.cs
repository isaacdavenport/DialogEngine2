using DialogGenerator.Model.Enum;

namespace DialogGenerator.CharacterSelection
{
    public interface ICharacterSelectionFactory
    {
        ICharacterSelection Create(SelectionMode _selectionMode);
    }
}
