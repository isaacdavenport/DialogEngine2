using System;
using System.Windows.Controls;
using System.Windows.Data;
using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace DialogGenerator.UI.ViewModels
{
    public class DebugViewModel : BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterDataProvider mCharacterDataProvider;
        private string mCharacter1Prefix;
        private string mCharacter2Prefix;

        #endregion

        #region - constructor -

        public DebugViewModel(ILogger logger, IUserLogger _userLogger
            , IEventAggregator _eventAggregator
            , ICharacterDataProvider _characterDataProvider)
        {
            mLogger = logger;
            UserLogger = _userLogger;
            mEventAggregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;


            _bindCommands();
        }

        #endregion

        #region - commands -

        public DelegateCommand<SelectionChangedEventArgs> RefreshTabItemCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            RefreshTabItemCommand = new DelegateCommand<SelectionChangedEventArgs>(_refreshTabItemCommand_Execute);
        }

        private void _refreshTabItemCommand_Execute(SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem
                    && (e.AddedItems[0] as TabItem).Content is ListView)
                {
                    var column = (((e.AddedItems[0] as TabItem).Content as ListView).View as GridView).Columns[0];
                    BindingOperations.GetBindingExpression(column, GridViewColumn.WidthProperty).UpdateTarget();
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("Error during refreshing tab item binding. " + ex.Message);
            }
        }


        #endregion

        #region - properties -


        public string Character1Prefix
        {
            get { return mCharacter1Prefix; }
            set
            {
                mCharacter1Prefix = value;
                RaisePropertyChanged();
            }
        }

        public string Character2Prefix
        {
            get { return mCharacter2Prefix; }
            set
            {
                mCharacter2Prefix = value;
                RaisePropertyChanged();
            }
        }

        public IUserLogger UserLogger { get; set; }

        #endregion
    }
}
