using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.ComponentModel;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class DialogModelsNavigationViewModel:BindableBase,INavigationViewModel
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IDialogModelDataProvider mDialogModelDataProvider;
        private string mFilterText;
        private bool mIsDialogStarted;
        private ModelDialogInfo mSelectedDialogModelInfo;
        private CollectionViewSource mDialogModelsInfoCollection;

        #endregion

        #region - constructor -

        public DialogModelsNavigationViewModel(ILogger logger,IEventAggregator _eventAggregator,
            IDialogModelDataProvider _dialogModelDataProvider)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mDialogModelDataProvider = _dialogModelDataProvider;

            mDialogModelsInfoCollection = new CollectionViewSource();
            FilterText = "";

            mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Subscribe(_onCharacterSelectionActionChanged);
            mDialogModelsInfoCollection.Filter += _mDialogModelsInfoCollection_Filter;

            _bindCommands();
        }

        private void _onCharacterSelectionActionChanged(bool _isDialogStarted)
        {
            IsDialogStarted = _isDialogStarted;
        }

        #endregion

        #region - Commands -

        public DelegateCommand<object> ChangeModelDialogStatusCommand { get; set; }

        #endregion

        #region - event handlers -

        private void _mDialogModelsInfoCollection_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }

            var mdi = e.Item as ModelDialogInfo;
            if (mdi.ModelsCollectionName.ToUpper().Contains(FilterText.ToUpper()))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            ChangeModelDialogStatusCommand = new DelegateCommand<object>((param) => _changeModelDialogStatusCommand_Execute(param));
        }

        private void _changeModelDialogStatusCommand_Execute(object param)
        {
            var parameters = (object[])param;
            var _modelDialogInfo = parameters[0] as ModelDialogInfo;
            var _newState = (ModelDialogState)parameters[1];

            if (_modelDialogInfo.State == _newState)
                return;

            if (_newState == ModelDialogState.Off)
            {
                var _availableDialogModels = mDialogModelDataProvider.GetAllByState(ModelDialogState.Available);

                if (_availableDialogModels.Count == 1)
                    return;

                if(_modelDialogInfo.SelectedModelDialogIndex >= 0)
                {
                    _modelDialogInfo.SelectedModelDialogIndex = -1;
                    Session.Set(Constants.SELECTED_DLG_MODEL, -1);

                    mEventAggregator.GetEvent<OpenDialogModelDetailViewEvent>()
                        .Publish(_modelDialogInfo.ModelsCollectionName);
                }
            }

            _modelDialogInfo.State = _newState;
            mEventAggregator.GetEvent<ChangedDialogModelStateEvent>().Publish();
        }

        #endregion

        #region - public functions -

        public void Load()
        {
            mDialogModelsInfoCollection.Source = mDialogModelDataProvider.GetAll();
            RaisePropertyChanged(nameof(ModelsDialogInfoCollection));
        }

        #endregion

        #region - properties -

        public ICollectionView ModelsDialogInfoCollection
        {
            get
            {
                return mDialogModelsInfoCollection.View;
            }
        }


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

        public string FilterText
        {
            get { return mFilterText; }
            set
            {
                mFilterText = value;
                mDialogModelsInfoCollection.View?.Refresh();
                RaisePropertyChanged();
            }
        }

        public bool IsDialogStarted
        {
            get { return mIsDialogStarted; }
            set
            {
                mIsDialogStarted = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
