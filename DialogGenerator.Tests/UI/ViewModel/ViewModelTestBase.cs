using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.Utilities;
using Moq;
using Prism.Events;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DialogGenerator.Tests.UI.ViewModel
{
    public class ViewModelTestBase
    {
        protected Mock<ILogger> loggerMock = new Mock<ILogger>();
        protected Mock<IEventAggregator> eventAggregatorMock = new Mock<IEventAggregator>();
        protected Mock<IMessageDialogService> messageDialogServiceMock = new Mock<IMessageDialogService>();
        protected Mock<ICharacterDataProvider> characterDataProviderMock = new Mock<ICharacterDataProvider>();
        protected Mock<IWizardDataProvider> wizardDataProviderMock = new Mock<IWizardDataProvider>();
        protected Mock<IDialogModelDataProvider> dialogModelDataProviderMock = new Mock<IDialogModelDataProvider>();
        protected Mock<IDialogDataRepository> dialogDataRepositoryMock = new Mock<IDialogDataRepository>();

        protected Mock<OpenCharacterDetailViewEvent> openCharacterDetailViewEventMock = new Mock<OpenCharacterDetailViewEvent>();

        protected ObservableCollection<Character> characters;

        public ViewModelTestBase()
        {
            characters = new ObservableCollection<Character>();

            _initializeCharacters();
        }

        private void _eventAggregatorSetup()
        {
            eventAggregatorMock.Setup(x => x.GetEvent<OpenCharacterDetailViewEvent>())
                .Returns(openCharacterDetailViewEventMock.Object);
        }

        private void _characterDataProviderSetup()
        {
            characterDataProviderMock.Setup(x => x.GetAll()).Returns(() => characters);
            characterDataProviderMock.Setup(x => x.GetByAssignedRadio(1)).Returns(characters.First());
            characterDataProviderMock.Setup(x => x.GetByAssignedRadio(-1)).Returns((Character)null);
            characterDataProviderMock.Setup(x => x.SaveAsync(It.IsAny<Character>())).Returns(Task.CompletedTask);
            characterDataProviderMock.Setup(x => x.GetByInitials("ch1")).Returns(characters.First());
            characterDataProviderMock.Setup(x => x.GetByInitials(It.Is<string>(p => string.IsNullOrEmpty(p)))).Returns((Character)null);
        }

        private void _initializeCharacters()
        {
            var character1 = new Character
            {
                CharacterPrefix = "ch1",
                RadioNum = 1
            };

            var character2 = new Character
            {
                CharacterPrefix = "ch2",
                RadioNum = 2
            };

            characters.Add(character1);
            characters.Add(character2);
        }

        protected void testSetup()
        {
            _eventAggregatorSetup();
            _characterDataProviderSetup();
        }
    }
}
