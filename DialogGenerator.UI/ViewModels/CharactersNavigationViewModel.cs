using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.Utilities;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class CharactersNavigationViewModel : BindableBase, INavigationViewModel
    {
        #region - fields -

        private ILogger mLogger;
        private IMessageDialogService mMessageDialogService;
        private IDialogDataRepository mDialogDataRepository;
        private ICharacterDataProvider mCharacterDataProvider;
        private IDialogModelDataProvider mDialogModelDataProvider;
        private IWizardDataProvider mWizardDataProvider;
        private IEventAggregator mEventAggregator;
        private Character mSelectedCharacter;
        private CollectionViewSource mCharactersViewSource;
        private string mFilterText;

        #endregion

        #region - constructor -

        public CharactersNavigationViewModel(ILogger logger,IEventAggregator _eventAggregator,IWizardDataProvider _wizardDataProvider,
            IDialogModelDataProvider _dialogModelDataProvider,ICharacterDataProvider _characterDataProvider, 
            IDialogDataRepository _dialogDataRepository,IMessageDialogService _messageDialogService)
        {
            mEventAggregator = _eventAggregator;
            mLogger = logger;
            mMessageDialogService = _messageDialogService;
            mDialogDataRepository = _dialogDataRepository;
            mCharacterDataProvider = _characterDataProvider;
            mDialogModelDataProvider = _dialogModelDataProvider;
            mWizardDataProvider = _wizardDataProvider;

            mCharactersViewSource = new CollectionViewSource();
            FilterText = "";

            mCharactersViewSource.Filter += _mCharacterViewSource_Filter;

            _bindCommands();
        }


        #endregion

        #region - commands -

        public DelegateCommand CreateNewCharacterCommand { get; set; }
        public DelegateCommand ImportCharacterCommand { get; set; }
        public DelegateCommand<object> ChangeCharacterStatusCommand { get; set; }

        #endregion

        #region - event handlers -

        private void _mCharacterViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }

            var character = e.Item as Character;
            if (character.CharacterName.ToUpper().Contains(FilterText.ToUpper()))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }


        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            CreateNewCharacterCommand = new DelegateCommand(_createNewCharacterCommand_Execute);
            ImportCharacterCommand = new DelegateCommand(_importCharacterCommand_Execute);
            ChangeCharacterStatusCommand = new DelegateCommand<object>( (p) => _changeCharacterStatusCommand_Execute(p));
        }

        private async void _changeCharacterStatusCommand_Execute(object obj)
        {
            try
            {
                var parameters = (object[])obj;
                var character = parameters[0] as Character;
                var _newState = (CharacterState)parameters[1];
                int index = int.Parse(parameters[2].ToString());
                int _forcedCharactersCount = Session.Get<int>(Constants.FORCED_CH_COUNT);

                if (_newState == character.State)
                    return;

                if (_newState == CharacterState.On)
                {
                    switch (_forcedCharactersCount)
                    {
                        case 0:
                            {
                                Session.Set(Constants.FORCED_CH_1, index);
                                Session.Set(Constants.FORCED_CH_COUNT, 1);
                                break;
                            }
                        case 1:
                            {
                                Session.Set(Constants.FORCED_CH_2, index);
                                Session.Set(Constants.FORCED_CH_COUNT, 2);
                                break;
                            }
                        default:
                            {
                                return;
                            }
                    }
                }
                else
                {
                    if(character.State == CharacterState.On)
                    {
                        if (Session.Get<int>(Constants.FORCED_CH_1) == index)
                        {
                            Session.Set(Constants.FORCED_CH_1, -1);
                            Session.Set(Constants.FORCED_CH_COUNT, _forcedCharactersCount - 1);

                            if(Session.Get<int>(Constants.FORCED_CH_COUNT) == 1)
                            {
                                Session.Set(Constants.FORCED_CH_1, Session.Get<int>(Constants.FORCED_CH_2));
                                Session.Set(Constants.FORCED_CH_2, -1);
                            }
                        }

                        if (Session.Get<int>(Constants.FORCED_CH_2) == index)
                        {
                            Session.Set(Constants.FORCED_CH_2, -1);
                            Session.Set(Constants.FORCED_CH_COUNT, _forcedCharactersCount - 1);
                        }
                    }
                }

                character.State = _newState;

                await mCharacterDataProvider.SaveAsync(character);

                mEventAggregator.GetEvent<ChangedCharacterStateEvent>().Publish();
                mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();
            }
            catch (Exception ex)
            {
                mLogger.Error("_changeCharacterStatusCommand_Execute " + ex.Message);
            }
        }

        private async void _importCharacterCommand_Execute()
        {
            try
            {
                System.Windows.Forms.OpenFileDialog _openFileDialog = new System.Windows.Forms.OpenFileDialog();
                _openFileDialog.Filter = "Zip file(*.zip)|*.zip";

                if (_openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var dialog = new BusyDialog();
                    mMessageDialogService.ShowDedicatedDialogAsync<MessageDialogResult>(dialog);

                    await Task.Run(() =>
                    {
                        Thread.CurrentThread.Name = "_importCharacterCommand_Execute";
                        ZipFile.ExtractToDirectory(_openFileDialog.FileName, ApplicationData.Instance.TempDirectory);

                        _processExtractedFiles();

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        // clear data from Temp directory
                        DirectoryInfo _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);

                        foreach (FileInfo file in _directoryInfo.GetFiles())
                        {
                            bool _isDeleted = false;
                            int counter = 0;

                            do
                            {
                                try
                                {
                                    File.Delete(file.FullName);

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
                                throw new Exception("Error during deleting file.");
                            }
                        }
                    });

                    DialogHost.CloseDialogCommand.Execute(null, dialog);
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_import_Click " + ex.Message);
            }
        }

        private async void _processExtractedFiles()
        {
            try
            {
                var _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);

                // load data from .json files

                FileInfo[] _jsonFiles = _directoryInfo.GetFiles("*.json");

                var _JSONObjectsTypesList = await mDialogDataRepository.LoadAsync(ApplicationData.Instance.TempDirectory);

                // copy .json file to Data directory

                foreach (var _jsonFile in _jsonFiles)
                {
                    File.Copy(_jsonFile.FullName, ApplicationData.Instance.DataDirectory, true);
                }

                // copy .mp3 files to Audio directory

                FileInfo[] _mp3Files = _directoryInfo.GetFiles("*.mp3");

                foreach (FileInfo _mp3File in _mp3Files)
                {
                    File.Copy(_mp3File.FullName, ApplicationData.Instance.AudioDirectory, true);
                }

                // copy images to Image directory
                var _supportedExtensions = new string[] { "*.jpg", "*.jpeg", "*.jpe", "*.jfif", "*.png" };
                FileInfo[] _imageFiles = _directoryInfo.GetFiles()
                    .Where(f => _supportedExtensions.Contains(f.Extension.ToLower()))
                    .ToArray();

                foreach(FileInfo _imageFile in _imageFiles)
                {
                    File.Copy(_imageFile.FullName, ApplicationData.Instance.ImagesDirectory, true);
                }

            }
            catch (Exception ex)
            {
                mLogger.Error("_processExtractedFiles " + ex.Message);
            }
        }

        private void _processLoadedData(JSONObjectsTypesList _JSONObjectsTypesList)
        {
            var characters = mCharacterDataProvider.GetAll();
            var dialogModels = mDialogModelDataProvider.GetAll();
            var wizards = mWizardDataProvider.GetAll();

            foreach(var character in _JSONObjectsTypesList.Characters)
            {
                characters.Add(character);
            }

            foreach(var dialogModelInfo in _JSONObjectsTypesList.DialogModels)
            {
                dialogModels.Add(dialogModelInfo);
            }

            foreach(var wizard in _JSONObjectsTypesList.Wizards)
            {
                wizards.Add(wizard);
            }
        }

        private void _createNewCharacterCommand_Execute()
        {
            mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>()
                .Publish(null);
        }

        #endregion

        #region - public functions -

        public void Load()
        {
            mCharactersViewSource.Source =  mCharacterDataProvider.GetAll();

            RaisePropertyChanged("CharactersViewSource");
        }

        #endregion

        #region - properties -

        public ICollectionView CharactersViewSource
        {
            get { return mCharactersViewSource.View; }
        }

        public string FilterText
        {
            get { return mFilterText; }
            set
            {
                mFilterText = value;
                mCharactersViewSource.View?.Refresh();
                RaisePropertyChanged();
            }
        }

        public Character SelectedCharacter
        {
            get { return mSelectedCharacter; }
            set
            {
                mSelectedCharacter = value;
                RaisePropertyChanged();
                if(mSelectedCharacter != null)
                {
                    mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>()
                        .Publish(mSelectedCharacter.CharacterPrefix);
                }
            }
        }

        #endregion
    }
}
