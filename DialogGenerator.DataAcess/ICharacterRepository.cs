using DialogGenerator.Model;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DialogGenerator.DataAccess
{
    public interface ICharacterRepository
    {
        ObservableCollection<Character> GetAll();

        Character GetByInitials(string initials);
        Task AddAsync(Character character);

        Task SaveAsync(Character character);

        Task Remove(Character character,string _imageFileName);
    }
}
