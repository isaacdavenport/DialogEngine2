using DialogGenerator.Model;
using System.Collections.Generic;

namespace DialogGenerator.Web
{
    public interface IContentProvider
    {
        IEnumerable<FileItem> GetCharacters();
    }
}
