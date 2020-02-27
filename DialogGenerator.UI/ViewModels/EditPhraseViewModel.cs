using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class EditPhraseViewModel : BindableBase
    {
        private PhraseEntry mPhraseEntry;
        private string mDialogLineText;
        private string mFileName;
        private string mEditFileName;
        private string mPhraseWeights = string.Empty;
        private ICharacterDataProvider mCharacterDataProvider;
        private Character mCharacter;
        private CollectionViewSource mPhraseWeightsCollection;
        private ObservableCollection<PhraseWeight> mWeights = new ObservableCollection<PhraseWeight>();

        private CollectionViewSource mPhraseTypesCollection;
        private CollectionViewSource mPhraseValuesCollection;

        public EditPhraseViewModel(Character _Character, PhraseEntry _PhraseEntry, ICharacterDataProvider _CharacterDataProvider)
        {
            mPhraseEntry = _PhraseEntry;
            DialogLineText = mPhraseEntry.DialogStr;
            mCharacterDataProvider = _CharacterDataProvider;
            mCharacter = _Character;

            FileName = Path.Combine(ApplicationData.Instance.AudioDirectory, _Character.CharacterPrefix + "_" + mPhraseEntry.FileName + ".mp3");
            EditFileName = Path.Combine(ApplicationData.Instance.AudioDirectory, _Character.CharacterPrefix + "_" + mPhraseEntry.FileName + "_edit.mp3");

            if (File.Exists(EditFileName))
            {
                File.Delete(EditFileName);
            }

            File.Copy(FileName, EditFileName);
            foreach (var entry in mPhraseEntry.PhraseWeights)
            {
                if (string.IsNullOrEmpty(PhraseWeights)) {
                    PhraseWeights = "";
                } else
                {
                    PhraseWeights += ", ";
                }

                PhraseWeights += entry.Key;
                PhraseWeights += "/";
                PhraseWeights += entry.Value.ToString();
                mWeights.Add(new PhraseWeight(entry.Key, entry.Value));
            }

            mPhraseWeightsCollection = new CollectionViewSource();
            mPhraseWeightsCollection.Source = mWeights;

            mPhraseTypesCollection = new CollectionViewSource();
            mPhraseValuesCollection = new CollectionViewSource();
            
            _bindCommands();

        }



        #region Properties

        public ICollectionView PhraseWeightsCollection
        {
            get
            {
                return mPhraseWeightsCollection.View;
            }
        }

        public string PhraseWeights
        {
            get
            {
                return mPhraseWeights;
            }

            set
            {
                mPhraseWeights = value;
                RaisePropertyChanged();
            }
        }

        public string DialogLineText
        {
            get
            {
                return mDialogLineText;
            }

            set
            {
                mDialogLineText = value;
                RaisePropertyChanged();
            }
        }

        public string FileName
        {
            get
            {
                return mFileName;
            }

            set
            {
                mFileName = value;
                RaisePropertyChanged();
            }
        }

        public string EditFileName
        {
            get
            {
                return mEditFileName;
            }
            
            set
            {
                mEditFileName = value;
                RaisePropertyChanged();
            }
        }

        public ICollectionView PhraseTypeValues
        {
            get
            {
                return mPhraseTypesCollection.View;
            }
        }

        public ICollectionView PhraseWeightValues { 
            get
            {
                return mPhraseValuesCollection.View;
            }
        } 

        #endregion

        public DelegateCommand CloseCommand { get; private set; }
        public DelegateCommand AddPhraseWeightCommand { get; private set; }
        public DelegateCommand<PhraseWeight> RemovePhraseWeightCommand { get; private set; }

        public async Task SaveChanges()
        {
            File.Delete(FileName);
            File.Copy(EditFileName, FileName);
            foreach(var _phraseEntry in mCharacter.Phrases)
            {
                if(_phraseEntry.Equals(mPhraseEntry))
                {
                    _phraseEntry.DialogStr = DialogLineText;
                    string[] _tokens = PhraseWeights.Split(',');
                    if(_tokens.Length > 0)
                    {
                        _phraseEntry.PhraseWeights.Clear();
                        foreach (var _token in _tokens)
                        {
                            string[] _parts = _token.Trim(' ').Split('/');
                            if(_parts.Length == 1)
                            {
                                string _key = _parts[0].Trim(' ');
                                _parts = new string[2];
                                _parts[0] = _key;
                                _parts[1] = "10";
                            }

                            _phraseEntry.PhraseWeights.Add(_parts[0].Trim(' '), Double.Parse(_parts[1].Trim(' ')));
                        }
                    }                    
                }
            }

            await mCharacterDataProvider.SaveAsync(mCharacter);            
        }

        public async Task SaveChanges2()
        {
            File.Delete(FileName);
            File.Copy(EditFileName, FileName);
            foreach (var _phraseEntry in mCharacter.Phrases)
            {
                if (_phraseEntry.Equals(mPhraseEntry))
                {
                    _phraseEntry.DialogStr = DialogLineText;

                    if(mWeights.Count > 0)
                    {
                        _phraseEntry.PhraseWeights.Clear();
                        foreach(var _phraseWeight in mWeights)
                        {
                            _phraseEntry.PhraseWeights.Add(_phraseWeight.Key, _phraseWeight.Value);
                        }
                    }                    
                }
            }

            await mCharacterDataProvider.SaveAsync(mCharacter);
        }

        private void _viewClose_Execute()
        {
            if(!string.IsNullOrEmpty(EditFileName) && File.Exists(EditFileName))
            {
                File.Delete(EditFileName);
            }
        }

        #region Commands implementation

        private void _bindCommands()
        {
            CloseCommand = new DelegateCommand(_viewClose_Execute);
            AddPhraseWeightCommand = new DelegateCommand(_addPhraseWeight_Execute, _addPhraseWeight_CanExecute);
            RemovePhraseWeightCommand = new DelegateCommand<PhraseWeight>(_removePhraseWeight_Execute, _removePhraseWeight_CanExecute);
        }

        private bool _removePhraseWeight_CanExecute(PhraseWeight arg)
        {
            return mWeights.Count > 1;
        }

        private bool _addPhraseWeight_CanExecute()
        {
            bool _hasEmptyEntries = mWeights.Where(phw => string.IsNullOrEmpty(phw.Key)).Count() > 0;
            return !_hasEmptyEntries;
        }

        private void _addPhraseWeight_Execute()
        {
            var _phraseWeight = new PhraseWeight(string.Empty, 0);
            
            _phraseWeight.PropertyChanged += _phraseWeight_PropertyChanged;
            mWeights.Add(_phraseWeight);
            mPhraseWeightsCollection.View?.Refresh();
            AddPhraseWeightCommand.RaiseCanExecuteChanged();
            RemovePhraseWeightCommand.RaiseCanExecuteChanged();
        }

        private void _phraseWeight_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals("Key"))
            {
                AddPhraseWeightCommand.RaiseCanExecuteChanged();
            }
        }

        private void _removePhraseWeight_Execute(PhraseWeight _phraseWeight)
        {
            mWeights.Remove(_phraseWeight);
            mPhraseWeightsCollection.View?.Refresh();
            AddPhraseWeightCommand.RaiseCanExecuteChanged();
            RemovePhraseWeightCommand.RaiseCanExecuteChanged();
        }

        #endregion


    }

    public class PhraseWeight : INotifyPropertyChanged
    {
        private string mKey;
        public event PropertyChangedEventHandler PropertyChanged;

        public PhraseWeight(string _Key, double _Value)
        {
            Key = _Key;
            Value = _Value;
            _initLists();
        }

        public ObservableCollection<string> Keys { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<double> Values { get; set; } = new ObservableCollection<double>();

        public string Key { 
            get
            {
                return mKey;
            }

            set
            {
                mKey = value;
                OnNofityPropertyChanged(nameof(Key));
            }
        }

        public double Value { get; set; }

        private void OnNofityPropertyChanged(string _PropertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(_PropertyName));
            }
        }

        private class PhraseTypesCollection
        {
            string mVersionNumber;
            ObservableCollection<string> mPhrases = new ObservableCollection<string>();

            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Phrases")]
            public ObservableCollection<string> Phrases
            {
                get
                {
                    return mPhrases;
                }
            }
        }

        private void _initLists()
        {
            string _filePath = ApplicationData.Instance.DataDirectory + "\\Phrases.cfg";
            try
            {
                using (var _reader = new StreamReader(_filePath))
                {
                    string _jsonString = _reader.ReadToEnd();
                    var _phraseTypesCollection = Serializer.Deserialize<PhraseTypesCollection>(_jsonString);
                    if (_phraseTypesCollection != null && _phraseTypesCollection.Phrases.Count() > 0)
                    {
                        Keys.AddRange(_phraseTypesCollection.Phrases);
                    }                    
                }

                var _phraseWeightValues = new ObservableCollection<double>();
                for (double i = 0; i < 500; i++)
                {
                    Values.Add(i);
                }                
            }
            catch (IOException)
            {                
            }
            
        }
    }
}
