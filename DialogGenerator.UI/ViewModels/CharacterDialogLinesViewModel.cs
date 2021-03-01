using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class CharacterDialogLinesViewModel : BindableBase
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IMessageDialogService mMessageDialogService;
        private ICharacterDataProvider mCharacterDataProvider;
        private IRegionManager mRegionManager;
        private Character mCharacter;
        private CollectionViewSource mPhrasesCollectionViewSource;
        private string mFilterText;
        private bool mIsDialogStarted;
        private bool mIsFromWizard = false;

        public CharacterDialogLinesViewModel(ILogger _Logger
            , IEventAggregator _EventAggregator
            , IMessageDialogService _MessageDialogService
            , ICharacterDataProvider _CharacterDataProvider
            , IRegionManager _RegionManager)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mRegionManager = _RegionManager;
            mPhrasesCollectionViewSource = new CollectionViewSource();
            mMessageDialogService = _MessageDialogService;
            mCharacterDataProvider = _CharacterDataProvider;
            FilterText = "";
            mPhrasesCollectionViewSource.Filter += _phrases_Filter;

            mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Subscribe(_onCharacterSelectionActionChanged);
            mEventAggregator.GetEvent<CharacterSavedEvent>().Subscribe(_onCharacterSaved);

            _bindCommands();
        }        

        public Character Character
        {
            get
            {
                return mCharacter;
            }

            set
            {
                mCharacter = value;
                mPhrasesCollectionViewSource.Source = mCharacter.Phrases;               
                RaisePropertyChanged();
                RaisePropertyChanged("PhrasesViewSource");
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

        public DelegateCommand ViewLoadedCommand { get; set; }
        public DelegateCommand<string> PlayDialogLineCommand { get; set; }
        public DelegateCommand<PhraseEntry>DeletePhraseCommand { get; set; }
        public DelegateCommand<PhraseEntry>EditPhraseCommand { get; set; }
        public DelegateCommand GoBackCommand { get; set; }

        public ICollectionView PhrasesViewSource
        {
            get { return mPhrasesCollectionViewSource.View; }
        }

        public string FilterText
        {
            get { return mFilterText; }
            set
            {
                mFilterText = value;
                mPhrasesCollectionViewSource.View?.Refresh();
                RaisePropertyChanged();
            }
        }

        private void _bindCommands()
        {
            ViewLoadedCommand = new DelegateCommand(_viewLoadedExecute);
            PlayDialogLineCommand = new DelegateCommand<string>(_playDialogLine_Execute, _playDialogLine_CanExecute);
            DeletePhraseCommand = new DelegateCommand<PhraseEntry>(_deletePhrase_Execute, _deletePrhase_CanExecute);
            EditPhraseCommand = new DelegateCommand<PhraseEntry>(_editPhrase_Execute, _editPhrase_CanExecute);
            GoBackCommand = new DelegateCommand(_goBackCommand_execute);
        }

        private void _goBackCommand_execute()
        {
            mLogger.Debug($"Character Dialog Lines View - exited for character '{Character.CharacterName}'.");
            if(mIsFromWizard)
            {
                mIsFromWizard = false;
                mRegionManager.RequestNavigate("ContentRegion", "DialogGenerator.UI.Views.WizardView");
            } else
            {
                mRegionManager.RequestNavigate("ContentRegion", "DialogGenerator.UI.Views.CharacterDetailView");
            }
        }

        private void _onCharacterSaved(string _characterPrefix)
        {
            Character _char = mCharacterDataProvider.GetByInitials(_characterPrefix);
            if (_char != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Character = _char;
                });
                
            }
        }

        private bool _editPhrase_CanExecute(PhraseEntry _phraseEntry)
        {
            return !IsDialogStarted;
        }

        private async void _editPhrase_Execute(PhraseEntry _phraseEntry)
        {            
            EditPhraseViewModel _editPhraseViewModel = new EditPhraseViewModel(Character, _phraseEntry, mCharacterDataProvider, mMessageDialogService, mEventAggregator, mLogger);
            EditPhraseView _editPhraseView = new EditPhraseView();
            _editPhraseView.DataContext = _editPhraseViewModel;

            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(_editPhraseView, "ContentDialogHost");
            _editPhraseView = null;

            mPhrasesCollectionViewSource.View?.Refresh();
        }

        private bool _deletePrhase_CanExecute(PhraseEntry arg)
        {
            return !IsDialogStarted;
        }

        private async void _deletePhrase_Execute(PhraseEntry _phrase)
        {
            try
            {
                MessageDialogResult result = await mMessageDialogService.ShowOKCancelDialogAsync("Are you sure about deleting this line?", "Warning");
                if (result == MessageDialogResult.Cancel)
                    return;

                mCharacterDataProvider.RemovePhrase(Character, _phrase);
                await mCharacterDataProvider.SaveAsync(Character);
                string _audioFileName = ApplicationData.Instance.AudioDirectory + "\\" + Character.CharacterPrefix + "_" + _phrase.FileName + ".mp3";
                if(File.Exists(_audioFileName))
                {
                    File.Delete(_audioFileName);
                }

                FilterText = "";
            }
            catch (Exception ex)
            {
                mLogger.Error($"_deletePhrase_Execute {ex.Message}");
            }
        }

        private bool _playDialogLine_CanExecute(string arg)
        {
            return !IsDialogStarted;
        }

        private async void _playDialogLine_Execute(string _fileName)
        {
            string _fullFileName = $"{Character.CharacterPrefix}_{_fileName}.mp3";
            string _fullPath = Path.Combine(ApplicationData.Instance.AudioDirectory, _fullFileName);
            MP3PlayerDialog dialog = new MP3PlayerDialog(_fullPath);

            mLogger.Debug($"Character Dialog Lines View - About to play dialog line for character {mCharacter.CharacterName}'");
            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(dialog);
            mLogger.Debug($"Character Dialog Lines View - Dialog line for character {mCharacter.CharacterName}' played!");
        }

        private void _viewLoadedExecute()
        
        {
            var _character = Session.Get<Character>(Constants.SELECTED_CHARACTER);            

            if(_character == null || Session.Get<bool>(Constants.CHARACTER_EDIT_MODE))
            {
                CreateCharacterViewModel _cvm = Session.Get<CreateCharacterViewModel>(Constants.CREATE_CHARACTER_VIEW_MODEL);
                Character = _cvm.Character;
                mIsFromWizard = true;
            } else
            {
                Character = _character;
            }
            
            mLogger.Debug($"Character Dialog Lines View - Loaded");
        }

        private void _phrases_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }

            var phrase = e.Item as PhraseEntry;
            if (phrase.DialogStr.ToUpper().Contains(FilterText.ToUpper()))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void _onCharacterSelectionActionChanged(bool _isDialogStarted)
        {
            IsDialogStarted = _isDialogStarted;
        }
    }
}
