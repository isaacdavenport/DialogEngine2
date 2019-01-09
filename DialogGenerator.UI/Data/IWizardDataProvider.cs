using DialogGenerator.Model;
using System.Collections.Generic;

namespace DialogGenerator.UI.Data
{
    public interface IWizardDataProvider
    {
        List<Wizard> GetAll();

        Wizard GetByIndex(int index);

        Wizard GetByName(string _wizardName);
    }
}
