using DialogGenerator.CharacterSelection;
using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class CreateCharacterViewModel : BindableBase
    {
        private string mCharacterName = String.Empty;
        private string mCharacterInitials = String.Empty;
        private string mCharacterIdentifier = String.Empty;
        private string mCharacterImage = String.Empty;
        private string mCharacterGender = "Female";
        private string mCharacterAuthor = String.Empty;
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
        private IRegionManager mRegionManager;
        private IMessageDialogService mMessageDialogService;
        private string mCurrentDialogWizard = String.Empty;
        private string mNextButtonText = "Next";
        private bool mIsFinished = false;

        private IBLEDataProviderFactory mBLEDataProviderFactory;
        private IBLEDataProvider mCurrentDataProvider;
        private CancellationTokenSource mCancellationTokenSource;

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
            IBLEDataProviderFactory _BLEDataProviderFactory)
        {
            mLogger = _logger;
            mEventAgregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;
            mRegionManager = _regionManager;
            mMessageDialogService = _messageDialogService;
            mBLEDataProviderFactory = _BLEDataProviderFactory;

            mWizard = new CreateCharacterWizard();
            mCurrentStep = mWizard.Steps[mCurrentStepIndex];

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

            //_openCreateSession();
            _bindCommands();
        }
        
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
                RaisePropertyChanged();
            }
        }

        private async void _selectToyToCharacter(int oldVal = -1)
        {
            if (mSelectedRadio.Key == -1)
                return;

            var _oldChars = mCharacterDataProvider.GetAll().Where(c => c.RadioNum == mSelectedRadio.Key);
            if(_oldChars.Count() > 0)
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
                    } else
                    {
                        mSelectedRadio = mRadiosCollection.First(p => p.Key == oldVal);
                        RaisePropertyChanged("SelectedRadio");
                    }
                }
            }
            
            Character.RadioNum = mSelectedRadio.Key;
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
        public Character Character {get;set;}
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
                mCharacterInitials = _getCharacterInitials();
                RaisePropertyChanged("CharacterName");
                RaisePropertyChanged("CharacterInitials");
                CharacterInitials = _getCharacterInitials();
                CharacterIdentifier = _getCharacterIdentifier();
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

        public ICommand ChooseImageCommand { get; set; }
        public ICommand HomeCommand { get; set; }
        public ICommand CreateCommand { get; set; }
        public ICommand CancelCommand {get;set;}
        public ICommand ResetCommand { get; set; }
        public ICommand PlayCommand { get; set; }

        public void nextStep()
        {
            int _currentStepIndex = CurrentStepIndex;
            Workflow.Fire((Triggers) (++_currentStepIndex));   
        }

        public void previousStep()
        {
            if(CurrentStepIndex > 0)
            {
                int _currentStepIndex = CurrentStepIndex;
                Workflow.Fire((Triggers) (--_currentStepIndex));
            }            
        }

        private async void _processFinish()
        {            
            int _idx = mCharacterDataProvider.IndexOf(Character);
            if(_idx == -1)
            {
                await mCharacterDataProvider.AddAsync(Character);
            }

            _initEntries();
            Character = new Character();
            Workflow.Fire(Triggers.SetName);
                        
        }

        private void _initEntries()
        {
            CharacterName = String.Empty;
            CharacterInitials = String.Empty;
            CharacterIdentifier = String.Empty;
            CharacterAge = 10;
            CharacterGender = "Male";
            CharacterImage = "Avatar.png";
            CurrentStepIndex = 0;
            NextButtonText = "Next";
            WizardPassthroughIndex = 0;
            mIsFinished = false;
            SelectedRadio = RadiosCollection[0];
            
            _closeCreateSession();
        }

        private string _getCharacterInitials()
        {
            if (String.IsNullOrEmpty(mCharacterName))
                return String.Empty;

            String[] tokens = mCharacterName.Split(' ');
            String result = String.Empty;
            for(int i = 0; i < tokens.Length; i++)
            {
                result += tokens[i].First();
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
        }
        
        private void _configureWorkflow()
        {
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
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetInitials)
                 .OnEntry(() => _stepEntered("Initials"))
                 .OnExit(() => _stepExited("Initials"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.SetAssignToy, States.EnteredSetAssignToy)
                 .Permit(Triggers.SetAuthor, States.EnteredSetAuthor)
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
                 .Permit(Triggers.CheckCounter, States.InCounter)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.InCounter)
                  .OnEntry(() => _stepEntered("CheckCounter"))
                  .Permit(Triggers.StartWizard, States.InWizard)
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
                .Permit(Triggers.SetName, States.EnteredSetName);
            
        }

        private void _stepExited(string stepName)
        {
            switch (stepName)
            {
                case "Name":
                    Character.CharacterName = CharacterName;
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
                case "AssignToy":
                    if(Session.Get<bool>(Constants.BLE_MODE_ON))
                    {
                        Character.RadioNum = SelectedRadio.Key;
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
                case "Name":
                    CurrentStepIndex = 0;                    
                    CharacterName = Character.CharacterName;
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
                    NextButtonText = "Save";
                    break;
                case "CheckCounter":
                    if(mWizardPassthroughIndex < mDialogWizards.Count)
                    {
                        CurrentDialogWizard = mDialogWizards[mWizardPassthroughIndex++];
                        Workflow.Fire(Triggers.StartWizard);
                    } else
                    {
                        mIsFinished = true;
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

                    // Start the wizard.
                    mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("WizardView");
                    break;
                case "Play":
                    // Prepare character to be shown on 'play'.
                    int _idx = mCharacterDataProvider.IndexOf(Character);
                    if (_idx == -1)
                    {
                        // First remove all that have state on
                        foreach(var _charMember in mCharacterDataProvider.GetAll())
                        {
                            if(_charMember.State == Model.Enum.CharacterState.On)
                            {
                                _charMember.State = Model.Enum.CharacterState.Available;
                            }
                        }

                        Character.State = Model.Enum.CharacterState.On;
                        await mCharacterDataProvider.AddAsync(Character);
                        _idx = mCharacterDataProvider.IndexOf(Character); 
                    }


                    Session.Set(Constants.NEXT_CH_1, _idx);
                    int _idx2 = Session.Get<int>(Constants.NEXT_CH_2);                    
                    mEventAgregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();
                    mEventAgregator.GetEvent<SelectedCharactersPairChangedEvent>().
                    Publish(new SelectedCharactersPairEventArgs
                    {
                        Character1Index = _idx,
                        Character2Index = _idx2
                    });

                    // Check if we have reached the end?
                    if(mWizardPassthroughIndex == mDialogWizards.Count)
                    {
                        await mMessageDialogService.ShowMessage("INFO", "You have successfully completed the guided character creation process!");
                        Workflow.Fire(Triggers.Finish);
                    }

                    // Call the play window.
                    mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("DialogView");
                    break;
                case "Finished":
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
                mDialogWizards.Add("IntermediateWizard");
                _wizardCollection.Wizards.Add("IntermediateWizard");
                mDialogWizards.Add("ScaryStoryWizard");
                _wizardCollection.Wizards.Add("ScaryStoryWizard");
                mDialogWizards.Add("ActionStoryWizard");
                _wizardCollection.Wizards.Add("ActionStoryWizard");               
                mDialogWizards.Add("Advanced1Wizard");
                _wizardCollection.Wizards.Add("Advanced1Wizard");
                mDialogWizards.Add("Advanced2Wizard");
                _wizardCollection.Wizards.Add("Advanced2Wizard");

                Serializer.Serialize(_wizardCollection, ApplicationData.Instance.DataDirectory + "\\WizardCollection.cfg");
            }             
        }

        private class WizardCollection
        {
            string mVersionNumber;
            ObservableCollection<string> mWizards = new ObservableCollection<string>();
            
            [JsonProperty("Version")]
            public string Version { get; set; }
            [JsonProperty("Wizards")]
            public ObservableCollection<string> Wizards
            {
                get
                {
                    return mWizards;
                }
            }
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
            } catch (IOException ioe)
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
            mIsFinished = false;
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
                _openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";

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
}
