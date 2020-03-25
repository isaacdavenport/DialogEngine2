using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace DialogGenerator.UI.ViewModels
{
    public class ArenaViewModel : BindableBase
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private ArenaAvatarViewModel mSelectedAvatar = null;
        private List<AvatarPair> mAvatarPairs = new List<AvatarPair>();
        private Random mRandom;
        
        public ArenaViewModel(ILogger _Logger
            , IEventAggregator _EventAggregator
            , ICharacterRepository _CharacterRepository
            , Random _Random)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;
            mRandom = _Random;

            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);
            mEventAggregator.GetEvent<CharactersInConversationEvent>().Subscribe(_onCharactersInConversation);
        }        

        public ObservableCollection<ArenaAvatarViewModel> AvatarGalleryItems { get; } = new ObservableCollection<ArenaAvatarViewModel>();

        public ObservableCollection<ArenaAvatarViewModel> PlaygroundAvatars { get; } = new ObservableCollection<ArenaAvatarViewModel>();
        
        public ArenaAvatarViewModel SelectedAvatar
        {
            get
            {
                return mSelectedAvatar;
            }

            set
            {
                mSelectedAvatar = value;
                RaisePropertyChanged();
            }
        }

        public Size CanvasBounds { get; set; } = new Size();

        public void AddAvatarToPlayground(ArenaAvatarViewModel _Avatar)
        {
            PlaygroundAvatars.Add(_Avatar);
            _createAvatarPairs();
            AvatarPair _closestPair = FindClosestAvatarPair();
            if(_closestPair != null)
            {
                // Send event.
            }
        }

        public AvatarPair FindClosestAvatarPair(bool _Create = false)
        {
            double _distance = Double.MaxValue;
            AvatarPair _closestPair = null;

            if(_Create)
            {
                _createAvatarPairs();
            }

            foreach (AvatarPair _ap in mAvatarPairs)
            {
                if (_ap.Distance < _distance)
                {
                    _distance = _ap.Distance;
                    _closestPair = _ap;
                }
            }

            if (_closestPair != null)
            {
                int _characterIndex1 = -1;
                int _characterIndex2 = -1;

                List<Character> _characters = mCharacterRepository.GetAll().ToList();
                for(int i = 0; i < _characters.Count; i++)
                {
                    Character _c = _characters[i];
                    if(_c.CharacterName.Equals(_closestPair.FirstAvatar.Character.CharacterName))
                    {
                        _characterIndex1 = i;
                    }

                    if (_c.CharacterName.Equals(_closestPair.SecondAvatar.Character.CharacterName))
                    {
                        _characterIndex2 = i;
                    }
                }

                if(_characterIndex1 != -1 && _characterIndex2 != -1)
                {
                    int _choice = mRandom.Next();
                    _choice = _choice % 2;
                    Session.Set(Constants.NEXT_CH_1, _choice == 0 ?_characterIndex1 : _characterIndex2);
                    Session.Set(Constants.NEXT_CH_2, _choice == 0 ?_characterIndex2 : _characterIndex1);
                }
            }

            return _closestPair;
        }

        private void _onCharactersInConversation(SelectedCharactersPairEventArgs obj)
        {
            foreach (ArenaAvatarViewModel _am in PlaygroundAvatars)
            {
                int _characterIndex = mCharacterRepository.IndexOf(_am.Character);
                if (_characterIndex == obj.Character1Index ||
                   _characterIndex == obj.Character2Index)
                {
                    _am.Active = true;
                }
                else
                {
                    _am.Active = false;
                }
            }
        }

        private void _onCharacterCollectionLoaded()
        {
            ObservableCollection<Character> _characters = mCharacterRepository.GetAll();                                   

            // Clear collections.
            PlaygroundAvatars.Clear();
            AvatarGalleryItems.Clear();
            foreach (Character _c in _characters)
            {
                ArenaAvatarViewModel _am = new ArenaAvatarViewModel
                {
                    Character = _c,
                    Active = false,
                    InPlayground = false,
                    Left = 0,
                    Top = 0, 
                    Random = mRandom
                };

                AvatarGalleryItems.Add(_am);                
            }

            int _selIndex1 = Session.Get<int>(Constants.NEXT_CH_1);
            int _selIndex2 = Session.Get<int>(Constants.NEXT_CH_2);

            if (PlaygroundAvatars.Count == 0 /* S.Ristic - and actually it always is */)
            {
                Character _c = null;                                
                if (_selIndex1 >= 0 && _selIndex1 < mCharacterRepository.GetAll().Count)
                {
                    _c = mCharacterRepository.GetAll()[_selIndex1];                    
                } else
                {
                    if (_selIndex2 != -1) {
                        _selIndex1 = _firstIndexNotInList(new List<int> { _selIndex2 });
                    } else
                    {
                        _selIndex1 = /* 0 */ _firstIndexNotInList(new List<int>());
                    }

                    _c = mCharacterRepository.GetAll()[_selIndex1];
                }

                if(_c != null)
                {
                    ArenaAvatarViewModel _aavm = AvatarGalleryItems.Where(_am => _am.Character.CharacterPrefix.Equals(_c.CharacterPrefix)).First();
                    ArenaAvatarViewModel _pgvm = _aavm.Clone();
                    _pgvm.Left = 50;
                    _pgvm.Top = 50;
                    PlaygroundAvatars.Add(_pgvm);
                }

                _c = null;

                if (_selIndex2 >= 0 && _selIndex2 < mCharacterRepository.GetAll().Count)
                {
                    if(_selIndex2 == _selIndex1)
                    {
                        _c = mCharacterRepository.GetAll()[_firstIndexNotInList(new List<int> { _selIndex1 })];
                    } else
                    {
                        _c = mCharacterRepository.GetAll()[_selIndex2];
                    }                    
                }
                else
                {
                    if (_selIndex1 != -1)
                    {
                        _selIndex2 = _firstIndexNotInList(new List<int> { _selIndex1 });
                    }
                    else
                    {
                        _selIndex2 = /* 0 */ _firstIndexNotInList(new List<int>());
                    }

                    _c = mCharacterRepository.GetAll()[_selIndex2];
                }

                if (_c != null)
                {
                    ArenaAvatarViewModel _aavm = AvatarGalleryItems.Where(_am => _am.Character.CharacterPrefix.Equals(_c.CharacterPrefix)).First();
                    ArenaAvatarViewModel _pgvm = _aavm.Clone();
                    _pgvm.Left = 250;
                    _pgvm.Top = 50;
                    PlaygroundAvatars.Add(_pgvm);
                }
            } 

            // If the indices have changed send the event.
            if((_selIndex1 != Session.Get<int>(Constants.NEXT_CH_1) || 
                _selIndex2 != Session.Get<int>(Constants.NEXT_CH_2)) &&
               (_selIndex1 != Session.Get<int>(Constants.NEXT_CH_2) || 
                _selIndex2 != Session.Get<int>(Constants.NEXT_CH_1))) {
                int _choice = mRandom.Next();
                _choice = _choice % 2;
                mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Publish(new SelectedCharactersPairEventArgs
                {
                    Character1Index = _choice == 0 ? _selIndex1 : _selIndex2,
                    Character2Index = _choice == 0 ? _selIndex2 : _selIndex1
                }); ;
            }
            
        }

        private int _firstIndexNotInList(List<int> _Lista)
        {           
            int i = mRandom.Next(mCharacterRepository.GetAll().Count());

            while (_Lista.Contains(i)) {
                i = mRandom.Next(mCharacterRepository.GetAll().Count());
            }

            return i;
        }

        private void _createAvatarPairs()
        {
            mAvatarPairs.Clear();

            if (PlaygroundAvatars.Count < 2)
                return;

            for(int i = 0; i < PlaygroundAvatars.Count - 1;i++)
            {
                for(int j = i + 1; j < PlaygroundAvatars.Count;j++)
                {
                    mAvatarPairs.Add(new AvatarPair
                    {
                        FirstAvatar = PlaygroundAvatars[i],
                        SecondAvatar = PlaygroundAvatars[j]
                    });
                }
            }
        }

    }

    public class AvatarPair
    {
        public ArenaAvatarViewModel FirstAvatar { get; set; }
        public ArenaAvatarViewModel SecondAvatar { get; set; }
        public double Distance
        {
            get
            {
                if (FirstAvatar == null || SecondAvatar == null)
                    return -1;

                double _xDistance = Math.Abs(FirstAvatar.Left - SecondAvatar.Left);
                double _yDistance = Math.Abs(FirstAvatar.Top - SecondAvatar.Top);

                return Math.Sqrt(Math.Pow(_xDistance, 2) + Math.Pow(_yDistance, 2));
            }
        }

    }
}
