using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Events;
using DialogGenerator.UI.View.Services;
using DialogGenerator.UI.Wrapper;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModel
{
    public class CharacterDetailViewModel : ViewModelBase, ICharacterDetailViewModel
    {
        #region - fields-
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterDataProvider mCharacterDataProvider;
        private IMessageDialogService mMessageDialogService;
        private CharacterWrapper mCharacter;
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
        public ICommand RunWizardOverCharacter { get; set; }

        #endregion

        #region - private functions -
        private void _bindCommands()
        {
            SaveCommand = new RelayCommand(_saveCommand_Execute, _saveCommand_CanExecute);
            DeleteCommand = new RelayCommand(_deleteCommand_Execute, _deleteCommand_CanExecute);
            EditWithJSONEditorCommand = new RelayCommand(_editWithJSONEditorCommand_Execute, _editWithJSONEditorCommand_CanExecute);
            ExportCharacterCommand = new RelayCommand(_exportCharacterCommand_Execute, _exportCharacterCommand_CanExecute);
            RunWizardOverCharacter = new RelayCommand(_runWizardOverCharacter_Execute);
        }

        private void _runWizardOverCharacter_Execute()
        {

        }

        private bool _exportCharacterCommand_CanExecute()
        {
            return true;
        }

        private async void _exportCharacterCommand_Execute()
        {
            await Task.Run(() =>
            {
                _generateZIPFile(Character.Model);

                // clear temp directory
                DirectoryInfo _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);

                foreach (FileInfo file in _directoryInfo.GetFiles())
                {
                    file.Delete();
                }
            });
        }

        private bool _editWithJSONEditorCommand_CanExecute()
        {
            return true;
        }

        private async void _editWithJSONEditorCommand_Execute()
        {
            await Task.Run(() =>
            {
                Process.Start(Path.Combine(ApplicationData.Instance.TutorialDirectory, ApplicationData.Instance.JSONEditorExeFileName),
                    Path.Combine(ApplicationData.Instance.DataDirectory, Character.Model.FileName));
            });
        }

        private bool _deleteCommand_CanExecute()
        {
            return true;
        }

        private void _deleteCommand_Execute()
        {
            MessageDialogResult result = mMessageDialogService
                .ShowOKCancelDialog("Are you sure about deleting this character?", "Delete character");
        }

        private bool _saveCommand_CanExecute()
        {
            return Character != null && !Character.HasErrors;
        }

        private void _saveCommand_Execute()
        {
            throw new NotImplementedException();
        }

        private void _onOpenCharacterDetailView(string _characterName)
        {
            Load(_characterName);
        }


        private void _generateZIPFile(Character _selectedCharacter)
        {
            try
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

                System.Windows.Forms.SaveFileDialog _saveFileDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "Zip file(*.zip)|*.zip",
                    FileName = Path.GetFileNameWithoutExtension(_selectedCharacter.FileName)
                };

                System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.Cancel;

                // send to application dispatcher(main thread)
                DispatcherHelper.UIDispatcher.Invoke(() =>
                {
                    result = _saveFileDialog.ShowDialog();
                });

                if (result == System.Windows.Forms.DialogResult.OK)
                {                    
                    ZipFile.CreateFromDirectory(ApplicationData.Instance.TempDirectory, _saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_generateZIPFile " + ex.Message);
            }
        }

        #endregion

        #region - public functions -
        public void Load(string _charactername)
        {
            var character = _charactername != null
                ? mCharacterDataProvider.GetByName(_charactername)
                : new Character();

            Character = new CharacterWrapper(character);
            Character.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName == nameof(Character.HasErrors))
                {
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            };

            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();

            if (string.IsNullOrEmpty(_charactername))
            {
                Character.CharacterName = "";
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
