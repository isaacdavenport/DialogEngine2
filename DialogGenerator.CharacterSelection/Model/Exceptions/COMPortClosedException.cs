using System;

namespace DialogGenerator.CharacterSelection.Model.Exceptions
{
    public class COMPortClosedException: Exception
    {
        public COMPortClosedException()
        {

        }
        public COMPortClosedException(string message) : base(message)
        {

        }
    }
}
