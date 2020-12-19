using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Model;
using DialogGenerator.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

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

            //  var logPath =   "${USERPROFILE}\Documents\DialogGenerator\Log\"; // can't use what log4net uses with {USERPROFILE} to get to log folder path
            var logPath = ApplicationData.Instance.AppDataDirectory + "\\Log\\";  // perhaps this should be done in applicationdata.cs or perhaps we should use log4nets folder
                        
            File.Delete(logPath + "WizardsList.json");
            File.Delete(logPath + "DialogModelsList.json");
            File.Delete(logPath + "CharactersList.json");
            File.Delete(logPath + "PotentialDialogModelProblems.json");
            File.Delete(logPath + "BadPhraseWeights.json");
            File.Delete(logPath + "MissingMp3Phrases.json");

            Serializer.Serialize(_JSONObjectTypesList.Wizards, logPath + "WizardsList.json");
            Serializer.Serialize(_JSONObjectTypesList.DialogModels, logPath + "DialogModelsList.json");
            Serializer.Serialize(_JSONObjectTypesList.Characters, logPath + "CharactersList.json");

            List<ModelDialog> _problemDialgModels = new List<ModelDialog>();
            foreach (var modelDialogGroup in _JSONObjectTypesList.DialogModels)
            {
                foreach (var modelDialog in modelDialogGroup.ArrayOfDialogModels)
                {
                    if (modelDialog.PhraseTypeSequence.Count < 2 || modelDialog.Popularity < 0.999 || modelDialog.Popularity > 99)
                    {
                        _problemDialgModels.Add(modelDialog);
                    }
                }
            }

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

            if (_noMp3Phrases.Count > 0)
            {
                Serializer.Serialize(_noMp3Phrases, logPath + "MissingMp3Phrases.json");
            }

            if (_problemPhrases.Count > 0)
            {
                Serializer.Serialize(_problemPhrases, logPath + "BadPhraseWeights.json");
            }

            return _JSONObjectTypesList;
        }
    }
}
