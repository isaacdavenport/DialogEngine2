using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.UI.Workflow.WizardWorkflow
{
    public enum WizardStates
    {
        Started,
        ChooseWizardDialogShown,
        WaitingForUserAction,
        UserActionStarted,
        PlayingInContext,
        SavingAndLoadingNextStep,
        LoadingNextStep,
        Finished,
        LeavingWizard
    }

    public enum WizardTriggers
    {
        Start,
        ShowChooseWizardDialog,
        ReadyForUserAction,
        UserStartedAction,
        PlayInContext,
        SaveAndLoadNextStep,
        LoadNextStep,
        Finish,
        LeaveWizard
    }

    public class WizardWorkflow : Stateless.StateMachine<WizardStates, WizardTriggers>, INotifyPropertyChanged
    {
        #region - fields -

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region - constructor -

        public WizardWorkflow(Action action) : base(WizardStates.Started)
        {
            OnTransitioned
            (
                (t) =>
                {
                    OnPropertyChanged("State");
                    CommandManager.InvalidateRequerySuggested();
                }
            );
        }

        #endregion

        #region - private functions -

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
