using DialogGenerator.Model;
using System.Collections.Generic;

namespace DialogGenerator.DataAccess
{
    public interface IWizardRepository
    {
        List<Wizard> GetAll();
    }
}
