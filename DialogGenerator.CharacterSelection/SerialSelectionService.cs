using DialogGenerator.CharacterSelection.Helper;
using DialogGenerator.CharacterSelection.Model.Exceptions;
using DialogGenerator.CharacterSelection.Workflow;
using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.UI.View.Dialogs;
using DialogGenerator.UI.View.Services;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DialogGenerator.CharacterSelection
{
    public class SerialSelectionService : ICharacterSelection
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IMessageDialogService mDialogService;
        public const int StrongRssiBufDepth = 12;
        private int mTempCh1;
        private int mRowNum;
        private int mTempch2;
        private int[,] mStrongRssiCharacterPairBuf = new int[2, StrongRssiBufDepth];
        private int[] mNewRow = new int[ApplicationData.Instance.NumberOfRadios + 1];
        private SerialPort mSerialPort;
        private CancellationTokenSource mCancellationTokenSource;
        private SerialSelectionWorkflow mWorkflow;
        private readonly DispatcherTimer mcHeatMapUpdateTimer = new DispatcherTimer();
        private Random mRandom = new Random();

        public int BigRssi = 0;
        public int CurrentCharacter1;
        public int CurrentCharacter2 = 1;
        public static int NextCharacter1 = 1;
        public static int NextCharacter2 = 2;
        public static int[,] HeatMap = new int[ApplicationData.Instance.NumberOfRadios, ApplicationData.Instance.NumberOfRadios];
        public static DateTime[] CharactersLastHeatMapUpdateTime = new DateTime[ApplicationData.Instance.NumberOfRadios];
        public readonly TimeSpan MaxLastSeenInterval = new TimeSpan(0, 0, 0, 3, 100);

        #endregion

        #region - constructor -

        public SerialSelectionService(ILogger logger,IEventAggregator _eventAggregator,IMessageDialogService _dialogService)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mDialogService = _dialogService;
            mWorkflow = new SerialSelectionWorkflow(() => { });

            _configureWorkflow();
            mcHeatMapUpdateTimer.Interval = TimeSpan.FromSeconds(3);
            mcHeatMapUpdateTimer.Tick += _heatMapUpdateTimer_Tick;
        }

        #endregion

        #region - event handlers -

        private void _heatMapUpdateTimer_Tick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }


        private void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            mLogger.Error("_serialPort_ErrorReceived " + e.EventType != null ? e.EventType.ToString() : "");
        }

        #endregion

        #region - private functions -

        private void _configureWorkflow()
        {
            mWorkflow.Configure(States.Start)
                .Permit(Triggers.Initialize, States.Init);

            mWorkflow.Configure(States.Init)
                .Permit(Triggers.ReadMessage, States.ReadMessage)
                .Permit(Triggers.SerialPortNameError, States.SerialPortNameError)
                .Permit(Triggers.Finish, States.Finish);

            mWorkflow.Configure(States.Idle)
                .Permit(Triggers.ReadMessage, States.ReadMessage)
                .Permit(Triggers.Finish, States.Finish);

            mWorkflow.Configure(States.SerialPortNameError)
                .Permit(Triggers.Initialize, States.Init)
                .Permit(Triggers.Start, States.Start);

            mWorkflow.Configure(States.USB_disconnectedError)
                .Permit(Triggers.Initialize, States.Init)
                .Permit(Triggers.Finish, States.Finish);

            mWorkflow.Configure(States.ReadMessage)
                .Permit(Triggers.FindClosestPair, States.FindClosestPair)
                .Permit(Triggers.USB_disconnectedError, States.USB_disconnectedError)
                .Permit(Triggers.Idle, States.Idle)
                .Permit(Triggers.Finish, States.Finish);

            mWorkflow.Configure(States.FindClosestPair)
                .Permit(Triggers.ReadMessage, States.ReadMessage)
                .Permit(Triggers.SelectNextCharacters, States.SelectNextCharacters)
                .Permit(Triggers.Finish, States.Finish);

            mWorkflow.Configure(States.SelectNextCharacters)
                .Permit(Triggers.ReadMessage, States.ReadMessage)
                .Permit(Triggers.Finish, States.Finish);

            mWorkflow.Configure(States.Finish)
                .OnEntry(t => _finishSelection())
                .Permit(Triggers.Start, States.Start);
        }



        private Triggers _initSerial()
        {
            try
            {
                NextCharacter1 = 0;
                NextCharacter2 = 0;

                mSerialPort = new SerialPort();
                mSerialPort.ErrorReceived += _serialPort_ErrorReceived;
                mSerialPort.PortName = ApplicationData.Instance.ComPortName;
                mSerialPort.BaudRate = 460800;
                mSerialPort.Handshake = Handshake.None;
                mSerialPort.ReadTimeout = 500;
                mSerialPort.Open();
                mSerialPort.DiscardInBuffer();

                mcHeatMapUpdateTimer.Start();

                return Triggers.ReadMessage;
            }
            catch (InvalidOperationException ex)  // Instance of SerialPort is already open and wi will redirect for reading messages
            {
                mLogger.Error("InvalidOperationException _initSerial  " + ex.Message);
                return Triggers.ReadMessage;
            }
            catch (ArgumentException ex) // invalid port name (name is not formed as COM + digit)
            {
                mLogger.Error("ArgumentException _initSerial  " + ex.Message);
                return Triggers.SerialPortNameError;
            }
            catch (IOException ex) // com port doesn't exists (usb is disconnected or not valid COM port name)
            {
                mLogger.Error("IOException _initSerial " + ex.Message);
                return Triggers.SerialPortNameError;
            }
        }


        private void _finishSelection()
        {
            try
            {
                mSerialPort.Close();  // Close() method calls Dispose() se we don't need to call Dispose()
                mcHeatMapUpdateTimer.Stop();
            }
            catch (IOException ex)
            {
                mLogger.Error("_finishSelection " + ex.Message);
            }

            mWorkflow.Fire(Triggers.Start);
        }


        private  Triggers _usbDisconectedError()
        {
            var result = mDialogService.ShowOKCancelDialog("USB disconected.Please check connection and try again.",
                "Error","Try again","Finish dialog");

            if (result == MessageDialogResult.OK)
                return Triggers.Initialize;
            else
            {
                StopCharacterSelection();

                return Triggers.Finish;
            }
        }


        private Triggers _serialPortError()
        {
            var result = mDialogService.ShowDedicatedDialog(DialogConstants.COM_PORT_ERROR_DLG);

            if (result == MessageDialogResult.OK)
            {
                return Triggers.Initialize;
            }
            else
            {
                return Triggers.Start;
            }
        }


        private Triggers _readMessage()
        {
            try
            {
                _resetData();
                string message = null;

                message = _readSerialInLine();

                if (message == null)
                {
                    return Triggers.Idle;
                }

                mRowNum = ParseMessageHelper.Parse(message, ref mNewRow);

                if (mRowNum < 0 || mRowNum >= ApplicationData.Instance.NumberOfRadios)
                {
                    return Triggers.Idle;
                }
                else
                {
                    ParseMessageHelper.ProcessMessage(mRowNum, mNewRow);
                    return Triggers.FindClosestPair;
                }
            }
            catch (COMPortClosedException ex)
            {
                mLogger.Error("_readMessage ");
                return Triggers.USB_disconnectedError;
            }
            catch (TimeoutException ex)
            {
                mLogger.Error("_readMessage  " + ex.Message);
                return Triggers.Idle;
            }
            catch (InvalidOperationException ex) // port is closed
            {
                mLogger.Error("_readMessage  " + ex.Message);
                return Triggers.Initialize;
            }
            catch (Exception ex)
            {
                mLogger.Error("_readMessage " + ex.Message);
                return Triggers.Idle;
            }
        }


        private void _resetData()
        {
            mTempCh1 = 0;
            mTempch2 = 0;
        }


        private bool _calculateRssiStable(int _ch1, int _ch2)
        {
            try
            {
                bool _rssiStable = true;

                for (int _i = 0; _i < StrongRssiBufDepth - 1; _i++)
                {
                    // scoot data in buffer back by one to make room for next
                    mStrongRssiCharacterPairBuf[0, _i] = mStrongRssiCharacterPairBuf[0, _i + 1];
                    mStrongRssiCharacterPairBuf[1, _i] = mStrongRssiCharacterPairBuf[1, _i + 1];
                }

                mStrongRssiCharacterPairBuf[0, StrongRssiBufDepth - 1] = _ch1;
                mStrongRssiCharacterPairBuf[1, StrongRssiBufDepth - 1] = _ch2;

                for (int _i = 0; _i < StrongRssiBufDepth - 2; _i++)
                {
                    if ((mStrongRssiCharacterPairBuf[0, _i] != mStrongRssiCharacterPairBuf[0, _i + 1]
                        || mStrongRssiCharacterPairBuf[1, _i] != mStrongRssiCharacterPairBuf[1, _i + 1])
                        &&
                          (mStrongRssiCharacterPairBuf[0, _i] != mStrongRssiCharacterPairBuf[1, _i + 1]
                        || mStrongRssiCharacterPairBuf[1, _i] != mStrongRssiCharacterPairBuf[0, _i + 1]))
                    {
                        _rssiStable = false;
                        break;
                    }
                }

                return _rssiStable;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private int _getCharacterMappedIndex(int _radioNum)
        {
            try
            {
                // if we find character return its index , or throw exception if there is no character with 
                //specified radio assigned.  First() - throws exception if no items found
                // FirstOrDefault() - returns first value or default value (for reference type it is null)
                int index = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS)
                    .Select((c, i) => new { Character = c, Index = i })
                    .Where(x => x.Character.RadioNum == _radioNum)
                    .Select(x => x.Index)
                    .First();

                return index;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("No character assigned to radio with number " + _radioNum + " .");
                return -1;
            }
        }


        private bool _assignNextCharacters(int _tempCh1, int _tempCh2)
        {
            try
            {
                int _nextCharacter1MappedIndex1, _nextCharacter1MappedIndex2;
                bool _nextCharactersAssigned = false;

                if (mRandom.NextDouble() > 0.5)
                {
                    _nextCharacter1MappedIndex1 = _getCharacterMappedIndex(_tempCh1);
                    _nextCharacter1MappedIndex2 = _getCharacterMappedIndex(_tempCh2);
                }
                else
                {
                    _nextCharacter1MappedIndex1 = _getCharacterMappedIndex(_tempCh2);
                    _nextCharacter1MappedIndex2 = _getCharacterMappedIndex(_tempCh1);
                }

                if (_nextCharacter1MappedIndex1 >= 0 && _nextCharacter1MappedIndex2 >= 0)
                {
                    NextCharacter1 = _nextCharacter1MappedIndex1;
                    NextCharacter2 = _nextCharacter1MappedIndex2;

                    if ((NextCharacter1 != CurrentCharacter1 || NextCharacter2 != CurrentCharacter2) &&
                         (NextCharacter2 != CurrentCharacter1 || NextCharacter1 != CurrentCharacter2))
                    {
                        _nextCharactersAssigned = true;
                    }
                }

                return _nextCharactersAssigned;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private Triggers _findBiggestRssiPair()
        {
            //  This method takes the RSSI values and combines them so that the RSSI for Ch2 looking at 
            //  Ch1 is added to the RSSI for Ch1 looking at Ch2

            try
            {
                bool _rssiStable = false;
                int i = 0, j = 0;

                var _currentTime = DateTime.Now;
                mTempCh1 = NextCharacter1;
                mTempch2 = NextCharacter2;

                if (mTempCh1 > ApplicationData.Instance.NumberOfRadios - 1 || mTempch2 > ApplicationData.Instance.NumberOfRadios - 1 ||
                    mTempCh1 < 0 || mTempch2 < 0 || mTempCh1 == mTempch2)
                {
                    mTempCh1 = 0;
                    mTempch2 = 1;
                }

                //only pick up new characters if bigRssi greater not =
                BigRssi = HeatMap[mTempCh1, mTempch2] + HeatMap[mTempch2, mTempCh1];

                for (i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                {
                    // it shouldn't happen often that a character has dissapeared, if so zero him out
                    if (_currentTime - CharactersLastHeatMapUpdateTime[i] > MaxLastSeenInterval)
                    {
                        for (j = 0; j < ApplicationData.Instance.NumberOfRadios; j++)
                        {
                            HeatMap[i, j] = 0;
                        }
                    }
                    for (j = i + 1; j < ApplicationData.Instance.NumberOfRadios; j++)
                    {
                        // only need data above the matrix diagonal
                        if (HeatMap[i, j] + HeatMap[j, i] > BigRssi)
                        {
                            // look at both characters view of each other
                            BigRssi = HeatMap[i, j] + HeatMap[j, i];
                            mTempCh1 = i;
                            mTempch2 = j;
                        }
                    }
                }

                _rssiStable = _calculateRssiStable(mTempCh1, mTempch2);

                if (_rssiStable)
                {
                    return Triggers.SelectNextCharacters;
                }
                else
                {
                    return Triggers.ReadMessage;
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_findBiggestRssiPair " + ex.Message);
                return Triggers.ReadMessage;
            }
        }


        private Triggers _selectNextCharacters()
        {
            try
            {
                bool _charactersAssigned = _assignNextCharacters(mTempCh1, mTempch2);

                if (_charactersAssigned)
                {

                        mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().
                            Publish(new SelectedCharactersPairEventArgs { Character1Index = NextCharacter1, Character2Index = NextCharacter2 });

                    mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();

                    CurrentCharacter1 = NextCharacter1;
                    CurrentCharacter2 = NextCharacter2;
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_selectNextCharacters " + ex.Message);
            }

            return Triggers.ReadMessage;
        }


        private string _readSerialInLine()
        {
            string _message = null;

            try
            {
                if (!mSerialPort.IsOpen)
                    throw new COMPortClosedException();

                if (mSerialPort.BytesToRead > 18)
                {
                    _message = mSerialPort.ReadLine();

                    if (mSerialPort.BytesToRead > 1000)
                    {
                        // got behind for some reason
                        mSerialPort.DiscardInBuffer();

                        mLogger.Debug("serial buffer over run.");
                    }
                }
            }
            catch (COMPortClosedException ex)
            {
                throw ex;
            }
            catch (TimeoutException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)  // port is not open
            {
                throw ex;
            }

            return _message;
        }

        #endregion

        public Task StartCharacterSelection()
        {
            throw new System.NotImplementedException();
        }

        public void StopCharacterSelection()
        {
            throw new System.NotImplementedException();
        }
    }
}
