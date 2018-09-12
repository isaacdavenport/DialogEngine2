using System.Collections.Generic;
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
    }
}
