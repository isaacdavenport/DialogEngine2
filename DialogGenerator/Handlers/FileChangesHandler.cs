using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.Utilities;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DialogGenerator.Handlers
{
    public class FileChangesHandler
    {
        private ILogger mLogger;
        private IMessageDialogService mMessageDialogService;
        private IDialogDataRepository mDialogDataRepository;
        private ICharacterRepository mCharacterRepository;
        private IWizardRepository mWizardRepository;
        private IDialogModelRepository mDialogModelRepository;
        private FileSystemWatcher mFileWatcher;
        private IEventAggregator mEventAggregator;

        public FileChangesHandler(ILogger logger,IMessageDialogService _messageDialogService, IEventAggregator _eventAgregator
            ,IDialogDataRepository _dialogDataRepository
            ,ICharacterRepository _characterRepository
            ,IWizardRepository _wizardRepository
            ,IDialogModelRepository _dialogModelRepository)
        {
            mLogger = logger;
            mMessageDialogService = _messageDialogService;
            mDialogDataRepository = _dialogDataRepository;
            mCharacterRepository = _characterRepository;
            mWizardRepository = _wizardRepository;
            mDialogModelRepository = _dialogModelRepository;
            mEventAggregator = _eventAgregator;
        }

        private async void _file_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                mFileWatcher.Changed -= _file_Changed;

                DateTime _fileWriteLastTime = File.GetLastWriteTime(e.FullPath);
                DateTime _folderWriteLastTime = Directory.GetLastWriteTime(ApplicationData.Instance.EditorTempDirectory);
                if (_fileWriteLastTime <= _folderWriteLastTime.AddSeconds(2))
                {
                    return;
                }

                FocusHelper.RequestFocus();

                MessageDialogResult result = await mMessageDialogService
                    .ShowOKCancelDialogAsync($"{e.Name} changed externally.", "Info", "Reload file", "Undo changes");

                if (result == MessageDialogResult.OK)
                {
                    await _processChangedFile(e);
                }
                else
                {
                    ProcessHandler.Remove(e.Name);
                }
            }
            catch (Exception ex)
            {
                mLogger.Error(ex.Message);
            }
            finally
            {
                mFileWatcher.Changed += _file_Changed;
            }
        }

        private async Task _processChangedFile(FileSystemEventArgs e)
        {
            IList<string> errors;
            var _JSONObjectTypesList = mDialogDataRepository.LoadFromFile(e.FullPath, out errors);

            if (errors.Count > 0)
            {
                MessageDialogResult _dialogResult = await mMessageDialogService
                    .ShowMessagesDialogAsync("Error", $"Changes you made in '{e.Name}' has errors:", errors, "Close message", true, "Undo changes");

                if (_dialogResult == MessageDialogResult.Cancel)
                {
                    ProcessHandler.Remove(e.Name);
                    File.Delete(e.FullPath);
                }

                return;
            }
            else
            {
                if (_JSONObjectTypesList.Characters != null)
                {
                    foreach (var character in _JSONObjectTypesList.Characters)
                    {
                        _validateCharacter(character, errors);                        
                    }

                    _validateAllCharacters(_JSONObjectTypesList.Characters, errors);
                }
            }

            if (errors.Count > 0)
            {
                MessageDialogResult _dialogResult = await mMessageDialogService
                    .ShowMessagesDialogAsync("Error", $"Changes you made in '{e.Name}' has errors:", errors, "Close message", true, "Undo changes");

                if (_dialogResult == MessageDialogResult.Cancel)
                {
                    ProcessHandler.Remove(e.Name);
                    File.Delete(e.FullPath);
                }

                return;
            }
            else
            {
                _addLoadedData(_JSONObjectTypesList);
                Character _selectedCharacter = Session.Get<Character>(Constants.SELECTED_CHARACTER);
                foreach(var _character in _JSONObjectTypesList.Characters)
                {
                    if(_selectedCharacter != null && _selectedCharacter.CharacterPrefix.Equals(_character.CharacterPrefix))
                    {
                        Session.Set(Constants.SELECTED_CHARACTER, _character);
                        mEventAggregator.GetEvent<CharacterSavedEvent>().Publish(_character.CharacterPrefix);
                    }
                    
                }

                File.Copy(e.FullPath, Path.Combine(ApplicationData.Instance.DataDirectory, e.Name), true);
                File.Delete(e.FullPath);
            }
        }

        private void _addLoadedData(JSONObjectsTypesList _JSONObjectTypesList)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var _existingCharacters = mCharacterRepository.GetAll();
                foreach (var character in _JSONObjectTypesList.Characters)
                {
                    if (_existingCharacters.Contains(character))
                    {
                        // S.Ristic - Fix of DLGEN-401 10/07/2019.
                        // Instead of removing and adding of character, we will implement
                        // the removing and inserting of character on the same spot/index 
                        // in the character collection, in order to maintain the forced 
                        // character indices.
                        var _existingCharacter = _existingCharacters.First(c => c.Equals(character));
                        int _idx = _existingCharacters.IndexOf(_existingCharacter);
                        _existingCharacters.Remove(_existingCharacter);
                        _existingCharacters.Insert(_idx, character);

                        // S.Ristic - Fix of DLGEN-404
                        // Check if the character file is opened in JSONEditor
                        // and close it.
                        if(ProcessHandler.Contains(character.FileName))
                        {
                            ProcessHandler.Remove(character.FileName);
                        }
                    }
                    else
                    {
                        _existingCharacters.Add(character);
                    }

                }

                var _existingDialogModels = mDialogModelRepository.GetAll();
                foreach(var _dialogModel in _JSONObjectTypesList.DialogModels)
                {
                    if(_existingDialogModels.Contains(_dialogModel))
                    {
                        var _existingDialogModel = _existingDialogModels.First(d => d.Equals(_dialogModel));
                        int _dlgModelIdx = _existingDialogModels.IndexOf(_existingDialogModel);
                        _existingDialogModels.Remove(_existingDialogModel);
                        _existingDialogModels.Insert(_dlgModelIdx, _dialogModel);
                    } else
                    {
                        _existingDialogModels.Add(_dialogModel);
                    }
                }

                Session.Set(Constants.CHARACTERS, _existingCharacters);
                Session.Set(Constants.DIALOG_MODELS, _existingDialogModels);

                // S.Ristic - Fix of DLGEN-401 10/07/2019.
                // Notify the dialog engine that it should re-initialize.
                mEventAggregator.GetEvent<CharacterUpdatedEvent>().Publish();
                // S.Ristic - Fix of DLGEN-406 10/17/2019.
                mEventAggregator.GetEvent<CharacterStructureChangedEvent>().Publish();                

            });
        }

        private void _validateAllCharacters(IEnumerable<Character> characters,IList<string> errors)
        {
            if (characters == null)
                return;

            

            //search characters to find is there radio number duplicates
            var duplicates =characters.GroupBy(ch => ch.RadioNum)
                                      .Select(ch => ch)
                                      .ToList();

            foreach(var duplicate in duplicates)
            {
                if(duplicate.Count() > 1)
                    errors.Add($"Characters: '{string.Join(", ", duplicate.Select(ch => ch.CharacterName).ToArray())}' assigned to toy {duplicate.Key}." +
                        $" Only one character can be assigned to a toy. Please change the value for 'RadioNum' property.");
            }        
        }

        private void _validateCharacter(Character character,IList<string> errors)
        {
            var _existingCharacter = mCharacterRepository.GetByInitials(character.CharacterPrefix);

            if (_existingCharacter == null)
            {
                errors.Add($"Changed value for 'CharacterPrefix' property of character '{character.CharacterName}'," +
                    $" but changes are not allowed for 'CharacterPrefix' property.");
            }

            if (_existingCharacter != null)
            {                
                if (character.RadioNum != _existingCharacter.RadioNum)
                {
                    var _assignedCharacter = mCharacterRepository.GetByAssignedRadio(character.RadioNum);

                    if (_assignedCharacter != null)
                    {
                        errors.Add($"You tried to assign character {character.CharacterName} to toy {character.RadioNum}," +
                            $" but toy {character.RadioNum} already assigned to character '{_assignedCharacter.CharacterName}'.");
                    }
                }
            }
        }

        public void StartWatching(string _directoryPath,string extension)
        {
            mFileWatcher = new FileSystemWatcher
            {
                Path = _directoryPath,
                Filter = extension,
                NotifyFilter = NotifyFilters.LastWrite
            };
            mFileWatcher.Changed += _file_Changed;
            mFileWatcher.EnableRaisingEvents = true;
        }

        public void StopWatching()
        {
            mFileWatcher.EnableRaisingEvents = false;
        }
    }
}
