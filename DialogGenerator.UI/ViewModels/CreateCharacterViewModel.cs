﻿using DialogGenerator.CharacterSelection;
using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Workflow.CreateCharacterWorkflow;
using DialogGenerator.Utilities;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class CreateCharacterViewModel : BindableBase
    {
        private string mCharacterName = string.Empty;
        private string mCharacterInitials = string.Empty;
        private string mCharacterIdentifier = string.Empty;
        private string mCharacterImage = string.Empty;
        private string mCharacterGender = "Female";
        private string mCharacterAuthor = string.Empty;
        private string mCharacterDescription = string.Empty;
        private string mCharacterNote = string.Empty;
        private int mCharacterAge = 10;
        private int mWizardPassthroughIndex = 0;
        private List<string> mDialogWizards = new List<string>();
        private List<int> mAgesCollection = new List<int>();
        private CreateCharacterWizard mWizard;
        private CreateCharacterWizardStep mCurrentStep = null;
        private int mCurrentStepIndex = 0;
        private ILogger mLogger;
        private IEventAggregator mEventAgregator;
        private ICharacterDataProvider mCharacterDataProvider;
        private IWizardDataProvider mWizardDataProvider;
        private IRegionManager mRegionManager;
        private IMessageDialogService mMessageDialogService;
        private string mCurrentDialogWizard = String.Empty;
        private string mNextButtonText = "Next";
        private IBLEDataProviderFactory mBLEDataProviderFactory;
        private IBLEDataProvider mCurrentDataProvider;
        private CancellationTokenSource mCancellationTokenSource;
        private string mSelectRadioTitle = Properties.Resources.ShakeRadio;
        private bool mHasNoVoice = false;
        private string mVoice = string.Empty;
        private int mSpeechRate = -1;

        private string mCharacterNameValidationError = string.Empty;
        private bool mCharacterNameHasError = false;
        private ICharacterRadioBindingRepository mCharacterRadionBindingRepository;

        private bool mResumePreviousSession = false;

        internal void SetCurrentStep(int index)
        {
            Workflow.Fire((Triggers)index);
        }

        private List<ToyEntry> mRadiosCollection = new List<ToyEntry>();
        private ToyEntry mSelectedRadio;

        public CreateCharacterViewModel(ILogger _logger,
            IEventAggregator _eventAggregator,
            ICharacterDataProvider _characterDataProvider,
            IRegionManager _regionManager,
            IMessageDialogService _messageDialogService,
            IBLEDataProviderFactory _BLEDataProviderFactory,
            IWizardDataProvider _WizardDataProvider,
            ICharacterRadioBindingRepository _CharacterRadioBindingRepository)
        {
            mLogger = _logger;
            mEventAgregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;
            mRegionManager = _regionManager;
            mMessageDialogService = _messageDialogService;
            mBLEDataProviderFactory = _BLEDataProviderFactory;
            mWizardDataProvider = _WizardDataProvider;
            mCharacterRadionBindingRepository = _CharacterRadioBindingRepository;

            mWizard = new CreateCharacterWizard();
            CurrentStep = mWizard.Steps[mCurrentStepIndex];            

            for (int i = 5; i < 100; i++)
            {
                mAgesCollection.Add(i);
            }

            mRadiosCollection.Add(new ToyEntry
            {
                Key = -1,
                Value = "Unassigned"
            });

            for (int i = 0; i < 6; i++)
            {
                mRadiosCollection.Add(new ToyEntry
                {
                    Key = i,
                    Value = i.ToString()
                });
            }

            mSelectedRadio = mRadiosCollection.FirstOrDefault(p => p.Key == -1);

            Workflow = new CreateCharacterWorkflow(action: () => { });
            Workflow.PropertyChanged += _workflow_PropertyChanged;
            _configureWorkflow();

            Character = new Character();   

            _initDialogWizards();
            _initVoiceCollection();            

            _bindCommands();
        }

        public CreateCharacterState State { get; set; } = new CreateCharacterState();

        public List<ToyEntry> RadiosCollection
        {
            get
            {
                return mRadiosCollection;
            }
        }

        public ToyEntry SelectedRadio
        {
            get
            {
                return mSelectedRadio;
            }

            set
            {
                int _oldVal = mSelectedRadio.Key;
                mSelectedRadio = value;
                _selectToyToCharacter(_oldVal);
                if(mSelectedRadio.Key == -1)
                {
                    SelectRadioTitle = Properties.Resources.ShakeRadio;
                } else
                {
                    SelectRadioTitle = Properties.Resources.RadioAttached;
                }

                RaisePropertyChanged();
            }
        }

        public string SelectRadioTitle
        {
            get
            {
                return mSelectRadioTitle;
            }

            set
            {
                mSelectRadioTitle = value;
                RaisePropertyChanged();
            }
        }       

        public int WizardPassthroughIndex
        {
            get
            {
                return mWizardPassthroughIndex;
            }

            set
            {
                mWizardPassthroughIndex = value;
                RaisePropertyChanged();
            }
        }

        public CreateCharacterWorkflow Workflow { get; set; }

        public Character Character { get; set; }

        public string CurrentDialogWizard
        {
            get
            {
                return mCurrentDialogWizard;
            }

            set
            {
                mCurrentDialogWizard = value;
                RaisePropertyChanged();
            }
        }
        public string CharacterName
        {
            get
            {
                return mCharacterName;
            }

            set
            {
                mCharacterName = value;
                RaisePropertyChanged("CharacterName");
                CharacterInitials = _getCharacterInitials();
                CharacterIdentifier = _getCharacterIdentifier();
                NextStepCommand.RaiseCanExecuteChanged();

                if (!string.IsNullOrEmpty(mCharacterName) 
                    &&  (mCharacterName.Length <= 2 
                    || mCharacterName.Length > 30 
                    || char.IsDigit(mCharacterName.Substring(0,1).ToCharArray()[0]) 
                    || !Regex.IsMatch(mCharacterName, Constants.FILENAME_CHECK_REGEX)))
                {
                    if(!Regex.IsMatch(mCharacterName, Constants.FILENAME_CHECK_REGEX))
                    {
                        CharacterNameValidationError = "The name contains the illegal characters!";
                        CharacterNameHasError = true;
                    }
                    else if (mCharacterName.Length <= 2 && !char.IsDigit(mCharacterName.Substring(0, 1).ToCharArray()[0]))
                    {
                        CharacterNameValidationError = "The name must consist of at least 3 characters!";
                        CharacterNameHasError = true;
                    } else if (mCharacterName.Length > 30 ) {
                        CharacterNameValidationError = "The name must not have more than 30 characters!";
                        CharacterNameHasError = true;
                    }                    
                    else
                    {
                        CharacterNameValidationError = "The first character of the name must be a letter!";
                        CharacterNameHasError = true;
                    }
                    
                } else
                {
                    CharacterNameValidationError = string.Empty;
                    CharacterNameHasError = false;                 
                    
                }
            }
        }

        public string CharacterNameValidationError
        {
            get
            {
                return mCharacterNameValidationError;
            }

            set
            {
                mCharacterNameValidationError = value;
                RaisePropertyChanged();
            }
        }

        public bool CharacterNameHasError
        {
            get
            {
                return mCharacterNameHasError;
            }

            set
            {
                mCharacterNameHasError = value;
                RaisePropertyChanged();
            }
        }

        public string CharacterInitials
        {
            get
            {
                return mCharacterInitials;
            }

            set
            {
                mCharacterInitials = value;
                RaisePropertyChanged();
            }
        }

        public string CharacterIdentifier
        {
            get
            {
                return mCharacterIdentifier;
            }

            set
            {
                mCharacterIdentifier = value;
                RaisePropertyChanged();
            }
        }

        public string CharacterPrefix
        {
            get
            {
                return CharacterInitials + (!String.IsNullOrEmpty(CharacterIdentifier) ? "_" + CharacterIdentifier : "");
            }
        }

        public int CharacterAge
        {
            get
            {
                return mCharacterAge;
            }

            set
            {
                mCharacterAge = value;
                RaisePropertyChanged();
            }
        }

        public String CharacterGender
        {
            get
            {
                return mCharacterGender;
            }

            set
            {
                mCharacterGender = value;
                RaisePropertyChanged();
            }
        }

        public string CharacterImage
        {
            get
            {
                return mCharacterImage;
            }

            set
            {
                mCharacterImage = value;
                RaisePropertyChanged();
            }
        }

        public string CharacterAuthor
        {
            get
            {
                return mCharacterAuthor;
            }

            set
            {
                mCharacterAuthor = value;
                RaisePropertyChanged();
            }
        }

        public string CharacterDescription
        {
            get => mCharacterDescription;
            set
            {
                mCharacterDescription = value;
                RaisePropertyChanged();
            }
        }

        public string CharacterNote
        {
            get => mCharacterNote;
            set
            {
                mCharacterNote = value;
                RaisePropertyChanged();
            }
        }

        public bool CharacterHasNoVoice
        {
            get
            {
                return mHasNoVoice;
            }

            set
            {
                mHasNoVoice = value;
                RaisePropertyChanged();
            }
        }

        public string CharacterVoice
        {
            get
            {
                return mVoice;
            }

            set
            {
                mVoice = value;
                RaisePropertyChanged();
            }
        }

        public int CharacterSpeechRate
        {
            get
            {
                return mSpeechRate;
            }

            set
            {
                mSpeechRate = value;
                RaisePropertyChanged();
            }
        }

        public List<string> VoiceCollection { get; set; } = new List<string>();


        public List<int> AgesCollection
        {
            get
            {
                return mAgesCollection;
            }
        }

        public List<string> Genders
        {
            get
            {
                List<string> genders = new List<string>();
                genders.Add("Female");
                genders.Add("Male");

                return genders;
            }
        }

        public List<string> Steps
        {
            get
            {
                var query = mWizard.Steps.Select((step, index) => "Step " + step.StepIndex + " - " + step.StepName);
                var results = new List<string>();
                results.AddRange(query);
                return results;
            }
        }

        public int CurrentStepIndex
        {
            get
            {
                return mCurrentStepIndex;
            }

            set
            {
                mCurrentStepIndex = value;
                RaisePropertyChanged("CurrentStepIndex");
                if(mWizard.Steps.Count > mCurrentStepIndex)
                {
                    CurrentStep = mWizard.Steps[mCurrentStepIndex];                    
                }                
            }
        }

        public CreateCharacterWizardStep CurrentStep
        {
            get
            {
                return mCurrentStep;
            }

            set
            {
                mCurrentStep = value;
                //handleStepChange();
                RaisePropertyChanged();
            }
        }

        public string NextButtonText
        {
            get
            {
                return mNextButtonText;
            }

            set
            {
                mNextButtonText = value;
                RaisePropertyChanged();
            }
        }

        public string NextWizardName
        {
            get
            {
                string _nextWizardName = "Finished";
                if(mWizardPassthroughIndex < mDialogWizards.Count)
                {
                    _nextWizardName = mDialogWizards[mWizardPassthroughIndex];
                }

                return _nextWizardName;
            }
        }

        public bool NextEnabled
        {
            get
            {
                return !string.IsNullOrEmpty(CharacterName);
            }
        }

        public ICommand ChooseImageCommand { get; set; }
        public ICommand HomeCommand { get; set; }
        public ICommand CreateCommand { get; set; }
        public ICommand CancelCommand {get;set;}
        public ICommand ResetCommand { get; set; }
        public ICommand PlayCommand { get; set; }
        public ICommand ViewLoadedCommand { get; set; }
        public ICommand ViewUnloadedCommand { get; set; }
        public DelegateCommand NextStepCommand { get; set; }
        public DelegateCommand PreviewPlayCommand { get; set; }

        public void nextStep()
        {
            int _currentStepIndex = CurrentStepIndex;
            Workflow.Fire((Triggers) (++_currentStepIndex));

            if (mWizard.Steps.Count == _currentStepIndex)
            {
                mLogger.Debug($"Create character view - Saving the initial character info of '{ mCharacterName }'. Moving on to the 'Basic Wizard'.");
            }
            else
            {
                var _stepName = mWizard.Steps[_currentStepIndex].StepName;
                mLogger.Debug($"Create character view - Advanced to '{_stepName}' step.");
            }
        }

        public void previousStep()
        {
            if(CurrentStepIndex > 0)
            {
                int _currentStepIndex = CurrentStepIndex;
                Workflow.Fire((Triggers) (--_currentStepIndex));
                
                var _stepName = mWizard.Steps[_currentStepIndex].StepName;
                mLogger.Debug($"Create character view - Back to '{_stepName}' step.");
            }            
        }

        private async void _processFinish()
        {            
            int _idx = mCharacterDataProvider.IndexOf(Character);
            if(_idx == -1)
            {
                await mCharacterDataProvider.AddAsync(Character);
                _idx = mCharacterDataProvider.IndexOf(Character);
                Session.Set(Constants.NEXT_CH_1, _idx);
                mEventAgregator.GetEvent<CharacterCollectionLoadedEvent>().Publish();
            }

            _initEntries();
            //Session.Set(Constants.LAST_WIZARD_STATE, new CreateCharacterState
            //{
            //    WizardName = string.Empty,
            //    StepIndex = 0,
            //    CharacterPrefix = string.Empty
            //});
            Character = new Character();
            Workflow.Fire(Triggers.SetName);
                        
        }

        private async void _selectToyToCharacter(int oldVal = -1)
        {
            if (mSelectedRadio.Key == -1)
                return;

            var _oldChars = mCharacterDataProvider.GetAll().Where(c => c.RadioNum == mSelectedRadio.Key);
            if (_oldChars.Count() > 0)
            {
                var _oldChar = _oldChars.First();
                _oldChar = mCharacterDataProvider.GetAll().Where(c => c.RadioNum == mSelectedRadio.Key).First();
                if (_oldChar != null)
                {
                    // izbaci message box
                    MessageDialogResult result = await mMessageDialogService.ShowOKCancelDialogAsync(String.Format("The toy with index {0} is assigned to character {1}. Are You sure that you want to re-asign it?", mSelectedRadio.Key, _oldChar.CharacterName), "Check");
                    if (result == MessageDialogResult.OK)
                    {
                        // settuj na Unassigned ako je Yes
                        _oldChar.RadioNum = -1;
                        await mCharacterDataProvider.SaveAsync(_oldChar);
                    }
                    else
                    {
                        mSelectedRadio = mRadiosCollection.First(p => p.Key == oldVal);
                        RaisePropertyChanged("SelectedRadio");
                        SelectRadioTitle = Properties.Resources.ShakeRadio;
                    }
                }
            }

            Character.RadioNum = mSelectedRadio.Key;
        }

        private void _initVoiceCollection()
        {

            using (var _synth = new SpeechSynthesizer())
            {
                foreach (var _installedVoice in _synth.GetInstalledVoices())
                {
                    VoiceCollection.Add(_installedVoice.VoiceInfo.Name);
                }

                if (VoiceCollection.Count > 0)
                {
                    CharacterVoice = VoiceCollection[0];
                }

            }

        }

        private void _initEntries()
        {
            CharacterName = string.Empty;
            CharacterInitials = string.Empty;
            CharacterIdentifier = string.Empty;
            CharacterAge = 10;
            CharacterGender = "Male";
            CharacterImage = "Avatar.png";
            CharacterAuthor = string.Empty;
            CharacterDescription = string.Empty;
            CharacterHasNoVoice = false;
            if(VoiceCollection.Count > 0)
            {
                CharacterVoice = VoiceCollection[0];
            }
            
            CurrentStepIndex = 0;
            NextButtonText = "Next";
            WizardPassthroughIndex = 0;
            SelectedRadio = RadiosCollection[0];

            Character = new Character();
            
            _closeCreateSession();
        }

        private string _getCharacterInitials()
        {
            if (String.IsNullOrEmpty(mCharacterName) || mCharacterName.Length <= 2)
                return String.Empty;

            String[] _tokens = mCharacterName.Split(' ');
            List<string> _nonEmptyTokens = _tokens.Where(t => !string.IsNullOrEmpty(t)).ToList();
            String result = String.Empty;
            switch(_nonEmptyTokens.Count)
            {
                case 0:
                    return result;
                case 1:
                    {
                        int _range = Math.Min(3, _nonEmptyTokens[0].Length);
                        result = _nonEmptyTokens[0].Substring(0, _range);
                    }

                    break;
                case 2:
                    {
                        int _range = Math.Min(_nonEmptyTokens[0].Length, 2);
                        result = _nonEmptyTokens[0].Substring(0, _range);
                        int _leftover = 3 - _range;
                        _range = Math.Min(_leftover, _nonEmptyTokens[1].Length);
                        result += _nonEmptyTokens[1].Substring(0, _range);
                    }
                    
                    break;
                default:
                    {
                        int _max = 5;
                        int _counter = 1;
                        foreach (var _token in _nonEmptyTokens)
                        {
                            result += _token.Substring(0, 1);
                            if (_counter == _max)
                                break;
                            _counter++;
                        }
                    }
                    
                    break;
            }           

            return result.ToUpper();
        }

        private string _getCharacterIdentifier()
        {
            if(!String.IsNullOrEmpty(CharacterIdentifier))
            {
                return CharacterIdentifier;
            }

            string _identifier = Guid.NewGuid().ToString();
            _identifier = _identifier.Substring(0, 4);
            return _identifier;
        }

        private void _bindCommands()
        {
            ChooseImageCommand = new DelegateCommand(_onChooseImage_execute);
            HomeCommand = new DelegateCommand(_onHomeCommand_execute);
            CreateCommand = new DelegateCommand(_onCreateCommand_execute);
            CancelCommand = new DelegateCommand(_onCancelCommand_execute);
            ResetCommand = new DelegateCommand(_onResetCommand_execute);
            PlayCommand = new DelegateCommand(_onPlayCommand_execute);
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_execute);
            ViewUnloadedCommand = new DelegateCommand(_viewUnloaded_execute);
            NextStepCommand = new DelegateCommand(_nextStep_Execute, _nextStep_CanExecute);
            PreviewPlayCommand = new DelegateCommand(_previewPlayCommand);
        }

        private void _previewPlayCommand()
        {
            using (var _synth = new SpeechSynthesizer())
            {
                _synth.SelectVoice(CharacterVoice);
                _synth.Rate = -1;
                _synth.Volume = 100;
                _synth.Speak(CharacterVoice);
            }
        }

        private bool _nextStep_CanExecute()
        {
            return !string.IsNullOrEmpty(CharacterName) &&
                CharacterName.Length >= 3 &&
                CharacterName.Length <= 30 &&
                !char.IsDigit(mCharacterName.Substring(0, 1).ToCharArray()[0]) &&
                Regex.IsMatch(CharacterName, Constants.FILENAME_CHECK_REGEX);

        }

        private void _nextStep_Execute()
        {
            nextStep();
        }

        private async void _viewLoaded_execute()
        {
            await _checkWizardConfiguration();
            Workflow.Fire(Triggers.Initialize);
            
            mLogger.Debug($"Create Character View - Guided character creation loaded!");
        }

        private void _viewUnloaded_execute()
        {
            mLogger.Debug($"Create Character View - Guided character creation exited!");
        }
        
        private void _configureWorkflow()
        {
            Workflow.Configure(States.EnteredInitialization)
                .OnEntry(() => _stepEntered("Initialize"))
                .PermitReentry(Triggers.Initialize)
                .Permit(Triggers.SetName, States.EnteredSetName)
                .Permit(Triggers.CheckCounter, States.InCounter);

            Workflow.Configure(States.EnteredSetName)
                 .OnEntry(() => _stepEntered("Name"))
                 .OnExit(() => _stepExited("Name"))
                 .PermitReentry(Triggers.SetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
                 .Permit(Triggers.SetAuthor, States.EnteredSetAuthor)
                 .Permit(Triggers.SetDescription, States.EnteredSetDescription)
                 .Permit(Triggers.SetNote, States.EnteredSetNote)
                 
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished)
                 .Permit(Triggers.Initialize, States.EnteredInitialization);

            Workflow.Configure(States.EnteredSetInitials)
                 .OnEntry(() => _stepEntered("Initials"))
                 .OnExit(() => _stepExited("Initials"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
                 .Permit(Triggers.SetAuthor, States.EnteredSetAuthor)
                 .Permit(Triggers.SetDescription, States.EnteredSetDescription)
                 .Permit(Triggers.SetNote, States.EnteredSetNote)
                 .Permit(Triggers.CheckCounter, States.InCounter)
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetAge)
                 .OnEntry(() => _stepEntered("Age"))
                 .OnExit(() => _stepExited("Age"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
                 .Permit(Triggers.SetAuthor, States.EnteredSetAuthor)
                 .Permit(Triggers.SetDescription, States.EnteredSetDescription)
                 .Permit(Triggers.SetNote, States.EnteredSetNote)
                 .Permit(Triggers.CheckCounter, States.InCounter)
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetGender)
                 .OnEntry(() => _stepEntered("Gender"))
                 .OnExit(() => _stepExited("Gender"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
                 .Permit(Triggers.SetAuthor, States.EnteredSetAuthor)
                 .Permit(Triggers.SetDescription, States.EnteredSetDescription)
                 .Permit(Triggers.SetNote, States.EnteredSetNote)
                 .Permit(Triggers.CheckCounter, States.InCounter)
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetAssignToy)
                 .OnEntry(() => _stepEntered("AssignToy"))
                 .OnExit(() => _stepExited("AssignToy"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.SetAuthor, States.EnteredSetAuthor)
                 .Permit(Triggers.SetDescription, States.EnteredSetDescription)
                 .Permit(Triggers.SetNote, States.EnteredSetNote)
                 .Permit(Triggers.CheckCounter, States.InCounter)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetAvatar)
                 .OnEntry(() => _stepEntered("Avatar"))
                 .OnExit(() => _stepExited("Avatar"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
                 .Permit(Triggers.SetAuthor, States.EnteredSetAuthor)
                 .Permit(Triggers.SetDescription, States.EnteredSetDescription)
                 .Permit(Triggers.SetNote, States.EnteredSetNote)
                 .Permit(Triggers.CheckCounter, States.InCounter)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetAuthor)
                 .OnEntry(() => _stepEntered("Author"))
                 .OnExit(() => _stepExited("Author"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.SetDescription, States.EnteredSetDescription)
                 .Permit(Triggers.SetNote, States.EnteredSetNote)
                 .Permit(Triggers.CheckCounter, States.InCounter)
                 .Permit(Triggers.Finish, States.Finished);
            
            Workflow.Configure(States.EnteredSetDescription)
                .OnEntry(() => _stepEntered("Description"))
                .OnExit(() => _stepExited("Description"))
                .Permit(Triggers.SetName, States.EnteredSetName)
                .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                .Permit(Triggers.SetAge, States.EnteredSetAge)
                .Permit(Triggers.SetGender, States.EnteredSetGender)
                .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
                .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                .Permit(Triggers.SetAuthor, States.EnteredSetAuthor)
                .Permit(Triggers.SetNote, States.EnteredSetNote)
                .Permit(Triggers.CheckCounter, States.InCounter)
                .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetNote)
               .OnEntry(() => _stepEntered("Note"))
               .OnExit(() => _stepExited("Note"))
               .Permit(Triggers.SetName, States.EnteredSetName)
               .Permit(Triggers.SetInitials, States.EnteredSetInitials)
               .Permit(Triggers.SetAge, States.EnteredSetAge)
               .Permit(Triggers.SetGender, States.EnteredSetGender)
               .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
               .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
               .Permit(Triggers.SetDescription, States.EnteredSetDescription)
               .Permit(Triggers.CheckCounter, States.InCounter)
               .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.InCounter)
                  .OnEntry(() => _stepEntered("CheckCounter"))
                  .Permit(Triggers.StartWizard, States.InWizard)
                  .Permit(Triggers.Initialize, States.EnteredInitialization)
                  .Permit(Triggers.Finish, States.Finished);
                    

            Workflow.Configure(States.InWizard)
                .OnEntry(() => _stepEntered("Wizard"))
                .Permit(Triggers.GoPlay, States.Playing)
                .Permit(Triggers.CheckCounter, States.InCounter)
                .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.Playing)
                .OnEntry(() => _stepEntered("Play"))
                .Permit(Triggers.CheckCounter, States.InCounter)
                .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.Finished)
                .OnEntry(() => _stepEntered("Finished"))
                .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                .Permit(Triggers.SetName, States.EnteredSetName)
                .Permit(Triggers.Initialize, States.EnteredInitialization);
            
        }

        private void _stepExited(string stepName)
        {
            switch (stepName)
            {
                case "Name":
                    Character.CharacterName = CharacterName;
                    Character.HasNoVoice = CharacterHasNoVoice;
                    if(CharacterHasNoVoice)
                    {
                        Character.Voice = CharacterVoice;
                    }

                    Character.SpeechRate = CharacterSpeechRate;
                    break;
                case "Initials":
                    Character.CharacterPrefix = CharacterPrefix;
                    break;
                case "Age":
                    Character.CharacterAge = CharacterAge;
                    break;
                case "Gender":                    
                    Character.CharacterGender = CharacterGender.Substring(0, 1);
                    break;
                case "Avatar":
                    Character.CharacterImage = CharacterImage;
                    break;
                case "Author":
                    Character.Author = CharacterAuthor;
                    NextButtonText = "Next";
                    break;
                case "Description":
                    Character.Description = CharacterDescription;
                    NextButtonText = "Next";
                    break;
                case "Note":
                    Character.InternalRemarks = CharacterNote;
                    break;
                case "AssignToy":
                    if(Session.Get<bool>(Constants.BLE_MODE_ON))
                    {
                        Character.RadioNum = SelectedRadio.Key;
                        mCharacterRadionBindingRepository.AttachRadioToCharacter(SelectedRadio.Key, Character.CharacterPrefix);
                        _stopScanForRadios();
                    }                    
                    
                    break;
                case "Wizard":
                    

                    break;
                default:
                    break;
            }
        }        

        private async void _stepEntered(string stepName)
        {
            switch(stepName)
            {
                case "Initialize":
                    var _lastWizardState = Session.Get<CreateCharacterState>(Constants.LAST_WIZARD_STATE);
                    if (_lastWizardState != null && _lastWizardState.Wizard != null)
                    {
                        MessageDialogResult _result = await mMessageDialogService.ShowOKCancelDialogAsync("Resume previous session?", "Question", "Yes", "No");
                        if(_result.Equals(MessageDialogResult.OK))
                        {
                            mResumePreviousSession = true;
                            Character = mCharacterDataProvider.GetByInitials(_lastWizardState.CharacterPrefix);
                            Workflow.Fire(Triggers.CheckCounter);
                            break;
                        } else
                        {
                            Session.Set(Constants.LAST_WIZARD_STATE, null);
                        }                                                
                    }

                    Workflow.Fire(Triggers.SetName);

                    break;
                case "Name":                    
                    CurrentStepIndex = 0;                    
                    CharacterName = Character.CharacterName;
                    CharacterHasNoVoice = Character.HasNoVoice;
                    if(!string.IsNullOrEmpty(Character.Voice))
                    {
                        CharacterVoice = CharacterVoice;
                    }

                    CharacterSpeechRate = Character.SpeechRate;

                    break;
                case "Initials":
                    CurrentStepIndex = 1;
                    break;
                case "Age":
                    CurrentStepIndex = 2;
                    CharacterAge = Character.CharacterAge;
                    break;
                case "Gender":
                    CurrentStepIndex = 3;
                    CharacterGender = Character.CharacterGender.Equals("M") ? "Male" : "Female";
                    break;
                case "Avatar":
                    CurrentStepIndex = 4;
                    CharacterImage = Character.CharacterImage;
                    break;
                case "AssignToy":
                    int _oldIndex = CurrentStepIndex;
                    CurrentStepIndex = 5;
                    if(Session.Get<bool>(Constants.BLE_MODE_ON) == false)
                    {
                        if(_oldIndex < CurrentStepIndex)
                        {
                            Workflow.Fire(Triggers.SetAuthor);
                        } else
                        {
                            Workflow.Fire(Triggers.SetAvatar);
                        }
                        
                    } else
                    {
                        string _radioText = Character.RadioNum == -1 ? "Unasigned" : Character.RadioNum.ToString();
                        var _selectedRadio = RadiosCollection.First(r => r.Key == Character.RadioNum);
                        SelectedRadio = _selectedRadio;

                        _startScanForRadios();
                    }
                    
                    break;
                case "Author":
                    CurrentStepIndex = 6;
                    CharacterAuthor = Character.Author;
                    NextButtonText = "Next";
                    break;
                case "Description":
                    CurrentStepIndex = 7;
                    CharacterDescription = Character.Description;
                    NextButtonText = "Next";
                    break;
                case "Note":
                    CurrentStepIndex = 8;
                    CharacterNote = Character.InternalRemarks;
                    NextButtonText = "Save";
                    break;
                case "CheckCounter":
                    _lastWizardState = Session.Get<CreateCharacterState>(Constants.LAST_WIZARD_STATE);
                    if(_lastWizardState != null && _lastWizardState.Wizard != null)                    
                    {
                        MessageDialogResult _result;
                        if (!mResumePreviousSession)
                        {                            
                           _result = await mMessageDialogService.ShowOKCancelDialogAsync("Resume previous session?", "Question", "Yes", "No");
                        } else
                        {
                            mResumePreviousSession = false;
                            _result = MessageDialogResult.OK;
                        }
                        
                        if(_result.Equals(MessageDialogResult.OK))
                        {
 
                            CurrentDialogWizard = _lastWizardState.Wizard.WizardName;
                            Workflow.Fire(Triggers.StartWizard);                            
                        } else
                        {
                            Session.Set(Constants.LAST_WIZARD_STATE, null);
                            Workflow.Fire(Triggers.Finish);
                            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("CreateCharacterView");
                            Workflow.Fire(Triggers.Initialize);                            
                        }

                        break;                        
                    }

                    if (mWizardPassthroughIndex < mDialogWizards.Count)
                    {
                        CurrentDialogWizard = mDialogWizards[/* mWizardPassthroughIndex++ */ mWizardPassthroughIndex];
                        Workflow.Fire(Triggers.StartWizard);
                    }
                    else
                    {
                        Workflow.Fire(Triggers.Finish);
                    }                                       
                    
                    break;
                case "Wizard":
                    // Prepare steps for starting of the dialog model wizard in 
                    // character creation mode.
                    if(!_checkCharacterCreateMode())
                    {
                        _openCreateSession(Character);                        
                    }

                    mLogger.Debug($"Create character view - Entering wizard");

                    // Start the wizard.
                    mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("WizardView");
                    break;
                case "Play":
                    // Prepare character to be shown on 'play'.
                    int _idx = mCharacterDataProvider.IndexOf(Character);
                    if (_idx == -1)
                    {
                        // Add the character to the collection.
                        await mCharacterDataProvider.AddAsync(Character);

                        // Get the index of new character.
                        _idx = mCharacterDataProvider.IndexOf(Character);
                        Session.Set(Constants.NEXT_CH_1, _idx);

                        // Notify all interested parties that the collection has new element (has changed).
                        mEventAgregator.GetEvent<CharacterCollectionLoadedEvent>().Publish();
                        
                        // If the character has the radio assigned notify the interested parties.
                        if (Character.RadioNum != -1)
                        {                            
                            mEventAgregator.GetEvent<RadioAssignedEvent>().Publish(Character.RadioNum);
                        }

                        // Reset the conversation in the case of the new character.
                        //Session.Set(Constants.NEXT_CH_1, -1);
                        //Session.Set(Constants.NEXT_CH_2, -1);

                    }

                    if(Session.Get<bool>(Constants.BLE_MODE_ON))
                    {
                        Session.Set(Constants.NEXT_CH_1, _idx);
                        int _idx2 = Session.Get<int>(Constants.NEXT_CH_2);
                        mEventAgregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();

                        if (_idx != -1 && _idx2 != -1)
                        {
                            mEventAgregator.GetEvent<SelectedCharactersPairChangedEvent>().
                            Publish(new SelectedCharactersPairEventArgs
                            {
                                Character1Index = _idx,
                                Character2Index = _idx2
                            });
                        }
                        else
                        {
                            mEventAgregator.GetEvent<SelectedCharactersPairChangedEvent>()
                                .Publish(null);
                        }
                    }

                    // Check if we have reached the end?
                    if (mWizardPassthroughIndex == mDialogWizards.Count)
                    {
                        await mMessageDialogService.ShowMessage("INFO", "You have successfully completed the guided character creation process!");
                        Workflow.Fire(Triggers.Finish);
                    }

                    mLogger.Debug($"Create character view - Entering play mode");

                    // Call the play window.
                    mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("DialogView");
                    break;
                case "Finished":
                    mLogger.Debug($"Create character view - Entering finish");
                    _processFinish();                    
                    break;
                default:
                    break;
            }
        }

        private async void _startScanForRadios()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mCurrentDataProvider = mBLEDataProviderFactory.Create(BLEDataProviderType.WinBLEWatcher);
            await Task.Run(async () =>
            {
                Thread.CurrentThread.Name = "StartCharacterMovingDetection";                
                Task _BLEDataReaderTask = mCurrentDataProvider.StartReadingData();
                int _oldIndex = -1;
                do
                {
                    // Read messages
                    BLE_Message message = mCurrentDataProvider.GetMessage();
                    if(message != null)
                    {
                        int _radioIndex = -1;
                        string outData = String.Empty;
                        for (int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                        {
                            if (message.msgArray[i] == 0xFF)
                            {
                                _radioIndex = i;
                            }
                            
                        }

                        // If motion vector is greater than zero, show message
                        int _motion = message.msgArray[ApplicationData.Instance.NumberOfRadios];
                        if(_motion > 10 && _radioIndex > -1)
                        {
                            if(_radioIndex != _oldIndex)
                            {
                                ToyEntry _tEntry = mRadiosCollection.FirstOrDefault(p => p.Key == _radioIndex);
                                if(_tEntry != null)
                                {
                                    SelectedRadio = _tEntry;
                                }

                                _oldIndex = _radioIndex;
                            }
                        }
                        
                    }

                    Thread.Sleep(1);
                } while (!mCancellationTokenSource.IsCancellationRequested);

                await _BLEDataReaderTask;

            });
        }

        private void _stopScanForRadios()
        {
            mCurrentDataProvider.StopReadingData();
            mCancellationTokenSource.Cancel();
        }
        
        private bool _checkCharacterCreateMode()
        {
            if(Session.Contains(Constants.CHARACTER_EDIT_MODE) && (bool) Session.Get(Constants.CHARACTER_EDIT_MODE) == true)
            {
                return true;
            }

            return false;
        }

        private void _initDialogWizards()
        {
            mDialogWizards.Clear();
            if(!_loadWizardCollection())
            {
                WizardCollection _wizardCollection = new WizardCollection
                {
                    Version = "1.1",                    
                };

                _wizardCollection.Version = "1.1";

                mDialogWizards.Add("BasicWizard");
                _wizardCollection.Wizards.Add("BasicWizard");
                mDialogWizards.Add("ResponsesToOtherStories");
                _wizardCollection.Wizards.Add("ResponsesToOtherStories");
                mDialogWizards.Add("SimpleStoryWizardA");
                _wizardCollection.Wizards.Add("SimpleStoryWizardA");
                mDialogWizards.Add("IntermediateWizard");
                _wizardCollection.Wizards.Add("IntermediateWizard");
                mDialogWizards.Add("ScaryStoryWizard");
                _wizardCollection.Wizards.Add("ScaryStoryWizard");
                mDialogWizards.Add("ActionStoryWizard");
                _wizardCollection.Wizards.Add("ActionStoryWizard");
                mDialogWizards.Add("SimpleStoryWizardB");
                _wizardCollection.Wizards.Add("SimpleStoryWizardB");                                          
                mDialogWizards.Add("Advanced1Wizard");
                _wizardCollection.Wizards.Add("Advanced1Wizard");
                mDialogWizards.Add("Advanced2Wizard");
                _wizardCollection.Wizards.Add("Advanced2Wizard");

                Serializer.Serialize(_wizardCollection, ApplicationData.Instance.DataDirectory + "\\WizardCollection.cfg");
            }            
            
        }

        private async Task _checkWizardConfiguration()
        {
            List<string> _wrongEntries = new List<string>();
            foreach (string _wName in mDialogWizards)
            {
                Wizard _w = mWizardDataProvider.GetByName(_wName);
                if (_w == null)
                {
                    await mMessageDialogService.ShowMessage("WARNING", String.Format("The wizard {0} doesn't exist in the collection of loaded wizards! It will be removed from configuration.", _wName));
                    _wrongEntries.Add(_wName);
                }
            }

            if (_wrongEntries.Count > 0)
            {
                foreach (string _entry in _wrongEntries)
                {
                    mDialogWizards.Remove(_entry);
                }
            }
        }

        private class WizardCollection
        {
            [JsonProperty("Version")]
            public string Version { get; set; }
            [JsonProperty("Wizards")]
            public ObservableCollection<string> Wizards { get; } = new ObservableCollection<string>();
        }

        private bool _loadWizardCollection()
        {
            string _filePath = ApplicationData.Instance.DataDirectory + "\\WizardCollection.cfg";
            try
            {
                using (var reader = new StreamReader(_filePath)) //creates new streamerader for fs stream. Could also construct with filename...
                {
                    string _jsonString = reader.ReadToEnd();

                    //json string to Object.
                    var _wizardCollection = Serializer.Deserialize<WizardCollection>(_jsonString);
                    if (_wizardCollection.Wizards != null && _wizardCollection.Wizards.Count() > 0)
                    {
                        foreach (string _wizard in _wizardCollection.Wizards)
                        {
                            mDialogWizards.Add(_wizard);
                        }

                        return true;
                    }
                                        
                }
            } catch (IOException)
            {
                return false;
            }
            
            return false;
        }

        private void _openCreateSession(Character _c = null)
        {
            if(_c == null)
            {
                Session.Set(Constants.NEW_CHARACTER, new Character());
            } else
            {
                Session.Set(Constants.NEW_CHARACTER, _c);
            }
            
            Session.Set(Constants.CHARACTER_EDIT_MODE, true);
            Session.Set(Constants.CREATE_CHARACTER_VIEW_MODEL, this);
            mEventAgregator.GetEvent<GuidedCharacterCreationModeChangedEvent>().Publish(true);
        }

        private void _closeCreateSession()
        {
            Session.Set(Constants.NEW_CHARACTER, null);
            Session.Set(Constants.CHARACTER_EDIT_MODE, false);
            Session.Set(Constants.CREATE_CHARACTER_VIEW_MODEL, null);            

            mEventAgregator.GetEvent<GuidedCharacterCreationModeChangedEvent>().Publish(false);
        }

        private void _onCreateCommand_execute()
        {
            _initEntries();
            Character = new Character();
            Workflow.Fire(Triggers.SetName);
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("CreateView");
        }

        private void _onResetCommand_execute()
        {
            _initEntries();
            Character = new Character();
            Workflow.Fire(Triggers.SetName);
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("CreateCharacterView");
        }

        private void _onPlayCommand_execute()
        {
            _initEntries();
            Character = new Character();
            Workflow.Fire(Triggers.SetName);
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("DialogView");
        }

        private void _onHomeCommand_execute()
        {
            _initEntries();
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("DialogView");
        }

        private void _onCancelCommand_execute()
        {
            _initEntries();
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("HomeView");
        }

        private void _workflow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        private async void _onChooseImage_execute()
        {            
            try
            {
                //if (!_chooseImage_CanExecute())
                //    return;

                System.Windows.Forms.OpenFileDialog _openFileDialog = new System.Windows.Forms.OpenFileDialog();
                _openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.apng, *.avif, *.gif, *.webp) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.apng; *.avif; *.gif; *.webp";
                _openFileDialog.InitialDirectory = ApplicationData.Instance.ImagesDirectory;

                if (_openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string _chCurrentImageFilePath = CharacterImage;
                string _filePath = _openFileDialog.FileName;
                string _newFileName = $"{CharacterInitials}_{Path.GetFileName(_filePath)}";
                CharacterImage = ApplicationData.Instance.DefaultImage;

                await Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "_chooseImage_Execute";

                    File.Copy(_filePath, Path.Combine(ApplicationData.Instance.ImagesDirectory, _newFileName), true);

                    if ( !String.IsNullOrEmpty(_chCurrentImageFilePath) && !_chCurrentImageFilePath.Equals(ApplicationData.Instance.DefaultImage))
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        File.Delete(Path.Combine(ApplicationData.Instance.ImagesDirectory, _chCurrentImageFilePath));
                    }

                    CharacterImage = _newFileName;
                });
            }
            catch (Exception ex)
            {
                mLogger.Error("_chooseImage_Execute " + ex.Message);
            }
        }
    }


    public class ToyEntry
    {
        public int Key { get; set; }
        public string Value { get; set; }

        override
        public string ToString()
        {
            return Value;
        }
    }

    public class CreateCharacterState
    {
        public Wizard Wizard { get; set; }
        public int StepIndex { get; set; } = 0;
        public string CharacterPrefix { get; set; }
    }
}
