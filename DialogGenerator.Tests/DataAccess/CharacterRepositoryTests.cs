using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.Tests.TestHelper;
using System.IO;
using System.Linq;
using Xunit;

namespace DialogGenerator.Tests.DataAccess
{
    public class CharacterRepositoryTests:RepositoryTestBase
    {
        private CharacterRepository mRepository;

        public CharacterRepositoryTests()
        {
            mRepository = new CharacterRepository(loggerMock.Object, wizardRepositoryMock.Object, dialogModelRepositoryMock.Object, eventAggregatorMock.Object);

            testSetup();
        }

        [Fact]
        public void GetAll_ShouldReturnData()
        {
            var _loadedCharacters = mRepository.GetAll();

            Assert.Equal(characters.Count, _loadedCharacters.Count);
        }

        [Theory]
        [InlineData("CB")]
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
        [InlineData(2)]
        [InlineData(3)]
        public void GetByAssignedRadio_ShouldFindCharacter(int _radioNum)
        {
            Assert.True(characters.Count == 2);

            characters[0].RadioNum = 2;
            characters[1].RadioNum = 3;

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

        [Fact]
        public async void Character_CheckAddExport()
        {
            Assert.Equal(2, characters.Count);

            Character _character = new Character
            {
                CharacterName = "John Smith",
                CharacterPrefix = "JOS",
                CharacterAge = 27,
                CharacterGender = "Male",
                FileName = "JohnSmith.json"
            };

            await mRepository.AddAsync(_character);
            Assert.Equal(_character, mRepository.GetByInitials("JOS"));

            mRepository.Export(_character, ApplicationDataHelper.DataDirectory);
            Assert.True(File.Exists(Path.Combine(ApplicationDataHelper.DataDirectory, _character.FileName)));

            if( File.Exists(Path.Combine(ApplicationDataHelper.DataDirectory, _character.FileName))) {
                File.Delete(Path.Combine(ApplicationDataHelper.DataDirectory, _character.FileName));
            }
        }

    }
}
