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

        private void _checkForMultipleRadioAssignments(ObservableCollection<Character> _Characters)
        {
            Dictionary<int, bool> _radioCheck = new Dictionary<int, bool>();
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
