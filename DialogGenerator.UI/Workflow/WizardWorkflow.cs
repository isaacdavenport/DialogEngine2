using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DialogGenerator.UI.Workflow.WizardWorkflow
{
    public enum States
    {
        Start,
        ShowFormDialog,
        ReadyForUserAction,
        VoiceRecorderAction,
        VoiceRecorderRecording,
        VoiceRecorderPlaying,
        VoiceRecorderPlayingInContext,
        VideoPlayerAction,
        VideoPlayerPlaying,
        SaveAndNext,
        SkipStep,
        Finish,
        LeaveWizard
    }

    public enum Triggers
    {
        Start,
        ShowFormDialog,
        ReadyForUserAction,
        VoiceRecorderAction,
        VoiceRecorderRecording,
        VoiceRecorderPlaying,
        VoiceRecorderPlayingInContext,
        VideoPlayerAction,
        VideoPlayerPlaying,
        SaveAndNext,
        SkipStep,
        Finish,
        LeaveWizard
    }

    public class WizardWorkflow : Stateless.StateMachine<States, Triggers>, INotifyPropertyChanged
    {
        #region - fields -

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region - constructor -

        public WizardWorkflow(Action action) : base(States.Start)
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
