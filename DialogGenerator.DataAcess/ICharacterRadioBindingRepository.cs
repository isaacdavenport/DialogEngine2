using DialogGenerator.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.DataAccess
{
    public interface ICharacterRadioBindingRepository
    {
        ObservableCollection<CharacterRadioBinding> GetAll();
        CharacterRadioBinding GetBindingByRadioNum(int _RadioNumber);
        CharacterRadioBinding GetBindingByCharacterPrefix(string _CharacterPrefix);
        void AttachRadioToCharacter(int _RadioNumber, string _CharacterPrefix);
        void DetachRadio(int _RadionNumber);        
        Task SaveAsync();
    }
}
