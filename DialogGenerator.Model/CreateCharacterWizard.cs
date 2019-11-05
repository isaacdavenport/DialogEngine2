using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.Model
{
    public class CreateCharacterWizard
    {
        private List<CreateCharacterWizardStep> mSteps = new List<CreateCharacterWizardStep>();

        public CreateCharacterWizard()
        {
            mSteps.Add(new CreateCharacterWizardStep
            {
                StepName = "Set Character Name",
                StepDescription = "Sets the name the character will have.",
                StepControl = "NameControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepName = "Set Character Initials",
                StepDescription = "Sets the character initials.",
                StepControl = "InitialsControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepName = "Set Character Age",
                StepDescription = "Sets the age of your character.",
                StepControl = "AgeControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepName = "Set Character Gender",
                StepDescription = "Set the gender of the character.",
                StepControl = "GenderControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepName = "Set Character Avatar",
                StepDescription = "Choose the avatar image of your character",
                StepControl = "AvatarControl"
            });
            

        }

        public List<CreateCharacterWizardStep> Steps
        {
            get
            {
                return mSteps;
            }
        }

        public string Name { get; set; }
        public string Description { get; set; }
    }
}
