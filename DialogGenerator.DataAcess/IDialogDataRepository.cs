using DialogGenerator.Model;
using System.Threading.Tasks;

namespace DialogGenerator.DataAccess
{
    public interface IDialogDataRepository
    {
        Task<JSONObjectsTypesList> LoadAsync(string path); 
    }
}
