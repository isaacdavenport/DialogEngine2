using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Events;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Prism.Events;

namespace DialogGenerator.UI.ViewModel
{
    public class DialogModelDetailViewModel : ViewModelBase, IDialogModelDetailViewModel
    {
        #region - fields -

        private IEventAggregator mEventAggregator;
        private IDialogModelDataProvider mDialogModelDataProvider;
        private ModelDialogInfo mDialogModel;

        #endregion

        #region - constructor -

        public DialogModelDetailViewModel(IEventAggregator _eventAggregator,IDialogModelDataProvider _dialogModelDataProvider)
        {
            mEventAggregator = _eventAggregator;
            mDialogModelDataProvider = _dialogModelDataProvider;
            mEventAggregator.GetEvent<OpenDialogModelDetailViewEvent>()
                .Subscribe(_onOpenDialogModelDetail);
        }

        #endregion

        #region - commands -

        public RelayCommand SetActiveDialogModel { get; set; }
        public RelayCommand ResetActiveDialogModel { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            SetActiveDialogModel = new RelayCommand(_setActiveDialogModel_Execute, _setActiveDialogModel_CanExecute);
            ResetActiveDialogModel = new RelayCommand(_resetActiveDialogModel_Execute, _resetActiveDialogModel_CanExeccute);
        }

        private bool _resetActiveDialogModel_CanExeccute()
        {
            return true;
        }

        private void _resetActiveDialogModel_Execute()
        {
            DialogModel.SelectedModelDialogIndex = -1;
            Session.Set(Constants.SELECTED_DLG_MODEL, -1);
        }

        private bool _setActiveDialogModel_CanExecute()
        {
            return true;
        }

        private void _setActiveDialogModel_Execute()
        {
            int result = -1;
            var _dialogModelInfoList = mDialogModelDataProvider.GetAll();
            int _selectedIndex = _dialogModelInfoList.IndexOf(DialogModel);

            for(int i = 0; i < _selectedIndex; i++)
            {
                if (_dialogModelInfoList[i].State == ModelDialogState.On)
                    result += _dialogModelInfoList[i].ArrayOfDialogModels.Count;
            }

            Session.Set(Constants.SELECTED_DLG_MODEL, result);
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
        }

        #endregion

        #region - properties -

        public ModelDialog SelectedDialogModel { get; set; }

        public ModelDialogInfo DialogModel
        {
            get { return mDialogModel; }
            set
            {
                mDialogModel = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
