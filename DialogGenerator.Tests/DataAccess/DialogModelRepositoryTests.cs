using DialogGenerator.DataAccess;
using Xunit;

namespace DialogGenerator.Tests.DataAccess
{
    public class DialogModelRepositoryTests:RepositoryTestBase
    {
        private DialogModelRepository mRepository;

        public DialogModelRepositoryTests()
        {
            mRepository = new DialogModelRepository();

            testSetup();
        }

        [Fact]
        public void GetAll_ShouldReturnData()
        {
            var _dialogModels = mRepository.GetAll();

            Assert.Equal(dialogModels.Count, _dialogModels.Count);
        }

        [Fact]
        public void GetAll_ShouldReturnData_ByFileName()
        {
            string _fileName = "test.json";
            var _dialogModels = mRepository.GetAll(_fileName);

            Assert.NotEmpty(_dialogModels);

            foreach(var _dialogModel in _dialogModels)
            {
                Assert.Equal(_fileName, _dialogModel.FileName);
            }              
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        public void GetAll_ShouldReturnEmptyList_ByFileName(string _fileName)
        {
            var _dialogModels = mRepository.GetAll(_fileName);

            Assert.Empty(_dialogModels);
        }
    }
}
