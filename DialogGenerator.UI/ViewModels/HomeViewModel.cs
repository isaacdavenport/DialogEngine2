using AutoUpdaterDotNET;
using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class HomeViewModel:BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IMessageDialogService mMessageDialogService;
        private string mSelectionMode;

        #endregion

        #region - constructor -

        public HomeViewModel(ILogger logger,IEventAggregator _eventAggregator,IMessageDialogService _messageDialogService)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mMessageDialogService = _messageDialogService;

            mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Subscribe(_onCharacterSelectionActionChanged);

            _bindCommands();
        }

        #endregion

        #region - commands -

        public ICommand ReadTutorialCommand { get; set; }
        public ICommand CheckForUpdatesCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            ReadTutorialCommand = new DelegateCommand(_onReadTutorial_Execute);
            CheckForUpdatesCommand = new DelegateCommand(_onCheckForUpdates_Execute);
        }

        private void _onCheckForUpdates_Execute()
        {
            AutoUpdater.Start(ApplicationData.Instance.URLToUpdateFile);
        }

        private void _onCharacterSelectionActionChanged(bool _isStarted)
        {
            if (_isStarted)
            {
                if (!Session.Get<bool>(Constants.BLE_MODE_ON))
                    SelectionMode = "Selection by toys";
                else
                    SelectionMode = "Random selection";
            }
            else
            {
                SelectionMode = "";
            }
        }

        private void _onReadTutorial_Execute()
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

        #endregion

        #region - properties -

        public string SelectionMode
        {
            get { return mSelectionMode; }
            set
            {
                mSelectionMode = value;
                RaisePropertyChanged();


            }
        }

        #endregion
    }
}
