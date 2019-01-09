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

                DirectoryInfo _directoryInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);

                foreach(FileInfo file in _directoryInfo.EnumerateFiles("*.json"))
                {
                    IList<string> errors;
                    var _JSONObjectsTypesList = mDialogDataRepository.LoadFromFile(file.FullName, out errors);
                    // validate against json schema
                    if (errors.Count > 0)
                    {
                        mMessageDialogService.CloseBusyDialog();
                        await mMessageDialogService.ShowMessagesDialogAsync("Error", "Imported file has errors: ", errors, "Close message", false);
                        continue;
                    }

                    foreach (var _importedCharacter in  _JSONObjectsTypesList.Characters.ToList()?? Enumerable.Empty<Character>())
                    {
                        if (mCharacterDataProvider.GetByInitials(_importedCharacter.CharacterPrefix) != null)
                        {
                            MessageDialogResult result = MessageDialogResult.Cancel;
                            mMessageDialogService.CloseBusyDialog();
                            result = await mMessageDialogService.ShowOKCancelDialogAsync($"Character '{_importedCharacter.CharacterName}' already exists." +
                                    $" Do you want to overwrite this characters?", "Warning", "Yes", "No");

                            if (result == MessageDialogResult.Cancel)
                            {
                                _JSONObjectsTypesList.Characters.Remove(_importedCharacter);
                                mMessageDialogService.ShowBusyDialog();
                                continue;
                            }

                            mMessageDialogService.ShowBusyDialog();

                            var _oldCharacter = mCharacterDataProvider.GetByInitials(_importedCharacter.CharacterPrefix);
                            string _imageFileName = _oldCharacter.CharacterImage;
                            _oldCharacter.CharacterImage = ApplicationData.Instance.DefaultImage;

                            await mCharacterDataProvider.Remove(_oldCharacter, _imageFileName);
                        }

                        _importedCharacter.RadioNum = -1;
                        _importedCharacter.State = CharacterState.Available;
                        mCharacterDataProvider.GetAll().Add(_importedCharacter);
                    }

                    foreach (var _importedDialogModel in _JSONObjectsTypesList.DialogModels.ToList()?? Enumerable.Empty<ModelDialogInfo>())
                    {
                        if (mDialogModelDataProvider.GetByName(_importedDialogModel.ModelsCollectionName) == null)
                        {
                            mDialogModelDataProvider.GetAll().Add(_importedDialogModel);
                        }
                        else
                        {
                            _JSONObjectsTypesList.DialogModels.Remove(_importedDialogModel);
                        }
                    }

                    foreach (var _importedWizard in _JSONObjectsTypesList.Wizards.ToList()?? Enumerable.Empty<Wizard>())
                    {
                        if (mWizardDataProvider.GetByName(_importedWizard.WizardName) == null)
                        {
                            mWizardDataProvider.GetAll().Add(_importedWizard);
                        }
                        else
                        {
                            _JSONObjectsTypesList.Wizards.Remove(_importedWizard);
                        }
                    }
                    
                    if(_JSONObjectsTypesList.Characters.Count > 0 
                       || _JSONObjectsTypesList.Wizards.Count > 0 
                       || _JSONObjectsTypesList.DialogModels.Count > 0)
                    {
                        await _processImportedData(_JSONObjectsTypesList, Path.Combine(ApplicationData.Instance.DataDirectory, file.Name));
                    }
                }
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

        private async Task _processImportedData(JSONObjectsTypesList _importedData,string path)
        {
            await Task.Run(() =>
            {
                var _supportedExtensions = new string[] { ".jpg", ".jpeg", ".jpe", ".png" };

                foreach (var character in _importedData.Characters.ToList()?? Enumerable.Empty<Character>())
                {
                    var _dirInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);
                    FileInfo[] _mp3Files = _dirInfo.GetFiles($"{character.CharacterPrefix}*.mp3");

                    foreach (FileInfo _mp3File in _mp3Files)
                    {
                        File.Copy(_mp3File.FullName, Path.Combine(ApplicationData.Instance.AudioDirectory, _mp3File.Name), true);
                    }

                    FileInfo _imageFile = _dirInfo.GetFiles()
                        .Where(f => _supportedExtensions.Contains(f.Extension.ToLower()) && f.Name.StartsWith(character.CharacterPrefix))
                        .ToList().FirstOrDefault();

                    if (_imageFile != null)
                    {
                        File.Copy(_imageFile.FullName, Path.Combine(ApplicationData.Instance.ImagesDirectory, _imageFile.Name), true);
                    }
                }

                if (File.Exists(path))
                {
                    path = Path.Combine(ApplicationData.Instance.DataDirectory, $"{Guid.NewGuid()}.json");
                }

                mDialogDataRepository.Save(_importedData, path);
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
