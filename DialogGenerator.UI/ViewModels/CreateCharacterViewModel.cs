using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Workflow.CreateCharacterWorkflow;
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
        private string mCurrentDialogWizard = String.Empty;
        private string mNextButtonText = "Next";
        private bool mIsFinished = false;

        public CreateCharacterViewModel(ILogger _logger,
            IEventAggregator _eventAggregator,
            ICharacterDataProvider _characterDataProvider,
            IRegionManager _regionManager)
        {
            mLogger = _logger;
            mEventAgregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;
            mRegionManager = _regionManager;

            mWizard = new CreateCharacterWizard();
            mCurrentStep = mWizard.Steps[mCurrentStepIndex];

            for (int i = 5; i < 100; i++)
            {
                mAgesCollection.Add(i);
            }

            Workflow = new CreateCharacterWorkflow(action: () => { });
            Workflow.PropertyChanged += _workflow_PropertyChanged;
            _configureWorkflow();

            Character = new Character();

            _initDialogWizards();

            //_openCreateSession();
            _bindCommands();
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
                RaisePropertyChanged("CharacterInitials");
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
                RaisePropertyChanged("CharacterIdentifier");
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
                RaisePropertyChanged("CharacterAge");
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
                RaisePropertyChanged("CharacterGender");
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
                RaisePropertyChanged("CharacterImage");
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

        public ICommand ChooseImageCommand { get; set; }
        public ICommand HomeCommand { get; set; }
        public ICommand CreateCommand { get; set; }
        public ICommand CancelCommand {get;set;}

        public void nextStep()
        {
            if(mIsFinished)
            {
                mIsFinished = false;
                NextButtonText = "Next";
                _processFinish();
            } else
            {
                CurrentStepIndex++;
                Workflow.Fire((Triggers) CurrentStepIndex);
            }
        }

        public void previousStep()
        {
            if(CurrentStepIndex > 0)
            {
                CurrentStepIndex--;
                Workflow.Fire((Triggers) CurrentStepIndex);
            }
            
        }

        private async void _processFinish()
        {            
            int _idx = mCharacterDataProvider.IndexOf(Character);
            if(_idx == -1)
            {
                await mCharacterDataProvider.AddAsync(Character);
            }

            _onCreateCommand_execute();
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
            
            _closeCreateSession();
        }

        private void handleStepChange()
        {
            if(CurrentStep.StepName.Equals("Run Dialog Wizard"))
            {
                Character _character = Session.Get(Constants.NEW_CHARACTER) as Character;
                if(_character == null)
                {
                    // If the character value was null in the session object, 
                    // it means that the CHARACTER_EDIT_MODE was also not on.
                    // So we have to turn it on too. 
                    _character = new Character();
                    Session.Set(Constants.CHARACTER_EDIT_MODE, true);
                }

                _character.CharacterName = CharacterName;
                _character.CharacterPrefix = CharacterPrefix;
                _character.CharacterAge = CharacterAge;
                _character.CharacterGender = CharacterGender.Substring(0,1);
                _character.CharacterImage = CharacterImage;
                Session.Set(Constants.NEW_CHARACTER, _character);
            }
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
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetInitials)
                 .OnEntry(() => _stepEntered("Initials"))
                 .OnExit(() => _stepExited("Initials"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetAge)
                 .OnEntry(() => _stepEntered("Age"))
                 .OnExit(() => _stepExited("Age"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetGender)
                 .OnEntry(() => _stepEntered("Gender"))
                 .OnExit(() => _stepExited("Gender"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetAvatar, States.EnteredSetAvatar)
                 .Permit(Triggers.StartWizard, States.InWizard)
                 .Permit(Triggers.Finish, States.Finished);

            Workflow.Configure(States.EnteredSetAvatar)
                 .OnEntry(() => _stepEntered("Avatar"))
                 .OnExit(() => _stepExited("Avatar"))
                 .Permit(Triggers.SetName, States.EnteredSetName)
                 .Permit(Triggers.SetInitials, States.EnteredSetInitials)
                 .Permit(Triggers.SetAge, States.EnteredSetAge)
                 .Permit(Triggers.SetGender, States.EnteredSetGender)
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
                .Permit(Triggers.SetAvatar, States.EnteredSetAvatar);
            
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
                case "CheckCounter":
                    if(mWizardPassthroughIndex < mDialogWizards.Count)
                    {
                        CurrentDialogWizard = mDialogWizards[mWizardPassthroughIndex++];
                        Workflow.Fire(Triggers.StartWizard);
                    } else
                    {
                        Workflow.Fire(Triggers.Finish);
                    }
                    
                    break;
                case "Wizard":
                    // Prepare steps for starting of the dialog model wizard in 
                    // character creation mode.
                    if(!_checkCharacterCreateMode())
                    {
                        Session.Set(Constants.CHARACTER_EDIT_MODE, true);
                        Session.Set(Constants.NEW_CHARACTER, Character);
                        Session.Set(Constants.CREATE_CHARACTER_VIEW_MODEL, this);
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

                    

                    //int _forcedCharacterCount = Session.Get<int>(Constants.FORCED_CH_COUNT);
                    //if (_forcedCharacterCount == 0)
                    //{
                    //    _forcedCharacterCount++;
                    //    Session.Set(Constants.FORCED_CH_COUNT, _forcedCharacterCount);
                    //}

                    //Session.Set(Constants.FORCED_CH_1, _idx);

                    // Call the play window.
                    mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("DialogView");
                    break;
                case "Finished":
                    mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("CreateCharacterView");
                    Workflow.Fire(Triggers.SetAvatar);
                    NextButtonText = "Finish";
                    mIsFinished = true;
                    break;
                default:
                    break;
            }
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
            mDialogWizards.Add("BasicWizard");            
            mDialogWizards.Add("ScaryStoryWizard");
            mDialogWizards.Add("ActionStoryWizard");
            mDialogWizards.Add("IntermediateWizard");
            mDialogWizards.Add("Advanced1Wizard");
        }

        private void _openCreateSession()
        {
            Session.Set(Constants.NEW_CHARACTER, new Character());
            Session.Set(Constants.CHARACTER_EDIT_MODE, true);
        }

        private void _closeCreateSession()
        {
            Session.Set(Constants.NEW_CHARACTER, null);
            Session.Set(Constants.CHARACTER_EDIT_MODE, false);
            Session.Set(Constants.CREATE_CHARACTER_VIEW_MODEL, null);
        }

        private void _onCreateCommand_execute()
        {
            _initEntries();
            Character = new Character();
            Workflow.Fire(Triggers.SetName);
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("CreateView");
        }

        private void _onHomeCommand_execute()
        {
            _initEntries();
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("HomeView");
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
}
