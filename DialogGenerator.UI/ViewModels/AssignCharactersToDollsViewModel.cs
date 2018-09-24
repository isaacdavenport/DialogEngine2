using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Helper;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class AssignCharactersToDollsViewModel : BindableBase
    {

        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterDataProvider mCharacterDataProvider;
        private Point mStartPosition;

        #endregion

        #region - constructor -
        public AssignCharactersToDollsViewModel(ILogger _logger,IEventAggregator _eventAggregator,
            ICharacterDataProvider _characterDataProvider)
        {
            mLogger = _logger;
            mEventAggregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;

            _bindCommands();
        }

        #endregion

        #region - commands -

        public DelegateCommand<MouseButtonEventArgs> PreviewMouseLeftButtonDownCommand { get; set; }
        public DelegateCommand<MouseEventArgs> PreviewMouseMoveCommand { get; set; }
        public DelegateCommand<DragEventArgs> DragEnterCommand { get; set; }
        public DelegateCommand<DragEventArgs> DropCommand { get; set; }
        public DelegateCommand<DragEventArgs> DragOverCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            PreviewMouseLeftButtonDownCommand = new DelegateCommand<MouseButtonEventArgs>(_previewMouseLeftButtonDownCommand_Execute);
            PreviewMouseMoveCommand = new DelegateCommand<MouseEventArgs>(_previewMouseMoveCommand_Execute);
            DragEnterCommand = new DelegateCommand<DragEventArgs>(_dragEnterCommand_Execute);
            DropCommand = new DelegateCommand<DragEventArgs>(_dropCommand_Execute);
            DragOverCommand = new DelegateCommand<DragEventArgs>(_dragOverCommand_Execute);
        }

        private void _dragOverCommand_Execute(DragEventArgs obj)
        {
            obj.Handled = true;
        }

        private void _dropCommand_Execute(DragEventArgs obj)
        {
            try
            {
                if (obj.Data.GetDataPresent("characterFormat"))
                {
                    Character _draggedCharacter = obj.Data.GetData("characterFormat") as Character;
                    var _objectSource = obj.OriginalSource is Run ? (obj.OriginalSource as Run).Parent : obj.OriginalSource;
                    Grid grid = VisualHelper.GetNearestContainer<Grid>(_objectSource as DependencyObject);
                    int _currentDollIndex = int.Parse(grid.Tag.ToString());
                    Character _assignedCharacter = CharacterRadioRelationshipList[_currentDollIndex];

                    if (_assignedCharacter == null)
                    {
                        var key = CharacterRadioRelationshipList
                            .Where(pair => pair.Value != null && pair.Value.Equals(_draggedCharacter))
                            .Select(pair => pair.Key);

                        if (key.Any())
                        {
                            CharacterRadioRelationshipList[key.First()] = null;
                        }

                        _draggedCharacter.RadioNum = _currentDollIndex;
                        CharacterRadioRelationshipList[_currentDollIndex] = _draggedCharacter;
                    }
                    else
                    {
                        if (_assignedCharacter.Equals(_draggedCharacter))
                        {
                            obj.Handled = true;
                            return;
                        }
                        else
                        {
                            var key = CharacterRadioRelationshipList
                                .Where(pair => pair.Value != null && pair.Value.Equals(_draggedCharacter))
                                .Select(pair => pair.Key);

                            if (key.Any())
                            {
                                CharacterRadioRelationshipList[key.First()] = null;
                            }

                            _assignedCharacter.RadioNum = -1;
                            _draggedCharacter.RadioNum = _currentDollIndex;

                            CharacterRadioRelationshipList[_currentDollIndex] = _draggedCharacter;
                        }
                    }

                    var _itemsControl = obj.Source as ItemsControl;
                    _itemsControl.Items.Refresh();

                    var _listbox = VisualHelper.GetVisualChild<ListBox>(_itemsControl.Parent as Grid);
                    _listbox.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("Error during executing drop command. " + ex.Message);
            }
            finally
            {
                obj.Handled = true;
            }
        }

        private void _dragEnterCommand_Execute(DragEventArgs obj)
        {
            try
            {
                if (!obj.Data.GetDataPresent("characterFormat") || !(obj.Source is ItemsControl))
                    obj.Effects = DragDropEffects.None;
                else
                    obj.Effects = DragDropEffects.Copy;
            }
            catch (Exception ex)
            {
                mLogger.Error("Drag enter command. " + ex.Message);
            }
            finally
            {
                obj.Handled = true;
            }
        }

        private void _previewMouseMoveCommand_Execute(MouseEventArgs obj)
        {
            try
            {
                Point _mousePos = obj.GetPosition(null);
                Vector diff = mStartPosition - _mousePos;

                if ((obj.LeftButton == MouseButtonState.Pressed)
                    && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    ListBox _listBox = obj.Source as ListBox;
                    ListBoxItem _listBoxItem = VisualHelper.GetNearestContainer<ListBoxItem>((DependencyObject)obj.OriginalSource);

                    Character character = (Character)_listBox.ItemContainerGenerator.ItemFromContainer(_listBoxItem);

                    DataObject _dragData = new DataObject("characterFormat", character);

                    DragDrop.DoDragDrop(_listBoxItem, _dragData, DragDropEffects.Copy);
                }

            }
            catch (Exception ex)
            {
                mLogger.Error("Error during preview mosue down. " + ex.Message);
            }
            finally
            {
                obj.Handled = true;
            }
        }

        private void _previewMouseLeftButtonDownCommand_Execute(MouseButtonEventArgs obj)
        {
            mStartPosition = obj.GetPosition(null);
        }

        #endregion

        #region - properties -

        public ObservableCollection<Character> Characters
        {
            get { return Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS); }
        }

        public Dictionary<int,Character> CharacterRadioRelationshipList
        {
            get { return Session.Get<Dictionary<int, Character>>(Constants.CH_RADIO_RELATIONSHIP); }
        }

        #endregion
    }
}
