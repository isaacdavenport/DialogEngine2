﻿using DialogGenerator.Core;
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

        public ArenaCharacterSelection(IEventAggregator _eventAggregator)
        {
            mEventAggregator = _eventAggregator;
        }
        public async Task StartCharacterSelection()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            await Task.Run(async () => 
            {
                Thread.CurrentThread.Name = "CharacterBoxesScanningThread";
                Session.Set(Constants.FORCED_CH_COUNT, 2);

                while (!mCancellationTokenSource.Token.IsCancellationRequested)
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