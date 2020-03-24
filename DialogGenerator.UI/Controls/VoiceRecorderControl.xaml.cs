using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Controls
{
    /// <summary>
    /// Interaction logic for VoiceRecorderControl.xaml
    /// </summary>
    public partial class VoiceRecorderControl : UserControl
    {
        public static readonly DependencyProperty EnableRecordingProperty = DependencyProperty.Register("EnableRecording", typeof(bool), typeof(VoiceRecorderControl), new UIPropertyMetadata(false));

        public VoiceRecorderControl()
        {
            InitializeComponent();
        }

        public bool EnableRecording
        {
            get { return (bool)GetValue(EnableRecordingProperty); }
            set
            {
                SetValue(EnableRecordingProperty, value);
            }
        }
    }
}
