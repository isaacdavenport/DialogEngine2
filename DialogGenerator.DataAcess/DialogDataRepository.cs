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
            if (wizards != null && wizards.Count > 0)
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
            File.Delete(logPath + "DialogModelsRawAfterLoading.json");
            Serializer.Serialize(_JSONObjectTypesList.DialogModels, logPath + "DialogModelsRawAfterLoading.json");
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

        private void FindMissingMp3sBadPhraseWeights(string logPath, JSONObjectsTypesList _JSONObjectTypesList)
        {
            File.Delete(logPath + "MissingMp3Files.json");
            File.Delete(logPath + "BadPhraseWeights.json");
            List<PhraseEntry> _problemPhrases = new List<PhraseEntry>();
            List<PhraseEntry> _noMp3Phrases = new List<PhraseEntry>();
            try
            {
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
                Serializer.Serialize(_noMp3Phrases, logPath + "MissingMp3Files.json");
                Serializer.Serialize(_problemPhrases, logPath + "BadPhraseWeights.json");
            }
            catch (Exception e)
            {
                mLogger.Error("**PROBLEM DETERMINING WHICH PHRASES LACK MP3 FILES OR HAVE BAD POPULARITY NUMBERS " + e.Message);
            }
        }

        private void SerializeMissingTagsByCharacter(string logPath, JSONObjectsTypesList _JSONObjectTypesList, List<string> _listOfPhraseTypeTags)
        {
            File.Delete(logPath + "MissingTagsByCharacter.json");
            var _allCharactersMissingPhraseTypes = new List<List<string>>();
            try
            {
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
            }
            catch (Exception e)
            {
                mLogger.Error("**PROBLEM LOOKING FOR MISSING PHRASE TYPE TAGS IN CHARACTERS " + e.Message);
            }
        }

