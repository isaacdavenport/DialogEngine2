using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine;
using DialogGenerator.Model;
using DialogGenerator.Workflow;
using System;
using System.Collections.Generic;

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
                .Permit(Triggers.BindCharacter2Radio, States.BindingCharacter2Radio)
                .Permit(Triggers.ProcessError, States.Error);

            mWorkflow.Configure(States.Error)
                .OnEntry(_processError);

            mWorkflow.Configure(States.BindingCharacter2Radio)
                .OnEntry(_bindCharacter2Radio)
                .Permit(Triggers.InitializeDialogEngine, States.DialogEngine);

            mWorkflow.Configure(States.DialogEngine)
                .OnEntry(_initializeDialogEngine)
                .Permit(Triggers.SetDefaultValues, States.DefaultValues);

            mWorkflow.Configure(States.DefaultValues)
                .OnEntry(_setDefaultValues);
        }


        private async void _loadData()
        {
            var _loadedData = await mDialogDataRepository.LoadAsync(ApplicationData.Instance.DataDirectory);

            if (_loadedData.DialogModels.Count == 0)
            {
                // TODO process error
                //mWorkflow.Fire(Triggers.ProcessError);
            }

            Session.Set(Constants.CHARACTERS, _loadedData.Characters);
            Session.Set(Constants.DIALOG_MODELS, _loadedData.DialogModels);
            Session.Set(Constants.WIZARDS, _loadedData.Wizards);

            mWorkflow.Fire(Triggers.BindCharacter2Radio);
        }


        private  void _bindCharacter2Radio()
        {
            var _character2RadioBindingDict = new Dictionary<int, Character>();

            for (int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
            {
                var character = mCharacterRepository.GetByAssignedRadio(i);

                _character2RadioBindingDict.Add(i, character);
            }

            Session.Set(Constants.CH_RADIO_RELATIONSHIP, _character2RadioBindingDict);

            mWorkflow.Fire(Triggers.InitializeDialogEngine);
        }


        private void _initializeDialogEngine()
        {
            mDialogEngine.Initialize();

            mWorkflow.Fire(Triggers.SetDefaultValues);
        }

        private void _setDefaultValues()
        {
            Session.Set(Constants.FORCED_CH_COUNT, 0);
            Session.Set(Constants.NEXT_CH_1, 1);
            Session.Set(Constants.NEXT_CH_2, 2);
            Session.Set(Constants.FORCED_CH_1, -1);
            Session.Set(Constants.FORCED_CH_2, -1);
            Session.Set(Constants.DIALOG_SPEED, 1000); // ms
            Session.Set(Constants.SELECTED_DLG_MODEL, -1);

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
