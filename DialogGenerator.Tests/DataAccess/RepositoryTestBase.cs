using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.Tests.TestHelper;
using DialogGenerator.Utilities;
using Moq;
using Prism.Events;
using System.Collections.ObjectModel;
using System.IO;

namespace DialogGenerator.Tests.DataAccess
{
    public class RepositoryTestBase
    {
        protected Mock<ILogger> loggerMock = new Mock<ILogger>();
        protected Mock<IUserLogger> userLoggerMock = new Mock<IUserLogger>();
        protected Mock<IWizardRepository> wizardRepositoryMock = new Mock<IWizardRepository>();
        protected Mock<IDialogModelRepository> dialogModelRepositoryMock = new Mock<IDialogModelRepository>();
        protected Mock<IEventAggregator> eventAggregatorMock = new Mock<IEventAggregator>();
        protected readonly ObservableCollection<Character> characters;
        protected readonly ObservableCollection<ModelDialogInfo> dialogModels;
        protected readonly ObservableCollection<Wizard> wizards;

        public RepositoryTestBase()
        {
            characters = new ObservableCollection<Character>();
            dialogModels = new ObservableCollection<ModelDialogInfo>();
            wizards = new ObservableCollection<Wizard>();
        }

        private void _initializeCharactersDialogModelsAndWizards()
        {
            string _filePath = Path.Combine(ApplicationDataHelper.DataDirectory, "test.json");
            using (var reader = new StreamReader(_filePath))
            {
                var _jsonObjectData = reader.ReadToEnd();
                JSONObjectsTypesList _jsonObjectsTypesList = Serializer.Deserialize<JSONObjectsTypesList>(_jsonObjectData);
                if(_jsonObjectsTypesList != null) {
                    if(_jsonObjectsTypesList.Characters.Count > 0)
                    {
                        foreach (var _character in _jsonObjectsTypesList.Characters)
                        {
                            _character.FileName = "test.json";
                        }

                        Session.Set(Constants.CHARACTERS, _jsonObjectsTypesList.Characters);                                               
                        characters.Clear();
                        characters.AddRange(_jsonObjectsTypesList.Characters);                         
                    } 
                    
                    if (_jsonObjectsTypesList.DialogModels.Count > 0)
                    {
                        foreach(var _dialog in _jsonObjectsTypesList.DialogModels)
                        {
                            _dialog.FileName = "test.json";
                        }

                        Session.Set(Constants.DIALOG_MODELS, _jsonObjectsTypesList.DialogModels);
                        dialogModels.Clear();
                        dialogModels.AddRange(_jsonObjectsTypesList.DialogModels);
                    }
                
                    if (_jsonObjectsTypesList.Wizards.Count > 0)
                    {
                        foreach (var _wizard in _jsonObjectsTypesList.Wizards)
                        {
                            _wizard.FileName = "test.json";
                        }

                        Session.Set(Constants.WIZARDS, _jsonObjectsTypesList.Wizards);
                        wizards.Clear();
                        wizards.AddRange(_jsonObjectsTypesList.Wizards);
                    }
                }
                
            }
                        
        }

        protected void testSetup()
        {
            _initializeCharactersDialogModelsAndWizards();

            eventAggregatorMock.Setup(x => x.GetEvent<CharacterSavedEvent>()).Returns(new CharacterSavedEvent());
        }
    }
}
