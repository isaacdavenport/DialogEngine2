using DialogGenerator.UI.ViewModels;
using System.Linq;
using Xunit;

namespace DialogGenerator.Tests.UI.ViewModel
{
    public class CharactersNavigationViewModelTests:ViewModelTestBase
    {
        private CharactersNavigationViewModel mViewModel;

        public CharactersNavigationViewModelTests()
        {
            mViewModel =new  CharactersNavigationViewModel(loggerMock.Object, eventAggregatorMock.Object
                ,dialogDataRepositoryMock.Object
                ,wizardDataProviderMock.Object
                ,dialogModelDataProviderMock.Object
                ,characterDataProviderMock.Object
                ,messageDialogServiceMock.Object);

            testSetup();
        }

        [Fact]
        public void Load_ShouldLoadCharacters()
        {
            mViewModel.Load();

            Assert.NotEmpty(mViewModel.CharactersViewSource.Cast<object>());
        }
    }
}
