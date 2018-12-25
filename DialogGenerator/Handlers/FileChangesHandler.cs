using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.Utilities;
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

        public FileChangesHandler(ILogger logger,IMessageDialogService _messageDialogService
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
                        _existingCharacters.Remove(character);
                    }
                    _existingCharacters.Add(character);
                }

                //var _existingDialogModels = mDialogModelRepository.GetAll();
                //foreach (var _dialogModel in _JSONObjectTypesList.DialogModels)
                //{
                //    _existingDialogModels.Add(_dialogModel);
                //}

                //var _existingWizards = mWizardRepository.GetAll();
                //foreach (var wizard in _JSONObjectTypesList.Wizards)
                //{
                //    _existingWizards.Add(wizard);
                //}
            });
        }

        private void _validateAllCharacters(IEnumerable<Character> characters,IList<string> errors)
        {
            if (characters == null)
                return;

            int _forcedCharacters = Session.Get<int>(Constants.FORCED_CH_COUNT);
            if(_forcedCharacters != 2)
            {
                var _forcedCharactersList = characters.Where(ch => ch.State == CharacterState.On).ToList();
                if(_forcedCharacters == 0 && _forcedCharactersList.Count > 2)
                {
                    errors.Add($"Characters: '{string.Join(",", _forcedCharactersList.Select(ch => ch.CharacterName).ToArray())}' are in ON state." +
                        $" Application allows maximum 2 characters in ON state.");
                }

                if(_forcedCharacters == 1 && _forcedCharactersList.Count > 1)
                {
                    errors.Add($"Charactes: '{string.Join(", ", _forcedCharactersList.Select(ch => ch.CharacterName).ToArray())}' are in ON state. " +
                        $"Application has one character in ON state, and maximum is 2.");
                }
            }

            //search characters to find is there radio number duplicates
            var duplicates =characters.GroupBy(ch => ch.RadioNum)
                                      .Select(ch => ch)
                                      .ToList();

            foreach(var duplicate in duplicates)
            {
                if(duplicate.Count() > 1)
                    errors.Add($"Characters: '{string.Join(", ", duplicate.Select(ch => ch.CharacterName).ToArray())}' assigned to doll {duplicate.Key}." +
                        $" Only one character can be assigned to doll. Please change value for 'RadioNum' property.");
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
                if (character.State == CharacterState.On && _existingCharacter.State != character.State)
                {
                    int _forcedCharacters = Session.Get<int>(Constants.FORCED_CH_COUNT);
                    if (_forcedCharacters == 2)
                    {
                        errors.Add($"Application already has 2 forced characters in dialog. " +
                            $"Please change state for character '{character.CharacterName}'.");
                    }
                }

                if (character.RadioNum != _existingCharacter.RadioNum)
                {
                    var _assignedCharacter = mCharacterRepository.GetByAssignedRadio(character.RadioNum);

                    if (_assignedCharacter != null)
                    {
                        errors.Add($"You tried to assign character {character.CharacterName} to doll {character.RadioNum}," +
                            $" but doll {character.RadioNum} already assigned to character '{_assignedCharacter.CharacterName}'.");
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
