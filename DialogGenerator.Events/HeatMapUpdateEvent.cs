using Prism.Events;

namespace DialogGenerator.Events
{
    public class HeatMapUpdateEvent:PubSubEvent<int[,]>
    {
    }
}
