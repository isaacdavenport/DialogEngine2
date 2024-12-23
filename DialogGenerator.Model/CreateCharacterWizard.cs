﻿using DialogGenerator.Core;
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
            int counter = 1;
            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "Name",
                StepName = "Set Character Name",
                StepDescription = "Sets the name the character will have.",
                StepControl = "NameControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "Initials",
                StepName = "Set Character Initials",
                StepDescription = "Sets the character initials.",
                StepControl = "InitialsControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "Age",
                StepName = "Set Character Age",
                StepDescription = "Sets the age of your character.",
                StepControl = "AgeControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "Gender",
                StepName = "Set Character Gender",
                StepDescription = "Set the gender of the character.",
                StepControl = "GenderControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "Avatar",
                StepName = "Set Character Avatar",
                StepDescription = "Choose the avatar image of your character",
                StepControl = "AvatarControl"
            });

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "AssignToy",
                StepName = "Assign Toy To Character",
                StepControl = "AssignToyToCharacterControl"
            });
            
            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "Author",
                StepName = "Set Author Nickname",
                StepControl = "AuthorControl"
            }) ;
            
            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "Description",
                StepName = "Set Character Description",
                StepControl = "DescriptionControl"
            }) ;

            mSteps.Add(new CreateCharacterWizardStep
            {
                StepIndex = counter++,
                Key = "Note",
                StepName = "Set Internal Note",
                StepControl = "NoteControl"
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
