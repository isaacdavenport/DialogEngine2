using System.Threading.Tasks;

namespace DialogGenerator.CharacterSelection.Data
{
    public interface IBLEDataProvider
    {
        string GetMessage();
        object StartReadingData();
        void StopReadingData();
    }
}
