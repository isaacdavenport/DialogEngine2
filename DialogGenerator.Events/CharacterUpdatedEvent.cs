using Prism.Events;

namespace DialogGenerator.Events
{
    /// <summary>
    /// S.Ristic - Fix of DLGEN-401 - 10/07/2019.
    /// Event that notifies the auditorium that the content and definition 
    /// of a character has changed.
    /// </summary>
    public class CharacterUpdatedEvent:PubSubEvent
    {
    }
}
