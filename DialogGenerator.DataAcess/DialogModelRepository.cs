using System.Collections.ObjectModel;
using System.Linq;
using DialogGenerator.Core;
using DialogGenerator.Model;

namespace DialogGenerator.DataAccess
{
    public class DialogModelRepository : IDialogModelRepository
    {
        public ObservableCollection<ModelDialogInfo> GetAll()
        {
            return Session.Get<ObservableCollection<ModelDialogInfo>>(Constants.DIALOG_MODELS);
        }

        public ModelDialogInfo GetByName(string name)
        {
            var _dialogModelInfo = Session.Get<ObservableCollection<ModelDialogInfo>>(Constants.DIALOG_MODELS)
                .Where(dm => dm.ModelsCollectionName.Equals(name))
                .FirstOrDefault();

            return _dialogModelInfo;
        }
    }
}
