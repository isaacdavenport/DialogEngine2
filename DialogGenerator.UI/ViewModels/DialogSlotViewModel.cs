using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class DialogSlotViewModel : BindableBase
    {
        string mDialogName = string.Empty;
        double mPopularity = 1.0;
        string mPhraseFullText = string.Empty;
        CollectionViewSource mPhraseDefinitionModels;
        CollectionViewSource mPopularityValues;
        IDialogModelRepository mDialogModelRepository;
        ICharacterDataProvider mCharacterDataProvider;
        IEventAggregator mEventAggregator;
        PhraseDefinitionModel mSelectedPhrase;
        IMessageDialogService mMessageDialogService;

        public DialogSlotViewModel(IDialogModelRepository _DialogModelRepository
            , ICharacterDataProvider _CharacterDataProvider
            , IEventAggregator _EventAggregator
            , IMessageDialogService _MessageDialogService)
        {
            mDialogModelRepository = _DialogModelRepository;
            mCharacterDataProvider = _CharacterDataProvider;
            mEventAggregator = _EventAggregator;
            mMessageDialogService = _MessageDialogService;
            mPhraseDefinitionModels = new CollectionViewSource();
            mPopularityValues = new CollectionViewSource();
            mPhraseDefinitionModels.Source = new ObservableCollection<PhraseDefinitionModel>();
            _initPopularityValues();
            _subscribeToEvents();
            _bindCommands();
        }

        

        #region Properties
        public string DialogName
        {
            get
            {
                return mDialogName;
            }

            set
            {
                mDialogName = value;
                RaisePropertyChanged();
            }
        }

        public double Popularity
        {
            get
            {
                return mPopularity;
            }

            set
            {
                mPopularity = value;
                RaisePropertyChanged();
            }
        }

        public string PhraseFullText
        {
            get
            {
                return mPhraseFullText;
            }

            set
            {
                mPhraseFullText = value;
                RaisePropertyChanged();
            }
        }

        public PhraseDefinitionModel SelectedPhrase
        {
            get
            {
                return mSelectedPhrase;
            }

            set
            {
                mSelectedPhrase = value;
                PhraseFullText = mSelectedPhrase != null ? mSelectedPhrase.Description : string.Empty;
                RaisePropertyChanged();
                RemovePhraseFromListCommand.RaiseCanExecuteChanged();
            }
        }

        public ICollectionView PhraseDefinitionModels
        {
            get
            {
                return mPhraseDefinitionModels.View;
            }
        }

        //public ICollectionView PopularityValues
        //{
        //    get
        //    {
        //        return mPopularityValues.View;
        //    }
        //}

        public ObservableCollection<double> PopularityValues { get; set; } = new ObservableCollection<double>();

        #endregion

        #region Commands

        public DelegateCommand RemovePhraseFromListCommand { get; set; }
        public DelegateCommand ViewLoadedCommand { get; set; }
        public DelegateCommand ViewUnloadedCommand { get; set; }

        #endregion

        #region Public methods        

        public bool SaveDialog(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(DialogName))
            {
                errorMessage = "Dialog name can't be empty";
                return false;
            }

            bool _exists = false;
            foreach (var _modelDialogInfo in mDialogModelRepository.GetAll())
            {
                if (_modelDialogInfo.ArrayOfDialogModels.Where(dm => dm.Name.Equals(DialogName)).Count() > 0)
                {
                    _exists = true;
                    break;
                }
            }

            if (_exists)
            {
                errorMessage = string.Format("The dialog with name {0} already exists in the dialogs collection.", DialogName);
                return false;
            }

            var _jsonObjectTypesList = new JSONObjectsTypesList
            {
                Characters = new ObservableCollection<Character>(),
                DialogModels = new ObservableCollection<ModelDialogInfo>(),
                Wizards = new List<Wizard>(),
                Editable = true,
                Version = "1.4"
            };

            var _dialogModel = new ModelDialog
            {
                Name = DialogName,
                Popularity = Popularity,
                PhraseTypeSequence = new List<string>(),
            };

            var _charactersForSaving = new List<Character>();
            int counter = 0;
            string _dialogNameBase = DialogName.Replace(" ", string.Empty);
            foreach (var _item in PhraseDefinitionModels.SourceCollection)
            {
                PhraseDefinitionModel _model = (PhraseDefinitionModel)_item;
                if (_model.PhraseEntry == null)
                {
                    _dialogModel.PhraseTypeSequence.Add(_model.Text);
                }
                else
                {
                    if (counter == 0) counter = 1;
                    var _phraseTag = _dialogNameBase + counter;
                    _dialogModel.PhraseTypeSequence.Add(_phraseTag);

                    var _phrase = _model.Character.Phrases.Where(p => p.DialogStr.Equals(_model.Description)).First();
                    if (_phrase != null)
                    {
                        _phrase.PhraseWeights.Add(_phraseTag, Popularity);
                        if (!_charactersForSaving.Contains(_model.Character))
                        {
                            _charactersForSaving.Add(_model.Character);
                        }
                    }
                }

                counter++;
            }

            _jsonObjectTypesList.DialogModels.Add(new ModelDialogInfo
            {
                ModelsCollectionName = "Custom Dialogs",
                ArrayOfDialogModels = new List<ModelDialog>(),
                Editable = true,
                FileName = _dialogNameBase + ".json"
            });

            _jsonObjectTypesList.DialogModels[0].ArrayOfDialogModels.Add(_dialogModel);
            mDialogModelRepository.GetAll().Add(_jsonObjectTypesList.DialogModels[0]);

            // Create file path.
            string _filePath = Path.Combine(ApplicationData.Instance.DataDirectory, _dialogNameBase + ".json");

            // Save dialog to file.
            Serializer.Serialize(_jsonObjectTypesList, _filePath );

            // Save character changes.
            foreach (var _character in _charactersForSaving)
            {
                mCharacterDataProvider.SaveAsync(_character);
            }

            // Restart the dialog engine.
            mEventAggregator.GetEvent<CharacterUpdatedEvent>().Publish();

            return true;

        }

        #endregion

        #region Private methods

        private void _bindCommands()
        {
            RemovePhraseFromListCommand = new DelegateCommand(_removePhraseFromList_Execute, _removePhraseFromList_CanExecute);
            ViewLoadedCommand = new DelegateCommand(_viewLoaded);
            ViewUnloadedCommand = new DelegateCommand(_viewUnloaded);
        }

        private void _viewUnloaded()
        {
            // Cleaning.
            DialogName = string.Empty;
            Popularity = 1;
            var _collection = mPhraseDefinitionModels.Source as ObservableCollection<PhraseDefinitionModel>;
            _collection.Clear();
            RemovePhraseFromListCommand.RaiseCanExecuteChanged();
        }

        private void _viewLoaded()
        {
            
        }

        private bool _removePhraseFromList_CanExecute()
        {
            return ((mPhraseDefinitionModels.Source as ObservableCollection<PhraseDefinitionModel>).Count > 0 && SelectedPhrase != null);
        }

        private void _removePhraseFromList_Execute()
        {
            var _phrasesCollection = mPhraseDefinitionModels.Source as ObservableCollection<PhraseDefinitionModel>;
            _phrasesCollection.Remove(SelectedPhrase);
            SelectedPhrase = null;
            PhraseDefinitionModels?.Refresh();
            mEventAggregator.GetEvent<AddedPhraseModelToDialogEvent>().Publish(_phrasesCollection.Count);
            RemovePhraseFromListCommand.RaiseCanExecuteChanged();
        }

        private void _subscribeToEvents()
        {
            mEventAggregator.GetEvent<AddingPhraseModelToDialogEvent>().Subscribe(_wantToAddPhraseModel);
        }

        private void _wantToAddPhraseModel(PhraseDefinitionModel _phraseModel)
        {
            var _phraseDefinitionModels = mPhraseDefinitionModels.Source as ObservableCollection<PhraseDefinitionModel>;
            if(_phraseDefinitionModels != null)
            {
                if(_phraseDefinitionModels.Count() > 0)
                {
                    var _lastAddedPhraseModel = _phraseDefinitionModels.Last();
                    if (_lastAddedPhraseModel.SlotNumber == _phraseModel.SlotNumber)
                    {                        
                        mMessageDialogService.ShowMessage("Wrong parameter", "This character can't add two consequtive phrases! Plase add the prase from the other character!");
                        return;
                    }
                }

                _phraseDefinitionModels.Add(_phraseModel);
                mPhraseDefinitionModels.View?.Refresh();
                mEventAggregator.GetEvent<AddedPhraseModelToDialogEvent>().Publish(_phraseDefinitionModels.Count);
                RemovePhraseFromListCommand.RaiseCanExecuteChanged();

            }
        }

        private void _initPopularityValues()
        {
            var _values = new double[] { .1, .2, .3, .4, .5, .6, .7, .8, .9 };

            foreach(var _value in _values)
            {
                PopularityValues.Add(_value);
            }

            for(double i = 1.0; i < 30; i ++)
            {
                PopularityValues.Add(i);
            }

        }

        #endregion
    }
}
