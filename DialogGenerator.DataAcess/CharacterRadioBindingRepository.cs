using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Events;
using DialogGenerator.Model;
using Newtonsoft.Json;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.DataAccess
{
    public class CharacterRadioBindingRepository : ICharacterRadioBindingRepository
    {
        ICharacterRepository mCharacterRepository;
        IEventAggregator mEventAggregator;

        public CharacterRadioBindingRepository(ICharacterRepository _CharacterRepository, IEventAggregator _EventAggregator)
        {
            mCharacterRepository = _CharacterRepository;
            mEventAggregator = _EventAggregator;

            if(mCharacterRepository != null && mEventAggregator != null)
            {
                _bindEvents();
            }
                        
        }

        

        [JsonProperty("Version")]
        public string Version { get; set; } = "1.0";

        [JsonProperty("CharacterRadioBindings")]
        public ObservableCollection<CharacterRadioBinding> CharacterRadioBindings { get; set; } = new ObservableCollection<CharacterRadioBinding>();
        

        public void AttachRadioToCharacter(int _RadioNumber, string _CharacterPrefix)
        {
            var _binding = GetBindingByRadioNum(_RadioNumber);
            if(_binding == null)
            {
                _binding = new CharacterRadioBinding
                {
                    RadioNumber = _RadioNumber,
                    CharacterPrefix = string.Empty
                };

                GetAll().Add(_binding);
            }

            if(!string.IsNullOrEmpty(_binding.CharacterPrefix) && !_binding.CharacterPrefix.Equals(_CharacterPrefix))
            {
                DetachRadio(_RadioNumber);
            }

            var _character = mCharacterRepository.GetByInitials(_CharacterPrefix);
            if(_character != null)
            {
                _character.RadioNum = _RadioNumber;
                _binding.CharacterPrefix = _CharacterPrefix;
            }
        }
        
        public void DetachRadio(int _RadioNumber)
        {
            if(CharacterRadioBindings.Where(b => b.RadioNumber == _RadioNumber).Count() != 0)
            {
                var _binding = GetBindingByRadioNum(_RadioNumber);
                if(!string.IsNullOrEmpty(_binding.CharacterPrefix))
                {
                    var _character = mCharacterRepository.GetAll().Where(c => c.CharacterPrefix.Equals(_binding.CharacterPrefix)).FirstOrDefault();
                    if(_character != null)
                    {
                        _character.RadioNum = -1;
                    }

                    _binding.CharacterPrefix = string.Empty;
                }
            }
        }

        public ObservableCollection<CharacterRadioBinding> GetAll()
        {
            return CharacterRadioBindings;
        }

        public CharacterRadioBinding GetBindingByCharacterPrefix(string _CharacterPrefix)
        {
            return GetAll().Where(c => c.CharacterPrefix.Equals(_CharacterPrefix)).FirstOrDefault();
        }

        public CharacterRadioBinding GetBindingByRadioNum(int _RadioNumber)
        {
            return GetAll().Where(c => c.RadioNumber == _RadioNumber).FirstOrDefault();
        }

        public async Task SaveAsync()
        {
            await Task.Run(() =>
            {
                Serializer.Serialize(this, ApplicationData.Instance.DataDirectory + "\\RadioBindings.cfg");
            });
                        
        }

        private void _bindEvents()
        {
            mEventAggregator.GetEvent<CharacterSavedEvent>().Subscribe(_onCharacterSaved);
            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_onCharacterCollectionLoaded);
        }

        private async void _onCharacterCollectionLoaded()
        {
            await _initRepository();
        }

        private async Task _initRepository()
        {

            if(File.Exists(ApplicationData.Instance.DataDirectory + "\\RadioBindings.cfg"))
            {
                using(var reader = new StreamReader(ApplicationData.Instance.DataDirectory + "\\RadioBindings.cfg"))
                {
                    string _jsonString = reader.ReadToEnd();
                    var _repo = Serializer.Deserialize<CharacterRadioBindingRepository>(_jsonString);
                    Version = _repo.Version;
                    CharacterRadioBindings = _repo.CharacterRadioBindings;

                    foreach(var _binding in CharacterRadioBindings)
                    {
                        if(!string.IsNullOrEmpty(_binding.CharacterPrefix))
                        {
                            var _character = mCharacterRepository.GetByInitials(_binding.CharacterPrefix);
                            if(_character != null)
                            {
                                _character.RadioNum = _binding.RadioNumber;
                            }
                        }
                    }
                }
            } else
            {
                var _charsWithRadios = mCharacterRepository.GetAll().Where(c => c.RadioNum != -1);
                if(_charsWithRadios.Count() > 0)
                {
                    foreach(var _character in _charsWithRadios)
                    {
                        CharacterRadioBindings.Add(new CharacterRadioBinding
                        {
                            RadioNumber = _character.RadioNum,
                            CharacterPrefix = _character.CharacterPrefix
                        });
                    }

                    await SaveAsync();
                }

                if(CharacterRadioBindings.Count == 0)
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        this.CharacterRadioBindings.Add(new CharacterRadioBinding
                        {
                            RadioNumber = i,
                            CharacterPrefix = string.Empty
                        });
                    }
                }
                
            }
        }

        private async void _onCharacterSaved(string _CharacterPrefix)
        {
            var _character = mCharacterRepository.GetByInitials(_CharacterPrefix);
            if(_character != null && _character.RadioNum != -1)
            {
                var _binding = CharacterRadioBindings.Where(b => b.RadioNumber == _character.RadioNum).FirstOrDefault();
                if(_binding != null)
                {
                    _binding.CharacterPrefix = _CharacterPrefix;
                    await SaveAsync();
                }
                
            }
        }
    }
}
