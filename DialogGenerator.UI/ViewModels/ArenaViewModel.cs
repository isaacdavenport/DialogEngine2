using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.Utilities;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

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
        private IMessageDialogService mMessageDialogService;
        public EventHandler<RemoveArenaAvatarViewEventArgs> RemoveAvatarRequested;
        private bool mCharactersHaveNoDialogs = false;
        private string mBackgroundImage;
        
        public ArenaViewModel(ILogger _Logger
            , IEventAggregator _EventAggregator
            , ICharacterRepository _CharacterRepository
            , Random _Random
            , IMessageDialogService _MessageDialogService)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;
            mRandom = _Random;
            mMessageDialogService = _MessageDialogService;

            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);
            mEventAggregator.GetEvent<CharactersInConversationEvent>().Subscribe(_onCharactersInConversation);
            mEventAggregator.GetEvent<CharactersHaveDialogsEvent>().Subscribe(_onCharactersHaveDialogs);
            mEventAggregator.GetEvent<ArenaBackgroundChangedEvent>().Subscribe(_onArenaBackgroundChanged);
            PlaygroundAvatars.CollectionChanged += PlaygroundAvatars_CollectionChanged;

            BackgroundImage = ApplicationData.Instance.BackgroundImage;
                        
        }

        private void _onArenaBackgroundChanged()
        {
            BackgroundImage = ApplicationData.Instance.BackgroundImage;
        }

        private void _onCharactersHaveDialogs(bool have)
        {
            CharactersHaveNoDialogs = !have;
        }

        private void PlaygroundAvatars_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var _item in e.NewItems)
                {
                    ArenaAvatarViewModel _model = (ArenaAvatarViewModel)_item;
                    if(_model != null)
                    {
                        _model.PropertyChanged += _model_PropertyChanged;
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var _item in e.OldItems)
                {
                    ArenaAvatarViewModel _model = (ArenaAvatarViewModel)_item;
                    if (_model != null)
                    {
                        _model.PropertyChanged -= _model_PropertyChanged;
                    }
                }
            }
        }

        private void _model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ArenaAvatarViewModel _model = (ArenaAvatarViewModel)sender;
            if(_model != null)
            {
                if(e.PropertyName.Equals("AboutToRemove"))
                {
                    if (_model.AboutToRemove)
                    {
                        if (System.Windows.MessageBox.Show(string.Format("The character {0} will be removed from playground. Are you sure you want to continue?"
                            , _model.CharacterName), "Warning"
                            , MessageBoxButton.YesNo
                            , MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                        {
                            RemoveAvatarRequested(this, new RemoveArenaAvatarViewEventArgs { AvatarModel = _model });
                        }
                        else
                        {
                            _model.AboutToRemove = false;
                        }

                    }
                    
                }
                
            }
        }

        public string BackgroundImage
        {
            get
            {
                return mBackgroundImage;
            }

            set
            {
                mBackgroundImage = value;
                RaisePropertyChanged();
            }
        }

        public bool CharactersHaveNoDialogs
        {
            get
            {
                return mCharactersHaveNoDialogs;
            }

            set
            {
                mCharactersHaveNoDialogs = value;
                RaisePropertyChanged();
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

        public Size ControlBounds { get; set; } = new Size();

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
                if (_ap.RealDistance < _distance)
                {
                    _distance = _ap.RealDistance;
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
            if(_characters.Count == 0)
            {
                mLogger.Error("No characters! The data folder is probably empty!");
                return;
            }

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
                    _pgvm.Left = 200;
                    _pgvm.Top = 50;
                    PlaygroundAvatars.Add(_pgvm);
                }

                // DLGEN-498 - Insert the third character
                int _thirdIndex = _firstIndexNotInList(new List<int> { _selIndex1, _selIndex2 });
                _c = mCharacterRepository.GetAll()[_thirdIndex];
                if(_c != null)
                {
                    ArenaAvatarViewModel _aavm = AvatarGalleryItems.Where(_am => _am.Character.CharacterPrefix.Equals(_c.CharacterPrefix)).First();
                    ArenaAvatarViewModel _pgvm = _aavm.Clone();
                    _pgvm.Left = 150;
                    _pgvm.Top = 150;
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

                Point _ptFirst = new Point(FirstAvatar.Left + FirstAvatar.Width / 2, FirstAvatar.Top + FirstAvatar.Height / 2);
                Point _ptSecond = new Point(SecondAvatar.Left + SecondAvatar.Width / 2, SecondAvatar.Top + SecondAvatar.Height / 2);

                return (_ptSecond - _ptFirst).Length;
            }
        }

        public enum AvatarRectRelativePosition
        {
            Left, 
            TopLeft,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft
        }

        public double RealDistance
        {
            get
            {
                Rect _rc1 = new Rect(new Point(FirstAvatar.Left, FirstAvatar.Top), new Size(FirstAvatar.Width, FirstAvatar.Height));
                Rect _rc2 = new Rect(new Point(SecondAvatar.Left, SecondAvatar.Top), new Size(SecondAvatar.Width, SecondAvatar.Height));

                if(_rc2.IntersectsWith(_rc1))
                {
                    Point _rcCenter1 = new Point(_rc1.Left + _rc1.Width / 2, _rc1.Top + _rc1.Height / 2);
                    Point _rcCenter2 = new Point(_rc2.Left + _rc2.Width / 2, _rc2.Top + _rc2.Height / 2);
                    return (_rcCenter2 - _rcCenter1).Length * 0.01;
                }

                // Find position now. 
                AvatarRectRelativePosition _relativePosition;
                if(_rc2.Bottom < _rc1.Top)
                {
                    if(_rc2.Left + _rc2.Width < _rc1.Left)
                    {
                        _relativePosition = AvatarRectRelativePosition.TopLeft;
                    } else if(_rc2.Left > _rc1.Left + _rc1.Width)
                    {
                        _relativePosition = AvatarRectRelativePosition.TopRight;
                    } else
                    {
                        _relativePosition = AvatarRectRelativePosition.Top;
                    }
                } else if(_rc2.Top > _rc1.Bottom)
                {
                    if (_rc2.Left + _rc2.Width < _rc1.Left)
                    {
                        _relativePosition = AvatarRectRelativePosition.BottomLeft;
                    }
                    else if (_rc2.Left > _rc1.Left + _rc1.Width)
                    {
                        _relativePosition = AvatarRectRelativePosition.BottomRight;
                    }
                    else
                    {
                        _relativePosition = AvatarRectRelativePosition.Bottom;
                    }
                } else
                {
                    if (_rc2.Left + _rc2.Width < _rc1.Left)
                    {
                        _relativePosition = AvatarRectRelativePosition.Left;
                    }
                    else 
                    {
                        _relativePosition = AvatarRectRelativePosition.Right;
                    }
                }

                double _distance;
                switch(_relativePosition)
                {
                    case AvatarRectRelativePosition.Top:
                        _distance = _rc1.Top - _rc2.Bottom;
                        break;
                    case AvatarRectRelativePosition.TopLeft:
                        _distance = (_rc1.TopLeft - _rc2.BottomRight).Length;
                        break;
                    case AvatarRectRelativePosition.TopRight:
                        _distance = (_rc2.BottomLeft - _rc1.TopRight).Length;
                        break;
                    case AvatarRectRelativePosition.Right:
                        _distance = _rc2.Left - _rc1.Right;
                        break;
                    case AvatarRectRelativePosition.BottomRight:
                        _distance = (_rc2.TopLeft - _rc1.BottomRight).Length;
                        break;
                    case AvatarRectRelativePosition.Bottom:
                        _distance = _rc2.Top - _rc1.Bottom;
                        break;
                    case AvatarRectRelativePosition.BottomLeft:
                        _distance = (_rc1.BottomLeft - _rc2.TopRight).Length;
                        break;
                    default: // Left
                        _distance = _rc1.Left - _rc2.Right;
                        break;
                }

                return _distance;
            }
        }

    }

    public class RemoveArenaAvatarViewEventArgs : EventArgs
    {
        public ArenaAvatarViewModel AvatarModel { get; set; }
    }
}
