using System.Runtime.CompilerServices;

namespace DialogGenerator.Core
{
    public interface ILogger
    {
        void Error(string _loggerType = null, string _message = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);
        void Info(string _loggerType = null, string _message = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);
        void Warning(string _loggerType = null, string _message = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);
        void Debug(string _loggerType = null, string _message = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0);

    }
}
