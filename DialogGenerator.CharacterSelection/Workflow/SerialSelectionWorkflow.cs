using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.CharacterSelection.Workflow
{
    //The word Serial is often used as the bluetooth input used to come in over a serial interface dongle
    public enum States
    {
        Waiting,
        Initializing,
        ProcessingMessage,
        CalculatingClosestPair,
        CheckMovement,
        SelectingNextCharacters,
        Finished
    }

    public enum Triggers
    {
        Wait,
        Initialize,
        ProcessMessage,
        CalculateClosestPair,
        CheckMovement,
        SelectNextCharacters,
        Finish
    }

    public class BLESelectionWorkflow : Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public BLESelectionWorkflow(Action action):base(States.Waiting)
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
