using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.Utilities;
using Moq;
using System.Collections.ObjectModel;

namespace DialogGenerator.Tests.DataAccess
{
    public class RepositoryTestBase
    {
        protected Mock<ILogger> loggerMock = new Mock<ILogger>();
        protected Mock<IUserLogger> userLoggerMock = new Mock<IUserLogger>();
        protected Mock<IWizardRepository> wizardRepositoryMock = new Mock<IWizardRepository>();
        protected Mock<IDialogModelRepository> dialogModelRepositoryMock = new Mock<IDialogModelRepository>();
        protected readonly ObservableCollection<Character> characters;
        protected readonly ObservableCollection<ModelDialogInfo> dialogModels;

        public RepositoryTestBase()
        {
            characters = new ObservableCollection<Character>();
            dialogModels = new ObservableCollection<ModelDialogInfo>();
        }

        private void _initializeDialogModels()
        {
            var dm1 = new ModelDialogInfo
            {
                FileName = "file1",
                ModelsCollectionName = "dialogModelsCollection1"
            };

            var dm2 = new ModelDialogInfo
            {
                FileName = "file2",
                ModelsCollectionName = "dialogModelsCollection2"
            };

            dialogModels.Add(dm1);
            dialogModels.Add(dm2);

            Session.Set(Constants.DIALOG_MODELS, dialogModels);
        }

        private void _initializeCharacters()
        {
            var character1 = new Character
            {
                CharacterName = "Test character1",
                CharacterPrefix = "tc1",
                RadioNum = 0,
            };

            var character2 = new Character
            {
                CharacterName = "Test character2",
                CharacterPrefix = "tc2",
                RadioNum = 1,
            };

            characters.Add(character1);
            characters.Add(character2);

            Session.Set(Constants.CHARACTERS, characters);
        }

        protected void testSetup()
        {
            _initializeCharacters();
            _initializeDialogModels();
        }
    }
}
