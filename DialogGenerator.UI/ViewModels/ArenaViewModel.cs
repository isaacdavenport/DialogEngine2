﻿using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.UI.ViewModels
{
    public class ArenaViewModel : BindableBase
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private ArenaAvatarViewModel mSelectedAvatar = null;
        private List<AvatarPair> mAvatarPairs = new List<AvatarPair>();
        
        public ArenaViewModel(ILogger _Logger, IEventAggregator _EventAggregator, ICharacterRepository _CharacterRepository)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;

            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);
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

        public void RemoveAvatarFromPlayground(ArenaAvatarViewModel avatar)
        {

        }

        public ArenaAvatarViewModel GetAvatarOnPosition(long x, long y)
        {
            return null;
        }

        public AvatarPair FindClosestAvatarPair()
        {
            double _distance = Double.MaxValue;
            AvatarPair _closestPair = null;

            foreach (AvatarPair _ap in mAvatarPairs)
            {
                if (_ap.Distance < _distance)
                {
                    _distance = _ap.Distance;
                    _closestPair = _ap;
                }
            }

            if(_closestPair != null)
            {
                foreach(ArenaAvatarViewModel _am in PlaygroundAvatars)
                {
                    if(_closestPair.Contains(_am))
                    {
                        _am.Active = true;
                    } else
                    {
                        _am.Active = false;
                    }
                }

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
                    Session.Set(Constants.NEXT_CH_1, _characterIndex1);
                    Session.Set(Constants.NEXT_CH_2, _characterIndex2);
                }
            }

            return _closestPair;
        }

        private void _onCharacterCollectionLoaded()
        {
            ObservableCollection<Character> _characters = mCharacterRepository.GetAll();
            foreach(Character _c in _characters)
            {
                AvatarGalleryItems.Add(new ArenaAvatarViewModel
                {
                    Character = _c,
                    Active = false,
                    InPlayground = false,
                    Left = 0, 
                    Top = 0
                });
            }
            
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

        public bool Contains(ArenaAvatarViewModel _Am)
        {
            if(FirstAvatar.Equals(_Am))
            {
                return true;
            }

            if(SecondAvatar.Equals(_Am))
            {
                return true;
            }

            return false;
        }
    }
}
