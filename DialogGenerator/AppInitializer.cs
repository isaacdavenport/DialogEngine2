﻿using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.Workflow;
using System;
using System.IO;

namespace DialogGenerator
{
    public class AppInitializer
    {
        #region - fields -

        private ILogger mLogger;
        private IDialogDataRepository mDialogDataRepository;
        private ICharacterRepository mCharacterRepository;
        private IDialogEngine mDialogEngine;
        private AppInitializerWorkflow mWorkflow;
        private States mCurrentState;
        public event EventHandler<string> Error;
        public event EventHandler Completed;

        #endregion

        #region - constructor -

        public AppInitializer(ILogger logger, IDialogDataRepository _dialogRepository,
            IDialogEngine _dialogEngine,ICharacterRepository _characterRepository)
        {
            mLogger = logger;
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
                .Permit(Triggers.InitializeDialogEngine, States.DialogEngineInitialization)
                .Permit(Triggers.ProcessError, States.Error);

            mWorkflow.Configure(States.Error)
                .OnEntry(_processError);

            mWorkflow.Configure(States.DialogEngineInitialization)
                .OnEntry(_initializeDialogEngine)
                .Permit(Triggers.SetDefaultValues, States.SettingDefaultValues);

            mWorkflow.Configure(States.SettingDefaultValues)
                .OnEntry(_setInitialValues);
        }


        private async void _loadData()
        {
            if (File.Exists(ApplicationData.Instance.TempDirectory))
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

            var _loadedData = await mDialogDataRepository.LoadAsync(ApplicationData.Instance.DataDirectory);

            if (_loadedData.DialogModels.Count == 0)
            {
                // TODO process error
                //mWorkflow.Fire(Triggers.ProcessError);
            }

            Session.Set(Constants.CHARACTERS, _loadedData.Characters);
            Session.Set(Constants.DIALOG_MODELS, _loadedData.DialogModels);
            Session.Set(Constants.WIZARDS, _loadedData.Wizards);

            mWorkflow.Fire(Triggers.InitializeDialogEngine);
        }


        private void _initializeDialogEngine()
        {
            mDialogEngine.Initialize();

            mWorkflow.Fire(Triggers.SetDefaultValues);
        }

        private void _setInitialValues()
        {
            var characters = mCharacterRepository.GetAll();
            var _forcedCharacters = mCharacterRepository.GetAllByState(Model.Enum.CharacterState.On);

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

            Completed(this,new EventArgs());
        }


        private void _processError()
        {
                     
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
