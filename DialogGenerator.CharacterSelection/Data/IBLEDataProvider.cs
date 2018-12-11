using System.Threading.Tasks;

namespace DialogGenerator.CharacterSelection.Data
{
    public interface IBLEDataProvider
    {
        string GetMessage();
        Task StartReadingData();
        void StopReadingData();
    }
}
