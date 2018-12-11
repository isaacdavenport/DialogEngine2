using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.Workflow
{
    public enum States
    {
        Started,
        LoadingData,
        Error,
        DialogEngineInitialization,
        SettingDefaultValues
    }

    public enum Triggers
    {
        Start,
        LoadData,
        ProcessError,
        InitializeDialogEngine,
        SetDefaultValues
    }

    public class AppInitializerWorkflow : Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public AppInitializerWorkflow(Action action) : base(States.Started)
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
