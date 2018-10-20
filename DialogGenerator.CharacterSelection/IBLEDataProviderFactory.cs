using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Model;

namespace DialogGenerator.CharacterSelection
{
    public interface IBLEDataProviderFactory
    {
        IBLEDataProvider Create(BLEDataProviderType _selectionType); 
    }
}
