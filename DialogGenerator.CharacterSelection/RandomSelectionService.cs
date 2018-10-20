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
        private static Random msRandom = new Random();
        public static int NextCharacter1 = 1;
        public static int NextCharacter2 = 2;
        private CancellationTokenSource mCancellationTokenSource;


        public RandomSelectionService(ILogger logger,IEventAggregator _eventAggregator,
            ICharacterRepository _characterRepository,IMessageDialogService _messageDialogService)
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
                        await mMessageDialogService.ShowMessage("Warning", "No available characters. Please change characters settings.");
                        //TODO uncomment
                        //MessageBox.Show("No available characters. Please change characters settings.");
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

                DateTime _nextCharacterSwapTime = DateTime.Now;

                while (!mCancellationTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(1000);

                    if (_nextCharacterSwapTime.CompareTo(DateTime.Now) < 0)
                    {
                        switch (Session.Get<int>(Constants.FORCED_CH_COUNT))
                        {
                            case 0:
                                {
                                    int _nextCharacter1Index = await GetNextCharacter();
                                    int _nextCharacter2Index = await GetNextCharacter(_nextCharacter1Index >= 0 ? _nextCharacter1Index : NextCharacter1);

                                    NextCharacter1 = _nextCharacter1Index >= 0 ? _nextCharacter1Index : NextCharacter1; //lower bound inclusive, upper exclusive
                                    NextCharacter2 = _nextCharacter2Index >= 0 ? _nextCharacter2Index : NextCharacter2; //lower bound inclusive, upper exclusive

                                    _nextCharacterSwapTime = DateTime.Now.AddSeconds(4 + msRandom.Next(0, 2));

                                    break;
                                }
                            case 1:
                                {
                                    NextCharacter1 = Session.Get<int>(Constants.FORCED_CH_1);
                                    int _nextCharacter2Index = await GetNextCharacter(NextCharacter1);

                                    NextCharacter2 = _nextCharacter2Index >= 0 ? _nextCharacter2Index : NextCharacter2;

                                    _nextCharacterSwapTime = DateTime.Now.AddSeconds(4 + msRandom.Next(0, 2));

                                    break;
                                }
                            case 2:
                                {
                                    _nextCharacterSwapTime = DateTime.Now.AddSeconds(4 + msRandom.Next(0, 2));
                                    NextCharacter1 = Session.Get<int>(Constants.FORCED_CH_1);
                                    NextCharacter2 = Session.Get<int>(Constants.FORCED_CH_2);
                                    break;
                                }
                        }
                    }

                    Session.Set(Constants.NEXT_CH_1, NextCharacter1);
                    Session.Set(Constants.NEXT_CH_2, NextCharacter2);

                    mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().
                        Publish(new SelectedCharactersPairEventArgs
                        {
                            Character1Index = NextCharacter1,
                            Character2Index = NextCharacter2
                        });

                }

            });
        }

        public void StopCharacterSelection()
        {
            mCancellationTokenSource.Cancel();
        }
    }
}
