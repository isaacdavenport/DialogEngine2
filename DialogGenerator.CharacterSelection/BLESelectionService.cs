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

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private IBLEDataProviderFactory mBLEDataProviderFactory;
        private IBLEDataProvider mCurrentDataProvider;
        private int mPossibleSpeakingCh1RadioNum;
        private int mPossibleSpeakingCh2RadioNum;
        private CancellationTokenSource mCancellationTokenSource;
        private BLESelectionWorkflow mWorkflow;
        private Timer mcHeatMapUpdateTimer;
        private Random mRandom = new Random();
        private States mCurrentState;
        private int mFailedBLEMessageAttempts = 0;
        private long mIdleTime = 0;
        private long mLastAccessTime = 0;
        private bool mRestartRequested = false;
        private bool mFirstFailMessage = true;
        private bool mFreshStart = true;

        private int CurrentCharacter1;
        private int CurrentCharacter2 = 1;
        private static int NextCharacter1 = 1;
        private static int NextCharacter2 = 2;
        public static int[,] HeatMap = new int[ApplicationData.Instance.NumberOfRadios, ApplicationData.Instance.NumberOfRadios];
        public static int[] MotionVector = new int[ApplicationData.Instance.NumberOfRadios];
        public static DateTime[] CharactersLastHeatMapUpdateTime = new DateTime[ApplicationData.Instance.NumberOfRadios];
        public readonly TimeSpan MaxLastSeenInterval = new TimeSpan(0, 0, 0, 6, 100);

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
                mPossibleSpeakingCh1RadioNum = 0;
                mPossibleSpeakingCh2RadioNum = 0;
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

                    mIdleTime += _toMilliseconds(_nowTime) - mLastAccessTime;
                    mLastAccessTime = _toMilliseconds(_nowTime);
                    mFailedBLEMessageAttempts++;
                    
                    if (mIdleTime > 1500)                    
                    {                        
                        mIdleTime = 0L;
                        mRestartRequested = true;
                        mLogger.Info("BLE Messages have not arrived in some time, switching to avatar arena");
                        mCancellationTokenSource.Cancel();                        
                    }

                    Console.Out.WriteLine("Iddle time: {0}", mIdleTime);
                    return Triggers.ProcessMessage;
                } 

                mLastAccessTime = _toMilliseconds(DateTime.Now);
                mIdleTime = 0L;
                mFailedBLEMessageAttempts = 0;
                BLE_Message mNewRow = new BLE_Message();  // added number holds sequence number
                
                var mRowNum = ParseMessageHelper.ParseBle(message, mNewRow);

                if (mRowNum < 0 || mRowNum >= ApplicationData.Instance.NumberOfRadios)
                {
                    return Triggers.ProcessMessage;
                }
                else
                {
                    ParseMessageHelper.UpdateHeatMapWithMessage(mRowNum, mNewRow);
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
        /// This method calculates whether all radios have been "still" for a set number (300) of milliseconds since 
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

                if (i < 5)
                    return false;  // not enough readings even if over timespan of > 300ms

                while (i >= 0)
                {
                    if (i<=0)
                    {
                        return false;  // didn't have long enough data in ReceivedMessages
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

        // are the two passed in radio numbers the strongest RSSI pair for the last _Milliseconds  at a
        // rate of _HitPercent?  Work back through time and check RSSI pair strength.
        private bool _calculateRssiStablity2(int _radio1, int _radio2, long _milliseconds = 300, double _HitPercent = 0.70)
        {   
            try
            {
                DateTime _currentTime = DateTime.Now;
                int mesg = ParseMessageHelper.ReceivedMessages.Count - 1;

                if (mesg < 5)
                    return false;  // not enough readings even if over timespan of > 300ms

                while (mesg >= 0)
                {
                    if (mesg <= 0)
                    {
                        return false;  // ran out of ReceivedMessages before desire time in ms
                    }
                    var _timeAgoOfCurrentMessage = _currentTime - ParseMessageHelper.ReceivedMessages[mesg].ReceivedTime;
                    if (_timeAgoOfCurrentMessage < TimeSpan.FromMilliseconds(_milliseconds))
                    {  // in the time window we are asked to check
                        //assemble a heat map here to pass to _findBigestRSSI
                        if (ParseMessageHelper.ReceivedMessages[mesg].Motion > 48)
                        {  // if the motion was high in the last 300 ms we aren't done moving
                            return false;
                        }
                    }
                    if (_timeAgoOfCurrentMessage >= TimeSpan.FromMilliseconds(300))
                    {  // in the needsToBeStableWindow
                        if (ParseMessageHelper.ReceivedMessages[mesg].Motion > 40)  // we had motion between 300-1500ms ago
                        {
                            ReceivedMessage msg = ParseMessageHelper.ReceivedMessages[mesg];
                            string dbgOut = msg.CharacterPrefix + " ";
                            dbgOut += msg.Motion.ToString("D3");
                            dbgOut += " - ";
                            dbgOut += msg.ReceivedTime.ToString("HH:mm:ss");
                            mLogger.Info(dbgOut);

                            return true;
                        }
                    }
                    mesg--;
                    if (_timeAgoOfCurrentMessage > TimeSpan.FromMilliseconds(1500))  //we are past the window
                        return false;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private bool _calculateRssiStablity(int _Ch1, int _Ch2, long _Milliseconds = 250, double _HitPercent = 0.70)
        {
                if ((_Ch1 != mStableRadioIndex1 || _Ch2 != mStableRadioIndex2) &&
                    (_Ch1 != mStableRadioIndex2 || _Ch2 != mStableRadioIndex1))
                {
                    mLastStableTime = _toMilliseconds(DateTime.Now);
                    mStableRadioIndex1 = _Ch1;
                    mStableRadioIndex2 = _Ch2;
                    return false;
                }

                long _currentTime = _toMilliseconds(DateTime.Now);
                if (_currentTime - mLastStableTime >= _Milliseconds)
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
            catch (Exception ex)
            {
                mLogger.Error("_getCharacterMappedIndex exception " + ex.Message);

                return -1;
            }
        }

        private void _updateTemporaryRadioIndexesToCurrentlySpeakingAndCheck()
        {
            try
            {
                Character _ch1 = mCharacterRepository.GetAll()[NextCharacter1];
                Character _ch2 = mCharacterRepository.GetAll()[NextCharacter2];

                if (_ch1 != null && _ch1.RadioNum != -1 && _ch2 != null && _ch2.RadioNum != -1 && _ch1.RadioNum != _ch2.RadioNum)
                {
                    mPossibleSpeakingCh1RadioNum = _ch1.RadioNum;
                    mPossibleSpeakingCh2RadioNum = _ch2.RadioNum;
                }
                else
                {
                    mPossibleSpeakingCh1RadioNum = 0;
                    mPossibleSpeakingCh2RadioNum = 1;
                }

                if (mPossibleSpeakingCh1RadioNum > ApplicationData.Instance.NumberOfRadios - 1 || mPossibleSpeakingCh2RadioNum > ApplicationData.Instance.NumberOfRadios - 1 ||
                    mPossibleSpeakingCh1RadioNum < 0 || mPossibleSpeakingCh2RadioNum < 0 || mPossibleSpeakingCh1RadioNum == mPossibleSpeakingCh2RadioNum)
                {
                    mPossibleSpeakingCh1RadioNum = 0;
                    mPossibleSpeakingCh2RadioNum = 1;
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_updateTemporaryRadioIndexesToCurrentlySpeakingAndCheck " + ex.Message);
            }
        }

        private void _zeroOutUnresponsiveRadiosInHeatMap()
        {
            try
            {
                var _currentTime = DateTime.Now;

                for (int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                {
                    // it shouldn't happen often that a character has dissapeared, if so zero him out
                    if (_currentTime - CharactersLastHeatMapUpdateTime[i] > MaxLastSeenInterval)
                    {
                        int alreadyZero = 0;   // to prevent logging messages each time we pass through
                        for (int k = 0; k < ApplicationData.Instance.NumberOfRadios; k++)
                        {
                            alreadyZero += HeatMap[i, k];
                            HeatMap[i, k] = 0;
                        }
                        if (alreadyZero > 0)
                        {
                            mLogger.Error("_zeroOutUnresponsiveRadiosInHeatMap MaxLastSeenInterval radio # " + i);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_zeroOutUnresponsiveRadiosInHeatMap " + ex.Message);
            }
        }

        private bool _areRadioPairsDifferent(int A1, int A2, int B1, int B2)
        {  //checks if the radio numbers for character pair A and character pair B are the same
            if ((A1 == B1 && A2 == B2) || (A1 == B2 && A2 == B1))
            { 
                return false;
            }
            else
            {
                return true;
            }
        }

        private Triggers _shouldCharactersChange()
        {
            //  This method takes the RSSI values and combines them so that the RSSI for Ch2 looking at 
            //  Ch1 is added to the RSSI for Ch1 looking at Ch2.  
            //  Characters should change if these conditions are met: 
            //  1. A message has come in, 
            //  2. We are in the motion window, 
            //  3. The heat map including the new message gives a higher RSSI pair to new characters vs. currently speaking 
            //  4. The new radio pair meets the RSSI stability threshold requirements of % highest over time

            try
            {
                bool _rssiStable = false;

                _zeroOutUnresponsiveRadiosInHeatMap();
                _updateTemporaryRadioIndexesToCurrentlySpeakingAndCheck();

                //only pick up new characters if bigRssi greater not ==
                int priorLargestRssiPair = HeatMap[mPossibleSpeakingCh1RadioNum, mPossibleSpeakingCh2RadioNum] + HeatMap[mPossibleSpeakingCh2RadioNum, mPossibleSpeakingCh1RadioNum];
                _findBiggestRssiPair(out int finalRow, out int finalColumn, out int newLargestRssiPair, in HeatMap);

                if (newLargestRssiPair > priorLargestRssiPair && 
                      _areRadioPairsDifferent(mPossibleSpeakingCh1RadioNum, mPossibleSpeakingCh2RadioNum, finalRow, finalColumn))
                {
                    mPossibleSpeakingCh1RadioNum = finalRow;
                    mPossibleSpeakingCh2RadioNum = finalColumn;
                }
                else
                {  // only consider changing characters when new RSSI is larger so back to start of state machine
                    return Triggers.ProcessMessage;
                }

                // TODO, _calculateRssiStableAfterChange and _calculateIfInMotionWindow should be their own states
                _rssiStable = _calculateRssiStablity(mPossibleSpeakingCh1RadioNum, mPossibleSpeakingCh2RadioNum);
                var _inMovementWindow = _calculateIfInMotionWindow();

                if ( _rssiStable && (_inMovementWindow || mFreshStart))
                {
                    if(mFreshStart)
                    {
                        if(_calculateRssiStablity(mPossibleSpeakingCh1RadioNum, mPossibleSpeakingCh2RadioNum, 1000, 0.95))
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
                mLogger.Error("_shouldCharactersChange " + ex.Message);
                return Triggers.ProcessMessage;
            }
        }

        private void _findBiggestRssiPair(out int outRow, out int outColumn, out int _rssiPairMax, in int[,] _heatMap)
        {
            //  This method takes the RSSI values and combines them so that the RSSI for Ch2 looking at 
            //  Ch1 is added to the RSSI for Ch1 looking at Ch2 to find the strongest signal pair
            _rssiPairMax = 0;
            outRow = 1;
            outColumn = 2;
            try
            {
                for (int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                {
                    for (int j = i + 1; j < ApplicationData.Instance.NumberOfRadios; j++)
                    {
                        // only need data above the matrix diagonal to look at both radios view of each other

                        int sum = _heatMap[i, j] + _heatMap[j, i];
                        if (sum > _rssiPairMax)
                        {
                            _rssiPairMax = sum;
                            outRow = i;
                            outColumn = j;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_findBiggestRssiPair " + ex.Message);
            }
        }


        private void _selectWhichOfNewCharactersSpeaksFirst(int _ch1RadioNum, int _ch2RadioNum, 
            out int _nextSpeakingCharacter1Index, out int _nextSpeakingCharacter2Index)
        {
            _nextSpeakingCharacter1Index = -1;
            _nextSpeakingCharacter2Index = -1;
            try
            {
                mLogger.Info(String.Format("Trying to assign radios ch1_radio = {0}, ch2_radio = {1} to speak",              
                                            _ch1RadioNum, _ch2RadioNum));

                if (mRandom.NextDouble() > 0.5)
                {
                    _nextSpeakingCharacter1Index = _getCharacterMappedIndex(_ch1RadioNum);
                    _nextSpeakingCharacter2Index = _getCharacterMappedIndex(_ch2RadioNum);
                }
                else
                {
                    _nextSpeakingCharacter1Index = _getCharacterMappedIndex(_ch2RadioNum);
                    _nextSpeakingCharacter2Index = _getCharacterMappedIndex(_ch1RadioNum);
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_selectWhichOfNewCharactersSpeaksFirst " + ex.Message);
            }
        }


        private bool _assignNextSpeakingCharactersIfNew(int _nextSpeakingCharacter1Index, int _nextSpeakingCharacter2Index)
        {
            try
            {
                bool _nextCharactersSelectedAreNew = false;
                if (_nextSpeakingCharacter1Index >= 0 && _nextSpeakingCharacter2Index >= 0)
                {
                    NextCharacter1 = _nextSpeakingCharacter1Index;
                    NextCharacter2 = _nextSpeakingCharacter2Index;

                    if (_areRadioPairsDifferent(NextCharacter1, NextCharacter2, CurrentCharacter1, CurrentCharacter2) 
                        || mFreshStart)
                    {
                        _nextCharactersSelectedAreNew = true;
                        mLogger.Info("New speaking characters assigned by BLE: Character 1 is " + NextCharacter1 +
                            " Character 2 is " + NextCharacter2);
                    }
                    else
                    {
                        mLogger.Info(String.Format("Characters not assigned! Current1 = {0}, Next1 = {1}, Current2 = {2}, Next2 = {3}"
                            , CurrentCharacter1
                            , NextCharacter1
                            , CurrentCharacter2
                            , NextCharacter2));
                    }
                }
                else
                {
                    mLogger.Info(String.Format("Characters not assigned. _speakingCh1Index = {0}, _speakingCh2Index = {1}"
                        , _nextSpeakingCharacter1Index, _nextSpeakingCharacter2Index));
                }
                return _nextCharactersSelectedAreNew;
            }
            catch (Exception ex)
            {
                mLogger.Error("_selectWhichOfNewCharactersSpeaksFirst " + ex.Message);
                return false;
            }
        }


        private Triggers _selectNextCharacters()
        {
            try
            {
                int _speakingCh1Index, _speakingCh2Index;
                _selectWhichOfNewCharactersSpeaksFirst(mPossibleSpeakingCh1RadioNum, mPossibleSpeakingCh2RadioNum,
                       out _speakingCh1Index,  out _speakingCh2Index);
                if (_assignNextSpeakingCharactersIfNew(_speakingCh1Index, _speakingCh2Index))
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
                                next = _shouldCharactersChange();
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
