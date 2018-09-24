using DialogGenerator.UI.ViewModels;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
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
        #region - fields -

        private readonly DispatcherTimer mcMediaTimer = new DispatcherTimer();
        private TimeSpan mMediaDuration;

        #endregion

        public MediaPlayerControl()
        {
            InitializeComponent();

            mcMediaTimer.Interval = TimeSpan.FromMilliseconds(25);
            mcMediaTimer.Tick += _mediaTimer_Tick;

            this.Loaded += _mediaPlayerControl_Loaded;
            this.VideoPlayer.MediaOpened += _mediaElement_MediaOpened;
            this.VideoPlayer.MediaEnded += _mediaElement_MediaEnded;
            this.VideoPlayer.MediaFailed += _mediaElement_MediaFailed;
            this.VideoPlayer.Loaded += _mediaElement_Loaded;
        }


        #region - dependency properties -

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(States), typeof(MediaPlayerControl), new PropertyMetadata(States.Start));

        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register("FilePath", typeof(string), typeof(MediaPlayerControl));

        public States State
        {
            get { return (States)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public string FilePath
        {
            get { return (string)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); }
        }

        #endregion

        #region - event handlers -

        private void _mediaPlayerControl_Loaded(object sender, RoutedEventArgs e)
        {
            (this.DataContext as MediaPlayerControlViewModel).PlayRequested += _mediaPlayerControl_PlayRequested;
            (this.DataContext as MediaPlayerControlViewModel).PauseRequested += _mediaPlayerControl_PauseRequested;
            (this.DataContext as MediaPlayerControlViewModel).StopRequested += _mediaPlayerControl_StopRequested;
        }

        private void _mediaPlayerControl_StopRequested(object sender, EventArgs e)
        {
            VideoPlayer.Stop();
            (this.DataContext as MediaPlayerControlViewModel).IsPlaying = false;
        }

        private void _mediaPlayerControl_PauseRequested(object sender, EventArgs e)
        {
            if (VideoPlayer.CanPause)
            {
                VideoPlayer.Pause();
                (this.DataContext as MediaPlayerControlViewModel).IsPlaying = false;
            }
        }

        private void _mediaPlayerControl_PlayRequested(object sender, EventArgs e)
        {
            VideoPlayer.Play();
            (this.DataContext as MediaPlayerControlViewModel).IsPlaying = true;
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
            (this.DataContext as MediaPlayerControlViewModel).IsPlaying = false;
            mcMediaTimer.Stop();
            VideoPlayer.Stop();
            //VideoPlayerSlider.Value = 0;
        }

        private void _mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            mMediaDuration = VideoPlayer.NaturalDuration.HasTimeSpan ? VideoPlayer.NaturalDuration.TimeSpan : new TimeSpan(0, 0, 0);
            //VideoPlayerSlider.Maximum = mMediaDuration.TotalMilliseconds;
            mcMediaTimer.Start();
        }

        private void _mediaTimer_Tick(object sender, EventArgs e)
        {
            //VideoPlayerSlider.Value = VideoPlayer.Position.TotalMilliseconds;
        }

        #endregion
    }
}
