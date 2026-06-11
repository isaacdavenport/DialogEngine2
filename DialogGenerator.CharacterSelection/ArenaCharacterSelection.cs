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
        private ILogger mLogger;

        public ArenaCharacterSelection( IEventAggregator _eventAggregator, ILogger _logger)
        {
            mEventAggregator = _eventAggregator;
            mLogger = _logger;            
        }

        public async Task StartCharacterSelection()
        {
            mCancellationTokenSource = new CancellationTokenSource();            
            
            await Task.Run(async () => 
            {
                bool _restartRequired = false;
                Thread.CurrentThread.Name = "CharacterBoxesScanningThread";
                Session.Set(Constants.FORCED_CH_COUNT, 2);

                do
                {
                    // Both characters are selected.
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
                            System.Console.WriteLine("Will send arena selection event");

                            mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();

                            Session.Set(Constants.CANCEL_DIALOG, true);

                            mLogger.Info($"ARENA CHARACTER SELECTION - BEFORE REQUESTING CHARACTER CHANGE");
                            
                            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().
                            Publish(new SelectedCharactersPairEventArgs
                            {
                                Character1Index = mFirstCharacterIndex,
                                Character2Index = mSecondCharacterIndex
                            });

                            mLogger.Info($"ARENA CHARACTER SELECTION - AFTER REQUESTING CHARACTER CHANGE");
                            
                        }
                    
                    DateTime _nowTime = DateTime.Now;
                    DateTime _lastAccessTime = _nowTime;
                    TimeSpan _difference = new TimeSpan(0);


                Thread.Sleep(500);
                } while (!mCancellationTokenSource.Token.IsCancellationRequested);
                
                mLogger.Info($"ARENA CHARACTER SELECTION - Exited from the loop. Request cancellation token requested - {mCancellationTokenSource.Token.IsCancellationRequested}");
               
                if(_restartRequired)
                {
                    Session.Set(Constants.NEEDS_RESTART, true);                    
                }                
            });
        }

        public void StopCharacterSelection()
        {                        
            mCancellationTokenSource.Cancel();
        }

    }
}
