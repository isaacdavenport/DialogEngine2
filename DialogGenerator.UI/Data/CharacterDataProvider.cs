﻿using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;

namespace DialogGenerator.UI.Data
{
    public class CharacterDataProvider : ICharacterDataProvider
    {
        private ILogger mLogger;
        private ICharacterRepository mCharacterRepository;

        public CharacterDataProvider(ILogger logger,ICharacterRepository _characterRepository)
        {
            mLogger = logger;
            mCharacterRepository = _characterRepository;
        }

        public Task AddAsync(Character character)
        {
            return mCharacterRepository.AddAsync(character);
        }

        public void Export(Character character,string _directoryPath)
        {
            mCharacterRepository.Export(character,_directoryPath);
            mLogger.Info("export character: " + character.CharacterName);

        }

        public Task SaveAsync(Character character)
        {
            return mCharacterRepository.SaveAsync(character);
        }

        public ObservableCollection<Character> GetAll()
        {
            return mCharacterRepository.GetAll();
        }

        public Character GetByInitials(string initials)
        {
            return mCharacterRepository.GetByInitials(initials);
        }

        public Character GetByAssignedRadio(int _radionNum)
        {
            return mCharacterRepository.GetByAssignedRadio(_radionNum);
        }

        public Task Remove(Character character,string _imageFileName)
        {
            return mCharacterRepository.Remove(character,_imageFileName);
        }

        public void RemovePhrase(Character character, PhraseEntry phrase)
        {
            mCharacterRepository.RemovePhrase(character, phrase);
        }

        public int IndexOf(Character character)
        {
            return mCharacterRepository.IndexOf(character);
        }
    }
}
