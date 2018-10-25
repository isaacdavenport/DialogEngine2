using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private const string mDollFileName = "doll.jpg";

        #endregion

        #region - constructor -
        public AssignCharactersToDollsViewModel(ILogger _logger, IEventAggregator _eventAggregator,
            ICharacterDataProvider _characterDataProvider)
        {
            mLogger = _logger;
            mEventAggregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;

            _bindCommands();
        }

        #endregion

        #region - commands -

        public DelegateCommand<object> UnbindCharacterCommand { get; set; }
        public DelegateCommand<object> SaveConfigurationCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            UnbindCharacterCommand = new DelegateCommand<object>(_unbindCharacterCommand_Execute);
            SaveConfigurationCommand = new DelegateCommand<object>(_saveConfigurationCommand_Execute);
        }

        private async void _saveConfigurationCommand_Execute(object param)
        {
            object[] parametes = (object[])param;
            int _indexOfSelectedCharacter = int.Parse(parametes[1].ToString());
            var popup = parametes[3] as PopupBox;

            if (_indexOfSelectedCharacter < 0)
            {
                popup.IsPopupOpen = false;
                return;
            }

            int _currentDoll = int.Parse(parametes[0].ToString());
            var _itemsControl = parametes[2] as ItemsControl;

            var _selectedCharacter = mCharacterDataProvider.GetAll()[_indexOfSelectedCharacter];

            if(_selectedCharacter.RadioNum >= 0)
            {
                CharacterRadioRelationshipList[_selectedCharacter.RadioNum] = null;
            }

            if(CharacterRadioRelationshipList[_currentDoll] != null)
            {
                var _oldCharacter = mCharacterDataProvider.GetByAssignedRadio(_currentDoll);
                _oldCharacter.RadioNum = -1;

                await mCharacterDataProvider.SaveAsync(_oldCharacter);
            }

            _selectedCharacter.RadioNum = _currentDoll;
            CharacterRadioRelationshipList[_currentDoll] = _selectedCharacter;

            await mCharacterDataProvider.SaveAsync(_selectedCharacter);

            popup.IsPopupOpen = false;
            _itemsControl.Items.Refresh();
        }

        private async void _unbindCharacterCommand_Execute(object parameters)
        {
            var args = (object[])parameters;
            int _dollNumber = int.Parse(args[0].ToString());
            var _itemsControl = args[1] as ItemsControl;

            if (CharacterRadioRelationshipList[_dollNumber] == null)
                return;

            CharacterRadioRelationshipList[_dollNumber].RadioNum = -1;

            await mCharacterDataProvider.SaveAsync(CharacterRadioRelationshipList[_dollNumber]);

            CharacterRadioRelationshipList[_dollNumber] = null;

            _itemsControl.Items.Refresh();
        }

        #endregion

        #region - properties -

        public ObservableCollection<Character> Characters
        {
            get { return mCharacterDataProvider.GetAll(); }
        }

        public Dictionary<int,Character> CharacterRadioRelationshipList
        {
            get { return Session.Get<Dictionary<int, Character>>(Constants.CH_RADIO_RELATIONSHIP); }
        }

        public string DollFileName
        {
            get { return Path.Combine(ApplicationData.Instance.ImagesDirectory, mDollFileName); }
        }

        #endregion
    }
}