private void CalculateAndSerializeWeightsAndModels(string logPath, JSONObjectsTypesList _JSONObjectTypesList, 
    Dictionary<string, double> totalGenericTagWeights, Dictionary<string, int> totalUseCountOfGenericTagsInAllDialogModels)
        {
            File.Delete(logPath + "DialogModelsListPostDeDupeAndFilter.json");
            File.Delete(logPath + "TotalTagWeights.json");
            File.Delete(logPath + "PotentialDialogModelProblems.json");
            File.Delete(logPath + "TotalTagCounts.json");            

            var _problemDialogModels = new List<ModelDialog>();
            var _dialogModelsTakenFromArrays = new List<ModelDialog>();
            double _genericDialogModelWeightSum = 0.0;
            double _contextualDialogModelWeightSum = 0.0;
            int _genericDialogModelCount = 0;
            int _contextualDialogModelCount = 0;

            try
            {
                foreach (var modelDialogGroup in _JSONObjectTypesList.DialogModels)
                {
                    foreach (var modelDialog in modelDialogGroup.ArrayOfDialogModels)
                    {
                        _dialogModelsTakenFromArrays.Add(modelDialog);
                        if (modelDialog.PhraseTypeSequence.Count < 2 || modelDialog.Popularity < 0.999 || modelDialog.Popularity > 125)
                        {
                            _problemDialogModels.Add(modelDialog);
                        }
                        // we want to add up how popular each generic phraseType in Phrases.cfg is within the currently active dialogModel set
                        bool _phraseTypeSequenceIsAllGeneric = true;
                        foreach (var _phraseType in modelDialog.PhraseTypeSequence)
                        {
                            if (!totalGenericTagWeights.ContainsKey(_phraseType))
                            {
                                _phraseTypeSequenceIsAllGeneric = false;
                                
                            }
                            else
                            {
                                totalUseCountOfGenericTagsInAllDialogModels[_phraseType]++;
                            }
                        }
                        if (_phraseTypeSequenceIsAllGeneric)
                        {
                            _genericDialogModelWeightSum += modelDialog.Popularity;
                            _genericDialogModelCount++;
                            foreach (var _phraseType in modelDialog.PhraseTypeSequence)
                            {
                                totalGenericTagWeights[_phraseType] += modelDialog.Popularity;
                            }
                        }
                        else
                        {
                            _contextualDialogModelCount++;
                            _contextualDialogModelWeightSum += modelDialog.Popularity;
                        }
                    }
                }
                mLogger.Info("Generic Dialog Model Weights Sum: " + _genericDialogModelWeightSum.ToString() + 
                        "   Contextual Dialog Model Weights Sum: " + _contextualDialogModelWeightSum.ToString());
                mLogger.Info("Generic Dialog Model Count: " + _genericDialogModelCount.ToString() +
                        "   Contextual Dialog Model Count: " + _contextualDialogModelCount.ToString());
                Serializer.Serialize(_dialogModelsTakenFromArrays, logPath + "DialogModelsListPostDeDupeAndFilter.json");
                Serializer.Serialize(totalGenericTagWeights, logPath + "TotalTagWeights.json");
                Serializer.Serialize(totalUseCountOfGenericTagsInAllDialogModels, logPath + "TotalTagCounts.json"); 
            }
            catch (Exception e)
            {
                mLogger.Error("**PROBLEM EXTRACTING AND ANALYZING DIALOG MODELS " + e.Message);
            }
            // TODO this generates an exception that gets sent out to the user's debug screen in the error window object missing version
            Serializer.Serialize(_problemDialogModels, logPath + "PotentialDialogModelProblems.json");
            mLogger.Info("Total dialog model count across all dialog model groups: " + _dialogModelsTakenFromArrays.Count);
        }

        private void GenerateWeightsAndPhraseTypeList(Dictionary<string, double> totalTagWeights, Dictionary<string, int> totalUseCountOfGenericTagsInAllDialogModels, List<string> listOfPhraseTypeTags)
        {
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
                            if (totalTagWeights.ContainsKey(_phraseKey.Name))
                            {
                                mLogger.Error("phrases.cfg contains duplicate entry " + _phraseKey.Name);
                                continue;
                            }
                            totalTagWeights.Add(_phraseKey.Name, 0.0);
                            totalUseCountOfGenericTagsInAllDialogModels.Add(_phraseKey.Name, 0);
                            listOfPhraseTypeTags.Add(_phraseKey.Name);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mLogger.Error("**PROBLEM WITH Phrases.cfg " + e.Message);
            }
        }

        public void LogSessionJsonStatsAndErrors(string _directoryPath, JSONObjectsTypesList _JSONObjectTypesList, List<List<string>> _dialogModelListPreFilter)
        {   
            var logPath = ApplicationData.Instance.AppDataDirectory + "\\Log\\";  // perhaps this should be done in applicationdata.cs or perhaps we should use log4nets folder

            File.Delete(logPath + "WizardsList.json");
            File.Delete(logPath + "CharactersList.json");
            File.Delete(logPath + "DialogModelListDeDupedPreFilter.json");

            Serializer.Serialize(_JSONObjectTypesList.Wizards, logPath + "WizardsList.json");
            Serializer.Serialize(_JSONObjectTypesList.Characters, logPath + "CharactersList.json");
            Serializer.Serialize(_dialogModelListPreFilter, logPath + "DialogModelListDeDupedPrePreFilter.json");

            FindMissingMp3sBadPhraseWeights(logPath, _JSONObjectTypesList);

            var totalGenericTagWeights = new Dictionary<string, double>();
            var totalUseCountOfGenericTagsInAllDialogModels = new Dictionary<string, int>();

            var listOfPhraseTypeTags = new List<string>();
            GenerateWeightsAndPhraseTypeList(totalGenericTagWeights, totalUseCountOfGenericTagsInAllDialogModels, listOfPhraseTypeTags);

            CalculateAndSerializeWeightsAndModels(logPath, _JSONObjectTypesList, totalGenericTagWeights, totalUseCountOfGenericTagsInAllDialogModels);
            SerializeMissingTagsByCharacter(logPath, _JSONObjectTypesList, listOfPhraseTypeTags);

            mLogger.Info("Deleted old session logs, wrote new WizardList.json, DialogModelsList.json, CharacterList.json and other json logs.");
        }
    }
}
