using DialogGenerator.Core;
using DialogGenerator.Utilities;
using Moq;
using Prism.Events;
using Xunit;

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

        //[Theory]
        //[InlineData("")]
        //[InlineData("invalid")]
        //public void Play_ShouldReturn1_WhenInvalidPathProvided(string path)
        //{
        //    int result = mPlayer.Play(path);

        //    Assert.Equal(1, result);
        //}
    }
}
