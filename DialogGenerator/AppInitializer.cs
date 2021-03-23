using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.Workflow;
using System;
using System.IO;
using System.Collections.Generic;
using DialogGenerator.Utilities;
using DialogGenerator.Model.Enum;
using System.Diagnostics;
using Prism.Events;
using DialogGenerator.Events;
using System.Collections.ObjectModel;
using DialogGenerator.Model;
using System.Linq;

namespace DialogGenerator
{
    public class AppInitializer
    {
        #region - fields -

        private ILogger mLogger;
        private IUserLogger mUserLogger;
        private IDialogDataRepository mDialogDataRepository;
        private ICharacterRepository mCharacterRepository;
        private IDialogEngine mDialogEngine;
        private AppInitializerWorkflow mWorkflow;
        private States mCurrentState;
        private IEventAggregator mEventAggregator;
        public event EventHandler Completed;


        #endregion

        #region - constructor -

        public AppInitializer(ILogger logger,IUserLogger _userLogger, IDialogDataRepository _dialogRepository,
            IDialogEngine _dialogEngine,ICharacterRepository _characterRepository, IEventAggregator _eventAggregator)
        {
            mLogger = logger;
            mUserLogger = _userLogger;
            mDialogDataRepository = _dialogRepository;
            mCharacterRepository = _characterRepository;
            mDialogEngine = _dialogEngine;
            mEventAggregator = _eventAggregator;

            mWorkflow = new AppInitializerWorkflow(() => { });
            mWorkflow.PropertyChanged += _mWorkflow_PropertyChanged;
            _configureWorkflow();
        }

        #endregion

        #region - event handlers -

