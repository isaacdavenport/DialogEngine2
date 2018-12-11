using DialogGenerator.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Controls
{
    /// <summary>
    /// Interaction logic for MediaPlayerControl.xaml
    /// </summary>
    public partial class MediaPlayerControl : UserControl
    {
        public MediaPlayerControl()
        {
            InitializeComponent();

            Loaded += _mediaPlayerControl_Loaded;
            VideoPlayer.MediaEnded += _mediaElement_MediaEnded;
            VideoPlayer.MediaFailed += _mediaElement_MediaFailed;
            VideoPlayer.Loaded += _mediaElement_Loaded;
        }

        #region - event handlers -

        private void _mediaPlayerControl_Loaded(object sender, RoutedEventArgs e)
        {
            (DataContext as MediaPlayerControlViewModel).PlayRequested += _mediaPlayerControl_PlayRequested;
            (DataContext as MediaPlayerControlViewModel).PauseRequested += _mediaPlayerControl_PauseRequested;
            (DataContext as MediaPlayerControlViewModel).StopRequested += _mediaPlayerControl_StopRequested;
        }

        private void _mediaPlayerControl_StopRequested(object sender, EventArgs e)
        {
            VideoPlayer.Stop();
            if((DataContext as MediaPlayerControlViewModel).StateMachine.CanFire(Workflow.VideoPlayerStateMachine.Triggers.On))
            {
                (DataContext as MediaPlayerControlViewModel).StateMachine.Fire(Workflow.VideoPlayerStateMachine.Triggers.On);
            }
        }

        private void _mediaPlayerControl_PauseRequested(object sender, EventArgs e)
        {
            if (VideoPlayer.CanPause)
            {
                VideoPlayer.Pause();
            }
        }

        private void _mediaPlayerControl_PlayRequested(object sender, EventArgs e)
        {
            VideoPlayer.Play();
        }

        private void _mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
        }

        private void _mediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Play();
            VideoPlayer.Stop();
        }

        private void _mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            if ((DataContext as MediaPlayerControlViewModel).StateMachine.CanFire(Workflow.VideoPlayerStateMachine.Triggers.On))
            {
                (DataContext as MediaPlayerControlViewModel).StateMachine.Fire(Workflow.VideoPlayerStateMachine.Triggers.On);
            }
        }

        #endregion
    }
}
