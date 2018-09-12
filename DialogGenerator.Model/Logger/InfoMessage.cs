
namespace DialogGenerator.Model.Logger
{
    /// <summary>
    /// Model for info message
    /// Extends <see cref="LogMessage"/>
    /// </summary>
    public class InfoMessage : LogMessage
    {
        /// <summary>
        /// Creates instance of InfoMessage
        /// </summary>
        /// <param name="_message">Message text</param>
        public InfoMessage(string _message) : base(_message)
        {
        }
    }
}
