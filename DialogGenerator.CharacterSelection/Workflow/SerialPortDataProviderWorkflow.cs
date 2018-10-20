using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.CharacterSelection.SerialPortDataProviderWorkflow
{
    public enum States
    {
        Waiting,
        Init,
        ReadingMessage,
        SerialCOMPortNameError,
        USBDisconnected,
        Finished
    }

    public enum Triggers
    {
        Wait,
        Initialize,
        ReadMessage,
        ProcessSerialPortNameError,
        ProcessUSBDiconnectedError,
        Finish
    }

    public class SerialPortDataReadingWorkflow: Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public SerialPortDataReadingWorkflow(Action action) : base(States.Waiting)
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
