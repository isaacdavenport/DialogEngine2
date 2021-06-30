using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Model;
using DialogGenerator.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;


namespace DialogGenerator.DataAccess
{
    public class DialogDataRepository : IDialogDataRepository
    {
        private ILogger mLogger;
        private IUserLogger mUserLogger;

        public DialogDataRepository(ILogger logger,IUserLogger _userLogger)
        {
            mLogger = logger;
            mUserLogger = _userLogger;
        }

        private void _processLoadedData(JSONObjectsTypesList _jsonObjectsTypesList, string _fileName)
        {
            var characters = _jsonObjectsTypesList.Characters;
            if (characters != null)
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    characters[i].FileName = _fileName;
                    characters[i].JsonArrayIndex = i;                    
                }
            }

            var wizards = _jsonObjectsTypesList.Wizards;
            if (wizards != null)
            {
                for (int i = 0; i < wizards.Count; i++)
                {
                    wizards[i].FileName = _fileName;
                    wizards[i].JsonArrayIndex = i;
                }
            }

            var _dialogModels = _jsonObjectsTypesList.DialogModels;
            if (_dialogModels != null)
            {
                for (int i = 0; i < _dialogModels.Count; i++)
                {
                    _dialogModels[i].FileName = _fileName;
                    _dialogModels[i].JsonArrayIndex = i;
                }
            }
        }


        public void Save(JSONObjectsTypesList _JSONObjectsTypesList,string path)
        {
            Serializer.Serialize(_JSONObjectsTypesList, path);
        }

        public JSONObjectsTypesList LoadFromFile(string _filePath,out IList<string> errors)
        {
            var _jsonObjectsTypesList = new JSONObjectsTypesList();
            using (var reader = new StreamReader(_filePath)) //creates new streamerader for fs stream. Could also construct with filename...
            {
                string _jsonString = reader.ReadToEnd();
                // validate according json schema
                if (!ValidationHelper.Validate(_jsonString, out errors))
                    return null;

                //json string to Object.
                _jsonObjectsTypesList = Serializer.Deserialize<JSONObjectsTypesList>(_jsonString);
                if (_jsonObjectsTypesList != null)
                {
                    _validateLoadedData(_jsonObjectsTypesList,out errors);
                    if (errors.Count > 0)
                        return null;

                    _processLoadedData(_jsonObjectsTypesList, Path.GetFileName(_filePath));
                }
            }

            return _jsonObjectsTypesList;
        }

        private void _validateLoadedData(JSONObjectsTypesList _jsonObjectsTypesList,out IList<string> errors)
        {
            errors = new List<string>();
            if(_jsonObjectsTypesList.Characters != null)
            {
                foreach(var character in _jsonObjectsTypesList.Characters)
                {
                    var context = new ValidationContext(character);
                    var results = new List<ValidationResult>();

                    Validator.TryValidateObject(character, context, results, true);

                    foreach (var _validationResult in results)
                    {
                        errors.Add(_validationResult.ErrorMessage);
                    }
                }
            }
        }

        public void LogRedundantDialogModelsInDataFolder(string _directoryPath, JSONObjectsTypesList _JSONObjectTypesList)
        {
            // perhaps this should be done in applicationdata.cs or perhaps we should use log4nets folder
            var logPath = ApplicationData.Instance.AppDataDirectory + "\\Log\\";              
            File.Delete(logPath + "DialogModelsWithDuplicates.json");
            Serializer.Serialize(_JSONObjectTypesList.DialogModels, logPath + "DialogModelsWithDuplicates.json");
        }

        public JSONObjectsTypesList LoadFromDirectory(string _directoryPath, out IList<string> _errorsList)
        {
            var _JSONObjectTypesList = new JSONObjectsTypesList();
            _errorsList = new List<string>();
            var _dataDir = new DirectoryInfo(_directoryPath);

            foreach (var _fileInfo in _dataDir.GetFiles("*.json"))
            {
                IList<string> errors;
                var data = LoadFromFile(_fileInfo.FullName, out errors);

                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        _errorsList.Add($"{_fileInfo} - {error}");
                    }
                    continue;
                }

                if (data == null)
                    continue;

                if (data.Characters != null)
                {
                    foreach (var character in data.Characters)
                    {
                        character.Editable = data.Editable;

                        _JSONObjectTypesList.Characters.Add(character);
                    }
                }

                if (data.DialogModels != null)
                {
                    foreach (var _dialogModel in data.DialogModels)
                    {
                        _dialogModel.Editable = data.Editable;

                        _JSONObjectTypesList.DialogModels.Add(_dialogModel);
                    }
                }

                if (data.Wizards != null)
                {
                    foreach (var wizard in data.Wizards)
                    {
                        wizard.Editable = data.Editable;

                        _JSONObjectTypesList.Wizards.Add(wizard);
                    }
                }
            }
            return _JSONObjectTypesList;
        }

        public void LogSessionJsonStatsAndErrors(string _directoryPath, JSONObjectsTypesList _JSONObjectTypesList, List<List<string>> _dialogModelListPreFilter)
        {   //TODO Isaac break this long method into helper methods
            var logPath = ApplicationData.Instance.AppDataDirectory + "\\Log\\";  // perhaps this should be done in applicationdata.cs or perhaps we should use log4nets folder

            File.Delete(logPath + "WizardsList.json");
            File.Delete(logPath + "DialogModelsList.json");
            File.Delete(logPath + "CharactersList.json");
            File.Delete(logPath + "PotentialDialogModelProblems.json");
            File.Delete(logPath + "BadPhraseWeights.json");
            File.Delete(logPath + "MissingMp3Files.json");
            File.Delete(logPath + "TotalTagWeights.json");
            File.Delete(logPath + "MissingTagsByCharacter.json");
            File.Delete(logPath + "DialogModelListPreFilter.json");

            Serializer.Serialize(_JSONObjectTypesList.Wizards, logPath + "WizardsList.json");
            Serializer.Serialize(_JSONObjectTypesList.Characters, logPath + "CharactersList.json");
            Serializer.Serialize(_dialogModelListPreFilter, logPath + "DialogModelListPreFilter.json");

            var _totalTagWeights = new Dictionary<string, double>();
            var _listOfPhraseTypeTags = new List<string>();

            string _filePath = ApplicationData.Instance.DataDirectory + "\\Phrases.cfg";
            try
            {
                using (var _reader = new StreamReader(_filePath))
                {
                    string _jsonString = _reader.ReadToEnd();
                    var _phraseKeysCollection = Serializer.Deserialize<PhraseKeysCollection>(_jsonString);
                    if (_phraseKeysCollection != null && _phraseKeysCollection.Phrases.Count() > 0)
                    {
                        foreach (var _phraseKey in _phraseKeysCollection.Phrases)
                        {
                            if (_phraseKey == null || _phraseKey.Name == "" || _phraseKey.Name == null)
                            {
                                mLogger.Error("Phrases.cfg contains empty entry");
                                continue;
                            }
                            if (_totalTagWeights.ContainsKey(_phraseKey.Name))
                            {
                                mLogger.Error("phrases.cfg contains duplicate entry " + _phraseKey.Name);
                                continue;
                            }
                            _totalTagWeights.Add(_phraseKey.Name, 0.0);
                            _listOfPhraseTypeTags.Add(_phraseKey.Name);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mLogger.Error("**PROBLEM WITH Phrases.cfg "+ e.Message);
            }

            var _problemDialgModels = new List<ModelDialog>();
            var _dialogModelsTakenFromArrays = new List<ModelDialog>();
            foreach (var modelDialogGroup in _JSONObjectTypesList.DialogModels)
            {
                foreach (var modelDialog in modelDialogGroup.ArrayOfDialogModels)
                {
                    _dialogModelsTakenFromArrays.Add(modelDialog);
                    if (modelDialog.PhraseTypeSequence.Count < 2 || modelDialog.Popularity < 0.999 || modelDialog.Popularity > 99)
                    {
                        _problemDialgModels.Add(modelDialog);
                    }
                    // we want to add up how popular each generic phraseType in Phrases.cfg is within the currently active dialogModel set
                    bool _phraseTypeSequenceIsAllGeneric = true;
                    foreach (var _phraseType in modelDialog.PhraseTypeSequence)
                    {
                        if (!_totalTagWeights.ContainsKey(_phraseType))
                        {
                            _phraseTypeSequenceIsAllGeneric = false;
                            continue;  // this PhraseTypeSequence is not all generic so don't score its popularity to generic tags it uses
                        }
                    }
                    if (_phraseTypeSequenceIsAllGeneric)
                    {
                        foreach (var _phraseType in modelDialog.PhraseTypeSequence)
                        {
                             _totalTagWeights[_phraseType] += modelDialog.Popularity;
                        }
                    }
                }
            }

            Serializer.Serialize(_dialogModelsTakenFromArrays, logPath + "DialogModelsList.json");
            Serializer.Serialize(_totalTagWeights, logPath + "TotalTagWeights.json");

            // TODO this generates an exception that gets sent out to the user's debug screen in the error window object missing version
            if (_problemDialgModels.Count > 0)
            {
                Serializer.Serialize(_problemDialgModels, logPath + "PotentialDialogModelProblems.json");
            }

            List<PhraseEntry> _problemPhrases = new List<PhraseEntry>();
            List<PhraseEntry> _noMp3Phrases = new List<PhraseEntry>();
            foreach (var character in _JSONObjectTypesList.Characters)
            {
                foreach (var phrase in character.Phrases)
                {
                    string _phraseFileName = $"{character.CharacterPrefix}_{phrase.FileName}.mp3";
                    string _phraseFileAbsolutePath = Path.Combine(ApplicationData.Instance.AudioDirectory, _phraseFileName);
                    foreach (var weight in phrase.PhraseWeights.Values)
                        if (weight < 0.999 || weight > 99)
                        {
                            _problemPhrases.Add(phrase);
                        }
                    if (!File.Exists(_phraseFileAbsolutePath))
                    {
                        _noMp3Phrases.Add(phrase);
                    }
                }
            }

            var _allCharactersMissingPhraseTypes = new List<List<string>>();
            foreach (var character in _JSONObjectTypesList.Characters)
            {
                var _thisCharactersMissingPhraseTypes = new List<string>();
                _thisCharactersMissingPhraseTypes.Add(character.CharacterPrefix);
                _thisCharactersMissingPhraseTypes.Add(character.CharacterName);
                _thisCharactersMissingPhraseTypes.Add(character.FileName);
                _thisCharactersMissingPhraseTypes.AddRange(_listOfPhraseTypeTags);
                foreach (var phrase in character.Phrases)
                {   // start with a complete list of all generic tags from phrases.cfg and remove the ones 
                    //    called out in character to get a list of whats missing 
                    foreach (var _phraseTypeTag in phrase.PhraseWeights.Keys)
                        if (_thisCharactersMissingPhraseTypes.Contains(_phraseTypeTag))
                        {
                            _thisCharactersMissingPhraseTypes.Remove(_phraseTypeTag);
                        }
                }
                _allCharactersMissingPhraseTypes.Add(_thisCharactersMissingPhraseTypes);
            }

            Serializer.Serialize(_allCharactersMissingPhraseTypes, logPath + "MissingTagsByCharacter.json");


            if (_noMp3Phrases.Count > 0)
            {
                Serializer.Serialize(_noMp3Phrases, logPath + "MissingMp3Files.json");
            }

            if (_problemPhrases.Count > 0)
            {
                Serializer.Serialize(_problemPhrases, logPath + "BadPhraseWeights.json");
            }
            mLogger.Info("Deleted old session logs, writing new WizardList.json, DialogModelsList.json, CharacterList.json and other logs.");
            mLogger.Info("Total dialog model count across all dialog model groups: " + _dialogModelsTakenFromArrays.Count);
        }
    }
}
