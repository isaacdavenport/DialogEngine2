using System.Collections.ObjectModel;
using System.Linq;
using DialogGenerator.Core;
using DialogGenerator.Model;

namespace DialogGenerator.DataAccess
{
    public class CharacterRepository : ICharacterRepository
    {
        public void Add(Character character)
        {
        }

        public ObservableCollection<Character> GetAll()
        {
            return Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
        }

        public Character GetByName(string name)
        {
            Character character = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS)
                .Where(c => c.CharacterName.Equals(name))
                .FirstOrDefault();

            return character;
        }
    }
}
