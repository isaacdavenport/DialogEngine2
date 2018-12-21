using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DialogGenerator.DataAccess
{
    public interface ICharacterRepository
    {
        ObservableCollection<Character> GetAll();
        Character GetByInitials(string initials);
        List<Character> GetAllByState(CharacterState state);
        Character GetByAssignedRadio(int _radioNum);
        Task AddAsync(Character character);
        void Export(Character character,string _directoryPath);
        Task SaveAsync(Character character);
        Task Remove(Character character,string _imageFileName);
    }
}
