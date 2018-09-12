using DialogGenerator.Core;
using DialogGenerator.DialogEngine;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Prism.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DialogGenerator.UI.ViewModel
{
    public class DialogViewModel:ViewModelBase,ICharacterDetailViewModel
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IDialogEngine mDialogEngine;
        private bool mIsDialogStarted;

        #endregion

        #region - constructor -

        public DialogViewModel(ILogger logger,IEventAggregator _eventAggregator,IDialogEngine _dialogEngine)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mDialogEngine = _dialogEngine;

            mEventAggregator.GetEvent<NewDialogLineEvent>().Subscribe(_onNewDialogLine);

            DialogLinesCollection.Add(new NewDialogLineEventArgs
            {
                DialogLine = "fadf  fadsf fad fafa sfaffas dfafsfasdf a fadf a fad fa",
                CharacterName = "Sasa basta"
            });

            DialogLinesCollection.Add(new NewDialogLineEventArgs
            {
                DialogLine = "fadf  fadsf fasdf fad fas fads fasd fad fasdf af afd afadsf afd  fafa sfaffas dfafsfasdf a fadf a fad fa",
                CharacterName = "Sasa Mijatovic"
            });

            _bindCommands();
        }

        #endregion

        #region - commands -

        public RelayCommand StartOrStopDialogCommand { get; set; }

        #endregion

        #region - private functions -

        public void _bindCommands()
        {
            StartOrStopDialogCommand = new RelayCommand(_startOrStopDialogCommand_Execute);
        }

        private async void _startOrStopDialogCommand_Execute()
        {
            try
            {
                if (mIsDialogStarted)
                {
                    mDialogEngine.StopDialogEngine();
                }
                else
                {
                    await mDialogEngine.StartDialogEngine();
                }
            }
            catch (System.Exception ex)
            {
                mLogger.Error("_startOrStopDialogCommand_Execute " + ex.Message);
            }
        }

        private void _onNewDialogLine(NewDialogLineEventArgs obj)
        {
            DialogLinesCollection.Add(obj);
        }

        #endregion

        #region - public functions -

        public void Load(string _charactername)
        {
        }

        #endregion

        #region - properties -

        public ObservableCollection<NewDialogLineEventArgs> DialogLinesCollection { get; set; } = new ObservableCollection<NewDialogLineEventArgs>();

        public List<int> DialogSpeedValues { get; set; } = new List<int>(Enumerable.Range(1, 20));

        public int SelectedDialogSpeed
        {
            get { return Session.Get<int>(Constants.DIALOG_SPEED); }
            set
            {
                Session.Set(Constants.DIALOG_SPEED, value);
            }
        }

        #endregion
    }
}
