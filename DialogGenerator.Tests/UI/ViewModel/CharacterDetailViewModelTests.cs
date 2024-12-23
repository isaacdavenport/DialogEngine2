﻿using DialogGenerator.CharacterSelection;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.Utilities;
using Moq;
using Prism.Regions;
using Xunit;

namespace DialogGenerator.Tests.UI.ViewModel
{
    public class CharacterDetailViewModelTests:ViewModelTestBase
    {
        private CharacterDetailViewModel mViewModel;
        private Mock<IMP3Player> mMP3PlayerMock = new Mock<IMP3Player>();        
        private Mock<IRegionManager> regionManagerMock = new Mock<IRegionManager>();
        private Mock<IBLEDataProviderFactory> bleDataProviderFactoryMock = new Mock<IBLEDataProviderFactory>();

        public CharacterDetailViewModelTests()
        {
            testSetup();

            mViewModel = new CharacterDetailViewModel(loggerMock.Object, eventAggregatorMock.Object
                , characterDataProviderMock.Object
                , messageDialogServiceMock.Object
                , mMP3PlayerMock.Object
                , regionManagerMock.Object
                , bleDataProviderFactoryMock.Object);
            
        }


        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Load_ShouldInitializeEmptyCharacter_WhenPrameterIsNotValidCharacterInitials(string initials)
        {
            mViewModel.Load(initials);

            Assert.NotNull(mViewModel.Character);
        }

        [Fact]
        public void Load_ShouldInitializeCharacter_WhenProvidedValidCharacterInitials()
        {
            string initials = "ch1";

            mViewModel.Load(initials);

            Assert.True(mViewModel.IsEditing);
            Assert.Equal(initials, mViewModel.Character.CharacterPrefix);
        }
    }
}
