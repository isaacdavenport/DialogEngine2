using DialogGenerator.Model;
using System.Collections.ObjectModel;

namespace DialogGenerator.DataAccess
{
    public interface IDialogModelRepository
    {
        ObservableCollection<ModelDialogInfo> GetAll();

        ObservableCollection<ModelDialogInfo> GetAll(string _fileName);
        
        ModelDialogInfo GetByName(string name);
    }
}
