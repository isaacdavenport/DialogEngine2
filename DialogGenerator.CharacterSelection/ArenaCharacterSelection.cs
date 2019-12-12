using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Utilities;
using Prism.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.CharacterSelection
{
    public class ArenaCharacterSelection : ICharacterSelection
    {
        private IEventAggregator mEventAggregator;
        private CancellationTokenSource mCancellationTokenSource;
        private int mFirstCharacterIndex = -1;
        private int mSecondCharacterIndex = -1;

        public ArenaCharacterSelection(ILogger logger, IEventAggregator _eventAggregator
            , ICharacterRepository _characterRepository
            , IMessageDialogService _messageDialogService)
        {
            mEventAggregator = _eventAggregator;
        }
        public async Task StartCharacterSelection()
        {
            System.Console.WriteLine("Character selection started ...");
            mCancellationTokenSource = new CancellationTokenSource();
            await Task.Run(async () => 
            {
                System.Console.WriteLine("Entered task ...");
                Thread.CurrentThread.Name = "CharacterBoxesScanningThread";
                Session.Set(Constants.FORCED_CH_COUNT, 2);
                Session.Set(Constants.NEXT_CH_1, 1);
                Session.Set(Constants.NEXT_CH_2, 2);
                while (!mCancellationTokenSource.Token.IsCancellationRequested)
                {
                    System.Console.WriteLine("looping ...");
                    System.Console.WriteLine("Character count = {0}", Session.Get<int>(Constants.FORCED_CH_COUNT));
                    
                    // Both characters are selected.
                    if (Session.Get<int>(Constants.FORCED_CH_COUNT) == 2)
                    {
                        int _char1Index = Session.Get<int>(Constants.NEXT_CH_1);
                        int _char2Index = Session.Get<int>(Constants.NEXT_CH_2);

                        System.Console.WriteLine("{0}, {1}", mFirstCharacterIndex, mSecondCharacterIndex);
                        System.Console.WriteLine("{0}, {1}", _char1Index, _char2Index);

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

                            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().
                            Publish(new SelectedCharactersPairEventArgs
                            {
                                Character1Index = mFirstCharacterIndex,
                                Character2Index = mSecondCharacterIndex
                            });

                            mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();

                        }                        
                    }

                    Thread.Sleep(1000);
                }

            });
        }

        public void StopCharacterSelection()
        {
            mCancellationTokenSource.Cancel();
        }
    }
}
