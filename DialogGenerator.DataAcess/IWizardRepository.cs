using DialogGenerator.Model;
using System.Collections.Generic;

namespace DialogGenerator.DataAccess
{
    public interface IWizardRepository
    {
        List<Wizard> GetAll();

        List<Wizard> GetAll(string _fileName);

        Wizard GetByIndex(int index);
    }
}
