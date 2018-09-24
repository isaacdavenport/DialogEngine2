using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class MediaPlayerControlViewModel:BindableBase
    {
        #region - fields -

        private bool mIsPlaying;
        public event EventHandler PlayRequested;
        public event EventHandler PauseRequested;
        public event EventHandler StopRequested;

        #endregion

        #region - constructor -

        /// <summary>
        /// Constructor
        /// </summary>
        public MediaPlayerControlViewModel()
        {
            _bindCommands();
        }

        #endregion

        #region - commands-

        /// <summary>
        /// Starts video
        /// </summary>
        public ICommand StartVideoCommand { get; set; }

        /// <summary>
        /// Stops video
        /// </summary>
        public ICommand PauseVideoCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            StartVideoCommand = new DelegateCommand(StartMediaPlayer);
            PauseVideoCommand = new DelegateCommand(_pauseMediaPlayer);
        }

        private void _pauseMediaPlayer()
        {
            PauseRequested(this, EventArgs.Empty);
        }
        #endregion

        #region - public functions -

        public void StartMediaPlayer()
        {
            PlayRequested(this, EventArgs.Empty);
        }

        public void StopMediaPlayer()
        {
            StopRequested(this, EventArgs.Empty);
        }

        #endregion

        #region - properties -

        /// <summary>
        /// Is media player playing
        /// </summary>
        public bool IsPlaying
        {
            get { return mIsPlaying; }
            set
            {
                if (mIsPlaying == value)
                    return;

                mIsPlaying = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
