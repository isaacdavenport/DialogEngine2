using DialogGenerator.Core;
using DialogGenerator.DataAccess.Helper;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.Utilities;
using NAudio.Lame;
using NAudio.Wave;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using Xceed.Wpf.Toolkit;

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
        private IEventAggregator mEventAggregator;
        private IMessageDialogService mMessageDialogService;
        private ILogger mLogger;
        private Character mCharacter;
        private CollectionViewSource mPhraseWeightsCollection;
        private ObservableCollection<PhraseWeight> mWeights = new ObservableCollection<PhraseWeight>();

        private CollectionViewSource mPhraseTypesCollection;
        private CollectionViewSource mPhraseValuesCollection;

        private bool mIsPlaying = false;
        private bool mIsRecording = false;
        private bool mCanCloseDialog = true;

        public EditPhraseViewModel(Character _Character
            , PhraseEntry _PhraseEntry
            , ICharacterDataProvider _CharacterDataProvider
            , IMessageDialogService _MessageDialogService
            , IEventAggregator _EventAggregator
            , ILogger _Logger)
        {
            mPhraseEntry = _PhraseEntry;
            DialogLineText = mPhraseEntry.DialogStr;
            mCharacterDataProvider = _CharacterDataProvider;
            mCharacter = _Character;
            mMessageDialogService = _MessageDialogService;
            mEventAggregator = _EventAggregator;
            mLogger = _Logger;

            MediaRecorderControlViewModel = new MediaRecorderControlViewModel(NAudioEngine.Instance, _MessageDialogService, _EventAggregator);
            MediaRecorderControlViewModel.PropertyChanged += MediaRecorderControlViewModel_PropertyChanged;
            MediaRecorderControlViewModel.RecordingEnabled = !_Character.HasNoVoice;

            FileName = Path.Combine(ApplicationData.Instance.AudioDirectory, _Character.CharacterPrefix + "_" + mPhraseEntry.FileName + ".mp3");
            EditFileName = Path.Combine(ApplicationData.Instance.AudioDirectory, _Character.CharacterPrefix + "_" + mPhraseEntry.FileName + "_edit.mp3");

            if (File.Exists(EditFileName))
            {
                File.Delete(EditFileName);
            }

            if (File.Exists(FileName))
            {
                File.Copy(FileName, EditFileName);
            } else
            {
                int _counter = 1;
                string _newFileName = Path.Combine(ApplicationData.Instance.AudioDirectory, _Character.CharacterPrefix + "_" + _Character.CharacterName + _counter + "_edit.mp3");
                while (File.Exists(_newFileName))
                {
                    _counter++;
                    _newFileName = Path.Combine(ApplicationData.Instance.AudioDirectory, _Character.CharacterPrefix + "_" + _Character.CharacterName + _counter + "_edit.mp3");
                }

                File.Create(_newFileName);
                EditFileName = _newFileName;
                FileName = EditFileName.Replace("_edit", string.Empty);
            }

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
                mWeights.Add(new PhraseWeight(entry.Key, entry.Value, mLogger));
            }

            mPhraseWeightsCollection = new CollectionViewSource();
            mPhraseWeightsCollection.Source = mWeights;

            mPhraseTypesCollection = new CollectionViewSource();
            mPhraseValuesCollection = new CollectionViewSource();

            _bindEvents();
            _bindCommands();

        }

        #region Properties

        public MediaRecorderControlViewModel MediaRecorderControlViewModel { get; set; }

        public bool CanCloseDialog
        {
            get
            {
                return mCanCloseDialog;
            }

            set
            {
                mCanCloseDialog = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get
            {
                return mIsPlaying;
            }

            set
            {
                mIsPlaying = value;
                RaisePropertyChanged();
                CanCloseDialog = !mIsRecording && !mIsPlaying;
            }
        }

        public bool IsRecording
        {
            get
            {
                return mIsRecording;
            }

            set
            {
                mIsRecording = value;
                RaisePropertyChanged();
                CanCloseDialog = !mIsRecording && !mIsPlaying;
            }
        }

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

        #region Commands
        public DelegateCommand LoadCommand { get; private set; }
        public DelegateCommand CloseCommand { get; private set; }
        public DelegateCommand AddPhraseWeightCommand { get; private set; }
        public DelegateCommand<PhraseWeight> RemovePhraseWeightCommand { get; private set; }

        #endregion

        #region Public methods
        public async Task SaveChanges()
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            if (File.Exists(EditFileName))
            {
                File.Copy(EditFileName, FileName);
            }

            foreach (var _phraseEntry in mCharacter.Phrases)
            {
                if (_phraseEntry.Equals(mPhraseEntry))
                {
                    _phraseEntry.DialogStr = DialogLineText;
                    string[] _tokens = PhraseWeights.Split(',');
                    if (_tokens.Length > 0)
                    {
                        _phraseEntry.PhraseWeights.Clear();
                        foreach (var _token in _tokens)
                        {
                            string[] _parts = _token.Trim(' ').Split('/');
                            if (_parts.Length == 1)
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
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            if (File.Exists(EditFileName))
            {
                File.Copy(EditFileName, FileName);
            }

            foreach (var _phraseEntry in mCharacter.Phrases)
            {
                if (_phraseEntry.Equals(mPhraseEntry))
                {
                    _phraseEntry.DialogStr = DialogLineText;

                    if (mWeights.Count > 0)
                    {
                        _phraseEntry.PhraseWeights.Clear();
                        foreach (var _phraseWeight in mWeights)
                        {
                            if (string.IsNullOrEmpty(_phraseWeight.Key.Key))
                            {
                                continue;
                            }

                            _phraseEntry.PhraseWeights.Add(_phraseWeight.Key.Key, _phraseWeight.Value);
                        }
                    }
                }
            }

            await mCharacterDataProvider.SaveAsync(mCharacter);

            mLogger.Debug($"Edit Phrase View Model - Saved changes for the character '{mCharacter.CharacterName}'");
        }

        #endregion

        #region Commands implementation

        private void _viewClose_Execute()
        {
            if (!string.IsNullOrEmpty(EditFileName) && File.Exists(EditFileName))
            {
                File.Delete(EditFileName);
            }

            mEventAggregator.GetEvent<RequestTranslationEvent>().Unsubscribe(_onTranslationRequired);
            mEventAggregator.GetEvent<SpeechConvertedEvent>().Unsubscribe(_onSpeechRecognized);

            mLogger.Debug($"Edit Phrase View - Closing for character '{mCharacter.CharacterName}'");
        }

        private bool _viewClose_CanExecute()
        {
            return (!mIsPlaying && !mIsRecording);
        }

        private bool _removePhraseWeight_CanExecute(PhraseWeight arg)
        {
            return mWeights.Count > 1;
        }

        private bool _addPhraseWeight_CanExecute()
        {
            bool _hasEmptyEntries = mWeights.Where(phw => phw.Key == null || string.IsNullOrEmpty(phw.Key.Key)).Count() > 0;
            return !_hasEmptyEntries;
        }

        private void _addPhraseWeight_Execute()
        {
            var _phraseWeight = new PhraseWeight(string.Empty, 10, mLogger);
            
            _phraseWeight.PropertyChanged += _phraseWeight_PropertyChanged;
            mWeights.Add(_phraseWeight);
            mPhraseWeightsCollection.View?.Refresh();
            AddPhraseWeightCommand.RaiseCanExecuteChanged();
            RemovePhraseWeightCommand.RaiseCanExecuteChanged();
        }        

        private void _removePhraseWeight_Execute(PhraseWeight _phraseWeight)
        {
            mWeights.Remove(_phraseWeight);
            mPhraseWeightsCollection.View?.Refresh();
            AddPhraseWeightCommand.RaiseCanExecuteChanged();
            RemovePhraseWeightCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods and event handlers

        private void _bindCommands()
        {
            LoadCommand = new DelegateCommand(_viewLoad_execute);
            CloseCommand = new DelegateCommand(_viewClose_Execute, _viewClose_CanExecute);
            AddPhraseWeightCommand = new DelegateCommand(_addPhraseWeight_Execute, _addPhraseWeight_CanExecute);
            RemovePhraseWeightCommand = new DelegateCommand<PhraseWeight>(_removePhraseWeight_Execute, _removePhraseWeight_CanExecute);
        }

        private void _viewLoad_execute()
        {
            mLogger.Debug($"Edit Phrase View - Loaded for '{mCharacter.CharacterName}'");
        }

        private void _bindEvents()
        {
            mEventAggregator.GetEvent<RequestTranslationEvent>().Subscribe(_onTranslationRequired);
            mEventAggregator.GetEvent<SpeechConvertedEvent>().Subscribe(_onSpeechRecognized);
        }

        private void _onSpeechRecognized(string _recognizedText)
        {
            if(string.IsNullOrEmpty(DialogLineText))
            {
                DialogLineText = _recognizedText;
            }
        }

        //public void UnbindEvents()
        //{
        //    mEventAggregator.GetEvent<RequestTranslationEvent>().Unsubscribe(_onTranslationRequired);
        //}

        private void MediaRecorderControlViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsPlaying") || e.PropertyName.Equals("IsRecording"))
            {
                CanCloseDialog = !MediaRecorderControlViewModel.IsPlaying && !MediaRecorderControlViewModel.IsRecording;
            }
        }

        private void _onTranslationRequired(string _Caller)
        {
            if(_Caller.Equals("MediaRecorderControlViewModel"))
            {
                if (!string.IsNullOrEmpty(DialogLineText))
                {
                    _generateSpeech(DialogLineText);
                    mMessageDialogService.ShowMessage("Notification", "The sound was generated from the text box");
                }
                else
                {
                    mMessageDialogService.ShowMessage("Error"
                        , "Since the recording was disabled, the text box should contain some meaningfull text which will be converted to speech and must not be empty!");


                }
            }
            
        }

        private void _phraseWeight_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Key"))
            {
                AddPhraseWeightCommand.RaiseCanExecuteChanged();
            }
        }

        private void _generateSpeech(string value)
        {
            string _outfile = string.Empty;

            using (SpeechSynthesizer _synth = new SpeechSynthesizer())
            {
                _synth.Volume = 100;
                _synth.Rate = mCharacter.SpeechRate;
                if (_synth.GetInstalledVoices().Count() == 0)
                {
                    return;
                }

                if (_synth.GetInstalledVoices().Where(iv => iv.VoiceInfo.Name.Equals(mCharacter.Voice)).Count() > 0)
                {
                    _synth.SelectVoice(mCharacter.Voice);
                }

                string _outfile_original = MediaRecorderControlViewModel.FilePath;
                
                MemoryStream _ms = new MemoryStream();
                _synth.SetOutputToWaveStream(_ms);
                _synth.Speak(value);
                _synth.SetOutputToNull();

                _ms.Position = 0;

                using (var _rdr = new WaveFileReader(_ms))
                using (var _outFileStream = File.OpenWrite(_outfile_original))
                using (var _wtr = new LameMP3FileWriter(_outFileStream, _rdr.WaveFormat, 128))
                {
                    _rdr.CopyTo(_wtr);
                }

                MediaRecorderControlViewModel.StartPlayingFileCommand.RaiseCanExecuteChanged();
                
            }

        }

        #endregion
    }

    public class PhraseWeight : INotifyPropertyChanged
    {
        private ComboEntry mKey;
        private ILogger mLogger;
        public event PropertyChangedEventHandler PropertyChanged;
        

        public PhraseWeight(string _Key, double _Value, ILogger _Logger)
        {
            mLogger = _Logger;
            _initLists();
            Key = new ComboEntry() {Key = _Key, Description = ""};
            Value = _Value;

        }

        public ObservableCollection<ComboEntry> Keys { get; set; } = new ObservableCollection<ComboEntry>();
        public ObservableCollection<double> Values { get; set; } = new ObservableCollection<double>();

        public ComboEntry Key { 
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

        private void _initLists()
        {
            string _filePath = ApplicationData.Instance.DataDirectory + "\\Phrases.cfg";

            try
            {
                using (var _reader = new StreamReader(_filePath))
                {
                    string _jsonString = _reader.ReadToEnd();
                    var _phraseKeysCollection = Serializer.Deserialize<PhraseKeysCollection>(_jsonString);

                    if (_phraseKeysCollection != null && _phraseKeysCollection.Phrases != null && _phraseKeysCollection.Phrases.Count() > 0)
                    {
                        Keys.AddRange(_phraseKeysCollection.Phrases.Where(p => p != null).Select(p => new ComboEntry
                            { Key = p.Name, Description = p.Description}));
                    }
                }

                for (double i = 0; i < 60; i++)
                {
                    Values.Add(i);
                }
            }
            catch (/* Newtonsoft.Json.JsonReaderException */ Exception e)
            {
                //MessageBox.Show(e.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

                mLogger.Error($"PhraseWeight exception in _initLists function - {e.Message}");
            }

        }
    }

    public class ComboEntry : Object, IComparable<ComboEntry>
    {
        public string Key { get; set; }
        public string Description { get; set; }

        public int CompareTo(ComboEntry other)
        {
            return other.Key.CompareTo(this.Key);
        }

        public override string ToString()
        {
            return Key;
        }
    }
}
