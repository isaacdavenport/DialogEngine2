﻿using DialogGenerator.Core;
using DialogGenerator.UI.ViewModels;
using System;
using System.IO;
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
        private const int INTERVAL = 5000;

        public MediaPlayerControl()
        {
            InitializeComponent();

            Loaded += _mediaPlayerControl_Loaded;
            Unloaded += MediaPlayerControl_Unloaded;
            VideoPlayer.MediaEnded += _mediaElement_MediaEnded;
            VideoPlayer.MediaFailed += _mediaElement_MediaFailed;
            VideoPlayer.Loaded += _mediaElement_Loaded;
            VideoPlayer.Unloaded += _mediaElement_Unloaded;
        }

        private void _mediaElement_Unloaded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Source = null;
        }

        #region - event handlers -

        private void _mediaPlayerControl_Loaded(object sender, RoutedEventArgs e)
        {
            MediaPlayerControlViewModel _mvm = ((MediaPlayerControlViewModel)DataContext);

            _mvm.PlayRequested += _mediaPlayerControl_PlayRequested;
            _mvm.PauseRequested += _mediaPlayerControl_PauseRequested;
            _mvm.StopRequested += _mediaPlayerControl_StopRequested;
            _mvm.ShiftForwardRequested += MediaPlayerControl_ShiftForwardRequested;
            _mvm.ShiftBackwardsRequested += MediaPlayerControl_ShiftBackwardsRequested;
            _mvm.PropertyChanged += MediaPlayerControl_PropertyChanged;


            mUpdateTimer = new DispatcherTimer();
            mUpdateTimer.Interval = TimeSpan.FromSeconds(0.1);
            mUpdateTimer.Tick += MUpdateTimer_Tick;

            if(this.VideoPlayer.Source == null && !string.IsNullOrEmpty(_mvm.CurrentVideoFilePath))
            {
                this.VideoPlayer.Source = new Uri(_mvm.CurrentVideoFilePath);
            }

        }

        private void MediaPlayerControl_ShiftBackwardsRequested(object sender, EventArgs e)
        {
            double _totalMilliseconds = VideoPlayer.Position.TotalMilliseconds;
            if(VideoPlayer.CanPause)
            {
                VideoPlayer.Pause();
                if (_totalMilliseconds - INTERVAL >= 0)
                {
                    VideoPlayer.Position -= new TimeSpan(0, 0, 0, 0, INTERVAL);

                }
                else
                {
                    VideoPlayer.Position = new TimeSpan(0, 0, 0, 0, 0);
                }

                VideoPlayer.Play();
            }
            
        }

        private void MediaPlayerControl_ShiftForwardRequested(object sender, EventArgs e)
        {
            double _totalMilliseconds = VideoPlayer.Position.TotalMilliseconds;
            if(VideoPlayer.CanPause)
            {
                VideoPlayer.Pause();

                if (_totalMilliseconds + INTERVAL <= VideoPlayer.NaturalDuration.TimeSpan.TotalMilliseconds)
                {
                    VideoPlayer.Position += new TimeSpan(0, 0, 0, 0, INTERVAL);
                }
                else
                {
                    VideoPlayer.Position = new TimeSpan(0, 0, 0, 0, (int)VideoPlayer.NaturalDuration.TimeSpan.TotalMilliseconds);
                }

                VideoPlayer.Play();
            }
            
        }

        private void MediaPlayerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            (DataContext as MediaPlayerControlViewModel).PlayRequested -= _mediaPlayerControl_PlayRequested;
            (DataContext as MediaPlayerControlViewModel).PauseRequested -= _mediaPlayerControl_PauseRequested;
            (DataContext as MediaPlayerControlViewModel).StopRequested -= _mediaPlayerControl_StopRequested;
            (DataContext as MediaPlayerControlViewModel).ShiftForwardRequested -= MediaPlayerControl_ShiftForwardRequested;
            (DataContext as MediaPlayerControlViewModel).ShiftBackwardsRequested -= MediaPlayerControl_ShiftBackwardsRequested;
            (DataContext as MediaPlayerControlViewModel).PropertyChanged -= MediaPlayerControl_PropertyChanged;

            mUpdateTimer.Stop();
        }

        private void MUpdateTimer_Tick(object sender, EventArgs e)
        {
            _showPosition();
        }

        private void MediaPlayerControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("CurrentVideoFilePath"))
            {
                try
                {
                    if (VideoPlayer.IsInputMethodEnabled)
                    {
                        VideoPlayer.Source = new Uri(((MediaPlayerControlViewModel)sender).CurrentVideoFilePath);
                    }

                    VideoPositionScroll.Value = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
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
            ((MediaPlayerControlViewModel)this.DataContext).LogMessage(0, e.ErrorException.Message);
        }

        private void _mediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            _initVideo();     
        }

        private void _mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            if ((DataContext as MediaPlayerControlViewModel).StateMachine.CanFire(Workflow.VideoPlayerStateMachine.Triggers.On))
            {
                (DataContext as MediaPlayerControlViewModel).StateMachine.Fire(Workflow.VideoPlayerStateMachine.Triggers.On);
            }

            (DataContext as MediaPlayerControlViewModel).ShiftBackwardCommand.RaiseCanExecuteChanged();
            (DataContext as MediaPlayerControlViewModel).ShiftForwardCommand.RaiseCanExecuteChanged();
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
            e.Handled = true;             
        }

        private void VideoPlayer_LayoutUpdated(object sender, EventArgs e)
        {
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                VideoPositionScroll.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            }
        }

        private void VideoPositionScroll_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;                           
        }

        private void _initVideo()
        {
            VideoPlayer.Play();
            VideoPlayer.Stop();
        }

    }
}
