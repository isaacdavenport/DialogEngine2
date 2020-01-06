using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.Utilities;
using Prism.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DialogGenerator.CharacterSelection
{
    public class ArenaCharacterSelection : ICharacterSelection
    {
        private IEventAggregator mEventAggregator;
        private CancellationTokenSource mCancellationTokenSource;
        private int mFirstCharacterIndex = -1;
        private int mSecondCharacterIndex = -1;
        private IBLEDataProviderFactory mBLEDataProviderFactory;
        private IBLEDataProvider mCurrentDataProvider;
        public static int[,] HeatMap = new int[ApplicationData.Instance.NumberOfRadios, ApplicationData.Instance.NumberOfRadios];
        public static int[] MotionVector = new int[ApplicationData.Instance.NumberOfRadios];
        public static DateTime[] CharactersLastHeatMapUpdateTime = new DateTime[ApplicationData.Instance.NumberOfRadios];
        public readonly TimeSpan MaxLastSeenInterval = new TimeSpan(0, 0, 0, 4, 100);
        private readonly DispatcherTimer mcHeatMapUpdateTimer = new DispatcherTimer();
        private ILogger mLogger;

        public ArenaCharacterSelection( IEventAggregator _eventAggregator, 
                                        IBLEDataProviderFactory _dataProviderFactory, ILogger _logger)
        {
            mEventAggregator = _eventAggregator;
            mBLEDataProviderFactory = _dataProviderFactory;
            mCurrentDataProvider = mBLEDataProviderFactory.Create(BLEDataProviderType.WinBLEWatcher);
            mLogger = _logger;
            mcHeatMapUpdateTimer.Interval = TimeSpan.FromMilliseconds(300);
            mcHeatMapUpdateTimer.Tick += _heatMapUpdateTimer_Tick;
        }

        private void _heatMapUpdateTimer_Tick(object sender, EventArgs e)
        {
            mEventAggregator.GetEvent<HeatMapUpdateEvent>().Publish(new HeatMapData
            {
                HeatMap = HeatMap,
                MotionVector = MotionVector,
                LastHeatMapUpdateTime = CharactersLastHeatMapUpdateTime,
                Character1Index = mFirstCharacterIndex,
                Character2Index = mSecondCharacterIndex
            });
        }

        public async Task StartCharacterSelection()
        {
            mCancellationTokenSource = new CancellationTokenSource();            
            
            await Task.Run(async () => 
            {
                bool _restartRequired = false;
                Task _BLEDataReaderTask = mCurrentDataProvider.StartReadingData();
                Thread.CurrentThread.Name = "CharacterBoxesScanningThread";
                Session.Set(Constants.FORCED_CH_COUNT, 2);
                mcHeatMapUpdateTimer.Start();

                do
                {
                    // Both characters are selected.
                    if (Session.Get<int>(Constants.FORCED_CH_COUNT) == 2)
                    {
                        int _char1Index = Session.Get<int>(Constants.NEXT_CH_1);
                        int _char2Index = Session.Get<int>(Constants.NEXT_CH_2);

                        bool _bChanged = false;

                        if (_char1Index != mFirstCharacterIndex)
                        {
                            mFirstCharacterIndex = _char1Index;
                            _bChanged = true;
                        }

                        if (_char2Index != mSecondCharacterIndex)
                        {
                            mSecondCharacterIndex = _char2Index;
                            _bChanged = true;
                        }

                        if (_bChanged)
                        {
                            System.Console.WriteLine("Will send event");

                            mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();

                            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().
                            Publish(new SelectedCharactersPairEventArgs
                            {
                                Character1Index = mFirstCharacterIndex,
                                Character2Index = mSecondCharacterIndex
                            });

                            
                        }
                    }

                    
                    DateTime _nowTime = DateTime.Now;
                    DateTime _lastAccessTime = _nowTime;
                    TimeSpan _difference = new TimeSpan(0);

                    BLE_Message message = mCurrentDataProvider.GetMessage();
                    if (message != null)
                    {
                        _restartRequired = true;
                        mCancellationTokenSource.Cancel();
                    }
                                        
                    Thread.Sleep(1000);
                } while (!mCancellationTokenSource.Token.IsCancellationRequested);

                

                if(_restartRequired)
                {
                    mCurrentDataProvider.StopReadingData();
                    Session.Set(Constants.BLE_MODE_ON, true);
                    Session.Set(Constants.NEEDS_RESTART, true);
                    
                }

                await _BLEDataReaderTask;
            });
        }

        public void StopCharacterSelection()
        {                        
            mcHeatMapUpdateTimer.Stop();
            mCurrentDataProvider.StopReadingData();
            mCancellationTokenSource.Cancel();
        }

        private bool _moreThanOneRadioIsTransmitting()
        {
            //  This method allows us to autoselect whether we should use incoming radio BLE
            //    or avatar arena based character selection.  Can be overidden in settings to
            //    force to Avatar Arena.  This method needs to be called on a timer.

            bool _atLeastTwoTransmitting = false;
            try
            {
                int i = 0, j = 0, k = 0;

                var _currentTime = DateTime.Now;

                for (i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                {
                    if (_currentTime - CharactersLastHeatMapUpdateTime[i] < MaxLastSeenInterval)
                    {
                        j++;
                    }
                }
                if (j > 1)
                {
                    _atLeastTwoTransmitting = true;
                }
                else
                {
                    _atLeastTwoTransmitting = false;
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_moreThanOneRadioIsTransmitting " + ex.Message);
            }
            return _atLeastTwoTransmitting;
        }
    }
}
