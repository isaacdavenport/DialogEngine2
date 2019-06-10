using System.Runtime.CompilerServices;

namespace DialogGenerator.Core
{
    public interface ILogger
    {
        void Error(string message, string _loggerType = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);
        void Info(string message, string _loggerType = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);
        void Warning(string message, string _loggerType = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);
        void Debug(string message, string _loggerType = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);
        void Dialog(string message, string _loggerType = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);

    }
}
