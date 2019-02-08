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

            return _JSONObjectTypesList;
        }
    }
}
