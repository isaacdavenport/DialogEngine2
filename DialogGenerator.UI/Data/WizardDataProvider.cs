using System.Collections.Generic;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;

namespace DialogGenerator.UI.Data
{
    public class WizardDataProvider:IWizardDataProvider
    {
        private IWizardRepository mWizardRepository;

        public WizardDataProvider(IWizardRepository _wizardRepository)
        {
            mWizardRepository = _wizardRepository;
        }

        public List<Wizard> GetAll()
        {
            return mWizardRepository.GetAll();
        }

        public Wizard GetByIndex(int index)
        {
            return mWizardRepository.GetByIndex(index);
        }

        public Wizard GetByName(string _wizardName)
        {
            return mWizardRepository.GetByName(_wizardName);
        }
    }
}
