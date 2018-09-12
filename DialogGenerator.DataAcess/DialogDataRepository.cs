using DialogGenerator.Core;
using DialogGenerator.Model;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.DataAccess
{
    public class DialogDataRepository : IDialogDataRepository
    {
        private ILogger mLogger;
        private JSONObjectsTypesList mJSONObjectsTypesList;

        public DialogDataRepository(ILogger logger)
        {
            mLogger = logger;
        }

        public async Task<JSONObjectsTypesList> LoadAsync(string path)
        {
            await Task.Run(() =>
            {
                Thread.CurrentThread.Name = "LoadDialogDataAsync";

                mJSONObjectsTypesList = new JSONObjectsTypesList();

                try
                {
                    var _directoryInfo = new DirectoryInfo(path);
                    FileInfo[] _fileInfo = _directoryInfo.GetFiles("*.json");


                    for (int i = 0; i < _fileInfo.Length; i++)
                    {
                        _processJSONFile(_fileInfo[i]);
                    }
                    //EventAggregator.GetEvent<DialogDataLoadedEvent>().Publish();
                }
                catch (UnauthorizedAccessException e)
                {
                    mLogger.Error(e.Message);
                }
                catch (DirectoryNotFoundException e)
                {
                    mLogger.Error(e.Message);
                }
                catch (OutOfMemoryException e)
                {
                    mLogger.Error(e.Message);
                }
            });

            return mJSONObjectsTypesList;
        }

        public  void _processJSONFile(FileInfo _fileInfo)
        {
            var _fileSteam = _fileInfo.OpenRead(); //open a read-only FileStream
            using (var reader = new StreamReader(_fileSteam)) //creates new streamerader for fs stream. Could also construct with filename...
            {
                try
                {
                    string _jsonString = reader.ReadToEnd();
                    //json string to Object.
                    JSONObjectsTypesList _jsonObjectsTypesList = JsonConvert.DeserializeObject
                        <JSONObjectsTypesList>(_jsonString);

                    if (_jsonObjectsTypesList != null)
                    {
                        //This assumes all characters/wizards in array read and are formatted correctly or the entire json read fails
                        // otherwise array member numbers will be off by one if we decide not to read one due to a parse error
                        //Application.Current.Dispatcher.Invoke(() =>
                        //{
                        _processJSONData(_jsonObjectsTypesList, _fileInfo.Name);
                        //});
                    }
                }
                catch (JsonReaderException e)
                {
                    //string _errorReadingMessage = "Error reading " + _fileInfo.Name;
                    //DialogDataHelper.AddMessage(new ErrorMessage(_errorReadingMessage));

                    //string _jsonParseErrorMessage = "JSON Parse error at " + e.LineNumber + ", " + e.LinePosition;
                    //DialogDataHelper.AddMessage(new ErrorMessage(_jsonParseErrorMessage));

                    mLogger.Error(e.Message);

                }
                catch (Exception ex)
                {
                    mLogger.Error("Error during parsing json file " + ex.Message);
                }
                finally
                {
                    reader.Close();
                }
            }
        }

        private  void _processJSONData(JSONObjectsTypesList _jsonObjectsTypesList, string _fileName)
        {
            try
            {
                int j = 0;
                if (_jsonObjectsTypesList.Characters != null)
                {
                    foreach (var _character in _jsonObjectsTypesList.Characters)
                    {
                        _character.FileName = _fileName;
                        _character.JsonArrayIndex = j;
                        j++;
                        mJSONObjectsTypesList.Characters.Add(_character);
                    }
                }

                j = 0;
                if (_jsonObjectsTypesList.Wizards != null)
                {
                    foreach (var _wizard in _jsonObjectsTypesList.Wizards)
                    {

                        _wizard.FileName = _fileName;
                        _wizard.JsonArrayIndex = j;
                        j++;
                        mJSONObjectsTypesList.Wizards.Add(_wizard);
                    }
                }

                j = 0;
                if (_jsonObjectsTypesList.DialogModels != null)
                {
                    foreach (var _dialogModel in _jsonObjectsTypesList.DialogModels)
                    {
                        _dialogModel.FileName = _fileName;
                        _dialogModel.JsonArrayIndex = j;
                        j++;
                        mJSONObjectsTypesList.DialogModels.Add(_dialogModel);
                    }
                }
            }
            catch (Exception ex)
            {
                mLogger.Error(ex.Message);
            }
        }

    }
}
