using DialogGenerator.Model;
using System.Collections.Generic;

namespace DialogGenerator.UI.Data
{
    public interface ICharacterDataProvider
    {
        IEnumerable<Character> GetAll();

        Character GetByName(string name);

        void Add(Character character);
    }
}
