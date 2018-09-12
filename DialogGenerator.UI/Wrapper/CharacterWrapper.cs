using DialogGenerator.Model;
using DialogGenerator.Model.Enum;

namespace DialogGenerator.UI.Wrapper
{
    public class CharacterWrapper:ModelWrapper<Character>
    {
        public CharacterWrapper(Character character):base(character)
        {
            character.PropertyChanged += _character_PropertyChanged;
        }

        private void _character_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }

        public string CharacterName
        {
            get { return getValue<string>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(CharacterName));
            }
        }

        public int CharacterAge
        {
            get { return getValue<int>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(CharacterAge));
            }
        }

        public string CharacterPrefix
        {
            get { return getValue<string>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(CharacterPrefix));
            }
        }

        public string CharacterGender
        {
            get { return getValue<string>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(CharacterGender));
            }
        }

        public CharacterState State
        {
            get { return Model.State; }
        }

        public int RadioNum
        {
            get { return Model.RadioNum; }
        }
    }
}
