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
using System.Text;
using System.Threading.Tasks;
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
        
        public ArenaViewModel(ILogger _Logger, IEventAggregator _EventAggregator, ICharacterRepository _CharacterRepository)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;

            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);
            mEventAggregator.GetEvent<CharactersInConversationEvent>().Subscribe(_onCharactersInConversation);
        }

        private void _onCharactersInConversation(SelectedCharactersPairEventArgs obj)
        {
            foreach (ArenaAvatarViewModel _am in PlaygroundAvatars)
            {
                int _characterIndex = mCharacterRepository.IndexOf(_am.Character);
                if(_characterIndex == obj.Character1Index ||
                   _characterIndex == obj.Character2Index)
                {
                    _am.Active = true;
                } else
                {
                    _am.Active = false;
                }                                
            }
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

        public void RemoveAvatarFromPlayground(ArenaAvatarViewModel avatar)
        {

        }

        public ArenaAvatarViewModel GetAvatarOnPosition(long x, long y)
        {
            return null;
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
                    Session.Set(Constants.NEXT_CH_1, _characterIndex1);
                    Session.Set(Constants.NEXT_CH_2, _characterIndex2);
                }
            }

            return _closestPair;
        }

        private void _onCharacterCollectionLoaded()
        {
            ObservableCollection<Character> _characters = mCharacterRepository.GetAll();                        
            
            // Rememeber the playground avatars if there are any.
            var _query = PlaygroundAvatars.Select((c, index) => new { 
                Index = index, 
                Prefix = c.Character.CharacterPrefix,
                Left = c.Left,
                Top = c.Top,
            });

            //List<int> _playgroundIndices = new List<int>();
            //foreach(var _item in _query)
            //{

            //}

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
                    Top = 0
                };

                AvatarGalleryItems.Add(_am);

                foreach(var item in _query)
                {
                    string _prefix = item.GetType().GetProperty("Prefix").GetValue(item).ToString();
                    if(_prefix.Equals(_am.Character.CharacterPrefix))
                    {
                        ArenaAvatarViewModel _playgroundAM = _am.Clone();
                        _am.Left = (int)item.GetType().GetProperty("Left").GetValue(item);
                        _am.Top = (int)item.GetType().GetProperty("Top").GetValue(item);
                        //_am.Active = (bool)item.GetType().GetProperty("Active").GetValue(item);
                        //_am.InPlayground = (bool)item.GetType().GetProperty("InPlayground").GetValue(item);
                        PlaygroundAvatars.Add(_playgroundAM);
                    }
                }
            }

            int _selIndex1 = Session.Get<int>(Constants.NEXT_CH_1);
            int _selIndex2 = Session.Get<int>(Constants.NEXT_CH_2);

            if (PlaygroundAvatars.Count == 0)
            {
                Character _c = null;                                
                if (_selIndex1 != -1)
                {
                    _c = mCharacterRepository.GetAll()[_selIndex1];
                    
                } else
                {
                    if (_selIndex2 != -1) {
                        _selIndex1 = _firstIndexNotInList(new List<int> { _selIndex2 });
                    } else
                    {
                        _selIndex1 = 0;
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

                if (_selIndex2 != -1)
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
                        _selIndex2 = 0;
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
            } else
            {
                List<int> _indices = new List<int> { _selIndex1, _selIndex2 };
                foreach (int _index in _indices)
                {
                    if (PlaygroundAvatars.Where(p => mCharacterRepository.IndexOf(p.Character) == _index).Count() == 0)
                    {
                        // Dodaj ga
                        ArenaAvatarViewModel _am = new ArenaAvatarViewModel
                        {
                            Character = mCharacterRepository.GetAll()[_index],
                            Left = 200,
                            Top = 200
                        };

                        PlaygroundAvatars.Add(_am);
                    }
                }
            }
            
        }

        private int _firstIndexNotInList(List<int> _Lista, int _Limit = 50) 
        {
            int _counter = 0;
            int _retval = -1;

            while(_retval == -1 && _counter != _Limit)
            {
                if(!_Lista.Contains(_counter))
                {
                    _retval = _counter;
                }

                _counter++;
            }

            return _retval;
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
