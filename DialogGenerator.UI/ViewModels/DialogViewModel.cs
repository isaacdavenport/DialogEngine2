using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Workflow.CreateCharacterWorkflow;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
        private IRegionManager mRegionManager;
        private bool mIsDialogStarted;
        private bool mIsStopBtnEnabled;
        private Visibility mIsDebugViewVisible = Visibility.Collapsed;
        private bool mCanGoBackToWizard = false;
        private ICharacterRepository mCharacterRepository;
        private IWizardRepository mWizardRepository;
        private IDialogModelRepository mDialogModelRepository;
        private Character mFirstSelectedCharacter;
        private Character mSecondSelectedCharacter;
        private Character mListSelectedCharacter;
        private ObservableCollection<Character> mCharacters;
        private string mFirstCharacterDialogLine;
        private string mSecondCharacterDialogLine;
        private bool mRadioModeOn = false;
        private ArenaViewModel mArenaViewModel;
        private AssignedRadiosViewModel mAssignedRadiosViewModel;
        private bool mCanPause = false;
        private bool mCanResume = false;


        #endregion

        #region - constructor -

        public DialogViewModel(ILogger logger,IEventAggregator _eventAggregator
            ,IDialogEngine _dialogEngine
            ,IMessageDialogService _messageDialogService
            ,ICharacterRepository _characterRepository
            ,IRegionManager _regionManager
            ,ArenaViewModel _ArenaViewModel
            ,AssignedRadiosViewModel _AssignedRadiosViewModel
            ,IDialogModelRepository _DialogModelRepository
            ,IWizardRepository _WizardRepository)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mMessageDialogService = _messageDialogService;
            mDialogEngine = _dialogEngine;
            mCharacterRepository = _characterRepository;
            mDialogModelRepository = _DialogModelRepository;
            mWizardRepository = _WizardRepository;
            mRegionManager = _regionManager;
            mArenaViewModel = _ArenaViewModel;
            mAssignedRadiosViewModel = _AssignedRadiosViewModel;

            mEventAggregator.GetEvent<NewDialogLineEvent>().Subscribe(_onNewDialogLine);
            mEventAggregator.GetEvent<ActiveCharactersEvent>().Subscribe(_onNewActiveCharacters);
            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);
            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Subscribe(_onSelectedCharactersPairChangedEvent);
            mEventAggregator.GetEvent<GuidedCharacterCreationModeChangedEvent>().Subscribe(_onGuidedCharacterCreationModeChanged);
            mEventAggregator.GetEvent<CharacterSelectionModelChangedEvent>().Subscribe(_onCharacterSelectionModelChanged);

            _bindCommands();

            if(ApplicationData.Instance.DebugModeOn)
            {
                IsDebugViewVisible = Visibility.Visible;
            }

            mDialogEngine.PropertyChanged += MDialogEngine_PropertyChanged;
        }

        public void CopyAndClear()
        {
            string strToCopy = string.Empty;
            foreach(NewDialogLineEventArgs line in DialogLinesCollection)
            {
                if(line.Selected)
                {
                    if(!string.IsNullOrEmpty(strToCopy))
                    {
                        strToCopy += "\n";                        
                    }

                    strToCopy += line.Character.CharacterName;
                    strToCopy += " - ";
                    strToCopy += line.DialogLine;
                    line.Selected = false;
                }
            }

            Clipboard.SetText(strToCopy);
        }

        private void MDialogEngine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PauseCommand.RaiseCanExecuteChanged();
            ResumeCommand.RaiseCanExecuteChanged();
            CanPause = mDialogEngine.Running;
            CanResume = mDialogEngine.PauseCancellationTokenSource != null;
        }

        private void _onCharacterSelectionModelChanged()
        {
            RadioModeOn = Session.Get<bool>(Constants.BLE_MODE_ON);
        }

        private void _onGuidedCharacterCreationModeChanged(bool obj)
        {
            CanGoBackToWizard = obj;
        }

        private void _onSelectedCharactersPairChangedEvent(SelectedCharactersPairEventArgs obj)
        {
            if(obj == null)
            {
                return;
            }

            if(obj.Character1Index != -1)
            {
                FirstSelectedCharacter = mCharacterRepository.GetAll()[obj.Character1Index];
            } else
            {
                FirstSelectedCharacter = null;
            }

            if (obj.Character2Index != -1)
            {
                SecondSelectedCharacter = mCharacterRepository.GetAll()[obj.Character2Index];
            }
            else
            {
                SecondSelectedCharacter = null;
            }
           
        }

        private void _onCharacterCollectionLoaded()
        {
            Characters = mCharacterRepository.GetAll();
        }

        private void _onRestartDialogEngineRecquired()
        {
            if(!Session.Get<bool>(Constants.BLE_MODE_ON))
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
        public bool CanPause
        {
            get
            {
                return mCanPause;
            }

            set
            {
                mCanPause = value;
                OnPropertyChanged();
            }
        }

        public bool CanResume
        {
            get
            {
                return mCanResume;
            }

            set
            {
                mCanResume = value;
                OnPropertyChanged();
            }
        }

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
                int _idx = -1;
                if(mFirstSelectedCharacter != null)
                {
                    _idx = mCharacterRepository.IndexOf(mFirstSelectedCharacter);
                }
                
                //Session.Set(Constants.NEXT_CH_1, _idx);

                RaisePropertyChanged();
            }
        }

        public string FirstCharacterDialogLine
        {
            get
            {
                return mFirstCharacterDialogLine;
            }

            set
            {
                mFirstCharacterDialogLine = value;
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
                int _idx = -1;
                if(mSecondSelectedCharacter != null)
                {
                    _idx = mCharacterRepository.IndexOf(mSecondSelectedCharacter);
                }
                
                //Session.Set(Constants.NEXT_CH_2, _idx);

                RaisePropertyChanged();
            }
        }

        public string SecondCharacterDialogLine
        {
            get
            {
                return mSecondCharacterDialogLine;
            }

            set
            {
                mSecondCharacterDialogLine = value;
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

        public bool RadioModeOn
        {
            get
            {
                return mRadioModeOn;
            }

            set
            {
                mRadioModeOn = value;
                RaisePropertyChanged();
            }
        }

        public ArenaViewModel ArenaViewModel
        {
            get
            {
                return mArenaViewModel;
            }
        }

        public AssignedRadiosViewModel AssignedRadiosViewModel
        {
            get
            {
                return mAssignedRadiosViewModel;
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
        public DelegateCommand ExpertModeCommand { get; set; }
        public DelegateCommand ToggleAssignedRadiosCommand { get; set; }
        public DelegateCommand ShowPDFHelpCommand { get; set; }

        public DelegateCommand PauseCommand { get; set; }
        public DelegateCommand ResumeCommand { get; set; }
        public DelegateCommand CopyLinesCommand { get; set; }
        public DelegateCommand SelectAllCommand { get; set; }


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
            ExpertModeCommand = new DelegateCommand(_expertModeExecute, _expertMode_CanExecute);
            ToggleAssignedRadiosCommand = new DelegateCommand(_toggleAssignedRadios_Execute);
            ShowPDFHelpCommand = new DelegateCommand(_showPDFHelpCommand_execute);
            PauseCommand = new DelegateCommand(_pauseCommandExecute, _pauseCommandCanExecute);
            ResumeCommand = new DelegateCommand(_resumeCommandExecute, _resumeCommandCanExecute);
            CopyLinesCommand = new DelegateCommand(_copyCommand_execute);
            SelectAllCommand = new DelegateCommand(_selectAllCommand_execute);
        }

        private void _selectAllCommand_execute()
        {
            foreach(NewDialogLineEventArgs line in DialogLinesCollection)
            {
                line.Selected = true;
            }
        }

        private void _copyCommand_execute()
        {
            CopyAndClear();
        }

        private bool _resumeCommandCanExecute()
        {
            return mDialogEngine.PauseCancellationTokenSource != null;
        }

        private void _resumeCommandExecute()
        {
            mDialogEngine.PauseCancellationTokenSource.Cancel();
            mLogger.Info("Dialog View - (Button Click) Dialog engine resumed");
        }

        private bool _pauseCommandCanExecute()
        {
            return mDialogEngine.Running;                       
        }

        private void _pauseCommandExecute()
        {
            mDialogEngine.PauseCancellationTokenSource = new CancellationTokenSource();
            CanPause = false;
            mLogger.Info("Dialog View - (Button Click) Pause Requested");
        }

        private void _showPDFHelpCommand_execute()
        {
            try
            {
                Process.Start(Path.Combine(ApplicationData.Instance.TutorialDirectory, ApplicationData.Instance.TutorialFileName));
            }
            catch (System.Exception ex)
            {
                mLogger.Error(ex.Message);
            }
        }

        private void _toggleAssignedRadios_Execute()
        {
            mMessageDialogService.ShowDedicatedDialogAsync<int?>(new AssignCharacterToRadioView(), "ContentDialogHost");            
        }

        private bool _expertMode_CanExecute()
        {
            return mWizardRepository.GetAll().Count > 0;
        }

        private async void _expertModeExecute()
        {
            CreateCharacterViewModel createCharacterViewModel = Session.Get(Constants.CREATE_CHARACTER_VIEW_MODEL) as CreateCharacterViewModel;
            if(createCharacterViewModel != null)
            {
                var _lastWizardState = Session.Get<CreateCharacterState>(Constants.LAST_WIZARD_STATE);
                if (_lastWizardState != null && _lastWizardState.Wizard != null)
                {
                    createCharacterViewModel.Workflow.Fire(Triggers.CheckCounter);
                } else
                {
                    string _nextWizardName = createCharacterViewModel.NextWizardName;
                    if (!_nextWizardName.Equals("Finished"))
                    {
                        if (await mMessageDialogService.ShowOKCancelDialogAsync(String.Format("You are ready to add more lines using {0} wizard.", _nextWizardName), "Info") == MessageDialogResult.OK)
                        {
                            createCharacterViewModel.Workflow.Fire(Triggers.CheckCounter);
                        }
                        else
                        {
                            Session.Set(Constants.LAST_WIZARD_STATE, null);

                            createCharacterViewModel.Workflow.Fire(Triggers.Finish);
                        }
                    }
                    else
                    {
                        await mMessageDialogService.ShowMessage("INFO", "You have successfully completed the guided character creation!");
                        createCharacterViewModel.Workflow.Fire(Triggers.Finish);
                    }
                }
                
            } else
            {
                mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("CreateCharacterView");
            }
            
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
                if(mDialogEngine.PauseCancellationTokenSource != null)
                {
                    mDialogEngine.PauseCancellationTokenSource.Cancel();
                }

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
                if(mWizardRepository.GetAll().Count == 0 || mDialogModelRepository.GetAll().Count == 0)
                {
                    await mMessageDialogService.ShowMessage("Error", "There has to be a problem with your installation. Your data folder is empty. Please close the application and re-install it properly!");
                    return;                    
                }

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
            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(new SettingsDialog(mEventAggregator, mDialogModelRepository, mLogger));
            if(ApplicationData.Instance.DebugModeOn)
            {
                IsDebugViewVisible = Visibility.Visible;
            } else
            {
                IsDebugViewVisible = Visibility.Collapsed;
            }
        }

        private async void _configureDialogCommand_Execute()
        {
            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(new SettingsDialog(mEventAggregator, mDialogModelRepository, mLogger));
        }

        private async void _startDialogCommand_Execute()
        {
            try
            {
                IsDialogStarted = true;
                IsStopBtnEnabled = true;
                mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Publish(true);
                mLogger.Info("_startDialogCommand_Execute start thread StartDialogEngine");
                await mDialogEngine.StartDialogEngine();
                mLogger.Info("_startDialogCommand_Execute end thread StartDialogEngine");

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
            //_processDialogItem(info);
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

            _dispatchLineToCharacter(item);
        }

        private void _dispatchLineToCharacter(object item)
        {
            NewDialogLineEventArgs args = (NewDialogLineEventArgs)item;

            if(FirstSelectedCharacter != null)
            {
                if (args.Character.CharacterPrefix.Equals(FirstSelectedCharacter.CharacterPrefix))
                {
                    FirstCharacterDialogLine = args.DialogLine;
                }
            }            

            if(SecondSelectedCharacter != null)
            {
                if (args.Character.CharacterPrefix.Equals(SecondSelectedCharacter.CharacterPrefix))
                {
                    SecondCharacterDialogLine = args.DialogLine;
                }
            }
            

        }

        #endregion

        #region - properties -
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
                RaisePropertyChanged("CreateButtonName");
            }
        }

        public string CreateButtonName
        {
            get
            {
                return CanGoBackToWizard ? "Next" : "Create";
            }
        }

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
