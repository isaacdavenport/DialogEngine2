using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Workflow.CreateCharacterWorkflow;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class DialogViewModel:BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IMessageDialogService mMessageDialogService;
        private IDialogEngine mDialogEngine;
        private bool mIsDialogStarted;
        private bool mIsStopBtnEnabled;
        private Visibility mIsDebugViewVisible = Visibility.Collapsed;
        private bool mCanGoBackToWizard = false;
        private ICharacterRepository mCharacterRepository;
        private Character mFirstSelectedCharacter;
        private Character mSecondSelectedCharacter;
        private Character mListSelectedCharacter;
        private ObservableCollection<Character> mCharacters;


        #endregion

        #region - constructor -

        public DialogViewModel(ILogger logger,IEventAggregator _eventAggregator
            ,IDialogEngine _dialogEngine
            ,IMessageDialogService _messageDialogService
            ,ICharacterRepository _characterRepository)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mMessageDialogService = _messageDialogService;
            mDialogEngine = _dialogEngine;
            mCharacterRepository = _characterRepository;

            mEventAggregator.GetEvent<NewDialogLineEvent>().Subscribe(_onNewDialogLine);
            mEventAggregator.GetEvent<ActiveCharactersEvent>().Subscribe(_onNewActiveCharacters);
            mEventAggregator.GetEvent<RestartDialogEngineEvent>().Subscribe(_onRestartDialogEngineRecquired);
            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);

            _bindCommands();
        }

        private void _onCharacterCollectionLoaded()
        {
            Characters = mCharacterRepository.GetAll();
        }

        private void _onRestartDialogEngineRecquired()
        {
            if(!ApplicationData.Instance.UseBLERadios)
            {                
                int startIndex = 1;
                if(FirstSelectedCharacter == null)
                {
                    FirstSelectedCharacter = mCharacterRepository.GetAll()[startIndex++];
                }

                if(SecondSelectedCharacter == null)
                {
                    SecondSelectedCharacter = mCharacterRepository.GetAll()[startIndex];
                }
                
            }
        }

        #endregion

        #region - properties
        public ObservableCollection<Character> Characters
        {
            get
            {
                if(mCharacters == null)
                {
                    mCharacters = new ObservableCollection<Character>();
                }

                return mCharacters;
            }

            set
            {
                mCharacters = value;
                RaisePropertyChanged();
            }
        }

        public Character FirstSelectedCharacter
        {
            get
            {
                return mFirstSelectedCharacter;
            }

            set
            {
                mFirstSelectedCharacter = value;
                int idx = mCharacterRepository.IndexOf(mFirstSelectedCharacter);
                Session.Set(Constants.NEXT_CH_1, idx);

                RaisePropertyChanged();
            }
        }

        public Character SecondSelectedCharacter
        {
            get
            {
                return mSecondSelectedCharacter;
            }

            set
            {
                mSecondSelectedCharacter = value;
                int idx = mCharacterRepository.IndexOf(mSecondSelectedCharacter);
                Session.Set(Constants.NEXT_CH_2, idx);

                RaisePropertyChanged();
            }
        }

        public Character ListSelectedCharacter
        {
            get
            {
                return mListSelectedCharacter;
            }

            set
            {
                mListSelectedCharacter = value;
                SelectFirstCharacterCommand.RaiseCanExecuteChanged();
                SelectSecondCharacterCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged();
            }
        }

        #endregion

        #region - commands -

        public ICommand StartDialogCommand { get; set; }
        public ICommand StopDialogCommand { get; set; }
        public ICommand ConfigureDialogCommand { get; set; } 
        public ICommand ClearAllMessagesCommand { get; set; }
        public ICommand OpenSettingsDialogCommand { get; set; }
        public DelegateCommand<object> ChangeDebugVisibilityCommand { get; set; }
        public DelegateCommand ViewLoadedCommand { get; set; }
        public DelegateCommand ViewUnloadedCommand { get; set; }
        public DelegateCommand GoBackToWizardCommand { get; set; }
        public DelegateCommand CreateCharacterCommand { get; set; }

        public DelegateCommand SelectFirstCharacterCommand { get; set; }
        public DelegateCommand SelectSecondCharacterCommand { get; set; }

        #endregion

        #region - private functions -

        public void _bindCommands()
        {
            StartDialogCommand = new DelegateCommand(_startDialogCommand_Execute);
            StopDialogCommand = new DelegateCommand(_stopDialogCommand_Execute);
            ConfigureDialogCommand = new DelegateCommand(_configureDialogCommand_Execute);
            ClearAllMessagesCommand = new DelegateCommand(_clearAllMessages_Execute);
            OpenSettingsDialogCommand = new DelegateCommand(_onOpenSettingsDialog_Execute);
            ChangeDebugVisibilityCommand = new DelegateCommand<object>(_changeDebugVisibilityCommand_Execute);
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
            ViewUnloadedCommand = new DelegateCommand(_viewUnloaded_Execute);
            GoBackToWizardCommand = new DelegateCommand(_goBackToWizard_Execute, _goBackToWizard_CanExecute);
            SelectFirstCharacterCommand = new DelegateCommand(_SelectFirstCharacter_Execute, _selectFirstCharacter_CanExecute);
            SelectSecondCharacterCommand = new DelegateCommand(_SelectSecondCharacter_Execute, _selectSecondCharacter_CanExecute);
        }

        private bool _selectSecondCharacter_CanExecute()
        {
            if (mListSelectedCharacter == null)
                return false;
            if (mListSelectedCharacter == FirstSelectedCharacter || mListSelectedCharacter == SecondSelectedCharacter)
                return false;
            return true;
        }

        private void _SelectSecondCharacter_Execute()
        {
            SecondSelectedCharacter = ListSelectedCharacter;
        }

        private bool _selectFirstCharacter_CanExecute()
        {
            if (ListSelectedCharacter == null)
                return false;
            if (ListSelectedCharacter == FirstSelectedCharacter || ListSelectedCharacter == SecondSelectedCharacter)
                return false;
            return true;
        }

        private void _SelectFirstCharacter_Execute()
        {
            FirstSelectedCharacter = ListSelectedCharacter;
        }

        public bool CanGoBackToWizard
        {
            get
            {
                return mCanGoBackToWizard;
            }      
            
            set
            {
                mCanGoBackToWizard = value;
                RaisePropertyChanged();
            }
        }

        private bool _goBackToWizard_CanExecute()
        {
            if(Session.Contains(Constants.CHARACTER_EDIT_MODE) && Session.Get<bool>(Constants.CHARACTER_EDIT_MODE))
            {
                return true;
            }
            
            return false;
        }

        private void _goBackToWizard_Execute()
        {
            CreateCharacterViewModel ccViewModel = Session.Get<CreateCharacterViewModel>(Constants.CREATE_CHARACTER_VIEW_MODEL);
            if(ccViewModel != null)
            {
                ccViewModel.Workflow.Fire(Triggers.CheckCounter);
            }
        }

        private void _viewUnloaded_Execute()
        {
            try
            {                
                mDialogEngine.StopDialogEngine();
            }
            catch (Exception ex)
            {
                mLogger.Error(ex.Message);
            }
        }

        private async void _viewLoaded_Execute()
        {
            try
            {
                CanGoBackToWizard = GoBackToWizardCommand.CanExecute();
                await mDialogEngine.StartDialogEngine();                
            }
            catch (Exception ex)
            {
                mLogger.Error(ex.Message);
            }
        }

        private void _changeDebugVisibilityCommand_Execute(object param)
        {
            var _itemsControl = param as ItemsControl;

            IsDebugViewVisible = IsDebugViewVisible == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;

            // workaround for hiding debugView in dialogView 
            if(IsDebugViewVisible == Visibility.Collapsed)
                (_itemsControl.Parent as Grid).RowDefinitions[2].Height = new GridLength(0);
            else
                (_itemsControl.Parent as Grid).RowDefinitions[2].Height = new GridLength(150);
        }

        private void _stopDialogCommand_Execute()
        {
            mDialogEngine.StopDialogEngine();
            IsStopBtnEnabled = false;
        }

        private void _clearAllMessages_Execute()
        {
            DialogLinesCollection.Clear();
        }

        private async void _onOpenSettingsDialog_Execute()
        {
            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(new SettingsDialog());
        }

        private async void _configureDialogCommand_Execute()
        {
            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(new SettingsDialog());
        }

        private async void _startDialogCommand_Execute()
        {
            try
            {
                IsDialogStarted = true;
                IsStopBtnEnabled = true;
                mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Publish(true);
                await mDialogEngine.StartDialogEngine();

                IsDialogStarted = false;
                mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Publish(false);
            }
            catch (Exception ex)
            {
                mDialogEngine.StopDialogEngine();
                IsDialogStarted = false;
                mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Publish(false);
                mLogger.Error("_startOrStopDialogCommand_Execute " + ex.Message);
            }
        }

        private void _onNewDialogLine(NewDialogLineEventArgs obj)
        {
            _processDialogItem(obj);
        }

        private void _onNewActiveCharacters(string info)
        {
            _processDialogItem(info);
        }

        private void _processDialogItem(object item)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                if (DialogLinesCollection.Count > 150)
                    DialogLinesCollection.RemoveAt(0);

                DialogLinesCollection.Add(item);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    if (DialogLinesCollection.Count > 150)
                        DialogLinesCollection.RemoveAt(0);

                    DialogLinesCollection.Add(item);
                });
            }
        }

        #endregion

        #region - properties -

        public ObservableCollection<object> DialogLinesCollection { get; set; } = new ObservableCollection<object>();

        public List<int> DialogSpeedValues { get; set; } = new List<int>(Enumerable.Range(1, 20));

        public int SelectedDialogSpeed
        {
            get { return Session.Get<int>(Constants.DIALOG_SPEED); }
            set
            {
                Session.Set(Constants.DIALOG_SPEED, value);
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

        public bool IsStopBtnEnabled
        {
            get { return mIsStopBtnEnabled; }
            set
            {
                mIsStopBtnEnabled = value;
                RaisePropertyChanged();
            }
        }

        public Visibility IsDebugViewVisible
        {
            get { return mIsDebugViewVisible; }
            set
            {
                mIsDebugViewVisible = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
