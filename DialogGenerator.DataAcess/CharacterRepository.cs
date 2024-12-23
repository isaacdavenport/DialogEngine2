﻿using System;
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
            try
            {
                string _fileName = string.IsNullOrEmpty(character.FileName)
                    ? character.CharacterName.Replace(" ", string.Empty) + ".json"
                    : character.FileName;

                character.FileName = _fileName;

                var _jsonObjectsTypesList = _findDataForFile(_fileName);
                _jsonObjectsTypesList.Editable = character.Editable;

                Serializer.Serialize(_jsonObjectsTypesList,
                    Path.Combine(ApplicationData.Instance.DataDirectory, _fileName));
                mLogger.Info("serializing JSON output for: " + character.CharacterName);
                mEventAggregator.GetEvent<CharacterSavedEvent>().Publish(character.CharacterPrefix);
            }
            catch (Exception e)
            {
                mLogger.Error("Save character exception - " + e.Message);
            }
            
        }

        private JSONObjectsTypesList _findDataForFile(string _fileName)
        {
            var _jsonObjectsTypesList = new JSONObjectsTypesList
            {
                Wizards = mWizardRepository.GetAll(_fileName) ?? new WizardRepository().GetAll(),
                DialogModels = mDialogModelRepository.GetAll(_fileName) ?? new DialogModelRepository().GetAll(),
                Characters = _getAll(_fileName), 
                Editable = true
            };

            return _jsonObjectsTypesList;
        }

        private void _removeFile(string _fileName)
        {
            string _fullPath = Path.Combine(ApplicationData.Instance.DataDirectory, _fileName);
            if (File.Exists(_fullPath))
            {
                FileSystem.DeleteFile(_fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
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

                //var _testList = _dlgModels?.Where(dm => dm.ModelsCollectionName.Equals("Custom Dialogs")).ToList();

                if (_dlgModels != null && _dlgModels.Count > 0)
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
                                    if (!_matchedDialogInfos.Contains(_dlginfo))
                                    {
                                        _matchedDialogInfos.Add(_dlginfo.Clone());
                                    }
                                    else
                                    {
                                        var _matchedDialogInfo =
                                            _matchedDialogInfos.First(d => d.ModelsCollectionName.Equals(_dlginfo.ModelsCollectionName));

                                        if (!_matchedDialogInfo.ArrayOfDialogModels.Any(d => d.Equals(_dialogModel)))
                                        {
                                            _matchedDialogInfo.ArrayOfDialogModels.Add(_dialogModel.Clone());
                                        }
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
                Editable = true,
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
            if (!_checkForSilence(character))
            {
                var _phrase = new PhraseEntry()
                {
                    DialogStr = "",
                    FileName = "HalfSecSilence_" + DateTime.Now.ToString("yyyy-dd-MM-HH-mm-ss"),
                    PhraseRating = "PG",
                    PhraseWeights = new Dictionary<string, double>(),
                };

                _phrase.PhraseWeights.Add("GiveSilence", 10.0);

                // Copy file
                var _destFileName = ApplicationData.Instance.AudioDirectory + "\\" + character.CharacterPrefix + "_" + _phrase.FileName + ".mp3";
                var _sourceFileName = ApplicationData.Instance.AudioDirectory + "\\XX_HalfSecSilence.mp3";
                File.Copy(_sourceFileName,_destFileName);
                
                character.Phrases.Add(_phrase);

            }

            // add character to list of characters, so we can grab its data to serialize to file
            GetAll().Add(character);            

            await Task.Run(() =>
            {
                _serializeCharacter(character);
            });
        }

        private bool _checkForSilence(Character character)
        {
            if (character.Phrases.Count(p => p.PhraseWeights.ContainsKey("GiveSilence")) > 0)
            {
                return true;
            }

            return false;

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

        #region Private methods        

        #endregion
    }
}
