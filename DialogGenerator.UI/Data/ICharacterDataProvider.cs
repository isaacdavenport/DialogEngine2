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

        Task AddAsync(Character character);

        Task SaveAsync(Character character);

        Task Remove(Character character,string _imageFileName);
    }
}
