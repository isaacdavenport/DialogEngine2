using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.Workflow;
using System;
using System.IO;
using System.Collections.Generic;
using DialogGenerator.Utilities;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;

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
        public event EventHandler Completed;

        #endregion

        #region - constructor -

        public AppInitializer(ILogger logger,IUserLogger _userLogger, IDialogDataRepository _dialogRepository,
            IDialogEngine _dialogEngine,ICharacterRepository _characterRepository)
        {
            mLogger = logger;
            mUserLogger = _userLogger;
            mDialogDataRepository = _dialogRepository;
            mCharacterRepository = _characterRepository;
            mDialogEngine = _dialogEngine;

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
            _checkDirectories();

            IList<string> errors;
            var _JSONObjectTypesList = mDialogDataRepository.LoadFromDirectory(ApplicationData.Instance.DataDirectory,out errors);

            foreach(var error in errors)
            {
                mUserLogger.Error(error);
                mLogger.Error(error);
            }
            
            Session.Set(Constants.CHARACTERS, _JSONObjectTypesList.Characters);
            Session.Set(Constants.DIALOG_MODELS, _JSONObjectTypesList.DialogModels);
            Session.Set(Constants.WIZARDS, _JSONObjectTypesList.Wizards);

            mWorkflow.Fire(Triggers.InitializeDialogEngine);
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
            characters.Insert(0, new Character { CharacterName = "", State = CharacterState.Off, FileName = $"{Guid.NewGuid()}.json" });

            var _forcedCharacters = mCharacterRepository.GetAllByState(CharacterState.On);

            switch (_forcedCharacters.Count)
            {
                case 1:
                    {
                        Session.Set(Constants.FORCED_CH_1, characters.IndexOf(_forcedCharacters[0]));
                        Session.Set(Constants.FORCED_CH_2, -1);
                        break;
                    }
                case 2:
                    {
                        Session.Set(Constants.FORCED_CH_1, characters.IndexOf(_forcedCharacters[0]));
                        Session.Set(Constants.FORCED_CH_2, characters.IndexOf(_forcedCharacters[1]));
                        break;
                    }
                default:
                    {
                        Session.Set(Constants.FORCED_CH_1, -1);
                        Session.Set(Constants.FORCED_CH_2, -1);
                        break;
                    }
            }

            Session.Set(Constants.FORCED_CH_COUNT, _forcedCharacters.Count);
            Session.Set(Constants.NEXT_CH_1, 1);
            Session.Set(Constants.NEXT_CH_2, 2);
            Session.Set(Constants.DIALOG_SPEED, 1000); // ms
            Session.Set(Constants.SELECTED_DLG_MODEL, -1);
            Session.Set(Constants.COMPLETED_DLG_MODELS, 0);

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
