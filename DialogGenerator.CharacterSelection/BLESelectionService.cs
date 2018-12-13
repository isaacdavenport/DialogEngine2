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
using System.Windows.Threading;
using Windows.Devices.Bluetooth;

namespace DialogGenerator.CharacterSelection
{
    public class BLESelectionService : ICharacterSelection
    {
        #region - fields -

        public const int StrongRssiBufDepth = 16;  // TODO we should use a timer as well as relying on a number of incoming packets to switch characters

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private IBLEDataProviderFactory mBLEDataProviderFactory;
        private IBLEDataProvider mCurrentDataProvider;
        private States mCurrentState;
        private int mTempCh1;
        private int mRowNum;
        private int mTempch2;
        private int[,] mStrongRssiCharacterPairBuf = new int[2, StrongRssiBufDepth];
        private int[] mNewRow = new int[ApplicationData.Instance.NumberOfRadios + 1];
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

        public BLESelectionService(ILogger logger, IEventAggregator _eventAggregator,
            ICharacterRepository _characterRepository, IBLEDataProviderFactory _BLEDataProviderFactory)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mCharacterRepository =  _characterRepository ;
            mBLEDataProviderFactory = _BLEDataProviderFactory;

            mWorkflow = new SerialSelectionWorkflow(() => { });
            mWorkflow.PropertyChanged += _mWorkflow_PropertyChanged;
            mcHeatMapUpdateTimer.Interval = TimeSpan.FromSeconds(3);
            mcHeatMapUpdateTimer.Tick += _heatMapUpdateTimer_Tick;

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
                .Permit(Triggers.Initialize, States.Init);

            mWorkflow.Configure(States.Init)
                .Permit(Triggers.ProcessMessage, States.MessageProcessing)
                .Permit(Triggers.Finish, States.Finished);

            mWorkflow.Configure(States.MessageProcessing)
                .PermitReentry(Triggers.ProcessMessage)
                .Permit(Triggers.CalculateClosestPair, States.CalculatingClosestPair);

            mWorkflow.Configure(States.CalculatingClosestPair)
                .Permit(Triggers.ProcessMessage, States.MessageProcessing)
                .Permit(Triggers.SelectNextCharacters, States.SelectNextCharacters)
                .Permit(Triggers.Finish, States.Finished);

            mWorkflow.Configure(States.SelectNextCharacters)
                .Permit(Triggers.ProcessMessage, States.MessageProcessing)
                .Permit(Triggers.Finish, States.Finished);

            mWorkflow.Configure(States.Finished)
                .OnEntry(t => _finishSelection())
                .Permit(Triggers.Wait, States.Waiting);
        }


        private async Task<Triggers> _initialize()
        {
            var _localAdapter = await BluetoothAdapter.GetDefaultAsync();

            if (_localAdapter.IsLowEnergySupported)
            {
                mCurrentDataProvider = mBLEDataProviderFactory.Create(BLEDataProviderType.WinBLEWatcher);
                Session.Set(Constants.BLE_DATA_PROVIDER, BLEDataProviderType.WinBLEWatcher);
            }
            else
            {
                mCurrentDataProvider = mBLEDataProviderFactory.Create(BLEDataProviderType.Serial);
                Session.Set(Constants.BLE_DATA_PROVIDER, BLEDataProviderType.Serial);
            }

            return Triggers.ProcessMessage;
        }

        private void _finishSelection()
        {
            mcHeatMapUpdateTimer.Stop();
            mWorkflow.Fire(Triggers.Wait);
        }

        private Triggers _processMessage()
        {
            try
            {
                _resetData();
                string message = null;

                message = mCurrentDataProvider.GetMessage();

                if (message == null)
                {
                    return Triggers.ProcessMessage;
                }

                mRowNum = ParseMessageHelper.ParseBle(message, ref mNewRow);

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
                    Session.Set(Constants.NEXT_CH_1, NextCharacter1);
                    Session.Set(Constants.NEXT_CH_2, NextCharacter2);

                    CurrentCharacter1 = NextCharacter1;
                    CurrentCharacter2 = NextCharacter2;

                    mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().
                        Publish(new SelectedCharactersPairEventArgs { Character1Index = CurrentCharacter1, Character2Index = CurrentCharacter2 });

                    mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();

                    mEventAggregator.GetEvent<HeatMapUpdateEvent>().Publish(new HeatMapData
                    {
                        HeatMap = HeatMap,
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

        public async Task StartCharacterSelection()
        {
            mCancellationTokenSource = new CancellationTokenSource();

            await Task.Run(async() =>
            {
                Thread.CurrentThread.Name = "StartCharacterSelection";

                mWorkflow.Fire(Triggers.Initialize);
                mcHeatMapUpdateTimer.Start();
                Triggers next = await _initialize();
                Task _BLEDataReaderTask = mCurrentDataProvider.StartReadingData();

                do
                {
                    switch (next)
                    {
                        case Triggers.Initialize:
                            {
                                next = await _initialize();
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
                }
                while (!mCancellationTokenSource.IsCancellationRequested);

                await _BLEDataReaderTask;

                mWorkflow.Fire(Triggers.Finish);
            });          
        }

        public void StopCharacterSelection()
        {
            mcHeatMapUpdateTimer.Stop();
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
