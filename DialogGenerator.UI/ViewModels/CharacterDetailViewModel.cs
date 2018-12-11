using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Helpers;
using DialogGenerator.UI.Wrapper;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class CharacterDetailViewModel : BindableBase, IDetailViewModel
    {
        #region - fields-
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterDataProvider mCharacterDataProvider;
        private IMessageDialogService mMessageDialogService;
        private CharacterWrapper mCharacter;
        private bool mIsDialogStarted;
        private bool mIsEditing;
        #endregion

        #region -constructor-
        public CharacterDetailViewModel(ILogger logger, IEventAggregator _eventAggregator
            , ICharacterDataProvider _characterDataProvider
            , IMessageDialogService  _messageDialogService)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;
            mMessageDialogService = _messageDialogService;

            mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>().Subscribe(_onOpenCharacterDetailView);
            mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Subscribe(_onCharacterSelectionActionChanged);

            _bindCommands();
        }

        #endregion

        #region - commands -

        public ICommand SaveCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand EditWithJSONEditorCommand { get; set; }
        public ICommand ExportCharacterCommand { get; set; }
        public ICommand ChooseImageCommand { get; set; }

        #endregion

        #region - private functions -
        private void _bindCommands()
        {
            SaveCommand = new DelegateCommand(_saveCommand_Execute, _saveCommand_CanExecute);
            DeleteCommand = new DelegateCommand(_deleteCommand_Execute, _deleteCommand_CanExecute);
            EditWithJSONEditorCommand = new DelegateCommand(_editWithJSONEditorCommand_Execute, _editWithJSONEditorCommand_CanExecute);
            ExportCharacterCommand = new DelegateCommand(_exportCharacterCommand_Execute, _exportCharacterCommand_CanExecute);
            ChooseImageCommand = new DelegateCommand(_chooseImage_Execute,_chooseImage_CanExecute);
        }

        private void _onCharacterSelectionActionChanged(bool _isDialogStarted)
        {
            IsDialogStarted = _isDialogStarted;
        }

        private bool _chooseImage_CanExecute()
        {
            if (string.IsNullOrEmpty(Character.CharacterPrefix) || Character.HasErrors)
            { 
               mMessageDialogService
                    .ShowMessage("Information", "You are not able to set image before all required fields are filled.");

                return false;
            }
            else
            {
                return true;
            }
        }

        private async void _chooseImage_Execute()
        {
            try
            {
                System.Windows.Forms.OpenFileDialog _openFileDialog = new System.Windows.Forms.OpenFileDialog();
                _openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";

                if (_openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string _chCurrentImageFilePath = Character.CharacterImage;
                string _filePath = _openFileDialog.FileName;
                string _newFileName = $"{Character.CharacterPrefix}_{Path.GetFileName(_filePath)}";
                Character.CharacterImage = ApplicationData.Instance.DefaultImage;

                await Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "_chooseImage_Execute";

                    File.Copy(_filePath, Path.Combine(ApplicationData.Instance.ImagesDirectory, _newFileName), true);

                    if (!_chCurrentImageFilePath.Equals(ApplicationData.Instance.DefaultImage))
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        File.Delete(Path.Combine(ApplicationData.Instance.ImagesDirectory, _chCurrentImageFilePath));
                    }

                    Character.CharacterImage = _newFileName;
                });
            }
            catch (Exception ex)
            {
                mLogger.Error("_chooseImage_Execute " + ex.Message);
            }
        }

        private bool _exportCharacterCommand_CanExecute()
        {
            return IsEditing;
        }

        private async void _exportCharacterCommand_Execute()
        {
            try
            {
                System.Windows.Forms.SaveFileDialog _saveFileDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "T2l file(*.t2l)|*.t2l",
                    FileName = Character.Model.CharacterName.Replace(" ", string.Empty)
                };

                System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.Cancel;
                result = _saveFileDialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                    return;

                if (File.Exists(_saveFileDialog.FileName))
                {
                    File.Delete(_saveFileDialog.FileName);
                }

                mMessageDialogService.ShowBusyDialog();

                await Task.Run(() =>
                {
                    FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
                    _generateZIPFile(Character.Model, _saveFileDialog.FileName);

                    // clear temp directory
                    FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
                });

                mMessageDialogService.CloseBusyDialog();
            }
            catch (Exception ex)
            {
                mLogger.Error("_exportCharacterCommand_Execute " + ex.Message);
            }
        }

        private bool _editWithJSONEditorCommand_CanExecute()
        {
            return IsEditing;
        }

        private void _editWithJSONEditorCommand_Execute()
        {
            try
            {
                Process.Start(Path.Combine(ApplicationData.Instance.ToolsDirectory, ApplicationData.Instance.JSONEditorExeFileName),
                    Path.Combine(ApplicationData.Instance.DataDirectory, Character.Model.FileName));
            }
            catch (Exception ex)
            {
                mLogger.Error("_editWithJSONEditorCommand_Execute " + ex.Message);
            }
        }

        private bool _deleteCommand_CanExecute()
        {
            return IsEditing;
        }

        private async void _deleteCommand_Execute()
        {
            try
            {
                var result = await mMessageDialogService
                    .ShowOKCancelDialogAsync("Are you sure about deleting this character?", "Delete character");

                if (result == MessageDialogResult.Cancel)
                    return;

                mMessageDialogService.ShowBusyDialog();

                string _imageFileName = Character.CharacterImage;
                Character.CharacterImage = ApplicationData.Instance.DefaultImage; // set to default image to avoid file in use exception

                await mCharacterDataProvider.Remove(Character.Model, _imageFileName);

                Load("");

                mMessageDialogService.CloseBusyDialog();
            }
            catch (Exception ex)
            {
                mLogger.Error("_deleteCommand_Execute " + ex.Message);
                mMessageDialogService.CloseBusyDialog();
                await mMessageDialogService.ShowMessage("Error", "Error occured during deleting character.");
                Load(Character.CharacterPrefix);
            }
        }

        private bool _saveCommand_CanExecute()
        {
           return Character != null && !Character.HasErrors;
        }

        private async void _saveCommand_Execute()
        {
            try
            {
                if (IsEditing)
                {
                    await mCharacterDataProvider.SaveAsync(Character.Model);
                }
                else
                {
                    await mCharacterDataProvider.AddAsync(Character.Model);
                }

                Load(Character.CharacterPrefix);
            }
            catch (Exception ex)
            {
                mLogger.Error("_saveCommand_Execute " + ex.Message);
                await mMessageDialogService.ShowMessage("Error", "Error occured during saving character. Please try again.");
            }
        }

        private void _onOpenCharacterDetailView(string _characterPrefix)
        {
            Load(_characterPrefix);
        }

        private void _generateZIPFile(Character _selectedCharacter,string path)
        {
            // create .json file with character's content and place to Temp dir
            mCharacterDataProvider.Export(_selectedCharacter);

            // move character related .mp3 files to Temp dir
            foreach (PhraseEntry phrase in _selectedCharacter.Phrases)
            {
                string _phraseFileName = $"{_selectedCharacter.CharacterPrefix}_{phrase.FileName}.mp3";
                string _phraseFileAbsolutePath = Path.Combine(ApplicationData.Instance.AudioDirectory, _phraseFileName);

                if (File.Exists(_phraseFileAbsolutePath))
                {
                    File.Copy(_phraseFileAbsolutePath, Path.Combine(ApplicationData.Instance.TempDirectory, _phraseFileName), true);
                }
            }

            // move character's image file to Temp dir if image is not default
            if (!_selectedCharacter.CharacterImage.Equals(ApplicationData.Instance.DefaultImage))
            {
                File.Copy(Path.Combine(ApplicationData.Instance.ImagesDirectory, _selectedCharacter.CharacterImage),
                    Path.Combine(ApplicationData.Instance.TempDirectory, _selectedCharacter.CharacterImage));
            }

            // zip content from temp directory
            ZipFile.CreateFromDirectory(ApplicationData.Instance.TempDirectory, path);
        }

        #endregion

        #region - public functions -

        public void Load(string _characterInitials)
        {
            if (string.IsNullOrEmpty(_characterInitials))
            {
                IsEditing = false;
            }
            else
            {
                IsEditing = true;
            }

            var character = !string.IsNullOrEmpty(_characterInitials)
                ? mCharacterDataProvider.GetByInitials(_characterInitials)
                : new Character();

            if (character == null)
                character = new Character();

            Character = new CharacterWrapper(character,mCharacterDataProvider);
            Character.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName == nameof(Character.HasErrors))
                {
                    ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            };

            ((DelegateCommand)DeleteCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)EditWithJSONEditorCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)ExportCharacterCommand).RaiseCanExecuteChanged();

            // used to force validation
            if (string.IsNullOrEmpty(_characterInitials))
            {
                Character.CharacterName = "";
                Character.CharacterPrefix = "";
            }
        }

        #endregion

        #region - properties -
        public CharacterWrapper Character
        {
            get { return mCharacter; }
            set
            {
                mCharacter = value;
                RaisePropertyChanged();
            }
        }

        public List<int> AgeValues { get; set; } = new List<int>(Enumerable.Range(2, 100));

        public List<string> GenderValues { get; set; } = new List<string>(new string[] { "Male", "Female" });

        public bool IsEditing
        {
            get { return mIsEditing; }
            set
            {
                mIsEditing = value;
                RaisePropertyChanged();
            }
        }

        public bool IsDialogStarted
        {
            get { return mIsDialogStarted; }
            set
            {
                mIsDialogStarted = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
