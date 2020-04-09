using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using Microsoft.VisualBasic.FileIO;
using Prism.Events;

namespace DialogGenerator.DataAccess
{
    public class CharacterRepository : ICharacterRepository
    {
        private ILogger mLogger;
        private IWizardRepository mWizardRepository;
        private IDialogModelRepository mDialogModelRepository;
        private IEventAggregator mEventAggregator;

        public CharacterRepository(ILogger logger
            ,IWizardRepository _wizardRepository
            ,IDialogModelRepository _dialogModelRepository
            ,IEventAggregator _EventAggregator)
        {
            mLogger = logger;
            mWizardRepository = _wizardRepository;
            mDialogModelRepository = _dialogModelRepository;
            mEventAggregator = _EventAggregator;
        }

        private ObservableCollection<Character> _getAll(string _fileName)
        {
            var result = GetAll().Where(ch => ch.FileName.Equals(_fileName))
                .Select(ch => ch)
                .OrderBy(ch => ch.JsonArrayIndex);

            return new ObservableCollection<Character>(result);
        }

        private void _serializeCharacter(Character character)
        {
            string _fileName = string.IsNullOrEmpty(character.FileName)
                ? character.CharacterName.Replace(" ", string.Empty) + ".json"
                : character.FileName;

            character.FileName = _fileName;

            var _jsonObjectsTypesList = _findDataForFile(_fileName);
            _jsonObjectsTypesList.Editable = character.Editable;
            _jsonObjectsTypesList.Version = "1.2";

            Serializer.Serialize(_jsonObjectsTypesList, Path.Combine(ApplicationData.Instance.DataDirectory, _fileName));
            mLogger.Info("serializing JSON output for: " + character.CharacterName);
            mEventAggregator.GetEvent<CharacterSavedEvent>().Publish(character.CharacterPrefix);
        }

        private JSONObjectsTypesList _findDataForFile(string _fileName)
        {
            var _jsonObjectsTypesList = new JSONObjectsTypesList
            {
                Wizards = mWizardRepository.GetAll(_fileName),
                DialogModels = mDialogModelRepository.GetAll(_fileName),
                Characters = _getAll(_fileName), 
                Editable = true
            };

            return _jsonObjectsTypesList;
        }

