//  Confidential Source Code Property Toys2Life LLC Colorado 2017
//  www.toys2life.org

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using DialogGenerator.Core;
using DialogGenerator.Utilities.Model;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DialogGenerator.Utilities
{
    public class NAudioEngine : INotifyPropertyChanged, ISpectrumPlayer, IDisposable
    {
        #region - Fields -
        private readonly int mcRepeatThreshold = 200;
        private readonly DispatcherTimer mcPositionTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
        private readonly int mcfftDataSize = (int)FFTDataSize.FFT2048;
        // singleton instance
        private string mCurrentFilePath;
        private static NAudioEngine msInstance;
        private bool mDisposed;
        // player's conditions
        private bool mCanPlay;
        private bool mCanPause;
        private bool mCanStop;

        // player's state
        private bool mIsPlaying;
        private bool mIsRecording;

        private bool mInChannelTimerUpdate;
        private double mChannelLength;
        private double mChannelPosition;
        private bool mInChannelSet;
        // records sound 
        private WaveIn mWaveInDevice;
        // writes recorded bytes to .mp3 file
        private WaveFileWriter mWaveFileWriter;
        // plays .mp3 file
        private WaveOut mWaveOutDevice;
        private WaveStream mActiveStream;
        private WaveChannel32 mInputStream;
        private SampleAggregator mSampleAggregator;
        private TimeSpan mRepeatStart;
        private TimeSpan mRepeatStop;
        private bool mInRepeatSet;

        #endregion

        #region  - Singleton -
        /// <summary>
        /// Singleton instance of <see cref="NAudioEngine"/>
        /// </summary>
        public static NAudioEngine Instance
        {
            get
            {
                if (msInstance == null)
                    msInstance = new NAudioEngine();
                return msInstance;
            }
        }

        #endregion

        #region - Constructor -
        /// <summary>
        /// Default constructor
        /// </summary>
        private NAudioEngine()
        {
            mcPositionTimer.Interval = TimeSpan.FromMilliseconds(25);
            mcPositionTimer.Tick += _positionTimer_Tick;
        }
        
        #endregion

        #region - IDisposable -

        /// <summary>
        /// Destructor
        /// </summary>
        public void Dispose()
        {
            dispose(true);            
            GC.SuppressFinalize(this);
        }

        protected virtual void dispose(bool disposing)
        {
            if(!mDisposed)
            {
                if(disposing)
                {
                    _stopAndCloseStream();
                }

                mDisposed = true;
            }
        }

        #endregion

        #region - ISpectrumPlayer -

        public bool GetFFTData(float[] _fftDataBuffer)
        {
            bool status = IsPlaying || IsRecording;

            if (status)
            {
                mSampleAggregator.GetFFTResults(_fftDataBuffer);
            }
            return status;
        }

        public int GetFFTFrequencyIndex(int frequency)
        {         
            double _maxFrequency;

            if (IsRecording)
            {
                _maxFrequency = mWaveInDevice.WaveFormat.SampleRate / 2.0d;
            }
            else
            {
                if (ActiveStream != null)
                    _maxFrequency = ActiveStream.WaveFormat.SampleRate / 2.0d;
                else
                    _maxFrequency = 22050; // Assume a default 44.1 kHz sample rate.
            }

            return (int)((frequency / _maxFrequency) * (mcfftDataSize / 2));
        }

        #endregion

        #region  - IWaveformPlayer -

        public TimeSpan SelectionBegin
        {
            get { return mRepeatStart; }
            set
            {
                if (!mInRepeatSet)
                {
                    mInRepeatSet = true;
                    TimeSpan _oldValue = mRepeatStart;
                    mRepeatStart = value;

                    if (_oldValue != mRepeatStart)
                        NotifyPropertyChanged("SelectionBegin");
                    mInRepeatSet = false;
                }
            }
        }

        public TimeSpan SelectionEnd
        {
            get { return mRepeatStop; }
            set
            {
                if (!mInChannelSet)
                {
                    mInRepeatSet = true;
                    TimeSpan oldValue = mRepeatStop;
                    mRepeatStop = value;

                    if (oldValue != mRepeatStop)
                        NotifyPropertyChanged("SelectionEnd");
                    mInRepeatSet = false;
                }
            }
        }        


        public double ChannelLength
        {
            get { return mChannelLength; }
            protected set
            {
                double _oldValue = mChannelLength;
                mChannelLength = value;

                if (_oldValue != mChannelLength)
                    NotifyPropertyChanged("ChannelLength");
            }
        }

        public double ChannelPosition
        {
            get { return mChannelPosition; }
            set
            {
                if (!mInChannelSet)
                {
                    mInChannelSet = true; // Avoid recursion
                    double _oldValue = mChannelPosition;
                    double position = Math.Max(0, Math.Min(value, ChannelLength));
                    if (!mInChannelTimerUpdate && ActiveStream != null)
                        ActiveStream.Position = (long)((position / ActiveStream.TotalTime.TotalSeconds) * ActiveStream.Length);
                    mChannelPosition = position;
                    if (_oldValue != mChannelPosition)
                        NotifyPropertyChanged("ChannelPosition");
                    mInChannelSet = false;
                }
            }
        }

        #endregion

        #region - INotifyPropertyChanged -

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        #endregion

        #region - Event Handlers -

        private void _inputStream_Sample(object sender, SampleEventArgs e)
        {
            mSampleAggregator.Add(e.Left, e.Right);
            long _repeatStartPosition = (long)((SelectionBegin.TotalSeconds / ActiveStream.TotalTime.TotalSeconds) * ActiveStream.Length);
            long _repeatStopPosition = (long)((SelectionEnd.TotalSeconds / ActiveStream.TotalTime.TotalSeconds) * ActiveStream.Length);
            if (((SelectionEnd - SelectionBegin) >= TimeSpan.FromMilliseconds(mcRepeatThreshold)) && ActiveStream.Position >= _repeatStopPosition)
            {
                mSampleAggregator.Clear();
                ActiveStream.Position = _repeatStartPosition;
            }
        }

        void _positionTimer_Tick(object sender, EventArgs e)
        {
            if (!IsRecording)
            {
                if(ActiveStream == null)
                {
                    Stop();
                    return;
                }

                mInChannelTimerUpdate = true;
                ChannelPosition = ((double)ActiveStream.Position / (double)ActiveStream.Length) * ActiveStream.TotalTime.TotalSeconds;
                mInChannelTimerUpdate = false;

                if (ChannelPosition == ActiveStream.TotalTime.TotalSeconds)
                {
                    Stop();
                }
            }
        }

        private  void _trimWavFile(WaveFileReader reader, WaveFileWriter writer, int _startPos, int _endPos)
        {
            reader.Position = _startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < _endPos)
            {
                int _bytesRequired = (int)(_endPos - reader.Position);
                if (_bytesRequired > 0)
                {
                    int _bytesToRead = Math.Min(_bytesRequired, buffer.Length);
                    int _bytesRead = reader.Read(buffer, 0, _bytesToRead);
                    if (_bytesRead > 0)
                    {
                        writer.Write(buffer, 0, _bytesRead);
                    }
                }
            }
        }

        private void _trimWavFile(string _inPath, string _outPath, TimeSpan _cutFromStart, TimeSpan _cutFromEnd)
        {
            using (WaveFileReader reader = new WaveFileReader(_inPath))
            {
                using (WaveFileWriter writer = new WaveFileWriter(_outPath, reader.WaveFormat))
                {
                    int _bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                    int _startPos = (int)_cutFromStart.TotalMilliseconds * _bytesPerMillisecond;
                    _startPos = _startPos - _startPos % reader.WaveFormat.BlockAlign;

                    int _endBytes = (int)_cutFromEnd.TotalMilliseconds * _bytesPerMillisecond;
                    _endBytes = _endBytes - _endBytes % reader.WaveFormat.BlockAlign;
                    int _endPos = (int)reader.Length - _endBytes;

                    _trimWavFile(reader, writer, _startPos, _endPos);
                }
            }
        }

        private void _waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            mWaveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);

            byte[] buffer = e.Buffer;
            int _bytesRecorded = e.BytesRecorded;
            int _bufferIncrement = mWaveInDevice.WaveFormat.BlockAlign;

            for (int index = 0; index < _bytesRecorded; index += _bufferIncrement)
            {
                float sample32 = BitConverter.ToSingle(buffer, index);
                mSampleAggregator.Add(sample32, 0.0f);
            }

            mWaveFileWriter.Flush();
        }


        private void _normalizeMP3File(string _filePath)
        {
            if (File.Exists(_filePath))
            {
                ProcessStartInfo _processStartInfo = new ProcessStartInfo();
                _processStartInfo.FileName = Path.Combine(ApplicationData.Instance.ToolsDirectory, "mp3gain.exe");
                _processStartInfo.Arguments = $"-r -c {_filePath}";
                _processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                Process _mp3GainProcess = Process.Start(_processStartInfo);
                _mp3GainProcess.WaitForExit(5000);
            }
        }

        private void _cleanDirectory(string _directoryPath)
        {
            DirectoryInfo di = new DirectoryInfo(_directoryPath);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }
        }

        private async void _waveIn_RecordingStopped(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                string _outputPath = Path.Combine(ApplicationData.Instance.TempDirectory, Path.GetFileName(mCurrentFilePath));
                string _outputPathMP3 = Path.Combine(ApplicationData.Instance.TempDirectory, "temp.mp3");
                try
                {
                    _trimWavFile(mCurrentFilePath, _outputPath, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
                    using (var reader = new WaveFileReader(_outputPath))
                    {
                        MediaFoundationEncoder.EncodeToMp3(reader, _outputPathMP3);
                    }
                    _normalizeMP3File(_outputPathMP3);
                    File.Copy(_outputPathMP3, mCurrentFilePath, true);
                }
                catch (Exception ex)
                {
                    
                }
                finally
                {
                    _cleanDirectory(ApplicationData.Instance.TempDirectory);
                    IsRecording = false;
                }
            });
        }

        #endregion

        #region - private functions -
        
        private void _stopAndCloseStream()
        {
            if (mWaveOutDevice != null)
            {
                mWaveOutDevice.Stop();
            }
            if (mActiveStream != null)
            {
                mInputStream.Close();
                mInputStream = null;
                ActiveStream.Close();
                ActiveStream = null;
            }
            if (mWaveOutDevice != null)
            {
                mWaveOutDevice.Dispose();
                mWaveOutDevice = null;
            }
        }        

        #endregion

        #region - Public Methods -

        /// <summary>
        /// Starts recording sound
        /// </summary>
        public void StartRecording(string path)
        {
            Stop();
            mWaveInDevice = new WaveIn();
            mWaveInDevice.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100,2);
            mWaveInDevice.DataAvailable += _waveIn_DataAvailable;
            mWaveInDevice.RecordingStopped += _waveIn_RecordingStopped;

            mSampleAggregator = new SampleAggregator(mcfftDataSize);
            mWaveFileWriter = new WaveFileWriter(path, mWaveInDevice.WaveFormat);

            mCurrentFilePath = path;
            mWaveInDevice.StartRecording();
            IsRecording = true;    
        }


        /// <summary>
        /// Stops recording sound
        /// </summary>
        public void StopRecording()
        {
            mWaveInDevice.Dispose();
            mWaveInDevice = null;
            mWaveFileWriter.Close();
            mWaveFileWriter.Dispose();
            mWaveFileWriter = null;
        }

        /// <summary>
        /// Stops playing of .mp3 file
        /// </summary>
        public void Stop()
        {
            if (mWaveOutDevice != null)
            {
                ChannelPosition = 0;
                mWaveOutDevice.Stop();
                mWaveOutDevice.Dispose();
                mWaveOutDevice = null;
                if (ActiveStream != null)
                {
                    ActiveStream.Dispose();
                    ActiveStream = null;
                }
                if (mInputStream != null)
                {
                    mInputStream.Dispose();
                    mInputStream = null;
                }
            }
            IsPlaying = false;
            CanStop = false;
            CanPlay = true;
            CanPause = false;
        }

        /// <summary>
        /// Pauses playing of .mp3 file
        /// </summary>
        public void Pause()
        {
            if (IsPlaying && CanPause)
            {
                mWaveOutDevice.Pause();
                IsPlaying = false;
                CanPlay = true;
                CanPause = false;
            }
        }

        /// <summary>
        /// Starts playing of .mp3 file
        /// </summary>
        public void Play()
        {
            if (CanPlay)
            {
                try
                {
                    mWaveOutDevice.Play();
                    IsPlaying = true;
                    CanPause = true;
                    CanPlay = false;
                    CanStop = true;
                }
                catch
                {
                    IsPlaying = false;
                    CanPlay = true;
                    CanPause = false;
                    CanStop = false;
                }
            }
        }

        /// <summary>
        /// Opens .mp3 file and prepares for playing 
        /// </summary>
        /// <param name="path">Path of .mp3 file which we want to play</param>
        public void OpenFile(string path)
        {
            Stop();

            if (ActiveStream != null)
            {
                SelectionBegin = TimeSpan.Zero;
                SelectionEnd = TimeSpan.Zero;
                ChannelPosition = 0;
            }
            
            _stopAndCloseStream();            

            if (File.Exists(path))
            {
                try
                {
                    mWaveOutDevice = new WaveOut()
                    {
                        DesiredLatency = 100
                    };
                    ActiveStream = new Mp3FileReader(path); 
                    mInputStream = new WaveChannel32(ActiveStream);
                    mSampleAggregator = new SampleAggregator(mcfftDataSize);
                    mInputStream.Sample += _inputStream_Sample;
                    mWaveOutDevice.Init(mInputStream);
                    ChannelLength = mInputStream.TotalTime.TotalSeconds;
                    CanPlay = true;
                }
                catch(Exception ex)
                {
                    ActiveStream = null;
                    CanPlay = false;
                }
            }
        }
        #endregion

        #region - Properties -

        /// <summary>
        /// Stream of loaded .mp3 file
        /// </summary>
        public WaveStream ActiveStream
        {
            get { return mActiveStream; }
            protected set
            {
                WaveStream _oldValue = mActiveStream;
                mActiveStream = value;
                if (_oldValue != mActiveStream)
                    NotifyPropertyChanged("ActiveStream");
            }
        }

        /// <summary>
        /// Can player play .mp3 file
        /// </summary>
        public bool CanPlay
        {
            get { return mCanPlay; }
            protected set
            {
                bool _oldValue = mCanPlay;
                mCanPlay = value;
                if (_oldValue != mCanPlay)
                    NotifyPropertyChanged("CanPlay");
            }
        }

        /// <summary>
        /// Can player pause playing
        /// </summary>
        public bool CanPause
        {
            get { return mCanPause; }
            protected set
            {
                bool _oldValue = mCanPause;
                mCanPause = value;
                if (_oldValue != mCanPause)
                    NotifyPropertyChanged("CanPause");
            }
        }

        /// <summary>
        /// Can player stop playing
        /// </summary>
        public bool CanStop
        {
            get { return mCanStop; }
            protected set
            {
                bool _oldValue = mCanStop;
                mCanStop = value;
                if (_oldValue != mCanStop)
                    NotifyPropertyChanged("CanStop");
            }
        }

        /// <summary>
        /// Is player playing .mp3 file
        /// </summary>
        public bool IsPlaying
        {
            get { return mIsPlaying; }
            protected set
            {
                bool _oldValue = mIsPlaying;
                mIsPlaying = value;
                if (_oldValue != mIsPlaying)
                    NotifyPropertyChanged("IsPlaying");
                mcPositionTimer.IsEnabled = value;
            }
        }
        
        /// <summary>
        /// Is player recording sound
        /// </summary>
        public bool IsRecording
        {
            get { return mIsRecording; }
            set
            {
                mIsRecording = value;
                NotifyPropertyChanged("IsRecording");
            }
        }
        #endregion
    }
}
