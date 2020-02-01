using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Helper;
using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.CharacterSelection.Workflow;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using Prism.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.CharacterSelection
{
    public class BLESelectionService : ICharacterSelection
    {
        #region - fields -

        public const int StrongRssiBufDepth = /* 26 */ 5;  // TODO we should use a timer as well as relying on a number of incoming packets to switch characters

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private IBLEDataProviderFactory mBLEDataProviderFactory;
        private IBLEDataProvider mCurrentDataProvider;
        private int mTempCh1;
        private int mTempch2;
        private int[,] mStrongRssiCharacterPairBuf = new int[2, StrongRssiBufDepth];
        private CancellationTokenSource mCancellationTokenSource;
        private BLESelectionWorkflow mWorkflow;
        private Timer mcHeatMapUpdateTimer;
        private Random mRandom = new Random();
        private States mCurrentState;
        private int mFailedBLEMessageAttempts = 0;
        private long mIddleTime = 0;
        private long mLastAccessTime = 0;
        private bool mRestartRequested = false;
        private bool mFirstFailMessage = true;
        private bool mFreshStart = true;

        private int BigRssi = 0;
        private int CurrentCharacter1;
        private int CurrentCharacter2 = 1;
        private static int NextCharacter1 = 1;
        private static int NextCharacter2 = 2;
        public static int[,] HeatMap = new int[ApplicationData.Instance.NumberOfRadios, ApplicationData.Instance.NumberOfRadios];
        public static int[] MotionVector = new int[ApplicationData.Instance.NumberOfRadios];
        public static DateTime[] CharactersLastHeatMapUpdateTime = new DateTime[ApplicationData.Instance.NumberOfRadios];
        public readonly TimeSpan MaxLastSeenInterval = new TimeSpan(0, 0, 0, 4, 100);

        public int mStableRadioIndex1 = -1;
        public int mStableRadioIndex2 = -1;
        public long mLastStableTime = 0;
        
        #endregion

        #region - constructor -

        public BLESelectionService(ILogger logger, IEventAggregator _eventAggregator,
            ICharacterRepository _characterRepository, IBLEDataProviderFactory _BLEDataProviderFactory)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mCharacterRepository =  _characterRepository ;
            mBLEDataProviderFactory = _BLEDataProviderFactory;

            mWorkflow = new BLESelectionWorkflow(() => { });
            mWorkflow.PropertyChanged += _mWorkflow_PropertyChanged;

            _configureWorkflow();
        }
        
        private void _mWorkflow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("State"))
            {
                CurrentState = mWorkflow.State;
            }
        }

        #endregion

        #region - event handlers -

        private void _heatMapUpdateTimer_Tick(object sender, EventArgs e)
        {
            mEventAggregator.GetEvent<HeatMapUpdateEvent>().Publish(new HeatMapData
            {
                HeatMap = HeatMap,
                MotionVector = MotionVector,
                LastHeatMapUpdateTime = CharactersLastHeatMapUpdateTime,
                Character1Index = NextCharacter1,
                Character2Index = NextCharacter2
            });
        }

        private void _updateTimer(object state)
        {
            mEventAggregator.GetEvent<HeatMapUpdateEvent>().Publish(new HeatMapData
            {
                HeatMap = HeatMap,
                MotionVector = MotionVector,
                LastHeatMapUpdateTime = CharactersLastHeatMapUpdateTime,
                Character1Index = NextCharacter1,
                Character2Index = NextCharacter2
            });
        }

        #endregion

        #region - private functions -

        private void _configureWorkflow()
        {
            mWorkflow.Configure(States.Waiting)
                .Permit(Triggers.Initialize, States.Initializing);

            mWorkflow.Configure(States.Initializing)
                .Permit(Triggers.ProcessMessage, States.ProcessingMessage)
                .Permit(Triggers.Finish, States.Finished);

            mWorkflow.Configure(States.ProcessingMessage)
                .PermitReentry(Triggers.ProcessMessage)
                .Permit(Triggers.Finish,States.Finished)
                .Permit(Triggers.CalculateClosestPair, States.CalculatingClosestPair);

            mWorkflow.Configure(States.CalculatingClosestPair)
                .Permit(Triggers.ProcessMessage, States.ProcessingMessage)
                .Permit(Triggers.SelectNextCharacters, States.SelectingNextCharacters)
                .Permit(Triggers.Finish, States.Finished);

            mWorkflow.Configure(States.SelectingNextCharacters)
                .Permit(Triggers.ProcessMessage, States.ProcessingMessage)
                .Permit(Triggers.Finish, States.Finished);

            mWorkflow.Configure(States.Finished)
                .OnEntry(t => _finishSelection())
                .Permit(Triggers.Wait, States.Waiting);
        }

        private  Triggers _initialize()
        {
            mCurrentDataProvider = mBLEDataProviderFactory.Create(BLEDataProviderType.WinBLEWatcher);
            return Triggers.ProcessMessage;
        }

        private void _finishSelection()
        {           
            mcHeatMapUpdateTimer.Dispose();
            mWorkflow.Fire(Triggers.Wait);
        }

        private long _toMilliseconds(DateTime date)
        {
            return (long)(date - new DateTime(2000, 1, 1)).TotalMilliseconds;
        }

        private Triggers _processMessage()
        {
            try
            {
                mTempCh1 = 0;
                mTempch2 = 0;
                BLE_Message message = new BLE_Message();
                DateTime nowTime = DateTime.Now;

                message = mCurrentDataProvider.GetMessage();

                if (message == null)
                {

                    DateTime _nowTime = DateTime.Now;

                    if (mFirstFailMessage)
                    {
                        mFirstFailMessage = false;
                        mLastAccessTime = _toMilliseconds(_nowTime) ;
                    }

                    mIddleTime += _toMilliseconds(_nowTime) - mLastAccessTime;
                    mLastAccessTime = _toMilliseconds(_nowTime);
                    mFailedBLEMessageAttempts++;
                    
                    if (mIddleTime > 1500)                    
                    {                        
                        mIddleTime = 0l;
                        mRestartRequested = true;
                        mLogger.Info("BLE Messages have not arrived in some time, switching to avatar arena");
                        mCancellationTokenSource.Cancel();                        
                    }

                    //Console.Out.WriteLine("Failed messages " + mFailedBLEMessageAttempts);
                    Console.Out.WriteLine("Iddle time: {0}", mIddleTime);
                    return Triggers.ProcessMessage;
                } else
                {
                    mLastAccessTime = _toMilliseconds(DateTime.Now);
                    mIddleTime = 0L;
                    mFailedBLEMessageAttempts = 0;
                }
                
                BLE_Message mNewRow = new BLE_Message();  // added number holds sequence number
                mLastAccessTime = _toMilliseconds(DateTime.Now);

                var mRowNum = ParseMessageHelper.ParseBle(message, mNewRow);

                if (mRowNum < 0 || mRowNum >= ApplicationData.Instance.NumberOfRadios)
                {
                    return Triggers.ProcessMessage;
                }
                else
                {
                    ParseMessageHelper.ProcessTheMessage(mRowNum, mNewRow);
                    return Triggers.CalculateClosestPair;
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_readMessage " + ex.Message);
                return Triggers.ProcessMessage;
            }
        }
 
        /// <summary>
        /// This method calculates whether all characters have been "still" for a set number (300) of milliseconds since 
        /// motion was last detected.  The talking characters should not change unless the toys moved.  The talking
        /// characters should not change unless the toys have been as still as they can be when held in the hand for
        /// a set number of milliseconds in the case the user is holding one or more of the toys they wish to speak.
        /// </summary>
        /// <returns></returns>
        private bool _calculateIfInMotionWindow()
        {
            try
            {
                DateTime _currentTime = DateTime.Now;
                int i = ParseMessageHelper.ReceivedMessages.Count - 1;

                if (i < 8)
                    return false;  // not enough readings even if over timespan of > 300ms

                while (i >= 0)
                {
                    if (i<=0)
                    {
                        return false;  // didn't have two seconds of data in ReceivedMessages
                    }
                    var _timeAgoOfMessage_i = _currentTime - ParseMessageHelper.ReceivedMessages[i].ReceivedTime;
                    if (_timeAgoOfMessage_i < TimeSpan.FromMilliseconds(300))
                    {  // in the needsToBeStableWindow
                        if (ParseMessageHelper.ReceivedMessages[i].Motion > 48)
                        {  // if the motion was high in the last 300 ms we aren't done moving
                            return false;
                        }
                    }
                    if (_timeAgoOfMessage_i >= TimeSpan.FromMilliseconds(300)) 
                    {  // in the needsToBeStableWindow
                        if (ParseMessageHelper.ReceivedMessages[i].Motion > 40)  // we had motion between 300-1500ms ago
                        {
                            ReceivedMessage msg = ParseMessageHelper.ReceivedMessages[i];
                            string dbgOut = msg.CharacterPrefix + " ";
                            dbgOut += msg.Motion.ToString("D3");
                            dbgOut += " - ";
                            dbgOut += msg.ReceivedTime.ToString("HH:mm:ss");
                            mLogger.Info(dbgOut);

                            return true;
                        }
                    }
                    i--;
                    if (_timeAgoOfMessage_i > TimeSpan.FromMilliseconds(1500))  //we are past the window
                        return false;
                }
                return false;  
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private bool _calculateRssiStableAfterChange(int _Ch1, int _Ch2, long _Milliseconds = 80)
        {           
            if((_Ch1 != mStableRadioIndex1 || _Ch2 != mStableRadioIndex2) && 
                (_Ch1 != mStableRadioIndex2 || _Ch2 != mStableRadioIndex1))
            {
                mLastStableTime = _toMilliseconds(DateTime.Now);
                mStableRadioIndex1 = _Ch1;
                mStableRadioIndex2 = _Ch2;
                return false;
            }

            long _currentTime = _toMilliseconds(DateTime.Now);
            if(_currentTime - mLastStableTime >= _Milliseconds)
            {
                return true;
            }

            return false;
        }

        private int _getCharacterMappedIndex(int _radioNum)
        {
            try
            {
                // if we find character return its index , or throw exception if there is no character with 
                //specified radio assigned.  First() - throws exception if no items found
                // FirstOrDefault() - returns first value or default value (for reference type it is null)
                int index = mCharacterRepository.GetAll()
                    .Select((c, i) => new { Character = c, Index = i })
                    .Where(x => x.Character.RadioNum == _radioNum)
                    .Select(x => x.Index)
                    .First();

                return index;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private bool _assignNextCharacters(int _tempCh1, int _tempCh2)
        {
            try
            {
                mLogger.Info(String.Format("Trying to assign characters ch1_radio = {0}, ch2_radio = {1}", _tempCh1, _tempCh2));

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
                         (NextCharacter2 != CurrentCharacter1 || NextCharacter1 != CurrentCharacter2) || mFreshStart)
                    {
                        _nextCharactersAssigned = true;
                        mLogger.Info("New characters assigned by BLE Character 1 is " + NextCharacter1 +
                            " Character 2 is " + NextCharacter2);
                    } else
                    {
                        mLogger.Info(String.Format("Characters not assigned! Current1 = {0}, Next1 = {1}, Current2 = {2}, Next2 = {3}"
                            , CurrentCharacter1
                            , NextCharacter1
                            , CurrentCharacter2
                            , NextCharacter2));
                    }
                } else
                {
                    mLogger.Info(String.Format("Characters not assigned. ch1 = {0}, ch2 = {1}", _nextCharacter1MappedIndex1, _nextCharacter1MappedIndex2));
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
                int i = 0, j = 0, k = 0;

                var _currentTime = DateTime.Now;

                Character _ch1 = mCharacterRepository.GetAll()[NextCharacter1];
                Character _ch2 = mCharacterRepository.GetAll()[NextCharacter2];

                if (_ch1 != null && _ch1.RadioNum != -1 && _ch2 != null && _ch2.RadioNum != -1 && _ch1.RadioNum != _ch2.RadioNum)
                {
                    mTempCh1 = _ch1.RadioNum;
                    mTempch2 = _ch2.RadioNum;
                }
                else
                {
                    mTempCh1 = 0;
                    mTempch2 = 1;
                }

                if (mTempCh1 > ApplicationData.Instance.NumberOfRadios - 1 || mTempch2 > ApplicationData.Instance.NumberOfRadios - 1 ||
                    mTempCh1 < 0 || mTempch2 < 0 || mTempCh1 == mTempch2)
                {
                    mTempCh1 = 0;
                    mTempch2 = 1;
                }

                //only pick up new characters if bigRssi greater not ==
                BigRssi = HeatMap[mTempCh1, mTempch2] + HeatMap[mTempch2, mTempCh1];

                for (i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                {
                    // it shouldn't happen often that a character has dissapeared, if so zero him out
                    if (_currentTime - CharactersLastHeatMapUpdateTime[i] > MaxLastSeenInterval)
                    {
                        int alreadyZero = 0;   // to prevent logging messages each time we pass through
                        for (k = 0; k < ApplicationData.Instance.NumberOfRadios; k++)
                        {
                            alreadyZero += HeatMap[i, k];
                            HeatMap[i, k] = 0;
                        }
                        if (alreadyZero > 0)
                        {
                            mLogger.Error("_findBiggestRssiPair MaxLastSeenInterval radio # " + i);
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

                // TODO, _calculateRssiStableAfterChange and _calculateIfInMotionWindow should be their own states
                _rssiStable = _calculateRssiStableAfterChange(mTempCh1, mTempch2);
                var _inMovementWindow = _calculateIfInMotionWindow();

                if ( _rssiStable && (_inMovementWindow || mFreshStart))
                {
                    if(mFreshStart)
                    {
                        if(_calculateRssiStableAfterChange(mTempCh1, mTempch2, 1000))
                        {
                            mFreshStart = false;
                        }                        
                    }

                    mLogger.Info("Select Next Characters Called");
                    return Triggers.SelectNextCharacters;
                }
                else
                {
                    return Triggers.ProcessMessage;
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_findBiggestRssiPair " + ex.Message);
                return Triggers.ProcessMessage;
            }
        }

        private Triggers _selectNextCharacters()
        {
            try
            {
                if (_assignNextCharacters(mTempCh1, mTempch2))
                {
                    mLogger.Info("Characters assigned");

                    Session.Set(Constants.NEXT_CH_1, NextCharacter1);
                    Session.Set(Constants.NEXT_CH_2, NextCharacter2);

                    CurrentCharacter1 = NextCharacter1;
                    CurrentCharacter2 = NextCharacter2;

                    mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();

                    mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().
                        Publish(new SelectedCharactersPairEventArgs { Character1Index = CurrentCharacter1, Character2Index = CurrentCharacter2 });
                   
                    mEventAggregator.GetEvent<HeatMapUpdateEvent>().Publish(new HeatMapData
                    {
                        HeatMap = HeatMap,
                        MotionVector = MotionVector,
                        LastHeatMapUpdateTime = CharactersLastHeatMapUpdateTime,
                        Character1Index = NextCharacter1,
                        Character2Index = NextCharacter2
                    });
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_selectNextCharacters " + ex.Message);
            }

            return Triggers.ProcessMessage;
        }

        #endregion

        #region - public functions - 

        /// <summary>
        /// This task starts the BLEWatcher and begins reading and processing of BLE messages.
        /// as well as looking for the bigest RSSI pairs to select the next characters
        /// </summary>
        public async Task StartCharacterSelection()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mFailedBLEMessageAttempts = 0;

            await Task.Run(async() =>
            {
                Thread.CurrentThread.Name = "StartCharacterSelection";

                mWorkflow.Fire(Triggers.Initialize);
                mcHeatMapUpdateTimer = new Timer(_updateTimer, null, 0, 300);
                Triggers next = _initialize();
                Task _BLEDataReaderTask = mCurrentDataProvider.StartReadingData();

                do
                {
                    switch (next)
                    {
                        case Triggers.Initialize:
                            {
                                next =  _initialize();
                                break;
                            }
                        case Triggers.ProcessMessage:
                            {
                                next = _processMessage();
                                break;
                            }
                        case Triggers.CalculateClosestPair:
                            {
                                next = _findBiggestRssiPair();
                                break;
                            }
                        case Triggers.SelectNextCharacters:
                            {
                                next = _selectNextCharacters();
                                break;
                            }
                    }

                    mWorkflow.Fire(next);

                    Thread.Sleep(1);
                    
                }
                while (!mCancellationTokenSource.IsCancellationRequested);

                
                if(mRestartRequested)
                {
                    mcHeatMapUpdateTimer.Dispose();
                    mCurrentDataProvider.StopReadingData();
                    Session.Set(Constants.BLE_MODE_ON, false);
                    Session.Set(Constants.NEEDS_RESTART, true);
                    Console.Out.WriteLine("Restart of Dialog Engine required!!!");
                               
                } 

                await _BLEDataReaderTask;

                mWorkflow.Fire(Triggers.Finish);
            });          
        }

        public void StopCharacterSelection()
        {
            mcHeatMapUpdateTimer.Dispose();
            mCurrentDataProvider.StopReadingData();
            mCancellationTokenSource.Cancel();
        }

        #endregion

        #region - properties -

        public States CurrentState
        {
            get { return mCurrentState; }
            private set { mCurrentState = value; }
        }

        #endregion
    }
}
