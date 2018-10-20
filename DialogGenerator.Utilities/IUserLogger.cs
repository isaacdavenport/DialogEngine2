using System.Runtime.CompilerServices;

namespace DialogGenerator.Utilities
{
    public interface IUserLogger
    {
        void Error(string message = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Info(string message = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Warning(string message = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    }
}
