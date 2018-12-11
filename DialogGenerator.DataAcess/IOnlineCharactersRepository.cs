using DialogGenerator.Model;
using System.Collections.Generic;

namespace DialogGenerator.DataAccess
{
    public interface IOnlineCharactersRepository
    {
         IEnumerable<FileItem> GetAll();
    }
}
