using DialogGenerator.Core;
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
        private ObservableCollection<ArenaAvatarViewModel> mToolboxAvatars = new ObservableCollection<ArenaAvatarViewModel>();
        private ObservableCollection<ArenaAvatarViewModel> mPlaygroundAvatars = new ObservableCollection<ArenaAvatarViewModel>();
        private ArenaAvatarViewModel mSelectedAvatar = null;
        
        public ArenaViewModel(ILogger _Logger, IEventAggregator _EventAggregator, ICharacterRepository _CharacterRepository)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;

            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);
        }
        
        public ObservableCollection<ArenaAvatarViewModel> ToolboxAvatars
        {
            get
            {
                return mToolboxAvatars;
            }

            set
            {
                mToolboxAvatars = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ArenaAvatarViewModel> PlaygroundAvatars
        {
            get
            {
                return mPlaygroundAvatars;
            }

            set
            {
                mPlaygroundAvatars = value;
                RaisePropertyChanged();
            }
        }

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

        public void AddAvatarToPlayground(ArenaAvatarViewModel avatar)
        {

        }

        public void RemoveAvatarFromPlayground(ArenaAvatarViewModel avatar)
        {

        }

        public ArenaAvatarViewModel GetAvatarOnPosition(long x, long y)
        {
            return null;
        }

        private void _onCharacterCollectionLoaded()
        {
            ObservableCollection<Character> _characters = mCharacterRepository.GetAll();
            foreach(Character _c in _characters)
            {
                mToolboxAvatars.Add(new ArenaAvatarViewModel
                {
                    Character = _c,
                    Active = false,
                    InPlayground = false,
                    Left = 0, 
                    Top = 0
                });
            }
            
        }
    }
}
