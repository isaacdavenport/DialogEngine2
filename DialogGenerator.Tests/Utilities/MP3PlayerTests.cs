using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Utilities;
using Moq;
using Prism.Events;
using System;
using Xunit;

namespace DialogGenerator.Tests.Utilities
{
    public class MP3PlayerTests
    {
        private Mock<ILogger> mLoggerMock = new Mock<ILogger>();
        private Mock<IEventAggregator> mEventAggregatorMock = new Mock<IEventAggregator>();
        private MP3Player mPlayer;
        private Mock<StopPlayingCurrentDialogLineEvent> stopPlayingCurrentDialogLineEvent = new Mock<StopPlayingCurrentDialogLineEvent>();
        private Mock<StopImmediatelyPlayingCurrentDialogLIne> stopPlayingImmediatellyCurrentDialogLine = new Mock<StopImmediatelyPlayingCurrentDialogLIne>();

        public MP3PlayerTests()
        {
            _initEvents();
            mPlayer = new MP3Player(mEventAggregatorMock.Object, mLoggerMock.Object);
        }

                [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        public void Play_ShouldReturn1_WhenInvalidPathProvided(string path)
        {
            int result = mPlayer.Play(path);

            Assert.Equal(1, result);
        }

        private void _initEvents()
        {
            mEventAggregatorMock.Setup(x => x.GetEvent<StopPlayingCurrentDialogLineEvent>())
                .Returns(stopPlayingCurrentDialogLineEvent.Object);
            mEventAggregatorMock.Setup(x => x.GetEvent<StopImmediatelyPlayingCurrentDialogLIne>())
                .Returns(stopPlayingImmediatellyCurrentDialogLine.Object);
        }


    }
}
