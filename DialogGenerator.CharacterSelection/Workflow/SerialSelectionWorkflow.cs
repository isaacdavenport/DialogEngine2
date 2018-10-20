using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.CharacterSelection.Workflow
{
    public enum States
    {
        Waiting,
        Init,
        MessageProcessing,
        FindClosestPair,
        SelectNextCharacters,
        Finish
    }

    public enum Triggers
    {
        Wait,
        Initialize,
        ProcessMessage,
        FindClosestPair,
        SelectNextCharacters,
        Finish
    }

    public class SerialSelectionWorkflow : Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public SerialSelectionWorkflow(Action action):base(States.Waiting)
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
