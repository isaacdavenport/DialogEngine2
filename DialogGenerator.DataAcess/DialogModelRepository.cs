using System.Collections.ObjectModel;
using System.Linq;
using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;

namespace DialogGenerator.DataAccess
{
    public class DialogModelRepository : IDialogModelRepository
    {
        public ObservableCollection<ModelDialogInfo> GetAll()
        {
            return Session.Get<ObservableCollection<ModelDialogInfo>>(Constants.DIALOG_MODELS);
        }

        public ObservableCollection<ModelDialogInfo> GetAll(string _fileName)
        {
            var result = GetAll().Where(dm => dm.FileName.Equals(_fileName))
                .Select(dm => dm)
                .OrderBy(dm => dm.JsonArrayIndex);

            return  new ObservableCollection<ModelDialogInfo>(result);
        }

        public ObservableCollection<ModelDialogInfo> GetAllByState(ModelDialogState state)
        {
            var result = Session.Get<ObservableCollection<ModelDialogInfo>>(Constants.DIALOG_MODELS)
                .Where(dm => dm.State == state)
                .OrderBy(dm => dm.JsonArrayIndex);

            return new ObservableCollection<ModelDialogInfo>(result);
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
