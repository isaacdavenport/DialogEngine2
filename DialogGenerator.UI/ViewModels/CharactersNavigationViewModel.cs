using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Helpers;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        private CollectionViewSource mCharactersCollectionViewSource;
        private string mFilterText;

        #endregion

        #region - constructor -

        public CharactersNavigationViewModel(ILogger logger,IEventAggregator _eventAggregator
            ,IDialogDataRepository _dialogDataRepository
            ,IWizardDataProvider _wizardDataProvider
            ,IDialogModelDataProvider _dialogModelDataProvider
            ,ICharacterDataProvider _characterDataProvider
            ,IMessageDialogService _messageDialogService)
        {
            mEventAggregator = _eventAggregator;
            mLogger = logger;
            mDialogDataRepository = _dialogDataRepository;
            mMessageDialogService = _messageDialogService;
            mCharacterDataProvider = _characterDataProvider;
            mDialogModelDataProvider = _dialogModelDataProvider;
            mWizardDataProvider = _wizardDataProvider;

            mCharactersCollectionViewSource = new CollectionViewSource();
            FilterText = "";

            mCharactersCollectionViewSource.Filter += _mCharacterViewSource_Filter;

            _bindCommands();
        }


        #endregion

        #region - commands -

        public DelegateCommand CreateNewCharacterCommand { get; set; }
        public DelegateCommand ImportCharacterCommand { get; set; }
        public DelegateCommand<object> ChangeCharacterStatusCommand { get; set; }
        public DelegateCommand OnlineCharactersCommand { get; set; }

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
            OnlineCharactersCommand = new DelegateCommand(_onOnlineCharacters_Execute);
        }

        private  void _onOnlineCharacters_Execute()
        {
            //await mMessageDialogService.ShowDedicatedDialogAsync<bool>(mOnlineCharactersDialog);
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
                    if(_forcedCharactersCount == 0)
                    {
                        Session.Set(Constants.FORCED_CH_1, index);
                        Session.Set(Constants.FORCED_CH_COUNT, 1);
                    }
                    else if(_forcedCharactersCount == 1)
                    {
                        Session.Set(Constants.FORCED_CH_2, index);
                        Session.Set(Constants.FORCED_CH_COUNT, 2);
                    }
                    else
                    {
                        return;
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
                _openFileDialog.Filter = "T2l file(*.t2l)|*.t2l";

                if (_openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                mMessageDialogService.ShowBusyDialog();

                // extract data to temp directory
                await Task.Run(() =>
                {
                    FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
                    ZipFile.ExtractToDirectory(_openFileDialog.FileName, ApplicationData.Instance.TempDirectory);
                });

                IList<string> errors;
                var _JSONObjectsTypesList = mDialogDataRepository.LoadFromDirectory(ApplicationData.Instance.TempDirectory,out errors);
                // validate against json schema
                if(errors.Count > 0)
                {
                    mMessageDialogService.CloseBusyDialog();
                    await mMessageDialogService.ShowMessagesDialogAsync("Error", "Imported file has errors: ",errors, "Close message", false);
                    return;
                }
                // validate is character found
                if(_JSONObjectsTypesList.Characters.Count == 0)
                {
                    mMessageDialogService.CloseBusyDialog();
                    await mMessageDialogService.ShowMessage("Error", "Couldn't find character inside loaded file.");
                    return;
                }

                var _importedCharacter = _JSONObjectsTypesList.Characters.First();
                if (mCharacterDataProvider.GetByInitials(_importedCharacter.CharacterPrefix) != null)
                {
                    MessageDialogResult result = MessageDialogResult.Cancel;
                    mMessageDialogService.CloseBusyDialog();
                    result = await mMessageDialogService.ShowOKCancelDialogAsync($"Character '{_importedCharacter.CharacterName}' already exists." +
                            $" Do you want to overwrite this characters?", "Warning", "Yes", "No");

                    if (result == MessageDialogResult.Cancel)
                    {
                        return;
                    }

                    mMessageDialogService.ShowBusyDialog();

                    var _oldCharacter = mCharacterDataProvider.GetByInitials(_importedCharacter.CharacterPrefix);
                    string _imageFileName = _oldCharacter.CharacterImage;
                    _oldCharacter.CharacterImage = ApplicationData.Instance.DefaultImage;

                    await mCharacterDataProvider.Remove(_oldCharacter, _imageFileName);
                }

                await _processImportedCharacter(_importedCharacter);
            }
            catch (Exception ex)
            {
                mLogger.Error("_import_Click " + ex.Message);
                mMessageDialogService.CloseBusyDialog();
                await mMessageDialogService.ShowMessage("Error", "Error during importing character.");
                //TODO add support for rollback files if exception occured
            }
            finally
            {
                FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
                mMessageDialogService.CloseBusyDialog();
            }
        }

        private async Task _processImportedCharacter(Character _importedCharacter)
        {
            // move character related files from temp to data related folders
            await _processExtractedFiles();

            _importedCharacter.RadioNum = -1; // unassign imported character to avoid duplicates
            _importedCharacter.State = CharacterState.Available;
            await mCharacterDataProvider.SaveAsync(_importedCharacter);

            // add character to list of characters
            mCharacterDataProvider.GetAll().Add(_importedCharacter);
        }

        private Task _processExtractedFiles()
        {
            return Task.Run(() =>
            {
                var _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);
                // copy .json file to Data directory

                foreach (var _jsonFile in _directoryInfo.GetFiles("*.json"))
                {
                    File.Copy(_jsonFile.FullName, Path.Combine(ApplicationData.Instance.DataDirectory, _jsonFile.Name), true);
                }

                // copy .mp3 files to Audio directory

                FileInfo[] _mp3Files = _directoryInfo.GetFiles("*.mp3");

                foreach (FileInfo _mp3File in _mp3Files)
                {
                    File.Copy(_mp3File.FullName, Path.Combine(ApplicationData.Instance.AudioDirectory, _mp3File.Name), true);
                }

                // copy images to Image directory
                var _supportedExtensions = new string[] { ".jpg", ".jpeg", ".jpe", ".png" };
                FileInfo[] _imageFiles = _directoryInfo.GetFiles()
                    .Where(f => _supportedExtensions.Contains(f.Extension.ToLower()))
                    .ToArray();

                foreach (FileInfo _imageFile in _imageFiles)
                {
                    File.Copy(_imageFile.FullName, Path.Combine(ApplicationData.Instance.ImagesDirectory, _imageFile.Name), true);
                }

                FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
            });
        }

        private void _createNewCharacterCommand_Execute()
        {
            mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>().Publish(null);
        }

        #endregion

        #region - public functions -

        public void Load()
        {
            mCharactersCollectionViewSource.Source =  mCharacterDataProvider.GetAll();
            RaisePropertyChanged(nameof(CharactersViewSource));
        }

        #endregion

        #region - properties -

        public ICollectionView CharactersViewSource
        {
            get { return mCharactersCollectionViewSource.View; }
        }


        public string FilterText
        {
            get { return mFilterText; }
            set
            {
                mFilterText = value;
                mCharactersCollectionViewSource.View?.Refresh();
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
                    mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>().Publish(mSelectedCharacter.CharacterPrefix);
                }
            }
        }

        #endregion
    }
}
