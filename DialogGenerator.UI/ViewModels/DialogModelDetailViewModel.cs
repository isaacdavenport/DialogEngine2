using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class DialogModelDetailViewModel : BindableBase, IDetailViewModel
    {
        #region - fields -

        private IEventAggregator mEventAggregator;
        private IDialogModelDataProvider mDialogModelDataProvider;
        private string mFilterText;
        private int mSelectedDialogModelIndex;
        private ModelDialogInfo mDialogModel;
        private ModelDialog mActiveDialogModel;
        private CollectionViewSource mDialogModelsCollection;

        #endregion

        #region - constructor -

        public DialogModelDetailViewModel(IEventAggregator _eventAggregator,IDialogModelDataProvider _dialogModelDataProvider)
        {
            mEventAggregator = _eventAggregator;
            mDialogModelDataProvider = _dialogModelDataProvider;
            mEventAggregator.GetEvent<OpenDialogModelDetailViewEvent>()
                .Subscribe(_onOpenDialogModelDetail);

            mDialogModelsCollection = new CollectionViewSource();
            mDialogModelsCollection.Filter += _mDialogModelsCollection_Filter;

            _bindCommands();
        }

        #endregion

        #region - commands -

        public DelegateCommand<ModelDialog> SetActiveDialogModel { get; set; }
        public DelegateCommand ResetActiveDialogModel { get; set; }

        #endregion

        #region - event handlers -

        private void _mDialogModelsCollection_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }
            else
            {
                var dm = e.Item as ModelDialog;

                if (dm.Name.ToUpper().Contains(FilterText.ToUpper()))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            SetActiveDialogModel = new DelegateCommand<ModelDialog>(_setActiveDialogModelCommand_Execute);
            ResetActiveDialogModel = new DelegateCommand(_resetActiveDialogModel_Execute, _resetActiveDialogModel_CanExecute);
        }

        private void _setActiveDialogModelCommand_Execute(ModelDialog obj)
        {
            SelectedDialogModelIndex = DialogModel.ArrayOfDialogModels.IndexOf(obj);

            int result = -1;
            var _dialogModelInfoList = mDialogModelDataProvider.GetAll();
            int _selectedIndex = _dialogModelInfoList.IndexOf(DialogModel);
            var _modelDialogInfo = _dialogModelInfoList.Where(dmi => dmi.SelectedModelDialogIndex > -1)
                                          .FirstOrDefault();

            if (_modelDialogInfo != null)
            {
                _modelDialogInfo.SelectedModelDialogIndex = -1;
            }

            for (int i = 0; i < _selectedIndex; i++)
            {
                if (_dialogModelInfoList[i].State == ModelDialogState.On)
                    result += _dialogModelInfoList[i].ArrayOfDialogModels.Count;
            }

            ActiveDialogModel = DialogModel.ArrayOfDialogModels[SelectedDialogModelIndex];
            DialogModel.SelectedModelDialogIndex = mSelectedDialogModelIndex;
            Session.Set(Constants.SELECTED_DLG_MODEL, result);
        }

        private bool _resetActiveDialogModel_CanExecute()
        {
            return ActiveDialogModel != null;
        }

        private void _resetActiveDialogModel_Execute()
        {
            var _dialogModelInfoList = mDialogModelDataProvider.GetAll();
            var _modelDialogInfo = _dialogModelInfoList.Where(dmi => dmi.SelectedModelDialogIndex > -1)
                                          .FirstOrDefault();

            if (_modelDialogInfo != null)
            {
                _modelDialogInfo.SelectedModelDialogIndex = -1;
            }
            Session.Set(Constants.SELECTED_DLG_MODEL, -1);
            ActiveDialogModel = null;
        }


        private void _onOpenDialogModelDetail(string _dialogModelName)
        {
            Load(_dialogModelName);
        }

        #endregion

        #region - public functions -

        public void Load(string name)
        {
            DialogModel = mDialogModelDataProvider.GetByName(name);
            mDialogModelsCollection.Source = DialogModel.ArrayOfDialogModels;

            RaisePropertyChanged("DialogModelCollection");
        }

        #endregion

        #region - properties -

        public ModelDialogInfo DialogModel
        {
            get { return mDialogModel; }
            set
            {
                mDialogModel = value;
                RaisePropertyChanged();
            }
        }

        public ModelDialog ActiveDialogModel
        {
            get { return mActiveDialogModel; }
            set
            {
                mActiveDialogModel = value;
                ((DelegateCommand)ResetActiveDialogModel).RaiseCanExecuteChanged();
                RaisePropertyChanged();
            }
        }

        public int SelectedDialogModelIndex
        {
            get { return mSelectedDialogModelIndex; }
            set
            {
                mSelectedDialogModelIndex = value;
                RaisePropertyChanged();
            }
        }

        public ICollectionView DialogModelCollection
        {
            get
            {
                return mDialogModelsCollection.View;
            }
        }

        public string FilterText
        {
            get { return mFilterText; }
            set
            {
                mFilterText = value;
                this.mDialogModelsCollection.View.Refresh();
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
