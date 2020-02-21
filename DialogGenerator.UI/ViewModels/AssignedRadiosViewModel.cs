using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DialogGenerator.UI.ViewModels
{
    public class AssignedRadiosViewModel : BindableBase
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private Visibility mVisible;
        
        public AssignedRadiosViewModel(ILogger _Logger, IEventAggregator _EventAggregator, ICharacterRepository _CharacterRepository)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;
            Visible = Visibility.Collapsed;

            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Subscribe(_characterPairChanged);
            mEventAggregator.GetEvent<RadioAssignedEvent>().Subscribe(_radioAsigned);

            _bindCommands();
        }


        #region Properties         

        public Visibility Visible
        {
            get
            {
                return mVisible;
            }

            set
            {
                mVisible = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ArenaAvatarViewModel> RadioCharacters { get; } = new ObservableCollection<ArenaAvatarViewModel>();

        #endregion

        #region Commands

        public DelegateCommand ViewLoadedCommand { get; set; }

        #endregion

        #region Private methods

        private void _bindCommands()
        {
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
        }

        private void _radioAsigned(int _RadioIndex)
        {

            // Remember the old selection.
            List<int> activeIndices = new List<int>();
            foreach(ArenaAvatarViewModel _am in RadioCharacters)
            {
                if(_am.Active)
                {
                    activeIndices.Add(_am.Character.RadioNum);
                }
            }

            // Re-initialize the list.
            _viewLoaded_Execute();

            // Bring back the selection.
            foreach(ArenaAvatarViewModel _am in RadioCharacters)
            {
                if(activeIndices.Contains(_am.Character.RadioNum))
                {
                    _am.Active = true;
                }
            }
        }

        private void _characterPairChanged(SelectedCharactersPairEventArgs obj)
        {
            if(obj == null)
            {
                return;
            }

            int _radioIndex1 = -1;
            int _radioIndex2 = -1;
            if (obj.Character1Index != -1)
            {
                _radioIndex1 = mCharacterRepository.GetAll()[obj.Character1Index].RadioNum;
            }

            if(obj.Character2Index != -1)
            {
                _radioIndex2 = mCharacterRepository.GetAll()[obj.Character2Index].RadioNum;
            }

            if(Session.Get<bool>(Constants.BLE_MODE_ON) && RadioCharacters.Count > 0)
            {
                foreach (ArenaAvatarViewModel _ch in RadioCharacters)
                {
                    _ch.Active = false;
                }

                if(_radioIndex1 != -1)
                {
                    if(RadioCharacters.Where(rc => rc.Character.RadioNum == _radioIndex1).ToList().Count > 0)
                    {
                        ArenaAvatarViewModel _am = RadioCharacters.Where(rc => rc.Character.RadioNum == _radioIndex1).First();
                        if (_am != null)
                        {
                            _am.Active = true;
                        }
                    }
                                                            
                }
                    
                if(_radioIndex2 != -1)
                {
                    if (RadioCharacters.Where(rc => rc.Character.RadioNum == _radioIndex2).ToList().Count > 0)
                    {
                        ArenaAvatarViewModel _am = RadioCharacters.Where(rc => rc.Character.RadioNum == _radioIndex2).First();
                        if (_am != null)
                        {
                            _am.Active = true;
                        }
                    }                    
                }
                    
            }
        }

        private class CharacterComparer : IComparer<Character>
        {
            int IComparer<Character>.Compare(Character x, Character y)
            {
                if (x.RadioNum < y.RadioNum)
                    return -1;
                if (x.RadioNum == y.RadioNum)
                    return 0;
                return 1;
            }
        }

        private void _viewLoaded_Execute()
        {
            RadioCharacters.Clear();
            List<Character> _charactersWithRadios = mCharacterRepository.GetAll().Where(c => c.RadioNum != -1).ToList();
            _charactersWithRadios.Sort(new CharacterComparer());
            foreach (Character _ch in _charactersWithRadios)
            {
                RadioCharacters.Add(new ArenaAvatarViewModel
                {
                    Character = _ch,
                    Active = false,
                    InPlayground = false,
                    Left = 0, 
                    Top = 0
                });                
            }
            
        }

        #endregion
    }
}
