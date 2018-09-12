using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Events;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace DialogGenerator.UI.ViewModel
{
    public class CharactersNavigationViewModel : ViewModelBase, INavigationViewModel
    {
        #region - fields -

        private ILogger mLogger;
        private IDialogDataRepository mDialogDataRepository;
        private ICharacterDataProvider mCharacterDataProvider;
        private IEventAggregator mEventAggregator;
        private Character mSelectedCharacter;

        #endregion

        #region - constructor -

        public CharactersNavigationViewModel(ILogger logger,IEventAggregator _eventAggregator,
            ICharacterDataProvider _characterDataProvider, IDialogDataRepository _dialogDataRepository)
        {
            mEventAggregator = _eventAggregator;
            mLogger = logger;
            mDialogDataRepository = _dialogDataRepository;
            mCharacterDataProvider = _characterDataProvider;

            _bindCommands();
        }

        #endregion

        #region - commands -

        public RelayCommand CreateNewCharacterCommand { get; set; }
        public RelayCommand ImportCharacterCommand { get; set; }
        public RelayCommand<object> ChangeCharacterStatusCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            CreateNewCharacterCommand = new RelayCommand(_createNewCharacterCommand_Execute);
            ImportCharacterCommand = new RelayCommand(_importCharacterCommand_Execute);
            ChangeCharacterStatusCommand = new RelayCommand<object>( (p) => _changeCharacterStatusCommand_Execute(p));
        }

        private void _changeCharacterStatusCommand_Execute(object obj)
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
                        }

                        if (Session.Get<int>(Constants.FORCED_CH_2) == index)
                        {
                            Session.Set(Constants.FORCED_CH_2, -1);
                            Session.Set(Constants.FORCED_CH_COUNT, _forcedCharactersCount - 1);
                        }
                    }
                }

                character.State = _newState;
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
                    await Task.Run(() =>
                    {
                        ZipFile.ExtractToDirectory(_openFileDialog.FileName, ApplicationData.Instance.TempDirectory);

                        _processExtractedFiles();

                        // clear data from Temp directory
                        DirectoryInfo _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);

                        foreach (FileInfo file in _directoryInfo.GetFiles())
                        {
                            file.Delete();
                        }
                    });
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
                DirectoryInfo _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);

                // process .json files
                FileInfo[] _jsonFiles = _directoryInfo.GetFiles("*.json");

                foreach (FileInfo _jsonFile in _jsonFiles)
                {
                    var _JSONObjectsTypesList = new JSONObjectsTypesList();

                    _JSONObjectsTypesList = await mDialogDataRepository.LoadAsync(ApplicationData.Instance.TempDirectory);

                    _processLoadedData(_JSONObjectsTypesList);

                    File.Copy(_jsonFile.FullName, ApplicationData.Instance.DataDirectory, true);
                }

                // process .mp3 files

                FileInfo[] _mp3Files = _directoryInfo.GetFiles("*.mp3");

                foreach (FileInfo _mp3File in _mp3Files)
                {
                    File.Copy(_mp3File.FullName, ApplicationData.Instance.AudioDirectory, true);
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_processExtractedFiles " + ex.Message);
            }
        }

        private void _processLoadedData(JSONObjectsTypesList _JSONObjectsTypesList)
        {
            var characters = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
            var dialogModels = Session.Get<ObservableCollection<ModelDialogInfo>>(Constants.DIALOG_MODELS);
            var wizards = Session.Get<ObservableCollection<Wizard>>(Constants.WIZARDS);

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
            var characters =  mCharacterDataProvider.GetAll();

            Characters.Clear();
            foreach (var character in characters)
            {
                Characters.Add(character);
            }
        }

        #endregion

        #region - properties -

        public ObservableCollection<Character> Characters { get; set; } = new ObservableCollection<Character>();

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
                        .Publish(mSelectedCharacter.CharacterName);
                }
            }
        }

        #endregion
    }
}
