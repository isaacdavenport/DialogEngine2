using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.DialogEngine.Workflow
{
    public enum States
    {
        Start,
        Init,
        Idle,
        GenerateADialog,
        PreparingDialogParameters,
        DialogStarted,
        DialogFinished
    }

    public enum Triggers
    {
        Start,
        Initialize,
        WaitForNewCharacters,
        GenerateADialog,
        PrepareDialogParameters,
        StartDialog,
        FinishDialog
    }

    public class DialogEngineWorkflow : Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DialogEngineWorkflow(Action action) : base(States.Start)
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

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
