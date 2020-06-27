﻿using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis.TtsEngine;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class CharacterSlotViewModel : BindableBase
    {
        ICharacterDataProvider mCharacterDataProvider;
        IEventAggregator mEventAggregator;
        Character mSelectedCharacter;
        ObservableCollection<PhraseDefinitionModel> mPhrases = new ObservableCollection<PhraseDefinitionModel>();
        CollectionViewSource mPhraseDefinitionModels;
        CollectionViewSource mCharacters;
        string mPhraseDescription;
        PhraseDefinitionModel mSelectedModel;

        public CharacterSlotViewModel(ICharacterDataProvider _CharacterDataProvider, IEventAggregator _EventAggregator)
        {
            mCharacterDataProvider = _CharacterDataProvider;
            mEventAggregator = _EventAggregator;
            mSelectedCharacter = null;
            mPhraseDefinitionModels = new CollectionViewSource();
            //mPhraseDefinitionModels.Filter += MPhraseDefinitionModels_Filter;
            mPhraseDefinitionModels.Source = mPhrases;

            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_charactersLoaded);
            //_initPhraseDefinitionModels();

            _bindCommands();
            
        }

        

        public Character SelectedCharacter
        {
            get
            {
                return mSelectedCharacter;
            }

            set
            {
                mSelectedCharacter = value;
                _initPhraseDefinitionModels();
                RaisePropertyChanged();
            }
        }    
        
        public ICollectionView Characters
        {
            get
            {
                return mCharacters.View;
            }
        }

        public ICollectionView PhraseDefinitionModels
        {
            get
            {
                return mPhraseDefinitionModels.View;
            }
        }

        public PhraseDefinitionModel SelectedPhraseModel
        {
            get
            {
                return mSelectedModel;
            }

            set
            {
                mSelectedModel = value;
                _updateDescription();
                RaisePropertyChanged();
            }
        }        

        public string PhraseDescription
        {
            get
            {
                return mPhraseDescription;
            }

            set
            {
                mPhraseDescription = value;
                RaisePropertyChanged();
            }
        }

        public DelegateCommand ViewLoadedCommand { get; set; }
        public DelegateCommand ViewUnloadedCommand { get; set; }

        private void MPhraseDefinitionModels_Filter(object sender, FilterEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _charactersLoaded()
        {
            _initCharactersCollection();
        }

        private void _updateDescription()
        {
            if (mSelectedModel == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(mSelectedModel.Description))
            {
                PhraseDescription = mSelectedModel.Description;
            }
            else
            {
                PhraseDescription = "No description yet!";
            }
        }

        private void _initPhraseDefinitionModels()
        {
            // Empty old collection if not empty.
            var _collection = mPhraseDefinitionModels.Source as ObservableCollection<PhraseDefinitionModel>;
            if(_collection != null)
            {
                _collection.Clear();
            }            

            if(SelectedCharacter == null || SelectedCharacter.CharacterName.Equals("GENERIC"))
            {
                string _filePath = ApplicationData.Instance.DataDirectory + "\\Phrases.cfg";
                try
                {
                    using (var _reader = new StreamReader(_filePath))
                    {
                        string _jsonString = _reader.ReadToEnd();
                        var _phraseKeysCollection = Serializer.Deserialize<PhraseKeysCollection>(_jsonString);
                        if (_phraseKeysCollection != null && _phraseKeysCollection.Phrases.Count() > 0)
                        {
                            foreach(var _phraseKey in _phraseKeysCollection.Phrases)
                            {
                                var _phraseDefinitionModel = new PhraseDefinitionModel
                                {
                                    Text = _phraseKey.Name,
                                    PhraseEntry = null,
                                    Description = _phraseKey.Description,
                                    Character = null
                                };

                                _collection.Add(_phraseDefinitionModel);
                                
                            }
                                        
                        }
                    }
                }
                catch (IOException)
                {
                }
            } else
            {
                foreach(var _phrase in SelectedCharacter.Phrases)
                {
                    var _phraseDefinitionModel = new PhraseDefinitionModel
                    {
                        Text = _phrase.DialogStr,
                        PhraseEntry = _phrase,     
                        Character = SelectedCharacter,
                    };

                    string _description = string.Empty;
                    foreach(var _phraseWeight in _phrase.PhraseWeights)
                    {
                        if(!string.IsNullOrEmpty(_description))
                        {
                            _description += ", ";                           
                        }

                        _description += _phraseWeight.Key;
                        _description += "[";
                        _description += _phraseWeight.Value;
                        _description += "]";
                    }

                    _phraseDefinitionModel.Description = _description;
                    _collection.Add(_phraseDefinitionModel);
                }
            }

            mPhraseDefinitionModels.View?.Refresh();
        }

        private void _initCharactersCollection()
        {
            mCharacters = new CollectionViewSource();
            var _characters = new ObservableCollection<Character>();
            _characters.Add(new Character
            {
                CharacterName = "GENERIC",
                CharacterPrefix = "GENERIC"
            });

            _characters.AddRange(mCharacterDataProvider.GetAll());
            mCharacters.Source = _characters;
            mCharacters.View?.Refresh();
        }

        private void _dummyInitialization()
        {
            var _models = new ObservableCollection<PhraseDefinitionModel>();
            var _charArray = new string[] { "Sinke", "Slavisa", "Steva", "Buca", "Tesla" };
            foreach(var _name in _charArray)
            {
                var _phraseModel = new PhraseDefinitionModel
                {
                    Text = _name,
                    Description = string.Empty,
                    Character = new Character
                    {
                        CharacterName = _name,
                        CharacterPrefix = _name
                    }
                };

                _phraseModel.PhraseEntry = new PhraseEntry
                {
                    DialogStr = _name,
                    FileName = _name + ".json"
                };

                _models.Add(_phraseModel);


            }

            mPhraseDefinitionModels.Source = _models;

        }

        private void _bindCommands()
        {
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
            ViewUnloadedCommand = new DelegateCommand(_viewUnloaded_Execute);
        }

        private void _viewUnloaded_Execute()
        {

        }

        private void _viewLoaded_Execute()
        {
            ObservableCollection<Character> _characters = mCharacters.Source as ObservableCollection<Character>;
            if(_characters != null)
            {
                SelectedCharacter = _characters[0];
            }
            
        }
    }
}
