using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.UI.Workflow.VideoPlayerStateMachine
{
    public enum States
    {
        Idle,
        Ready,
        Playing
    }

    public enum Triggers
    {
        Off,
        On,
        Play
    }

    public class VideoPlayerStateMachine: Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public VideoPlayerStateMachine(Action action) : base(States.Idle)
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
