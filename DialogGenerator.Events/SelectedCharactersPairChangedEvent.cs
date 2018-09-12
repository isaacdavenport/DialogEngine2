using DialogGenerator.Events.EventArgs;
using Prism.Events;

namespace DialogGenerator.Events
{
    public class SelectedCharactersPairChangedEvent:PubSubEvent<SelectedCharactersPairEventArgs>
    {
    }
}