        private void _removeFile(string _fileName)
        {
            var _jsonObjectsTypesList = _findDataForFile(_fileName);

            if (!_isListNullOrEmpty(_jsonObjectsTypesList.Characters)
                || !_isListNullOrEmpty(_jsonObjectsTypesList.Wizards)
                || !_isListNullOrEmpty(_jsonObjectsTypesList.DialogModels))
            {
                // S.Ristic 10/12/2019.
                // For each of the custom dialog models and wizards
                // create a separate json file.
                if (_isListNullOrEmpty(_jsonObjectsTypesList.Characters))
                {
                    foreach(var _dialogModel in _jsonObjectsTypesList.DialogModels)
                    {
                        var _jsonList = new JSONObjectsTypesList();
                        _jsonList.DialogModels.Add(_dialogModel);
                        var _separateFileName = _dialogModel.ModelsCollectionName + ".json";
                        Serializer.Serialize(_jsonList, Path.Combine(ApplicationData.Instance.DataDirectory, _separateFileName));
                    }

                    foreach( var _wizard in _jsonObjectsTypesList.Wizards)
                    {
                        var _jsonList = new JSONObjectsTypesList();
                        _jsonList.Wizards.Add(_wizard);
                        var _separateFileName = _wizard.WizardName + ".json";
                        Serializer.Serialize(_jsonList, Path.Combine(ApplicationData.Instance.DataDirectory, _separateFileName));
                    }

                    // S.Ristic 10/12/2019.
                    // And now delete the original file.
                    string _fullPath = Path.Combine(ApplicationData.Instance.DataDirectory, _fileName);
                    if (File.Exists(_fullPath))
                    {
                        FileSystem.DeleteFile(_fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }

                }

            }
            else
            {
                string _fullPath = Path.Combine(ApplicationData.Instance.DataDirectory, _fileName);
                if (File.Exists(_fullPath))
                {
                    FileSystem.DeleteFile(_fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }
            mLogger.Info("removing file: " + _fileName);

        }

        private void _removeImage(string _imageFileName)
        {
            //remove character's image if image is not default

            if (!string.IsNullOrEmpty(_imageFileName)
               && !_imageFileName.Equals(ApplicationData.Instance.DefaultImage)
               && File.Exists(Path.Combine(ApplicationData.Instance.ImagesDirectory, _imageFileName)))
            {
                // used to be sure that image is not used by application anymore so we can delete it
                GC.Collect();
                GC.WaitForPendingFinalizers();
                bool _isDeleted = false;
                int counter = 0;

                // used because it can happen that image is not released by application instantly
                do
                {
                    try
                    {
                        FileSystem.DeleteFile(Path.Combine(ApplicationData.Instance.ImagesDirectory, _imageFileName)
                            , UIOption.OnlyErrorDialogs
                            , RecycleOption.SendToRecycleBin);

                        _isDeleted = true;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(500);
                        counter++;
                    }
                }
                while (!_isDeleted && counter <= 16); // wait 8 seconds to delete file 

                if (!_isDeleted)
                {
                    throw new Exception("Error during deleting image file.");
                }
            }
        }

        private bool _isListNullOrEmpty(IEnumerable<object> list)
        {
            return list == null || !list.Any();
        }

        private void _removeMP3Files(Character character)
        {
            var _audioDir = new DirectoryInfo(ApplicationData.Instance.AudioDirectory);
            FileInfo[] _mp3Files = _audioDir.GetFiles($"{character.CharacterPrefix}*.mp3");

            foreach (var _fileInfo in _mp3Files)
            {
                FileSystem.DeleteFile(_fileInfo.FullName, UIOption.OnlyErrorDialogs
                    , RecycleOption.SendToRecycleBin);
            }
            mLogger.Info("removing mp3s for: " + character.CharacterName);
        }

        public async Task SaveAsync(Character character)
        {
            await Task.Run(() =>
            {
                _serializeCharacter(character);
            });
            mLogger.Info("saving character: " + character.CharacterName);
        }

        public void Export(Character character,string _directoryPath)
        {
            var _fileName = character.CharacterName.Replace(" ", string.Empty) + ".json";
            var characters = new ObservableCollection<Character>();
            characters.Add(character);

            // Collection phrase weights keys from character phrases.
            List<string> _phraseWeights = new List<string>();
            if(character.Phrases.Count > 0)
            {
                foreach(var _phraseEntry in character.Phrases)
                {
                    foreach(KeyValuePair<string, double> _entry in _phraseEntry.PhraseWeights)
                    {
                        if(!_phraseWeights.Contains(_entry.Key))
                        {
                            _phraseWeights.Add(_entry.Key);
                        }
                    }
                }
            }

            // Find dialog models collections that match the above selected phrase weights.            
            List<ModelDialogInfo> _matchedDialogInfos = new List<ModelDialogInfo>();
            
            if(_phraseWeights.Count > 0)
            {
                ObservableCollection<ModelDialogInfo> _dlgModels = Session.Get(Constants.DIALOG_MODELS) as ObservableCollection<ModelDialogInfo>;
                if(_dlgModels != null && _dlgModels.Count > 0)
                {
                    foreach(var _dlginfo in _dlgModels)
                    {
                        List<ModelDialog> _dialogModels = _dlginfo.ArrayOfDialogModels;
                        foreach(var _dialogModel in _dialogModels)
                        {
                            foreach (var _phraseType in _dialogModel.PhraseTypeSequence)
                            {
                                if(_phraseWeights.Contains(_phraseType))
                                {
                                    if(!_matchedDialogInfos.Contains(_dlginfo))
                                    {
                                        _matchedDialogInfos.Add(_dlginfo);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            var _jsonObjectsTypesList = new JSONObjectsTypesList
            {
                Characters = characters,                
            };

            if(_matchedDialogInfos.Count > 0)
            {
                _jsonObjectsTypesList.DialogModels.AddRange(_matchedDialogInfos);
            }

            Serializer.Serialize(_jsonObjectsTypesList, Path.Combine(_directoryPath, _fileName));

            // export mp3 files
            foreach (PhraseEntry phrase in character.Phrases)
            {
                string _phraseFileName = $"{character.CharacterPrefix}_{phrase.FileName}.mp3";
                string _phraseFileAbsolutePath = Path.Combine(ApplicationData.Instance.AudioDirectory, _phraseFileName);

                if (File.Exists(_phraseFileAbsolutePath))
                {
                    File.Copy(_phraseFileAbsolutePath, Path.Combine(ApplicationData.Instance.TempDirectory, _phraseFileName), true);
                }
            }

            // move character's image file to Temp dir if image is not default
            if (!character.CharacterImage.Equals(ApplicationData.Instance.DefaultImage))
            {
                string _imageFilePath = Path.Combine(ApplicationData.Instance.ImagesDirectory, character.CharacterImage);
                if (File.Exists(_imageFilePath))
                {
                    File.Copy(_imageFilePath,Path.Combine(ApplicationData.Instance.TempDirectory, character.CharacterImage));
                }
            }
        }

        public async Task AddAsync(Character character)
        {
            // add character to list of characters, so we can grab its data to serialize to file
            GetAll().Add(character);

            await Task.Run(() =>
            {
                _serializeCharacter(character);
            });
        }

        public ObservableCollection<Character> GetAll()
        {
            return Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
        }

        public int IndexOf(Character character)
        {
            return Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS).IndexOf(character);
        }

        public Character GetByInitials(string initials)
        {
            Character character = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS)
                .Where(c => c.CharacterPrefix.Equals(initials))
                .FirstOrDefault();

            return character;
        }
        

        public Character GetByAssignedRadio(int _radioNum)
        {
            if (_radioNum < 0)
                return null;

            var character = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS)
                .Where(c => c.RadioNum == _radioNum)
                .FirstOrDefault();

            return character;
        }

        public  Task Remove(Character character,string _imageFileName)
        {
            //TODO change array index for rest of characters after deleting 
            return Task.Run(() =>
            {
                string _fileName = character.FileName;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    //remove character from list of characters
                    GetAll().Remove(character);
                });

                // remove or update json file
                _removeFile(_fileName);

                //serialize data without character or delete json file if character is in own file
                _removeImage(_imageFileName);

                // remove all character related .mp3 files
                _removeMP3Files(character);
            });
        }

        public void RemovePhrase(Character character, PhraseEntry phrase)
        {
            string _fileName = $"{character.CharacterPrefix}_{phrase.FileName}.mp3";
            if (File.Exists(_fileName))
            {
                File.Delete(_fileName);
            }

            character.Phrases.Remove(phrase);
            mLogger.Info("removing phrase: " + phrase.DialogStr);

        }
    }
}
