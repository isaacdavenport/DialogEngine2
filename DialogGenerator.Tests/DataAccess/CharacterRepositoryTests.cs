using DialogGenerator.DataAccess;
using DialogGenerator.Model.Enum;
using System.Linq;
using Xunit;

namespace DialogGenerator.Tests.DataAccess
{
    public class CharacterRepositoryTests:RepositoryTestBase
    {
        private CharacterRepository mRepository;

        public CharacterRepositoryTests()
        {
            mRepository = new CharacterRepository(loggerMock.Object, wizardRepositoryMock.Object, dialogModelRepositoryMock.Object);

            testSetup();
        }

        [Fact]
        public void GetAll_ShouldReturnData()
        {
            var _loadedCharacters = mRepository.GetAll();

            Assert.Equal(characters.Count, _loadedCharacters.Count);
        }

        [Theory]
        [InlineData("tc1")]
        public void GetByInitials_ShouldFindCharacter(string initials)
        {
            var character = mRepository.GetByInitials(initials);

            Assert.NotNull(character);
            Assert.Equal(initials, character.CharacterPrefix);
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData(null)]
        public void GetByInitials_ShouldNotFindCharacter(string initials)
        {
            var character = mRepository.GetByInitials(initials);

            Assert.Null(character);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void GetByAssignedRadio_ShouldFindCharacter(int _radioNum)
        {
            var character = mRepository.GetByAssignedRadio(_radioNum);

            Assert.NotNull(character);
            Assert.Equal(_radioNum, character.RadioNum);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(20)]
        public void GetByAssignedRadio_ShouldNotFindCharacter(int _radioNum)
        {
            var character = mRepository.GetByAssignedRadio(_radioNum);

            Assert.Null(character);
        }

        [Theory]
        [InlineData(CharacterState.Available)]
        [InlineData(CharacterState.On)]
        public void GetAllByState_ShouldFindCharacters(CharacterState state)
        {
            var characters = mRepository.GetAllByState(state);

            Assert.NotEmpty(characters);

            foreach(var character in characters)
            {
                Assert.Equal(state, character.State);
            }
        }

        [Fact]
        public void GetAllByState_ShouldReturnEmptyList()
        {
            CharacterState state = CharacterState.Off;

            var characters = mRepository.GetAllByState(state);

            Assert.Empty(characters);
        }
    }
}
