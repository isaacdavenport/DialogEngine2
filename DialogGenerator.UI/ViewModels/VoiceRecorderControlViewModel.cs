using DialogGenerator.Core;
using DialogGenerator.UI.Controls.VoiceRecorder;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class VoiceRecorderControlViewModel:BindableBase
    {
        #region - fields -

        private double mChannelPosition;
        private string mCurrentFilePath;
        private bool mIsRecording;
        private bool mIsPlaying;
        private bool mIsLineRecorded;
        private bool mIsPlayingLineInContext;
        private NAudioEngine mSoundPlayer;

        #endregion

        #region - contructor -

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="player">Instance of <see cref="ISoundPlayer"/></param>
        public VoiceRecorderControlViewModel(NAudioEngine player)
        {
            this.SoundPlayer = player;
            this.mSoundPlayer.PropertyChanged += _soundPlayer_PropertyChanged;

            _bindCommands();
        }

        #endregion

        #region - commands -

        /// <summary>
        /// Starts or stops recording
        /// </summary>
        public ICommand StartRecordingCommand { get; set; }

        /// <summary>
        /// Starts or stops playing
        /// </summary>
        public ICommand PlayContentCommand { get; set; }

        #endregion

        #region - event handlers-

        private void _soundPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ChannelPosition":
                    {
                        ChannelPosition = (double)mSoundPlayer.ActiveStream.Position / (double)mSoundPlayer.ActiveStream.Length;
                        break;
                    }
                case "IsPlaying":
                    {
                        if (!mSoundPlayer.IsPlaying)
                        {
                            if (mSoundPlayer.ChannelPosition == mSoundPlayer.ChannelLength)
                                ChannelPosition = 0;

                            IsPlaying = false;
                        }
                        break;
                    }
            }
        }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            StartRecordingCommand = new DelegateCommand(_startOrStopRecording_Execute);
            PlayContentCommand = new DelegateCommand(_playContent_Execute);
        }

        private void _startOrStopRecording_Execute()
        {
            if (IsRecording)
            {
                mSoundPlayer.StopRecording();
                IsRecording = false;
            }
            else
            {
                mSoundPlayer.StartRecording(Path.Combine(ApplicationData.Instance.AudioDirectory, CurrentFilePath + ".mp3"));
                IsRecording = true;
            }
        }

        private void _playContent_Execute()
        {
            PlayOrStop(mCurrentFilePath);
        }

        #endregion

        #region - public functions -

        public void ResetData()
        {
            IsLineRecorded = false;
        }

        public void PlayOrStop(string path)
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                if (mSoundPlayer.CanStop)
                    mSoundPlayer.Stop();
            }
            else
            {
                IsPlaying = true;
                if (ChannelPosition == 0)
                    mSoundPlayer.OpenFile(Path.Combine(ApplicationData.Instance.AudioDirectory, path + ".mp3"));

                mSoundPlayer.Play();
            }
        }

        #endregion

        #region - properties -

        public NAudioEngine SoundPlayer
        {
            get { return mSoundPlayer; }
            set
            {
                mSoundPlayer = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Is player recording
        /// </summary>
        public bool IsRecording
        {
            get { return mIsRecording; }
            set
            {
                bool _oldValue = mIsRecording;
                if (_oldValue == value)
                    return;

                mIsRecording = value;
                if (mIsRecording == false)
                {
                    IsLineRecorded = true;
                }

                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Is player playing
        /// </summary>
        public bool IsPlaying
        {
            get { return mIsPlaying; }
            set
            {
                bool _oldValue = mIsPlaying;
                if (_oldValue == value)
                    return;

                mIsPlaying = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Is player playing line in context
        /// </summary>
        public bool IsPlayingLineInContext
        {
            get { return mIsPlayingLineInContext; }
            set
            {
                mIsPlayingLineInContext = value;
                RaisePropertyChanged();
            }
        }


        public bool IsLineRecorded
        {
            get { return mIsLineRecorded; }
            set
            {
                mIsLineRecorded = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Position of current stream
        /// </summary>
        public double ChannelPosition
        {
            get { return mChannelPosition; }
            set
            {
                mChannelPosition = value * 100;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Path of last recorded .mp3 file
        /// </summary>
        public string CurrentFilePath
        {
            get { return mCurrentFilePath; }
            set
            {
                mCurrentFilePath = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
