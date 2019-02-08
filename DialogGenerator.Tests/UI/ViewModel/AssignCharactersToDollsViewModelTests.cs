using DialogGenerator.Core;
using DialogGenerator.Tests.TestHelper;
using DialogGenerator.UI.ViewModels;
using Moq;
using System.Linq;
using System.Windows.Controls;
using Xunit;

namespace DialogGenerator.Tests.UI.ViewModel
{
    public class AssignCharactersToDollsViewModelTests:ViewModelTestBase
    {
        private AssignCharactersToDollsViewModel mViewModel;

        public AssignCharactersToDollsViewModelTests()
        {
            mViewModel = new AssignCharactersToDollsViewModel(loggerMock.Object, eventAggregatorMock.Object, characterDataProviderMock.Object);

            testSetup();
        }

        [Fact]
        public void TestIsDataProperlyInitialized()
        {
            Assert.Equal(ApplicationData.Instance.NumberOfRadios, mViewModel.Dolls.Count);
            Assert.Equal(mViewModel.Characters.Count, characters.Count);
        }
    }
}