        private void _mWorkflow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("State"))
                CurrentState = mWorkflow.State;
        }

        #endregion

        #region - private functions -

        private void _configureWorkflow()
        {
            mWorkflow.Configure(States.Started)
                .Permit(Triggers.LoadData, States.LoadingData);

            mWorkflow.Configure(States.LoadingData)
                .OnEntry(_loadData)
                .Permit(Triggers.InitializeDialogEngine, States.DialogEngineInitialization);

            mWorkflow.Configure(States.DialogEngineInitialization)
                .OnEntry(_initializeDialogEngine)
                .Permit(Triggers.SetDefaultValues, States.SettingDefaultValues);

            mWorkflow.Configure(States.SettingDefaultValues)
                .OnEntry(_setInitialValues);
        }

        private  void _loadData()
        {
            mLogger.Info("-------------------- Starting DialogEngine Version " +                 
                $"{ FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationData.Instance.RootDirectory, "DialogGenerator.exe")).FileVersion.ToString()}" + 
                " -----------------------");
            _checkDirectories();

            IList<string> errors;            
            var _JSONObjectTypesList = mDialogDataRepository.LoadFromDirectory(ApplicationData.Instance.DataDirectory,out errors);
            mDialogDataRepository.LogRedundantDialogModelsInDataFolder(ApplicationData.Instance.DataDirectory, _JSONObjectTypesList);
            _removeDuplicateDialogModelsFromCollection(_JSONObjectTypesList.DialogModels);
            mDialogDataRepository.LogSessionJsonStatsAndErrors(ApplicationData.Instance.DataDirectory, _JSONObjectTypesList);
            foreach(var error in errors)
            {
                mUserLogger.Error(error);
                mLogger.Error(error);
            }

            _checkForMultipleRadioAssignments(_JSONObjectTypesList.Characters);

            Session.Set(Constants.CHARACTERS, _JSONObjectTypesList.Characters);
            Session.Set(Constants.DIALOG_MODELS, _JSONObjectTypesList.DialogModels);
            Session.Set(Constants.WIZARDS, _JSONObjectTypesList.Wizards);
            Session.Set(Constants.NEXT_CH_1, -1);
            Session.Set(Constants.NEXT_CH_2, -1);
            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Publish();

            mLogger.Info("Finished importing characters: " + _JSONObjectTypesList.Characters.Count + " DialogModelGroups: " + 
                _JSONObjectTypesList.DialogModels.Count + " Wizards: " + _JSONObjectTypesList.Wizards.Count);

            mWorkflow.Fire(Triggers.InitializeDialogEngine);
        }

        private bool _testValueEqualityOnStringLists(List<string> a, List<string> b)
        {
            if(a.Count != b.Count)
            {
                return false;  //no need to check individual strings
            }
            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                {
                    return false;
                }
            }
            return true;
        }
        private void _removeDuplicateDialogModelsFromCollection(ObservableCollection<ModelDialogInfo> DialogModelCollections)
        {
            var _modelsToBeRemoved = new List<int[]>();
            var _alreadySeenDialogModelTagLists = new List<List<string>>();
            for (int i = 0; i < DialogModelCollections.Count; i++)
                {
                for (int j = 0; j < DialogModelCollections[i].ArrayOfDialogModels.Count; j++)
                { // if the phraseType list in this dialog model matches a previous, delete this dialog model from collection
                    var _curentTags = DialogModelCollections[i].ArrayOfDialogModels[j].PhraseTypeSequence;
                    foreach (var _tagList in _alreadySeenDialogModelTagLists)
                    {
                        if (_testValueEqualityOnStringLists(_tagList, _curentTags))  // if already in the list mark for removal
                        {
                            var _indexPair = new int[2];
                            _indexPair[0] = i;
                            _indexPair[1] = j;
                            _modelsToBeRemoved.Add(_indexPair);
                            break;
                        }
                    }
                    _alreadySeenDialogModelTagLists.Add(_curentTags);
                }
            }
            for ( int t = 0; t < _modelsToBeRemoved.Count; t++)
            {  //TODO Isaac this probably fails because the locations in the arrayofDialogModels is actually a list and the indexes move after some RemoveAt commands
                DialogModelCollections[_modelsToBeRemoved[t][0]].ArrayOfDialogModels.RemoveAt(_modelsToBeRemoved[t][1]);
            }

        }
        private void _checkForMultipleRadioAssignments(ObservableCollection<Character> _Characters)
        {
            var _radioCheck = new Dictionary<int, bool>();
            for(int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
            {
                _radioCheck.Add(i, false);            
            }

            var _charsWithRadios = _Characters.Where(_r => _r.RadioNum != -1);
            foreach(var _character in _charsWithRadios)
            {                
                // If there is already a character with that 
                // radio number, set the radio number to -1.
                if(_radioCheck[_character.RadioNum])
                {
                    _character.RadioNum = -1;
                } else
                {
                    _radioCheck[_character.RadioNum] = true;
                }                
            }
        }

        private  void _checkDirectories()
        {
            if (Directory.Exists(ApplicationData.Instance.TempDirectory))
            {
                var _dirInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);

                foreach (FileInfo file in _dirInfo.EnumerateFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in _dirInfo.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(ApplicationData.Instance.TempDirectory);
            }

            if (Directory.Exists(ApplicationData.Instance.EditorTempDirectory))
            {
                var _dirInfo = new DirectoryInfo(ApplicationData.Instance.EditorTempDirectory);

                foreach (FileInfo file in _dirInfo.EnumerateFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in _dirInfo.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(ApplicationData.Instance.EditorTempDirectory);
            }
        }

        private void _initializeDialogEngine()
        {
            mDialogEngine.Initialize();

            mWorkflow.Fire(Triggers.SetDefaultValues);
        }

        private void _setInitialValues()
        {
            var characters = mCharacterRepository.GetAll();

            
            Session.Set(Constants.NEXT_CH_1, -1);
            Session.Set(Constants.NEXT_CH_2, -1);
            Session.Set(Constants.DIALOG_SPEED, 1000); // ms
            Session.Set(Constants.SELECTED_DLG_MODEL, -1);
            Session.Set(Constants.COMPLETED_DLG_MODELS, 0);
            Session.Set(Constants.BLE_MODE_ON, true);

            Completed?.Invoke(this, new EventArgs());
        }

        #endregion

        #region - public functions -

        public void Initialize()
        {
            mWorkflow.Fire(Triggers.LoadData);
        }

        #endregion

        #region - properties -

        public States CurrentState
        {
            get { return mCurrentState; }
            set
            {
                mCurrentState = value;
                mLogger.Info(mCurrentState.ToString("g"));
            }
        }

        #endregion
    }
}
