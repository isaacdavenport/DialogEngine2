using System.IO;
using System.Runtime.CompilerServices;

namespace DialogGenerator.Model.Logger
{
    /// <summary>
    /// Model for error message
    /// Extends <see cref="LogMessage"/>
    /// </summary>
    public class ErrorMessage : LogMessage
    {
        /// <summary>
        /// Creates instance of ErrorMessage
        /// </summary>
        /// <param name="_message">Message text</param>
        public ErrorMessage(string _message) : base(_message)
        {
        }
    }
}
