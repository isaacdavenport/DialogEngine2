using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class CharacterDialogLinesViewModel : BindableBase
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IMessageDialogService mMessageDialogService;
        private ICharacterDataProvider mCharacterDataProvider;
        private Character mCharacter;
        private CollectionViewSource mPhrasesCollectionViewSource;
        private string mFilterText;
        private bool mIsDialogStarted;


        public CharacterDialogLinesViewModel(ILogger _Logger
            , IEventAggregator _EventAggregator
            , IMessageDialogService _MessageDialogService
            , ICharacterDataProvider _CharacterDataProvider)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mPhrasesCollectionViewSource = new CollectionViewSource();
            mMessageDialogService = _MessageDialogService;
            mCharacterDataProvider = _CharacterDataProvider;
            FilterText = "";
            mPhrasesCollectionViewSource.Filter += _phrases_Filter;

            mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Subscribe(_onCharacterSelectionActionChanged);

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

            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(dialog);
        }

        private void _viewLoadedExecute()
        {
            Character = Session.Get<Character>(Constants.SELECTED_CHARACTER);            
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
