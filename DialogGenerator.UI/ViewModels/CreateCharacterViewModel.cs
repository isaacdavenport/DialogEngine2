using DialogGenerator.Core;
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
        private string characterName = String.Empty;
        private string characterInitials = String.Empty;
        private string characterInitialsPrefix = String.Empty;
        private string characterImage = String.Empty;
        private string characterGender = "Female";
        
        private int characterAge = 10;
        private List<int> agesCollection = new List<int>();
        private List<string> steps = new List<string>();
        private int currentStep = 0;

        private ILogger mLogger;

        public CreateCharacterViewModel(ILogger logger)
        {
            mLogger = logger;

            for(int i = 5; i < 100; i++)
            {
                agesCollection.Add(i);
            }

            steps.Add("NameControl");
            steps.Add("InitialsControl");
            steps.Add("AgeControl");
            steps.Add("GenderControl");
            steps.Add("AvatarControl");

            _bindCommands();
        }

        public string CharacterName
        {
            get
            {
                return characterName;
            }

            set
            {
                characterName = value;
                characterInitials = _getCharacterInitials();
                RaisePropertyChanged("CharacterName");
                RaisePropertyChanged("CharacterInitials");
            }
        }

        public string CharacterInitials
        {
            get
            {
                return characterInitials;
            }
        }

        public string CharacterInitialsPrefix
        {
            get
            {
                return characterInitialsPrefix;
            }

            set
            {
                characterInitialsPrefix = value;
                RaisePropertyChanged("CharacterInitialsPrefix");
            }
        }

        public int CharacterAge
        {
            get
            {
                return characterAge;
            }

            set
            {
                characterAge = value;
                RaisePropertyChanged("CharacterAge");
            }
        }

        public String CharacterGender
        {
            get
            {
                return characterGender;
            }

            set
            {
                characterGender = value;
                RaisePropertyChanged("CharacterGender");
            }
        }

        public string CharacterImage
        {
            get
            {
                return characterImage;
            }

            set
            {
                characterImage = value;
                RaisePropertyChanged("CharacterImage");
            }
        }

        public List<int> AgesCollection
        {
            get
            {
                return agesCollection;
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

        public ICommand ChooseImageCommand { get; set; }

        public string nextStep()
        {
           if(currentStep + 1 < steps.Count)
            {
                currentStep++;                
            }

            return steps[currentStep];
        }

        public string previousStep()
        {
            if(currentStep > 0)
            {
                currentStep--;
            }

            return steps[currentStep];
        }
            
        private string _getCharacterInitials()
        {
            if (String.IsNullOrEmpty(characterName))
                return String.Empty;

            String[] tokens = characterName.Split(' ');
            String result = String.Empty;
            for(int i = 0; i < tokens.Length; i++)
            {
                result += tokens[i].First();
            }

            return result.ToUpper();
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
