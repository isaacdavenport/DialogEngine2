using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Helpers;
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
using System.Windows;
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

        public CharactersNavigationViewModel(ILogger logger,IEventAggregator _eventAggregator
            ,IWizardDataProvider _wizardDataProvider
            ,IDialogModelDataProvider _dialogModelDataProvider
            ,ICharacterDataProvider _characterDataProvider
            ,IDialogDataRepository _dialogDataRepository
            ,IMessageDialogService _messageDialogService)
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
            var dialog = new BusyDialog();
            try
            {
                System.Windows.Forms.OpenFileDialog _openFileDialog = new System.Windows.Forms.OpenFileDialog();
                _openFileDialog.Filter = "T2l file(*.t2l)|*.t2l";

                if (_openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    mMessageDialogService.ShowDedicatedDialogAsync<MessageDialogResult>(dialog);

                    await Task.Run(async() =>
                    {
                        // clear temp directory if directory is not empty
                        FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);

                        ZipFile.ExtractToDirectory(_openFileDialog.FileName, ApplicationData.Instance.TempDirectory);

                        var _JSONObjectsTypesList = await mDialogDataRepository.LoadAsync(ApplicationData.Instance.TempDirectory);

                        if (_JSONObjectsTypesList.Characters == null || !_JSONObjectsTypesList.Characters.Any())
                        {
                            await Application.Current.Dispatcher.Invoke(async() =>
                            {
                                DialogHost.CloseDialogCommand.Execute(null, dialog);
                                await mMessageDialogService.ShowMessage("Warning", "An error occured during importing character.");
                            });
                        }
                        else
                        {
                            var _importedCharacter = _JSONObjectsTypesList.Characters.First();

                            if(mCharacterDataProvider.GetByInitials(_importedCharacter.CharacterPrefix) != null)
                            {
                                MessageDialogResult result = MessageDialogResult.Cancel;
                                await Application.Current.Dispatcher.Invoke(async() =>
                                {
                                    DialogHost.CloseDialogCommand.Execute(null,dialog);
                                    result = await mMessageDialogService
                                        .ShowOKCancelDialogAsync($"Character '{_importedCharacter.CharacterName}' already exists. Do you want to overwrite this characters?", "Warning", "Yes", "No");
                                    mMessageDialogService.ShowDedicatedDialogAsync<MessageDialogResult>(dialog);
                                });

                                if (result == MessageDialogResult.Cancel)
                                {
                                    FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
                                    return;
                                }

                                var _oldCharacter = mCharacterDataProvider.GetByInitials(_importedCharacter.CharacterPrefix);
                                string _imageFileName = _oldCharacter.CharacterImage;
                                _oldCharacter.CharacterImage = ApplicationData.Instance.DefaultImage;
                                await mCharacterDataProvider.Remove(_oldCharacter, _imageFileName);
                            }

                            _processExtractedFiles();

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                mCharacterDataProvider.GetAll().Add(_importedCharacter);
                            });
                        }

                        FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
                    });
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_import_Click " + ex.Message);
                DialogHost.CloseDialogCommand.Execute(null, dialog);
                await mMessageDialogService.ShowMessage("Error","Error during importing character.");
            }

            DialogHost.CloseDialogCommand.Execute(null, dialog);
        }

        private  void _processExtractedFiles()
        {
            try
            {
                var _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);
                // copy .json file to Data directory

                foreach (var _jsonFile in _directoryInfo.GetFiles("*.json"))
                {
                    File.Copy(_jsonFile.FullName,Path.Combine(ApplicationData.Instance.DataDirectory,_jsonFile.Name), true);
                }

                // copy .mp3 files to Audio directory

                FileInfo[] _mp3Files = _directoryInfo.GetFiles("*.mp3");

                foreach (FileInfo _mp3File in _mp3Files)
                {
                    File.Copy(_mp3File.FullName,Path.Combine(ApplicationData.Instance.AudioDirectory,_mp3File.Name), true);
                }

                // copy images to Image directory
                var _supportedExtensions = new string[] { ".jpg", ".jpeg", ".jpe", ".png" };
                FileInfo[] _imageFiles = _directoryInfo.GetFiles()
                    .Where(f => _supportedExtensions.Contains(f.Extension.ToLower()))
                    .ToArray();

                foreach (FileInfo _imageFile in _imageFiles)
                {
                    File.Copy(_imageFile.FullName,Path.Combine(ApplicationData.Instance.ImagesDirectory,_imageFile.Name), true);
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_processExtractedFiles " + ex.Message);
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
