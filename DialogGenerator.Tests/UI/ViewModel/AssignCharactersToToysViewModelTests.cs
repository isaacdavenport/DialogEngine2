using DialogGenerator.Core;
using DialogGenerator.Tests.TestHelper;
using DialogGenerator.UI.ViewModels;
using Moq;
using System.Linq;
using System.Windows.Controls;
using Xunit;

namespace DialogGenerator.Tests.UI.ViewModel
{
    public class AssignCharactersToToysViewModelTests:ViewModelTestBase
    {
        private AssignCharactersToToysViewModel mViewModel;

        public AssignCharactersToToysViewModelTests()
        {
            mViewModel = new AssignCharactersToToysViewModel(loggerMock.Object, eventAggregatorMock.Object, characterDataProviderMock.Object);

            testSetup();
        }

        [Fact]
        public void TestIsDataProperlyInitialized()
        {
            Assert.Equal(ApplicationData.Instance.NumberOfRadios, mViewModel.Toys.Count);
            Assert.Equal(mViewModel.Characters.Count, characters.Count);
        }
    }
}
