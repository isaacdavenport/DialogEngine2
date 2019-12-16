using System;
using System.ComponentModel;
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
        EnteredSetAssignToy,
        EnteredSetAuthor,
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
        SetAssignToy,
        SetAuthor,
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
