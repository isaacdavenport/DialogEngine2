using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DialogGenerator.UI.Workflow.CreateCharacterWorkflow
{
    public enum States
    {
        EnteredSetName,
        EnteredSetInitials,
        EnteredSetAge,
        EnteredSetGender,
        EnteredSetAvatar,
        InCounter,
        InWizard,
        Playing,
        Finished
    }

    public enum Triggers
    {        
        SetName,
        SetInitials,
        SetAge,
        SetGender,
        SetAvatar,
        CheckCounter,
        StartWizard,
        GoPlay,
        Finish
    }



    public class CreateCharacterWorkflow : Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public CreateCharacterWorkflow(Action action) : base(States.EnteredSetName)
        {
            OnTransitioned((t) =>
            {
                OnPropertyChanged("State");
                CommandManager.InvalidateRequerySuggested();
            });
            
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
