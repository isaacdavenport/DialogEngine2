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
    public class AssignedRadiosViewModel : BindableBase
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private ObservableCollection<Character> mCharacters = new ObservableCollection<Character>();

        public AssignedRadiosViewModel(ILogger _Logger, IEventAggregator _EventAggregator, ICharacterRepository _CharacterRepository)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;            

            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);
        }

        #region Properties

        public ObservableCollection<Character> Characters
        {
            get
            {
                return mCharacters;
            }
        }

        #endregion

        #region Private methods

        private void _onCharacterCollectionLoaded()
        {
            ObservableCollection<Character> _characters = mCharacterRepository.GetAll();
            foreach(Character _ch in _characters)
            {
                Characters.Add(_ch);
            }
        }

        #endregion
    }
}
