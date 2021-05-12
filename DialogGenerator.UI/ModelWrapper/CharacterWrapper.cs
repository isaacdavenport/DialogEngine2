using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Core;
using DialogGenerator.UI.Data;
using System.Collections.Generic;

namespace DialogGenerator.UI.Wrapper
{
    public class CharacterWrapper:ModelWrapper<Character>
    {
        private ICharacterDataProvider mCharacterDataProvider; 
        public CharacterWrapper(Character character,ICharacterDataProvider _characterDataProvider)
            :base(character)
        {
            mCharacterDataProvider = _characterDataProvider;
            character.PropertyChanged += _character_PropertyChanged;
        }

        private void _character_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }

        protected override IEnumerable<string> validateProperty(string _propertyName)
        {
            List<string> errors = new List<string>();

            //switch (_propertyName)
            //{
                //case nameof(CharacterPrefix):
                //    {
                //        var character = mCharacterDataProvider.GetByInitials(CharacterPrefix);

                //        if (character != null)
                //            errors.Add("Character initials must be unique.");
                //        break;
                //    }                    
            //}

            return errors;
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
        
        public string CharacterImage
        {
            get { return getValue<string>(); }
            set
            {
                setValue(value);
            }
        }

        public Character CharacterModel
        {
            get { return base.Model; }
        }


        public int RadioNum
        {
            get { return Model.RadioNum; }
        }

        public string Author
        {
            get { return getValue<string>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(Author));
            }
        }

        public string Description
        {
            get
            {
                return getValue<string>();
            }

            set
            {
                setValue(value);
                validateProperty(nameof(Description));
            }
        }

        public string InternalRemarks
        {
            get
            {
                return getValue<string>();
            }

            set
            {
                setValue(value);
                validateProperty(nameof(InternalRemarks));
            }
        }

        public bool HasNoVoice
        {
            get { return getValue<bool>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(HasNoVoice));
            }            
        }

        public string Voice
        {
            get { return getValue<string>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(Voice));
            }
        }

        public int SpeechRate
        {
            get { return getValue<int>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(SpeechRate));
            }
        }
        
    }
}
