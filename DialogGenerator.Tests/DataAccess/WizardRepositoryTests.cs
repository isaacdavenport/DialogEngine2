using DialogGenerator.DataAccess;
using System.Globalization;
using Xunit;

namespace DialogGenerator.Tests.DataAccess
{
    public class WizardRepositoryTests : RepositoryTestBase
    {
        public WizardRepository mRepository;
        
        public WizardRepositoryTests()
        {
            mRepository = new WizardRepository();
            testSetup();
        }

        [Fact] 
        public void GetAll_ShouldReturnData()
        {
            var _wizards = mRepository.GetAll();
            Assert.True(wizards.Count == _wizards.Count);
        }

        [Theory]
        [InlineData("BasicWizard")]
        public void GetAll_FindByName_ShouldReturnData(string wizardName)
        {
            var _wizard = mRepository.GetByName(wizardName);
            Assert.NotNull(_wizard);
        }

        [Theory]
        [InlineData("SomeWizard")]
        [InlineData("")]
        public void GetAll_FindByName_ShouldNotReturnData(string wizardName)
        {
            var _wizard = mRepository.GetByName(wizardName);
            Assert.Null(_wizard);
        }
    }
}
