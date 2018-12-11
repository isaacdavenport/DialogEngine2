using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.UI.Workflow.MP3RecorderStateMachine
{
    public enum States
    {
        Idle,
        Ready,
        Stopped,
        Recording,
        Playing
    }

    public enum Triggers
    {
        Off,
        On,
        Stop,
        Record,
        Play
    }

    public class MP3RecorderStateMachine : Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MP3RecorderStateMachine(Action action) : base(States.Idle)
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

        private void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }
    }
}
