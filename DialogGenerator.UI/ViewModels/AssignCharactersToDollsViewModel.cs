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
            Button btn = parametes[1] as Button;
            Grid parent = btn.Parent as Grid;
            ComboBox cbx = (ComboBox)parent.Children[0];
             
            int _indexOfSelectedCharacter = cbx.SelectedIndex;
            if (_indexOfSelectedCharacter < 0)
            {
                return;
            }

            int _currentDoll = int.Parse(parametes[0].ToString());
            var _selectedCharacter = mCharacterDataProvider.GetAll()[_indexOfSelectedCharacter];

            if(mCharacterDataProvider.GetByAssignedRadio(_currentDoll) != null)
            {
                var _oldCharacter = mCharacterDataProvider.GetByAssignedRadio(_currentDoll);
                _oldCharacter.RadioNum = -1;

                await mCharacterDataProvider.SaveAsync(_oldCharacter);
            }

            _selectedCharacter.RadioNum = _currentDoll;

            await mCharacterDataProvider.SaveAsync(_selectedCharacter);

            var _itemsControl = VisualHelper.GetNearestContainer<ItemsControl>(parent);
            _itemsControl?.Items.Refresh();
        }

        private async void _unbindCharacterCommand_Execute(object parameters)
        {
            var args = (object[])parameters;
            int _dollNumber = int.Parse(args[0].ToString());
            var _assignedCharacter = mCharacterDataProvider.GetByAssignedRadio(_dollNumber);

            if (_assignedCharacter == null)
                return;

            _assignedCharacter.RadioNum = -1;

            await mCharacterDataProvider.SaveAsync(_assignedCharacter);

            var _itemsControl = VisualHelper.GetNearestContainer<ItemsControl>(args[1] as Button);
            _itemsControl.Items.Refresh();
        }

        #endregion

        #region - properties -

        public ObservableCollection<Character> Characters
        {
            get { return mCharacterDataProvider.GetAll(); }
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
