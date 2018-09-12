using DialogGenerator.Model;
using System.Collections.ObjectModel;

namespace DialogGenerator.DataAccess
{
    public interface ICharacterRepository
    {
        ObservableCollection<Character> GetAll();

        Character GetByName(string name);
        void Add(Character character);
    }
}
