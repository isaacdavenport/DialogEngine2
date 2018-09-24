using DialogGenerator.UI.Workflow.WizardWorkflow;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Controls
{
    /// <summary>
    /// Interaction logic for VoiceRecorderControl.xaml
    /// </summary>
    public partial class VoiceRecorderControl : UserControl
    {
        public VoiceRecorderControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(States), typeof(VoiceRecorderControl), new PropertyMetadata(States.Start));

        public static readonly DependencyProperty RecordingAllowedProperty =
            DependencyProperty.Register("RecordingAllowed", typeof(bool), typeof(VoiceRecorderControl), new PropertyMetadata(false));

        public static readonly DependencyProperty IsPlayingLineInContextProperty =
            DependencyProperty.Register("IsPlayingLineInContext", typeof(bool), typeof(VoiceRecorderControl), new PropertyMetadata(false));

        public States State
        {
            get { return (States)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public bool RecordingAllowed
        {
            get { return (bool)GetValue(RecordingAllowedProperty); }
            set { SetValue(RecordingAllowedProperty, value); }
        }

        public bool IsPlayingLineInContext
        {
            get { return (bool)GetValue(IsPlayingLineInContextProperty); }
            set { SetValue(IsPlayingLineInContextProperty, value); }
        }
    }
}
