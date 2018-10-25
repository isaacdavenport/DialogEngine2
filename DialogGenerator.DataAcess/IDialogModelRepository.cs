using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using System.Collections.ObjectModel;

namespace DialogGenerator.DataAccess
{
    public interface IDialogModelRepository
    {
        ObservableCollection<ModelDialogInfo> GetAll();

        ObservableCollection<ModelDialogInfo> GetAll(string _fileName);

        ObservableCollection<ModelDialogInfo> GetAllByState(ModelDialogState state);

        ModelDialogInfo GetByName(string name);
    }
}
