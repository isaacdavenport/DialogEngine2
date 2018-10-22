using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Model;
using Microsoft.VisualBasic.FileIO;

namespace DialogGenerator.DataAccess
{
    public class CharacterRepository : ICharacterRepository
    {
        private ILogger mLogger;
        private IWizardRepository mWizardRepository;
        private IDialogModelRepository mDialogModelRepository;

        public CharacterRepository(ILogger logger,IWizardRepository _wizardRepository,IDialogModelRepository _dialogModelRepository)
        {
            mLogger = logger;
            mWizardRepository = _wizardRepository;
            mDialogModelRepository = _dialogModelRepository;
        }

        private ObservableCollection<Character> _getAll(string _fileName)
        {
            var result = GetAll().Where(ch => ch.FileName.Equals(_fileName))
                .Select(ch => ch)
                .OrderBy(ch => ch.JsonArrayIndex);

            return new ObservableCollection<Character>(result);
        }

        private void _serializeCharacter(Character character)
        {
            string _fileName = character.CharacterName.Replace(" ", string.Empty) + ".json";

            character.FileName = _fileName;

            var _jsonObjectsTypesList = _findDataForFile(_fileName);

            Serializer.Serialize(_jsonObjectsTypesList, Path.Combine(ApplicationData.Instance.DataDirectory, _fileName));
        }

        private JSONObjectsTypesList _findDataForFile(string _fileName)
        {
            var _jsonObjectsTypesList = new JSONObjectsTypesList
            {
                Wizards = mWizardRepository.GetAll(_fileName),
                DialogModels = mDialogModelRepository.GetAll(_fileName),
                Characters = _getAll(_fileName)
            };

            return _jsonObjectsTypesList;
        }

        public async Task SaveAsync(Character character)
        {
            await Task.Run(() =>
            {
                _serializeCharacter(character);
            });
        }

        public async Task AddAsync(Character character)
        {
            GetAll().Add(character);

            await Task.Run(() =>
            {
                _serializeCharacter(character);
            });
        }

        public ObservableCollection<Character> GetAll()
        {
            return Session.Get<ObservableCollection<Character>>(Core.Constants.CHARACTERS);
        }

        public Character GetByInitials(string initials)
        {
            Character character = Session.Get<ObservableCollection<Character>>(Core.Constants.CHARACTERS)
                .Where(c => c.CharacterPrefix.Equals(initials))
                .FirstOrDefault();

            return character;
        }

        public  Task Remove(Character character,string _imageFileName)
        {
            //TODO change array index for rest of characters after deleting 
            return Task.Run(() =>
            {
                string _fileName = character.FileName;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    //remove character from list of characters
                    GetAll().Remove(character);
                    // clear radio bindings for this character 

                    var bindings = Session.Get <Dictionary<int, Character>>(Core.Constants.CH_RADIO_RELATIONSHIP);

                    if (bindings.ContainsKey(character.RadioNum))
                    {
                        bindings[character.RadioNum] = null;
                    }
                });

                var _jsonObjectsTypesList = _findDataForFile(_fileName);

                //serialize data without character or delete json file if character is in own file
                if((_jsonObjectsTypesList.Characters != null && _jsonObjectsTypesList.Characters.Any()) 
                   || ( _jsonObjectsTypesList.Wizards !=null && _jsonObjectsTypesList.Wizards.Any())
                   || (_jsonObjectsTypesList.DialogModels != null && _jsonObjectsTypesList.DialogModels.Any()))
                {
                    Serializer.Serialize(_jsonObjectsTypesList,Path.Combine(ApplicationData.Instance.DataDirectory,_fileName));
                }
                else
                {
                    FileSystem.DeleteFile(Path.Combine(ApplicationData.Instance.DataDirectory, _fileName),
                        UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin);
                    //File.Delete(Path.Combine(ApplicationData.Instance.DataDirectory, _fileName));
                }

                //remove character's image if image is not default

                if(!string.IsNullOrEmpty(_imageFileName) 
                   && !_imageFileName.Equals(ApplicationData.Instance.DefaultImage) 
                   && File.Exists(Path.Combine(ApplicationData.Instance.ImagesDirectory, _imageFileName)))
                {
                    // used to be sure that image is not used by application anymore so we can delete it
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    bool _isDeleted = false;
                    int counter = 0;

                    do
                    {
                        try
                        {
                            FileSystem.DeleteFile(Path.Combine(ApplicationData.Instance.ImagesDirectory, _imageFileName)
                                , UIOption.OnlyErrorDialogs
                                , RecycleOption.SendToRecycleBin);
                            //File.Delete(Path.Combine(ApplicationData.Instance.ImagesDirectory, _imageFileName));

                            _isDeleted = true;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(500);
                            counter++;
                        }
                    }
                    while (!_isDeleted && counter <= 16); // wait 8 seconds to delete file 

                    if (!_isDeleted)
                    {
                        throw new Exception("Error during deleting image file.");
                    }
                }

                // remove all character related .mp3 files
                var _audioDir = new DirectoryInfo(ApplicationData.Instance.AudioDirectory);
                FileInfo[] _mp3Files = _audioDir.GetFiles($"{character.CharacterPrefix}*.mp3");

                foreach(var _fileInfo in _mp3Files)
                {
                    FileSystem.DeleteFile(_fileInfo.FullName
                        , UIOption.OnlyErrorDialogs
                        , RecycleOption.SendToRecycleBin);

                    //_fileInfo.Delete();
                }
            });
        }
    }
}
