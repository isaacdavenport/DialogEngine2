using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Wrapper;
using DialogGenerator.Utilities;
using MaterialDesignThemes.Wpf;
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
using System.Windows;
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
        private bool mIsEditing;
        #endregion

        #region -constructor-
        public CharacterDetailViewModel(ILogger logger, IEventAggregator _eventAggregator
            , ICharacterDataProvider _characterDataProvider, IMessageDialogService  _messageDialogService)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;
            mMessageDialogService = _messageDialogService;

            mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>()
                .Subscribe(_onOpenCharacterDetailView);

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

        private bool _chooseImage_CanExecute()
        {
            if (string.IsNullOrEmpty(Character.CharacterPrefix) || Character.HasErrors)
            { 
               mMessageDialogService.ShowMessage("Information", "You are not able to set image before all required fields are filled.");

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

                if (_openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string _chCurrentImageFilePath = Character.CharacterImage;
                    string _filePath = _openFileDialog.FileName;
                    string _newFileName = $"{Character.CharacterPrefix}_{Path.GetFileName(_filePath)}";
                    Character.CharacterImage = ApplicationData.Instance.DefaultImage;

                    await Task.Run(() =>
                    {
                        Thread.CurrentThread.Name = "_chooseImage_Execute";

                        File.Copy(_filePath, Path.Combine(ApplicationData.Instance.ImagesDirectory, _newFileName),true);

                        if (!_chCurrentImageFilePath.Equals(ApplicationData.Instance.DefaultImage))
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            File.Delete(Path.Combine(ApplicationData.Instance.ImagesDirectory, _chCurrentImageFilePath));
                        }

                        Character.CharacterImage = _newFileName;
                    });
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_chooseImage_Execute " + ex.Message);
            }
        }


        private bool _exportCharacterCommand_CanExecute()
        {
            return mIsEditing;
        }

        private async void _exportCharacterCommand_Execute()
        {
            try
            {
                System.Windows.Forms.SaveFileDialog _saveFileDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "Zip file(*.zip)|*.zip",
                    FileName = Path.GetFileNameWithoutExtension(Character.Model.FileName)
                };

                System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.Cancel;

                result = _saveFileDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    if (File.Exists(_saveFileDialog.FileName))
                    {
                        File.Delete(_saveFileDialog.FileName);
                    }
                }
                else
                {
                    return;
                }

                var busyDialog = new BusyDialog();
                mMessageDialogService.ShowDedicatedDialogAsync<bool>(busyDialog);

                await Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "_exportCharacterCommand_Execute";
                    _generateZIPFile(Character.Model,_saveFileDialog.FileName);

                    // clear temp directory
                    DirectoryInfo _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);

                    foreach (FileInfo file in _directoryInfo.GetFiles())
                    {
                        file.Delete();
                    }
                });

                DialogHost.CloseDialogCommand.Execute(null, busyDialog);
            }
            catch (Exception ex)
            {
                mLogger.Error("_exportCharacterCommand_Execute " + ex.Message);
            }
        }

        private bool _editWithJSONEditorCommand_CanExecute()
        {
            return mIsEditing;
        }

        private void _editWithJSONEditorCommand_Execute()
        {
            Process.Start(Path.Combine(ApplicationData.Instance.TutorialDirectory, ApplicationData.Instance.JSONEditorExeFileName),
                Path.Combine(ApplicationData.Instance.DataDirectory, Character.Model.FileName));
        }

        private bool _deleteCommand_CanExecute()
        {
            return mIsEditing;
        }

        private async  void _deleteCommand_Execute()
        {
            try
            {
                var result = await mMessageDialogService
                    .ShowOKCancelDialogAsync("Are you sure about deleting this character?", "Delete character");

                if (result == MessageDialogResult.OK)
                {
                    string _imageFileName = Character.CharacterImage;
                    Character.CharacterImage = ApplicationData.Instance.DefaultImage;
                    await mCharacterDataProvider.Remove(Character.Model,_imageFileName);

                    Load("");
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_deleteCommand_Execute " + ex.Message);
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
                if (mIsEditing)
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
            string _fileName = _selectedCharacter.FileName;
            string _fileAbsolutePath = Path.Combine(ApplicationData.Instance.DataDirectory, _fileName);

            // copy file to Temp directory
            File.Copy(_fileAbsolutePath, Path.Combine(ApplicationData.Instance.TempDirectory, _fileName), true);

            foreach (PhraseEntry phrase in _selectedCharacter.Phrases)
            {
                string _phraseFileName = _selectedCharacter.CharacterPrefix + "_" + phrase.FileName + ".mp3";
                string _phraseFileAbsolutePath = Path.Combine(ApplicationData.Instance.AudioDirectory, _phraseFileName);

                if (File.Exists(_phraseFileAbsolutePath))
                {
                    File.Copy(_phraseFileAbsolutePath, Path.Combine(ApplicationData.Instance.TempDirectory, _phraseFileName), true);
                }
            }

            ZipFile.CreateFromDirectory(ApplicationData.Instance.TempDirectory, path);
        }

        #endregion

        #region - public functions -
        public void Load(string _characterInitials)
        {
            if (string.IsNullOrEmpty(_characterInitials))
            {
                mIsEditing = false;
            }
            else
            {
                mIsEditing = true;
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
        #endregion
    }
}
