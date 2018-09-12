using System.Collections.Generic;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;

namespace DialogGenerator.UI.Data
{
    class CharacterDataProvider : ICharacterDataProvider
    {
        private ILogger mLogger;
        private ICharacterRepository mCharacterRepository;

        public CharacterDataProvider(ILogger logger,ICharacterRepository _characterRepository)
        {
            mLogger = logger;
            mCharacterRepository = _characterRepository;
        }

        public void Add(Character character)
        {
            mCharacterRepository.Add(character);
        }

        public IEnumerable<Character> GetAll()
        {
            return mCharacterRepository.GetAll();
        }

        public Character GetByName(string name)
        {
            return mCharacterRepository.GetByName(name);
        }
    }
}
