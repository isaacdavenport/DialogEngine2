using DialogGenerator.Core;
using DialogGenerator.Utilities;
using Moq;
using Prism.Events;

namespace DialogGenerator.Tests.Utilities
{
    public class MP3PlayerTests
    {
        private Mock<ILogger> mLoggerMock = new Mock<ILogger>();
        private Mock<IEventAggregator> mEventAggregatorMock = new Mock<IEventAggregator>();
        private MP3Player mPlayer;

        public MP3PlayerTests()
        {
            mPlayer = new MP3Player(mEventAggregatorMock.Object, mLoggerMock.Object);
        }
    }
}
