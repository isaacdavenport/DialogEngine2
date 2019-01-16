using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model.Enum;
using DialogGenerator.Utilities;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.CharacterSelection
{
    public class RandomSelectionService : ICharacterSelection
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private IMessageDialogService mMessageDialogService;
        private static int mRandGenNextCharacter1 = 1;
        private static int mRandGenNextCharacter2 = 2;
        private CancellationTokenSource mCancellationTokenSource;

        public RandomSelectionService(ILogger logger,IEventAggregator _eventAggregator
            ,ICharacterRepository _characterRepository
            ,IMessageDialogService _messageDialogService)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mCharacterRepository = _characterRepository;
            mMessageDialogService = _messageDialogService;
        }

        /// <summary>
        /// Random selection of next available character
        /// </summary>
        /// <param name="_indexToSkip"> Number which must be ignored, so we can avoid the same index of selected characters </param>
        /// <returns> Character index or -1 if there is not available characters </returns>
        public async Task<int> GetNextCharacter(params int[] _indexToSkip)
        {
            int index;
            int result = -1;

            var characters = mCharacterRepository.GetAll();
            // list with indexes of available characters
            List<int> _allowedIndexes = characters.Select((c, i) => new { Character = c, Index = i })
                .Where(x => x.Character.State == CharacterState.Available)
                .Select(x => x.Index).ToList();

            switch (_allowedIndexes.Count)
            {
                case 0:  // no avaialbe characters
                    {
                        await mMessageDialogService.ShowMessage("Warning", "No available characters. Please change characters settings.","ContentDialogHost");
                        break;
                    }
                case 1: // 1 available character
                    {
                        // if we don't want duplicate index
                        if (_indexToSkip.Length > 0 && _allowedIndexes[0] == _indexToSkip[0])
                        {
                            break;
                        }
                        else
                        {
                            result = _allowedIndexes[0];
                        }
                        break;
                    }
                default:  // more than 1 available characters 
                    {
                        Random random = new Random();
                        bool _isIndexTheSame;
                        // get random element form list with indexes of available characters
                        do
                        {
                            index = _allowedIndexes[random.Next(0, _allowedIndexes.Count)];
                            _isIndexTheSame = false;

                            if (_indexToSkip.Length > 0)
                            {
                                if (index == _indexToSkip[0])
                                    _isIndexTheSame = true;
                            }
                        }
                        while (_isIndexTheSame);

                        result = index;
                        break;
                    }
            }

            return result;
        }

        public async Task StartCharacterSelection()
        {
            mCancellationTokenSource = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                Thread.CurrentThread.Name = "OccasionallyChangeToRandNewCharacterAsyncThread";

                // used for computers with no serial input radio for random, or forceCharacter mode
                // TODO is this still true?  does not include final character the silent schoolhouse, not useful in noSerial mode 

                while (!mCancellationTokenSource.Token.IsCancellationRequested)
                {

                    switch (Session.Get<int>(Constants.FORCED_CH_COUNT))
                    {
                        case 0:
                            {
                                int _nextCharacter1Index = await GetNextCharacter();
                                int _nextCharacter2Index = await GetNextCharacter(_nextCharacter1Index >= 0 ? _nextCharacter1Index : mRandGenNextCharacter1);

                                mRandGenNextCharacter1 = _nextCharacter1Index >= 0 ? _nextCharacter1Index : mRandGenNextCharacter1; //lower bound inclusive, upper exclusive
                                mRandGenNextCharacter2 = _nextCharacter2Index >= 0 ? _nextCharacter2Index : mRandGenNextCharacter2; //lower bound inclusive, upper exclusive

                                break;
                            }
                        case 1:
                            {
                                mRandGenNextCharacter1 = Session.Get<int>(Constants.FORCED_CH_1);
                                int _nextCharacter2Index = await GetNextCharacter(mRandGenNextCharacter1);

                                mRandGenNextCharacter2 = _nextCharacter2Index >= 0 ? _nextCharacter2Index : mRandGenNextCharacter2;

                                break;
                            }
                        case 2:
                            {
                                mRandGenNextCharacter1 = Session.Get<int>(Constants.FORCED_CH_1);
                                mRandGenNextCharacter2 = Session.Get<int>(Constants.FORCED_CH_2);
                                break;
                            }
                    }

                    Session.Set(Constants.NEXT_CH_1, mRandGenNextCharacter1);
                    Session.Set(Constants.NEXT_CH_2, mRandGenNextCharacter2);

                    mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().
                        Publish(new SelectedCharactersPairEventArgs
                        {
                            Character1Index = mRandGenNextCharacter1,
                            Character2Index = mRandGenNextCharacter2
                        });
                }

                Thread.Sleep(1000);
            });
        }

        public void StopCharacterSelection()
        {
            mCancellationTokenSource.Cancel();
        }
    }
}
