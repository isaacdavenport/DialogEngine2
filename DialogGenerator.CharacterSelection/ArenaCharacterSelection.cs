using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
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
        private ILogger mLogger;

        public ArenaCharacterSelection( IEventAggregator _eventAggregator, 
                                        IBLEDataProviderFactory _dataProviderFactory, ILogger _logger)
        {
            mEventAggregator = _eventAggregator;
            mBLEDataProviderFactory = _dataProviderFactory;
            mCurrentDataProvider = mBLEDataProviderFactory.Create(BLEDataProviderType.WinBLEWatcher);
            mLogger = _logger;
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

                            Session.Set(Constants.CANCEL_DIALOG, true);

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
                        mLogger.Info("BLE messages arriving, switch to BLE Mode.");
                        mCancellationTokenSource.Cancel();
                    }
                                        
                    Thread.Sleep(1);
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
            mCurrentDataProvider.StopReadingData();
            mCancellationTokenSource.Cancel();
        }

    }
}
