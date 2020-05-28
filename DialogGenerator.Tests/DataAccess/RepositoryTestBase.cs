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

        public RepositoryTestBase()
        {
            characters = new ObservableCollection<Character>();
            dialogModels = new ObservableCollection<ModelDialogInfo>();
        }

        private void _initializeDialogModels()
        {
            var dm1 = new ModelDialogInfo
            {
                FileName = "file1",
                ModelsCollectionName = "dialogModelsCollection1"
            };

            var dm2 = new ModelDialogInfo
            {
                FileName = "file2",
                ModelsCollectionName = "dialogModelsCollection2"
            };

            dialogModels.Add(dm1);
            dialogModels.Add(dm2);

            Session.Set(Constants.DIALOG_MODELS, dialogModels);
        }

        private void _initializeCharacters()
        {
            string _filePath = Path.Combine(ApplicationDataHelper.DataDirectory, "test.json");
            using (var reader = new StreamReader(_filePath))
            {
                var _jsonObjectData = reader.ReadToEnd();
                JSONObjectsTypesList _jsonObjectsTypesList = Serializer.Deserialize<JSONObjectsTypesList>(_jsonObjectData);
                if(_jsonObjectsTypesList != null && _jsonObjectsTypesList.Characters.Count > 0)
                {
                    foreach(var _character in _jsonObjectsTypesList.Characters)
                    {
                        _character.FileName = "test.json";
                    }

                    Session.Set(Constants.CHARACTERS, _jsonObjectsTypesList.Characters);
                    Session.Set(Constants.DIALOG_MODELS, _jsonObjectsTypesList.DialogModels);
                    Session.Set(Constants.WIZARDS, _jsonObjectsTypesList.Wizards);

                    characters.Clear();
                    characters.AddRange(_jsonObjectsTypesList.Characters);
                }
                
            }
                        
        }

        protected void testSetup()
        {
            _initializeCharacters();
            _initializeDialogModels();

            eventAggregatorMock.Setup(x => x.GetEvent<CharacterSavedEvent>()).Returns(new CharacterSavedEvent());
        }
    }
}
