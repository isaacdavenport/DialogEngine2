using DialogGenerator.Core;
using DialogGenerator.UI.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DialogGenerator.UI.Controls
{
    /// <summary>
    /// Interaction logic for MediaPlayerControl.xaml
    /// </summary>
    public partial class MediaPlayerControl : UserControl
    {
        private DispatcherTimer mUpdateTimer;

        public MediaPlayerControl()
        {
            InitializeComponent();

            Loaded += _mediaPlayerControl_Loaded;
            Unloaded += MediaPlayerControl_Unloaded;
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
            (DataContext as MediaPlayerControlViewModel).PropertyChanged += MediaPlayerControl_PropertyChanged;
            
            mUpdateTimer = new DispatcherTimer();
            mUpdateTimer.Interval = TimeSpan.FromSeconds(0.1);
            mUpdateTimer.Tick += MUpdateTimer_Tick;                          
        }

        private void MediaPlayerControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals("CurrentVideoFilePath"))
            {
                VideoPositionScroll.Value = 0;
            }
        }

        private void MediaPlayerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            mUpdateTimer.Stop();
        }

        private void MUpdateTimer_Tick(object sender, EventArgs e)
        {
            _showPosition();
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
                mUpdateTimer.Stop();
            }
        }

        private void _mediaPlayerControl_PlayRequested(object sender, EventArgs e)
        {
            mUpdateTimer.Start();
            VideoPlayer.Play();            
        }

        private void _mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
        }

        private void _mediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Play();
            VideoPlayer.Stop();

            Thread.Sleep(1000);

            if(VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                VideoPositionScroll.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            }
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

        #region Private methods

        private void _showPosition()
        {
            VideoPositionScroll.Value = VideoPlayer.Position.TotalMilliseconds;
        }

        #endregion

        private void VideoPositionScroll_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VideoPlayer.Position = new TimeSpan(0, 0, 0, 0, (int)VideoPositionScroll.Value);
        }

        private void VideoPlayer_LayoutUpdated(object sender, EventArgs e)
        {
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                VideoPositionScroll.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            }
        }
    }
}
