using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.Utilities;
using Moq;
using Prism.Events;
using System.Collections.ObjectModel;

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
            characters = new ObservableCollection<Character>
            {
                new Character(),
                new Character(),
                new Character()
            };
        }

        private void _eventAggregatorSetup()
        {
            eventAggregatorMock.Setup(x => x.GetEvent<OpenCharacterDetailViewEvent>())
                .Returns(openCharacterDetailViewEventMock.Object);
        }

        private void _characterDataProviderSetup()
        {
            characterDataProviderMock.Setup(x => x.GetAll()).Returns(() => characters);
        }

        protected void testSetup()
        {
            _eventAggregatorSetup();
            _characterDataProviderSetup();
        }
    }
}
