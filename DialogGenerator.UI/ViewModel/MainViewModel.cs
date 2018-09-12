using DialogGenerator.Core;
using GalaSoft.MvvmLight;

namespace DialogGenerator.UI.ViewModel
{
    public class MainViewModel:ViewModelBase
    {
        private ILogger mLogger;


        public MainViewModel(ILogger logger,INavigationViewModel _navigationViewModel,
            ICharacterDetailViewModel _characterDetailViewModel)
        {
            mLogger = logger;
            NavigationViewModel = _navigationViewModel;
            DetailViewModel = _characterDetailViewModel;
        }

        public void Load()
        {
            NavigationViewModel.Load();
        }

        public INavigationViewModel NavigationViewModel { get; }
        public ICharacterDetailViewModel DetailViewModel { get; }
    }
}
