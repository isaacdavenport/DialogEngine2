﻿using DialogGenerator.Core;
using DialogGenerator.DialogEngine;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class DialogViewModel:BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IDialogEngine mDialogEngine;
        private bool mIsDialogStarted;
        private bool mIsStopBtnEnabled;
        private Visibility mIsDebugViewVisible = Visibility.Collapsed;

        #endregion

        #region - constructor -

        public DialogViewModel(ILogger logger,IEventAggregator _eventAggregator,IDialogEngine _dialogEngine)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mDialogEngine = _dialogEngine;

            mEventAggregator.GetEvent<NewDialogLineEvent>().Subscribe(_onNewDialogLine);
            mEventAggregator.GetEvent<ActiveCharactersEvent>().Subscribe(_onNewActiveCharacters);

            _bindCommands();
        }

        #endregion

        #region - commands -

        public ICommand StartDialogCommand { get; set; }
        public ICommand StopDialogCommand { get; set; }
        public ICommand ConfigureDialogCommand { get; set; } 
        public ICommand ClearAllMessagesCommand { get; set; }
        public ICommand ChangeDebugVisibilityCommand { get; set; }

        #endregion

        #region - private functions -

        public void _bindCommands()
        {
            StartDialogCommand = new DelegateCommand(_startDialogCommand_Execute);
            StopDialogCommand = new DelegateCommand(_stopDialogCommand_Execute);
            ConfigureDialogCommand = new DelegateCommand(_configureDialogCommand_Execute);
            ClearAllMessagesCommand = new DelegateCommand(_clearAllMessages_Execute);
            ChangeDebugVisibilityCommand = new DelegateCommand(_changeDebugVisibilityCommand_Execute);
        }

        private void _changeDebugVisibilityCommand_Execute()
        {
            IsDebugViewVisible = IsDebugViewVisible == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
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

        private void _configureDialogCommand_Execute()
        {
        }

        private async void _startDialogCommand_Execute()
        {
            try
            {
                IsDialogStarted = true;
                IsStopBtnEnabled = true;
                mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Publish(true);
                await mDialogEngine.StartDialogEngine();

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
            if (Application.Current.Dispatcher.CheckAccess())
            {
                if (DialogLinesCollection.Count > 150)
                    DialogLinesCollection.RemoveAt(0);

                DialogLinesCollection.Add(obj);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action) delegate
                {
                    if (DialogLinesCollection.Count > 150)
                        DialogLinesCollection.RemoveAt(0);

                    DialogLinesCollection.Add(obj);
                });
            }
        }

        private void _onNewActiveCharacters(string info)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                if (DialogLinesCollection.Count > 150)
                    DialogLinesCollection.RemoveAt(0);

                DialogLinesCollection.Add(info);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    if (DialogLinesCollection.Count > 150)
                        DialogLinesCollection.RemoveAt(0);

                    DialogLinesCollection.Add(info);
                });
            }
        }

        #endregion

        #region - properties -

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
