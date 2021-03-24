using DialogGenerator.Model;
using System.Collections.Generic;

namespace DialogGenerator.DataAccess
{
    public interface IDialogDataRepository
    {
        JSONObjectsTypesList LoadFromFile(string _filePath,out IList<string> errors);
        JSONObjectsTypesList LoadFromDirectory(string _directoryPath, out IList<string> _errorsList);
        void LogSessionJsonStatsAndErrors(string _directoryPath, JSONObjectsTypesList _JSONObjectTypesList, List<List<string>> _dialogModelListPreFilter);

        void LogRedundantDialogModelsInDataFolder(string _directoryPath, JSONObjectsTypesList _JSONObjectTypesList);

        void Save(JSONObjectsTypesList _JSONObjectsTypesList, string path);
    }
}
