using DialogGenerator.Core;
using DialogGenerator.DialogEngine.Model;
using Prism.Events;
using System.Collections.Generic;

namespace DialogGenerator.DialogEngine
{
    public class DialogModelsManager
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private List<HistoricalDialog> mHistoricalDialogs = new List<HistoricalDialog>();
        private List<HistoricalPhrase> mHistoricalPhrases = new List<HistoricalPhrase>();

        #endregion

        #region - constructor -

        public DialogModelsManager(ILogger logger,IEventAggregator _eventAggregator)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
        }

        #endregion
    }
}
