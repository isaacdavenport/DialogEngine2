﻿using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Model;
using DialogGenerator.Utilities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.DataAccess
{
    public class DialogDataRepository : IDialogDataRepository
    {
        private ILogger mLogger;
        private IUserLogger mUserLogger;
        private JSONObjectsTypesList mJSONObjectsTypesList;

        public DialogDataRepository(ILogger logger,IUserLogger _userLogger)
        {
            mLogger = logger;
            mUserLogger = _userLogger;
        }

        public async Task<JSONObjectsTypesList> LoadAsync(string path)
        {
            await Task.Run(() =>
            {
                Thread.CurrentThread.Name = "LoadDialogDataAsync";

                mJSONObjectsTypesList = new JSONObjectsTypesList();

                var _directoryInfo = new DirectoryInfo(path);
                FileInfo[] _filesInfo = _directoryInfo.GetFiles("*.json");

                foreach (var _fileInfo in _filesInfo)
                {
                    try
                    {
                        _processJSONFile(_fileInfo);
                    }
                    catch (Exception ex)
                    {
                        mUserLogger.Error("Error during reading file: " + _fileInfo.Name);
                        mLogger.Error(ex.Message);
                    }
                }
            });

            return mJSONObjectsTypesList;
        }

        public  void _processJSONFile(FileInfo _fileInfo)
        {
            var _fileSteam = _fileInfo.OpenRead(); //open a read-only FileStream
            using (var reader = new StreamReader(_fileSteam)) //creates new streamerader for fs stream. Could also construct with filename...
            {
                string _jsonString = reader.ReadToEnd();
                //json string to Object.
                var _jsonObjectsTypesList = Serializer.Deserialize<JSONObjectsTypesList>(_jsonString);

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
        }

        private void _processJSONData(JSONObjectsTypesList _jsonObjectsTypesList, string _fileName)
        {
            if (_jsonObjectsTypesList.Characters != null)
            {
                foreach (var _character in _jsonObjectsTypesList.Characters)
                {
                    _character.FileName = _fileName;
                    _character.JsonArrayIndex = mJSONObjectsTypesList.Characters.Count;
                    mJSONObjectsTypesList.Characters.Add(_character);
                }
            }

            if (_jsonObjectsTypesList.Wizards != null)
            {
                foreach (var _wizard in _jsonObjectsTypesList.Wizards)
                {

                    _wizard.FileName = _fileName;
                    _wizard.JsonArrayIndex = mJSONObjectsTypesList.Wizards.Count;
                    mJSONObjectsTypesList.Wizards.Add(_wizard);
                }
            }

            if (_jsonObjectsTypesList.DialogModels != null)
            {
                foreach (var _dialogModel in _jsonObjectsTypesList.DialogModels)
                {
                    _dialogModel.FileName = _fileName;
                    _dialogModel.JsonArrayIndex = mJSONObjectsTypesList.DialogModels.Count;
                    mJSONObjectsTypesList.DialogModels.Add(_dialogModel);
                }
            }
        }
    }
}
