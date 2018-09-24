using DialogGenerator.Core;
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
        private string mBtnContent = "Start dialog";

        #endregion

        #region - constructor -

        public DialogViewModel(ILogger logger,IEventAggregator _eventAggregator,IDialogEngine _dialogEngine)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mDialogEngine = _dialogEngine;

            mEventAggregator.GetEvent<NewDialogLineEvent>().Subscribe(_onNewDialogLine);
            mEventAggregator.GetEvent<ActiveCharactersEvent>().Subscribe(_onActiveCharacters);

            _bindCommands();
        }

        #endregion

        #region - commands -

        public ICommand StartOrStopDialogCommand { get; set; }
        public ICommand ConfigureDialogCommand { get; set; } 

        #endregion

        #region - private functions -

        public void _bindCommands()
        {
            StartOrStopDialogCommand = new DelegateCommand(_startOrStopDialogCommand_Execute);
            ConfigureDialogCommand = new DelegateCommand(_configureDialogCommand_Execute);
        }

        private void _configureDialogCommand_Execute()
        {
        }

        private async void _startOrStopDialogCommand_Execute()
        {
            try
            {
                if (mIsDialogStarted)
                {
                    mIsDialogStarted = !mIsDialogStarted;
                    mDialogEngine.StopDialogEngine();
                    BtnContent = "Start dialog";
                }
                else
                {
                    mIsDialogStarted = !mIsDialogStarted;
                    BtnContent = "Stop dialog";
                    await mDialogEngine.StartDialogEngine();
                }
            }
            catch (Exception ex)
            {
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

        private void _onActiveCharacters(string info)
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

        public string BtnContent
        {
            get { return mBtnContent; }
            set
            {
                mBtnContent = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
