using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using System.Collections.ObjectModel;

namespace DialogGenerator.UI.Data
{
    public interface IDialogModelDataProvider
    {
        ObservableCollection<ModelDialogInfo> GetAll();
        ObservableCollection<ModelDialogInfo> GetAllByState(ModelDialogState state);
        ModelDialogInfo GetByName(string name);
    }
}
