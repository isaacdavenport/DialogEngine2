using System.Threading.Tasks;

namespace DialogGenerator.CharacterSelection
{
    public interface ICharacterSelection
    {
        Task StartCharacterSelection();
        void StopCharacterSelection();
    }
}
