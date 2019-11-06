using DialogGenerator.Core;
using DialogGenerator.Model;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class CreateCharacterViewModel : BindableBase
    {
        private string mCharacterName = String.Empty;
        private string mCharacterInitials = String.Empty;
        private string mCharacterIdentifier = String.Empty;
        private string mCharacterImage = String.Empty;
        private string mCharacterGender = "Female";        
        private int mCharacterAge = 10;
        private List<int> mAgesCollection = new List<int>();
        private List<string> mSteps = new List<string>();
        private CreateCharacterWizard mWizard;
        private CreateCharacterWizardStep mCurrentStep = null;
        private int mCurrentStepIndex = 0;
        private ILogger mLogger;

        public CreateCharacterViewModel(ILogger logger)
        {
            mLogger = logger;
            mWizard = new CreateCharacterWizard();
            mCurrentStep = mWizard.Steps[mCurrentStepIndex];

            for(int i = 5; i < 100; i++)
            {
                mAgesCollection.Add(i);
            }

            _bindCommands();
        }

        public string CharacterName
        {
            get
            {
                return mCharacterName;
            }

            set
            {
                mCharacterName = value;
                mCharacterInitials = _getCharacterInitials();                
                RaisePropertyChanged("CharacterName");
                RaisePropertyChanged("CharacterInitials");
                CharacterInitials = _getCharacterInitials();
                CharacterIdentifier = _getCharacterIdentifier();
            }
        }        

        public string CharacterInitials
        {
            get
            {
                return mCharacterInitials;
            }

            set
            {
                mCharacterInitials = value;
                RaisePropertyChanged("CharacterInitials");
            }
        }

        public string CharacterIdentifier
        {
            get
            {
                return mCharacterIdentifier;
            }

            set
            {                                
                mCharacterIdentifier = value;
                RaisePropertyChanged("CharacterIdentifier");
            }
        }

        public int CharacterAge
        {
            get
            {
                return mCharacterAge;
            }

            set
            {
                mCharacterAge = value;
                RaisePropertyChanged("CharacterAge");
            }
        }

        public String CharacterGender
        {
            get
            {
                return mCharacterGender;
            }

            set
            {
                mCharacterGender = value;
                RaisePropertyChanged("CharacterGender");
            }
        }

        public string CharacterImage
        {
            get
            {
                return mCharacterImage;
            }

            set
            {
                mCharacterImage = value;
                RaisePropertyChanged("CharacterImage");
            }
        }

        public List<int> AgesCollection
        {
            get
            {
                return mAgesCollection;
            }
        }

        public List<string> Genders
        {
            get
            {
                List<string> genders = new List<string>();
                genders.Add("Female");
                genders.Add("Male");

                return genders;
            }
        }

        public List<string> Steps
        {
            get
            {
                var query = mWizard.Steps.Select((step, index) => "Step " + step.StepIndex + " - " + step.StepName);
                var results = new List<string>();
                results.AddRange(query);
                return results;
            }
        }

        public int CurrentStepIndex
        {
            get
            {
                return mCurrentStepIndex;
            }

            set
            {
                mCurrentStepIndex = value;
                RaisePropertyChanged("CurrentStepIndex");
                CurrentStep = mWizard.Steps[mCurrentStepIndex];
            }
        }

        public CreateCharacterWizardStep CurrentStep
        {
            get
            {
                return mCurrentStep;
            }

            set
            {
                mCurrentStep = value;
                RaisePropertyChanged("CurrentStep");
            }
        }
          

        public ICommand ChooseImageCommand { get; set; }

        public void nextStep()
        {
           if(CurrentStepIndex + 1 < mWizard.Steps.Count)
            {
                CurrentStepIndex++;                
            }

        }

        public void previousStep()
        {
            if(CurrentStepIndex > 0)
            {
                CurrentStepIndex--;
            }
            
        }
            
        private string _getCharacterInitials()
        {
            if (String.IsNullOrEmpty(mCharacterName))
                return String.Empty;

            String[] tokens = mCharacterName.Split(' ');
            String result = String.Empty;
            for(int i = 0; i < tokens.Length; i++)
            {
                result += tokens[i].First();
            }

            return result.ToUpper();
        }

        private string _getCharacterIdentifier()
        {
            if(!String.IsNullOrEmpty(CharacterIdentifier))
            {
                return CharacterIdentifier;
            }

            string _identifier = Guid.NewGuid().ToString();
            _identifier = _identifier.Substring(0, 4);
            return _identifier;
        }

        private void _bindCommands()
        {
            ChooseImageCommand = new DelegateCommand(_onChooseImage_execute);
        }

        private async void _onChooseImage_execute()
        {
            try
            {
                //if (!_chooseImage_CanExecute())
                //    return;

                System.Windows.Forms.OpenFileDialog _openFileDialog = new System.Windows.Forms.OpenFileDialog();
                _openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";

                if (_openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string _chCurrentImageFilePath = CharacterImage;
                string _filePath = _openFileDialog.FileName;
                string _newFileName = $"{CharacterInitials}_{Path.GetFileName(_filePath)}";
                CharacterImage = ApplicationData.Instance.DefaultImage;

                await Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "_chooseImage_Execute";

                    File.Copy(_filePath, Path.Combine(ApplicationData.Instance.ImagesDirectory, _newFileName), true);

                    if ( !String.IsNullOrEmpty(_chCurrentImageFilePath) && !_chCurrentImageFilePath.Equals(ApplicationData.Instance.DefaultImage))
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        File.Delete(Path.Combine(ApplicationData.Instance.ImagesDirectory, _chCurrentImageFilePath));
                    }

                    CharacterImage = _newFileName;
                });
            }
            catch (Exception ex)
            {
                mLogger.Error("_chooseImage_Execute " + ex.Message);
            }
        }
    }
}
