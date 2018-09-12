using DialogGenerator.Model;
using System.Collections.ObjectModel;

namespace DialogGenerator.UI.Data
{
    public interface IDialogModelDataProvider
    {
        ObservableCollection<ModelDialogInfo> GetAll();

        ModelDialogInfo GetByName(string name);
    }
}
