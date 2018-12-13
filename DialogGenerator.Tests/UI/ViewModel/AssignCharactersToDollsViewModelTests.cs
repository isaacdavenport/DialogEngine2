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

        [Fact]
        public async void UnbindCharacterCommand_ShouldSetCharacterToDefalutValue_WhenUserClickOnUnbindBtn()
        {
            await STATaskHelper.StartSTATask(() =>
            {
                const int _dollNumber = 1;
                var args = new object[] { _dollNumber, new ItemsControl() };

                mViewModel.UnbindCharacterCommand.Execute(args);

                Assert.Equal(-1, characters.First().RadioNum);

                characterDataProviderMock.Verify(m => m.SaveAsync(characters.First()), Times.Once);
            });
        }

        [Fact]
        public async void UnbindCharacterCommand_ShouldNotSaveChanges_WhenIsInvalidRadioNumber()
        {
            await STATaskHelper.StartSTATask(() =>
            {
                const int _dollNumber = -1;
                var args = new object[] { _dollNumber, new ItemsControl() };

                mViewModel.UnbindCharacterCommand.Execute(args);
                characterDataProviderMock.Verify(m => m.SaveAsync(characters.First()), Times.Never);
            });
        }

        //public async void SaveConfigurationCommand_

    }
}
