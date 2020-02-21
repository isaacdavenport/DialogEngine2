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
    public class DebugViewModel:BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterDataProvider mCharacterDataProvider;
        private string mCharacter1Prefix;
        private string mCharacter2Prefix;
        private HeatMapData mHeatMap;

        #endregion

        #region - constructor -

        public DebugViewModel(ILogger logger,IUserLogger _userLogger
            ,IEventAggregator _eventAggregator
            ,ICharacterDataProvider _characterDataProvider)
        {
            mLogger = logger;
            UserLogger = _userLogger;
            mEventAggregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;

            mEventAggregator.GetEvent<HeatMapUpdateEvent>().Subscribe(_onHeatMapUpdate);
            mHeatMap = new HeatMapData();
            HeatMap.HeatMap = new int[ApplicationData.Instance.NumberOfRadios, ApplicationData.Instance.NumberOfRadios];
            HeatMap.MotionVector = new int[ApplicationData.Instance.NumberOfRadios];
            HeatMap.LastHeatMapUpdateTime = new DateTime[ApplicationData.Instance.NumberOfRadios];

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

        private void _onHeatMapUpdate(HeatMapData data)
        {
            var characters = mCharacterDataProvider.GetAll();
            int _characterCount = mCharacterDataProvider.GetAll().Count;
            HeatMap = data;

            if(data.Character1Index < _characterCount)
            {
                Character1Prefix = characters[data.Character1Index].CharacterPrefix;
            }

            if(data.Character2Index < _characterCount)
            {
                Character2Prefix = characters[data.Character2Index].CharacterPrefix;
            }            
        }

        #endregion

        #region - properties -

        public HeatMapData HeatMap
        {
            get { return mHeatMap; }
            set
            {
                mHeatMap = value;
                RaisePropertyChanged();
            }
        }
        
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
