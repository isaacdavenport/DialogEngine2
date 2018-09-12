using DialogGenerator.Model;
using System.Collections.ObjectModel;

namespace DialogGenerator.DataAccess
{
    public interface IDialogModelRepository
    {
        ObservableCollection<ModelDialogInfo> GetAll();

        ModelDialogInfo GetByName(string name);
    }
}
