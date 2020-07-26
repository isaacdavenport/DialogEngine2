using DialogGenerator.UI.ViewModels;
using System.Linq;
using Xunit;

namespace DialogGenerator.Tests.UI.ViewModel
{
    public class CharactersNavigationViewModelTests:ViewModelTestBase
    {
        private CreateViewModel mViewModel;

        public CharactersNavigationViewModelTests()
        {
            mViewModel =new  CreateViewModel(loggerMock.Object, eventAggregatorMock.Object
                ,dialogDataRepositoryMock.Object
                ,wizardDataProviderMock.Object
                ,dialogModelDataProviderMock.Object
                ,characterDataProviderMock.Object
                ,messageDialogServiceMock.Object
                ,regionManagerMock.Object);

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
