using DialogGenerator.Core;
using DialogGenerator.Events;
using Prism.Events;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace DialogGenerator.Utilities
{
    /// <summary>
    /// Wrapper for <see cref="MediaPlayer"/>
    /// Used for playing dialog's .mp3 files
    /// </summary>
    public class MP3Player : IMP3Player
    {
        #region - fields -

        private static readonly object mcPadlock = new object();
        // length of .mp3 file in seconds
        private double mDuration;
        private bool mIsLoaded;
        private bool mIsPlayingStopped;
        // started time of playing .mp3 file
        private TimeSpan mStartedTime;
        private Timer mTimer;
        private Timer mVolumeTimer;
        // wpf media player
        public MediaPlayer Player = new MediaPlayer();
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;

        #endregion

        #region - constructor -

        /// <summary>
        /// Creates instance of MP3Player
        /// </summary>
        public MP3Player(IEventAggregator _eventAggregator,ILogger logger)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;

            mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Subscribe(_stopPlayingCurrentDialogLine);
            mEventAggregator.GetEvent<StopImmediatelyPlayingCurrentDialogLIne>().Subscribe(_stopImmediatelyPlayingCurrentDialogLine);

            mVolumeTimer = new Timer(_volumeTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            mTimer = new Timer(_timerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            Player.MediaOpened += _player_MediaOpened;
        }


        #endregion

        #region - event handlers -

        private void _timerElapsed(object state)
        {
            double _durationOfPlaying = DateTime.Now.TimeOfDay.TotalSeconds - mStartedTime.TotalSeconds;

            Thread.CurrentThread.Name = "timer elapsed thread";
            // 0.5 seconds we need to mute player
            if (_durationOfPlaying > (ApplicationData.Instance.MaxTimeToPlayFile - 0.5))
            {
                mTimer.Change(Timeout.Infinite, Timeout.Infinite);
                mVolumeTimer.Change(0, 100);
            }
        }


        private void _volumeTimerElapsed(object state)
        {
            try
            {
                Thread.CurrentThread.Name = "volume timer thread";
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(() =>
                 {
                     if (Player.Volume == 0)
                     {
                         mVolumeTimer.Change(Timeout.Infinite, Timeout.Infinite);
                         Player.Stop();
                         mIsPlayingStopped = true;
                         return;
                     }
                     else
                     {
                         Player.Volume -= 0.2; // percentage
                     }
                 }));
            }
            catch (Exception ex)
            {
                mLogger.Error("VolumeTimer. " + ex.Message);
            };
        }


        private void _player_MediaOpened(object sender, EventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                mDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
            }

            Debug.WriteLine("loaded + " + mDuration);
            mIsLoaded = true;
        }

        #endregion

        #region - private functions -

        private void _stopPlayingCurrentDialogLine()
        {
            mTimer.Change(Timeout.Infinite, Timeout.Infinite);
            mVolumeTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (IsPlaying())
            {
                if (mDuration > ApplicationData.Instance.MaxTimeToPlayFile)
                {
                    mTimer.Change(0, 1000);
                }
            }
        }

        // when this function is inveoked, we stops player without any condition
        private void _stopImmediatelyPlayingCurrentDialogLine()
        {
            if (IsPlaying())
            {
                try
                {
                    Player.Stop();
                    mIsPlayingStopped = true;
                }
                catch (Exception ex)
                {
                    mLogger.Error("StopImmediatelyPlayingCurrentDialogLine error. Message: " + ex.Message);
                }
            }
        }

        #endregion

        #region - public functions -

        /// <summary>
        /// Check is player playing
        /// </summary>
        /// <returns>Is player playing</returns>
        public bool IsPlaying()
        {
            try
            {
                //Debug.WriteLine(Player.Position.TotalSeconds);
                if (mIsLoaded)
                {
                    bool _isPlaying = false;
                    Player.Dispatcher.Invoke(() =>
                    {
                        _isPlaying = Player.Position.TotalSeconds < mDuration
                                     && !mIsPlayingStopped;
                    });

                    return _isPlaying;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("IsPlaying " + ex.Message);
                // application is busy
                return true;
            }
        }


        public int Play(string path)
        {
            try
            {
                
                mIsPlayingStopped = false;
                mIsLoaded = false;
                mTimer.Change(Timeout.Infinite, Timeout.Infinite);
                mVolumeTimer.Change(Timeout.Infinite, Timeout.Infinite);

                Player.Dispatcher.Invoke(() =>
                {
                    Player.Volume = 0.8;
                    Player.Open(new Uri(path));
                    Player.Play();
                });

                mLogger.Debug(path);
                mStartedTime = DateTime.Now.TimeOfDay;
                return 0;  //TODO add error handling    
            }
            catch (Exception ex)
            {
                //player is busy
                mLogger.Error("PlayMp3 error. " + ex.Message);
                return 1;
            }
        }

        #endregion
    }
}
