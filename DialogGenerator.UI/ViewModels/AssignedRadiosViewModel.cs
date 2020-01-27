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
        private ObservableCollection<Character> mRadioCharacters = new ObservableCollection<Character>();
        private Visibility mVisible;
        
        public AssignedRadiosViewModel(ILogger _Logger, IEventAggregator _EventAggregator, ICharacterRepository _CharacterRepository)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;
            Visible = Visibility.Collapsed;

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

        public ObservableCollection<Character> RadioCharacters
        {
            get
            {
                return mRadioCharacters;
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
            List<Character> _charactersWithRadios = mCharacterRepository.GetAll().Where(c => c.RadioNum != -1).ToList();
            _charactersWithRadios.Sort(new CharacterComparer());
            foreach (Character _ch in _charactersWithRadios)
            {
                mRadioCharacters.Add(_ch);
            }                                        
        }

        #endregion
    }
}
