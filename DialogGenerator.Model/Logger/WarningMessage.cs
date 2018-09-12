namespace DialogGenerator.Model.Logger
{
    /// <summary>
    /// Model for warning message
    /// Extends <see cref="LogMessage"/>
    /// </summary>
    public class WarningMessage : LogMessage
    {
        /// <summary>
        /// Create instance of WarningMessage
        /// </summary>
        /// <param name="_message">Message text</param>
        public WarningMessage(string _message) : base(_message)
        {
        }
    }
}
