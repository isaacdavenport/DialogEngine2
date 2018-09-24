using System.Collections.Generic;
using System.Linq;
using DialogGenerator.Core;
using DialogGenerator.Model;

namespace DialogGenerator.DataAccess
{
    public class WizardRepository : IWizardRepository
    {
        public List<Wizard> GetAll()
        {
            return Session.Get<List<Wizard>>(Constants.WIZARDS);
        }

        public List<Wizard> GetAll(string _fileName)
        {
            var result = GetAll().Where(w => w.FileName.Equals(_fileName))
                .Select(w => w)
                .OrderBy(w => w.JsonArrayIndex)
                .ToList();

            return result;
        }

        public Wizard GetByIndex(int index)
        {
            return Session.Get<List<Wizard>>(Constants.WIZARDS)[index];
        }
    }
}
