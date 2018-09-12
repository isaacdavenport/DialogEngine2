using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Events;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Prism.Events;
using System.Collections.ObjectModel;

namespace DialogGenerator.UI.ViewModel
{
    public class DialogModelsNavigationViewModel:ViewModelBase,INavigationViewModel
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IDialogModelDataProvider mDialogModelDataProvider;
        private ModelDialogInfo mSelectedDialogModelInfo;

        #endregion

        #region - constructor -

        public DialogModelsNavigationViewModel(ILogger logger,IEventAggregator _eventAggregator,
            IDialogModelDataProvider _dialogModelDataProvider)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mDialogModelDataProvider = _dialogModelDataProvider;

            _bindCommands();
        }

        #endregion

        #region - Commands -

        public RelayCommand<object> ChangeModelDialogStatusCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            ChangeModelDialogStatusCommand = new RelayCommand<object>((param) => _changeModelDialogStatusCommand_Execute(param));
        }

        private void _changeModelDialogStatusCommand_Execute(object param)
        {
            var parameters = (object[])param;
            var _modelDialogInfo = parameters[0] as ModelDialogInfo;
            var _newState = (ModelDialogState)parameters[1];

            if (_modelDialogInfo.State == _newState)
                return;

            _modelDialogInfo.State = _newState;
        }

        #endregion

        #region - public functions -

        public void Load()
        {
            var _dialogModels = mDialogModelDataProvider.GetAll();

            DialogModels.Clear();
            foreach(var _dialogModelInfo in _dialogModels)
            {
                DialogModels.Add(_dialogModelInfo);
            }
        }

        #endregion

        #region - properties -

        public ObservableCollection<ModelDialogInfo> DialogModels { get; set; } = new ObservableCollection<ModelDialogInfo>();

        public ModelDialogInfo SelectedDialogModelInfo
        {
            get { return mSelectedDialogModelInfo; }
            set
            {
                mSelectedDialogModelInfo = value;
                RaisePropertyChanged();

                if (mSelectedDialogModelInfo != null)
                {
                    mEventAggregator.GetEvent<OpenDialogModelDetailViewEvent>()
                        .Publish(mSelectedDialogModelInfo.ModelsCollectionName);
                }
            }
        }

        #endregion
    }
}
