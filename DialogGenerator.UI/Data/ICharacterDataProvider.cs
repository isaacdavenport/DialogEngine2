using DialogGenerator.Model;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DialogGenerator.UI.Data
{
    public interface ICharacterDataProvider
    {
        ObservableCollection<Character> GetAll();
        Character GetByInitials(string initials);
        Character GetByAssignedRadio(int _radionNum);
        int IndexOf(Character character);
        Task AddAsync(Character character);
        Task SaveAsync(Character character);
        void Export(Character character,string _directoryPath);
        Task Remove(Character character,string _imageFileName);
        void RemovePhrase(Character character, PhraseEntry phrase);
    }
}
