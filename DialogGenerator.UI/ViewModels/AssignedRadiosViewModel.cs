using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
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

namespace DialogGenerator.UI.ViewModels
{
    public class AssignedRadiosViewModel : BindableBase
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private ObservableCollection<Character> mCharacters = new ObservableCollection<Character>();
        private Character mRadioCharacter0;
        private Character mRadioCharacter1;
        private Character mRadioCharacter2;
        private Character mRadioCharacter3;
        private Character mRadioCharacter4;
        private Character mRadioCharacter5;
        private Visibility mVisible;
        
        public AssignedRadiosViewModel(ILogger _Logger, IEventAggregator _EventAggregator, ICharacterRepository _CharacterRepository)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;
            Visible = Visibility.Collapsed;

            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);

            _bindCommands();
        }

        

        #region Properties

        public ObservableCollection<Character> Characters
        {
            get
            {
                return mCharacters;
            }
        }
        
        public Character RadioCharacter0
        {
            get
            {
                return mRadioCharacter0;
            }

            set
            {
                mRadioCharacter0 = value;
                RaisePropertyChanged();
            }
        }

        public Character RadioCharacter1
        {
            get
            {
                return mRadioCharacter1;
            }

            set
            {
                mRadioCharacter1 = value;
                RaisePropertyChanged();
            }
        }

        public Character RadioCharacter2
        {
            get
            {
                return mRadioCharacter2;
            }

            set
            {
                mRadioCharacter2 = value;
                RaisePropertyChanged();
            }
        }

        public Character RadioCharacter3
        {
            get
            {
                return mRadioCharacter3;
            }

            set
            {
                mRadioCharacter3 = value;
                RaisePropertyChanged();
            }
        }

        public Character RadioCharacter4
        {
            get
            {
                return mRadioCharacter4;
            }

            set
            {
                mRadioCharacter4 = value;
                RaisePropertyChanged();
            }
        }

        public Character RadioCharacter5
        {
            get
            {
                return mRadioCharacter5;
            }

            set
            {
                mRadioCharacter5 = value;
                RaisePropertyChanged();
            }
        }

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

        #endregion

        #region Commands

        public DelegateCommand ViewLoadedCommand { get; set; }

        #endregion

        #region Private methods

        private void _bindCommands()
        {
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
        }

        private void _viewLoaded_Execute()
        {
            List<Character> _charactersWithRadios = mCharacterRepository.GetAll().Where(c => c.RadioNum != -1).ToList();
            foreach (Character _c in _charactersWithRadios)
            {
                switch (_c.RadioNum)
                {
                    case 1:
                        //RadioCharacter1 = _c;
                        mRadioCharacter0 = _c;
                        break;
                    case 2:
                        //RadioCharacter2 = _c;
                        mRadioCharacter2 = _c;

                        break;
                    case 3:
                        //RadioCharacter3 = _c;
                        mRadioCharacter3 = _c;
                        break;
                    case 4:
                        //RadioCharacter4 = _c;
                        mRadioCharacter4 = _c;
                        break;
                    case 5:
                        //RadioCharacter5 = _c;
                        mRadioCharacter5 = _c;
                        break;
                }
            }

            //Visible = Visibility.Collapsed;
        }

        private void _onCharacterCollectionLoaded()
        {
            ObservableCollection<Character> _characters = mCharacterRepository.GetAll();
            foreach(Character _ch in _characters)
            {
                Characters.Add(_ch);
                switch(_ch.RadioNum)
                {
                    case 0:
                        RadioCharacter0 = _ch;
                        break;
                    case 1:
                        RadioCharacter1 = _ch;
                        break;
                    case 2:
                        RadioCharacter2 = _ch;
                        break;
                    case 3:
                        RadioCharacter3 = _ch;
                        break;
                    case 4:
                        RadioCharacter4 = _ch;
                        break;
                    case 5:
                        RadioCharacter5 = _ch;
                        break;
                    default:
                        break;

                }
            }
        }

        #endregion
    }
}
