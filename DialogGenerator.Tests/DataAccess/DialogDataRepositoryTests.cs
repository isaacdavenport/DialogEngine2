using DialogGenerator.DataAccess;
using DialogGenerator.Tests.TestHelper;
using System.Linq;
using Xunit;

namespace DialogGenerator.Tests.DataAccess
{
    public class DialogDataRepositoryTests : RepositoryTestBase
    {
        private const string mcValidFileName = "test.json";
        private readonly DialogDataRepository mDialogDataRepository;

        public DialogDataRepositoryTests()
        {
            mDialogDataRepository = new DialogDataRepository(loggerMock.Object, userLoggerMock.Object);
        }

        [Fact]
        public async void LoadAsync_ShouldLoadData()
        {
            var _JSONObjectsTypesList = await mDialogDataRepository.LoadAsync(ApplicationDataHelper.DataDirectory);

            // test is data properly loaded
            Assert.Equal(2,_JSONObjectsTypesList.Characters.Count);
            Assert.NotEmpty(_JSONObjectsTypesList.DialogModels);
            Assert.NotEmpty(_JSONObjectsTypesList.Wizards);
            
            var _character1 = _JSONObjectsTypesList.Characters.First();
            var _character2 = _JSONObjectsTypesList.Characters[1];

            // test is properly set property JsonArrayIndex
            Assert.Equal(0, _character1.JsonArrayIndex);
            Assert.Equal(1, _character2.JsonArrayIndex);

            // test is file name properly set
            Assert.Equal(mcValidFileName, _character1.FileName);
            Assert.Equal(mcValidFileName, _character2.FileName);

            // test for phrase correctly loaded
            Assert.NotEmpty(_character1.Phrases);
            Assert.NotEmpty(_character2.Phrases);
        }
    }
}
