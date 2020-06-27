using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        PhraseDefinitionModel mSelectedPhrase;

        public DialogSlotViewModel(IDialogModelRepository _DialogModelRepository, ICharacterDataProvider _CharacterDataProvider)
        {
            mDialogModelRepository = _DialogModelRepository;
            mCharacterDataProvider = _CharacterDataProvider;
            mPhraseDefinitionModels = new CollectionViewSource();
            mPopularityValues = new CollectionViewSource();
            mPhraseDefinitionModels.Source = new ObservableCollection<PhraseDefinitionModel>();
            _initPopularityValues();
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
                RaisePropertyChanged();
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

        #region Public methods

        public bool AddPhraseDefinition(PhraseDefinitionModel _Model, out string error)
        {
            error = string.Empty;
            var _collection = (mPhraseDefinitionModels.Source as ObservableCollection<PhraseDefinitionModel>);
            if(_collection.Contains(_Model))
            {
                error = "This model is already in collection!";
                return false;
            }

            _collection.Add(_Model);
            mPhraseDefinitionModels.View?.Refresh();

            return true;
        }

        public void RemovePhraseDefinition(PhraseDefinitionModel _Model)
        {
            var _collection = (mPhraseDefinitionModels.Source as ObservableCollection<PhraseDefinitionModel>);
            _collection.Remove(_Model);
            mPhraseDefinitionModels.View?.Refresh();
        }

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
                PhraseTypeSequence = new List<string>()
            };

            var _charactersForSaving = new List<Character>();
            int counter = 0;
            string _dialogNameBase = DialogName.Trim(' ');
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

                    var _phrase = _model.Character.Phrases.Where(p => p.DialogStr.Equals(_model.Text)).First();
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
            });

            _jsonObjectTypesList.DialogModels[0].ArrayOfDialogModels.Add(_dialogModel);

            // Save dialog to file.
            var _filePath = ApplicationData.Instance.DataDirectory + "\\" + DialogName + ".json";
            Serializer.Serialize(_jsonObjectTypesList, _filePath);

            // Save character changes.
            foreach (var _character in _charactersForSaving)
            {
                mCharacterDataProvider.SaveAsync(_character);
            }

            return true;

        }

        #endregion

        #region Private methods

        private void _initPopularityValues()
        {
            var _values = new ObservableCollection<double>();

            for(double i = 0.1; i < 1.0; i += 0.1)
            {
                //_values.Add(i);
                PopularityValues.Add(i);
            }

            for(double i = 1.0; i < 30; i ++)
            {
                //_values.Add(i);
                PopularityValues.Add(i);
            }

            //mPopularityValues.Source = _values;
        }

        #endregion
    }
}
