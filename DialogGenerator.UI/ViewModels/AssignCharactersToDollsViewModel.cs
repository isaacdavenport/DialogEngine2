using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Helper;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace DialogGenerator.UI.ViewModels
{
    public class AssignCharactersToDollsViewModel : BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterDataProvider mCharacterDataProvider;
        private List<int> mDolls;

        #endregion

        #region - constructor -
        public AssignCharactersToDollsViewModel(ILogger _logger, IEventAggregator _eventAggregator,
            ICharacterDataProvider _characterDataProvider)
        {
            mLogger = _logger;
            mEventAggregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;

            Dolls = Enumerable.Range(0, ApplicationData.Instance.NumberOfRadios).ToList();

            _bindCommands();
        }

        #endregion

        #region - commands -

        public DelegateCommand ViewLoadedCommand { get; set; }
        public DelegateCommand<object> SaveConfigurationCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
            SaveConfigurationCommand = new DelegateCommand<object>(_saveConfigurationCommand_Execute);
        }

        private void _viewLoaded_Execute()
        {
            ApplicationData.Instance.UseBLERadios = true;
            ApplicationData.Instance.Save();
        }

        private async void _saveConfigurationCommand_Execute(object param)
        {
            object[] parameters = (object[])param;
            var cbx = parameters[1] as ComboBox;
             
            int _indexOfSelectedCharacter = cbx.SelectedIndex;
            if (_indexOfSelectedCharacter < 0)
            {
                return;
            }

            int _currentDoll = int.Parse(parameters[0].ToString());
            var _selectedCharacter = Characters[_indexOfSelectedCharacter];

            if(mCharacterDataProvider.GetByAssignedRadio(_currentDoll) != null)
            {
                var _oldCharacter = mCharacterDataProvider.GetByAssignedRadio(_currentDoll);
                _oldCharacter.RadioNum = -1;

                await mCharacterDataProvider.SaveAsync(_oldCharacter);
            }

            if (!_selectedCharacter.Unassigned)
            {
                _selectedCharacter.RadioNum = _currentDoll;

                await mCharacterDataProvider.SaveAsync(_selectedCharacter);
            }           

            var _itemsControl = VisualHelper.GetNearestContainer<ItemsControl>(cbx.Parent);
            _itemsControl?.Items.Refresh();
        }

        #endregion

        #region - properties -

        public ObservableCollection<Character> Characters
        {
            get
            {
                var characters = new ObservableCollection<Character>(mCharacterDataProvider.GetAll());

                if (!characters.Any(c => c.Unassigned))
                {
                    var unassigned = new Character
                    {
                        CharacterName = "Unassigned",
                        Unassigned = true
                    };

                    characters.Insert(0, unassigned);
                }

                return characters;
            }
        }

        public List<int> Dolls
        {
            get { return mDolls; }
            set
            {
                mDolls = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
