using DialogGenerator.Core;
using Moq;
using Prism.Events;

namespace DialogGenerator.Tests.CharacterSelection
{
    public class RandomSelectionServiceTests
    {
        protected Mock<ILogger> loggerMock = new Mock<ILogger>();
        protected Mock<IEventAggregator> eventAggregator = new Mock<IEventAggregator>();
    }
}
