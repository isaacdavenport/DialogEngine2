using System.IO;
using System.Runtime.CompilerServices;

namespace DialogGenerator.Model.Logger
{
    /// <summary>
    /// Base class for debug messages
    /// </summary>
    public abstract class LogMessage
    {
        /// <summary>
        /// Set message
        /// </summary>
        /// <param name="_message">Message text</param>
        protected LogMessage(string _message ,[CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            Message = _message;
            SourceFile = Path.GetFileName(_file);
            Line = _line;
        }

        /// <summary>
        /// Message text
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// File where message generated
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Line in file where message generated
        /// </summary>
        public int Line { get; set; }

    }
}
