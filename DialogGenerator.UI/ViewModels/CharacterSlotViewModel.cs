using DialogGenerator.Core;
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
        int mModelsInDialogCount = 0;
        bool mCharacterSelectionEnabled = true;

        public CharacterSlotViewModel(ICharacterDataProvider _CharacterDataProvider
            , IEventAggregator _EventAggregator
            , int _SlotNumber)
        {
            mCharacterDataProvider = _CharacterDataProvider;
            mEventAggregator = _EventAggregator;
            mSelectedCharacter = null;
            mPhraseDefinitionModels = new CollectionViewSource();
            mPhraseDefinitionModels.Source = mPhrases;
            SlotNumber = _SlotNumber;

            _bindCommands();
            _subscribeForEvents();

        }

        public int SlotNumber { get; set; } = 0;

        public bool CharacterSelectionEnabled
        {
            get
            {
                return mCharacterSelectionEnabled;
            }

            set
            {
                mCharacterSelectionEnabled = value;
                RaisePropertyChanged();
            }
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
                AddPhraseToDialogCommand.RaiseCanExecuteChanged();
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
        public DelegateCommand AddPhraseToDialogCommand { get; set; }

        #region Private Methods

        private void _subscribeForEvents()
        {
            mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Subscribe(_charactersLoaded);
            mEventAggregator.GetEvent<AddedPhraseModelToDialogEvent>().Subscribe(_modelAdded);
        }

        private void _modelAdded(int _count)
        {
            if(_count > 0)
            {
                CharacterSelectionEnabled = false;
            } else
            {
                CharacterSelectionEnabled = true;
            }
        }

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
                                if (_phraseKey == null)
                                    continue;
                                var _phraseDefinitionModel = new PhraseDefinitionModel
                                {
                                    Text = _phraseKey.Name,
                                    PhraseEntry = null,
                                    Description = _phraseKey.Description,
                                    Character = null, 
                                    SlotNumber = SlotNumber
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
                    string _excerpt = string.Empty;
                    if(_phrase.DialogStr.Length > 50)
                    {
                        _excerpt = _phrase.DialogStr.Substring(0, 50) + "...";
                    } else
                    {
                        _excerpt = _phrase.DialogStr;
                    }

                    var _phraseDefinitionModel = new PhraseDefinitionModel
                    {
                        Text = _excerpt,
                        PhraseEntry = _phrase,     
                        Character = SelectedCharacter,
                        SlotNumber = SlotNumber
                    };

                    _phraseDefinitionModel.Description = _phrase.DialogStr;
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

        private void _bindCommands()
        {
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
            ViewUnloadedCommand = new DelegateCommand(_viewUnloaded_Execute);
            AddPhraseToDialogCommand = new DelegateCommand(_addPhraseToDialog_Execute, _addPhraseToDialog_CanExecute);
        }

        private void _addPhraseToDialog_Execute()
        {
            mEventAggregator.GetEvent<AddingPhraseModelToDialogEvent>().Publish(SelectedPhraseModel);
        }

        private bool _addPhraseToDialog_CanExecute()
        {
            return SelectedPhraseModel != null;
        }

        private void _viewUnloaded_Execute()
        {
            // Do some cleaning
            SelectedCharacter = null;
            SelectedPhraseModel = null;
            PhraseDescription = string.Empty;
            CharacterSelectionEnabled = true;
        }

        private void _viewLoaded_Execute()
        {
            ObservableCollection<Character> _characters = mCharacters.Source as ObservableCollection<Character>;
            if(_characters != null)
            {
                SelectedCharacter = _characters[0];
            }
            
        }

        #endregion
    }
}
