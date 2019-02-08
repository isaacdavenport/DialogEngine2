using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.UI.Views;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace DialogGenerator.ViewModels
{
    public class ShellViewModel:BindableBase
    {
        #region - fields -

        private IRegionManager mRegionManager;
        private IEventAggregator mEventAggregator;

        #endregion

        #region - constructor -

        public ShellViewModel(IRegionManager _regionManager,IEventAggregator _eventAggregator)
        {
            mRegionManager = _regionManager;
            mEventAggregator = _eventAggregator;

            _subscribeForEvents();
        }

        #endregion

        #region - private functions -

        private void _navigate(object _navigatePath)
        {
            var parameters = (object[])_navigatePath;

            if (_navigatePath != null)
                mRegionManager.RequestNavigate(parameters[0].ToString(), parameters[1].ToString());
        }

        private void _subscribeForEvents()
        {
            mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>().Subscribe(_onOpenCharacterDetailView);
            mEventAggregator.GetEvent<OpenDialogModelDetailViewEvent>().Subscribe(_onOpenDialogModelDetailView);
        }

        private void _onOpenDialogModelDetailView(string obj)
        {
            //var _activeView = mRegionManager.Regions[Constants.ContentRegion].GetView(typeof(DialogModelDetailView).FullName);

            //if (_activeView == null)
            //{
            //    mRegionManager.RequestNavigate(Constants.ContentRegion, typeof(DialogModelDetailView).FullName);
            //}
        }

        private void _onOpenCharacterDetailView(string obj)
        {
            var _activeView = mRegionManager.Regions[Constants.ContentRegion].GetView(typeof(CharacterDetailView).FullName);

            if(_activeView == null)
            {
                mRegionManager.RequestNavigate(Constants.ContentRegion, typeof(CharacterDetailView).FullName);
            }
        }

        #endregion

    }
}
