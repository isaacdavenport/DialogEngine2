using System.Collections.Generic;
using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.Web;

namespace DialogGenerator.DataAccess
{
    public class OnlineCharactersRepository : IOnlineCharactersRepository
    {
        private ILogger mLogger;
        private IContentProvider mContentProvider;

        public OnlineCharactersRepository(ILogger logger,IContentProvider _contentProvider)
        {
            mLogger = logger;
            mContentProvider = _contentProvider;
        }

        public IEnumerable<FileItem> GetAll()
        {
            return mContentProvider.GetCharacters();
        }
    }
}
