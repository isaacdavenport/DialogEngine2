using DialogGenerator.CharacterSelection;
using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Helpers;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Wrapper;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class CharacterDetailViewModel : BindableBase, IDetailViewModel
    {
        #region - fields-
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterDataProvider mCharacterDataProvider;
        private IMessageDialogService mMessageDialogService;
        private IMP3Player mMP3Player;
        private IRegionManager mRegionManager;
        private CharacterWrapper mCharacter;
        private CollectionViewSource mPhrasesCollectionViewSource;
        private string mFilterText;
        private bool mIsDialogStarted;
        private bool mIsEditing;
        private string mUniqueIdentifier;
        private ILogger object1;
        private IEventAggregator object2;
        private ICharacterDataProvider object3;
        private IMessageDialogService object4;
        private IMP3Player object5;

        private IBLEDataProviderFactory mBLEDataProviderFactory;
        private IBLEDataProvider mCurrentDataProvider;
        private CancellationTokenSource mCancellationTokenSource;

        #endregion

        #region -constructor-
        public CharacterDetailViewModel(ILogger logger, IEventAggregator _eventAggregator
            , ICharacterDataProvider _characterDataProvider
            , IMessageDialogService  _messageDialogService
            , IMP3Player _mp3Player
            , IRegionManager _regionManager
            , IBLEDataProviderFactory _BLEDataProviderFactory)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mCharacterDataProvider = _characterDataProvider;
            mMessageDialogService = _messageDialogService;
            mMP3Player = _mp3Player;
            mRegionManager = _regionManager;
            mBLEDataProviderFactory = _BLEDataProviderFactory;
            mPhrasesCollectionViewSource = new CollectionViewSource();
            FilterText = "";
            mPhrasesCollectionViewSource.Filter += _phrases_Filter;

            mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>().Subscribe(_onOpenCharacterDetailView);
            mEventAggregator.GetEvent<CharacterSelectionActionChangedEvent>().Subscribe(_onCharacterSelectionActionChanged);
            mEventAggregator.GetEvent<CharacterStructureChangedEvent>().Subscribe(_onCharacterStructureChanged);

            using (var _synth = new SpeechSynthesizer())
            {
                foreach(var _voice in _synth.GetInstalledVoices())
                {
                    VoicesCollection.Add(_voice.VoiceInfo.Name);
                }
            }

            _bindCommands();
        }       

        public CharacterDetailViewModel(ILogger object1, IEventAggregator object2, ICharacterDataProvider object3, IMessageDialogService object4, IMP3Player object5)
        {
            this.object1 = object1;
            this.object2 = object2;
            this.object3 = object3;
            this.object4 = object4;
            this.object5 = object5;
        }

        #endregion

        #region - commands -

        public ICommand SaveCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand EditWithJSONEditorCommand { get; set; }
        public ICommand ExportCharacterCommand { get; set; }
        public ICommand ChooseImageCommand { get; set; }
        public ICommand CopyToClipboardCommand { get; set; }
        public ICommand ViewLoadedCommand { get; set; }
        public ICommand ViewClosedCommand { get; set; }
        public DelegateCommand<PhraseEntry> DeletePhraseCommand { get; set; }
        public DelegateCommand<string> PlayDialogLineCommand { get; set; }

        #endregion

        #region - event handlers -

        private void _phrases_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }

            var phrase = e.Item as PhraseEntry;
            if (phrase.DialogStr.ToUpper().Contains(FilterText.ToUpper()))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        #endregion

        #region - private functions -
        private void _bindCommands()
        {
            SaveCommand = new DelegateCommand(_saveCommand_Execute, _saveCommand_CanExecute);
            DeleteCommand = new DelegateCommand(_deleteCommand_Execute, _deleteCommand_CanExecute);
            EditWithJSONEditorCommand = new DelegateCommand(_editWithJSONEditorCommand_Execute, _editWithJSONEditorCommand_CanExecute);
            ExportCharacterCommand = new DelegateCommand(_exportCharacterCommand_Execute, _exportCharacterCommand_CanExecute);
            ChooseImageCommand = new DelegateCommand(_chooseImage_Execute);
            CopyToClipboardCommand = new DelegateCommand(_copyToClipboard_Execute);
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
            ViewClosedCommand = new DelegateCommand(_viewClosed_Execute);
            DeletePhraseCommand = new DelegateCommand<PhraseEntry>(_deletePhrase_Execute,_deletePrhase_CanExecute);
            PlayDialogLineCommand = new DelegateCommand<string>(_playDialogLine_Execute,_playDialogLine_CanExecute);
        }

        private async void _viewLoaded_Execute()
        {
            var character =mRegionManager.Regions[Constants.ContentRegion].Context as Character;
            Load(string.IsNullOrEmpty(character.CharacterPrefix) ? "" : character.CharacterPrefix);
            Session.Set(Constants.SELECTED_CHARACTER, character);
            await _startRadioScanning();
        }        

        private void _viewClosed_Execute()
        {
            // S.Ristic - 10/19/2019 - Fixing of DLGEN-407 Bug
            // I had to comment this because there were cases when the Character.Model.FileName was null.
            // Killing of opened JSONEdit process is already implemented on another place. But I will 
            // leave this handler for the future use.

            //// S.Ristic 10/17/2019.
            //// Kill JSONEditor for this character if opened.
            //if (ProcessHandler.Contains(Character.Model.FileName))
            //{
            //    ProcessHandler.Remove(Character.Model.FileName);                
            //}

            _stopRadioScanning();
            //Session.Set(Constants.SELECTED_CHARACTER, null);
        }

        private async Task _startRadioScanning()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mCurrentDataProvider = mBLEDataProviderFactory.Create(BLEDataProviderType.WinBLEWatcher);
            await Task.Run(async () =>
            {
                Thread.CurrentThread.Name = "StartCharacterMovingDetection";
                Task _BLEDataReaderTask = mCurrentDataProvider.StartReadingData();
                int _oldIndex = -1;
                do
                {
                    // Read messages
                    BLE_Message message = mCurrentDataProvider.GetMessage();
                    if (message != null)
                    {
                        int _radioIndex = -1;
                        string outData = String.Empty;
                        for (int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                        {
                            if (message.msgArray[i] == 0xFF)
                            {
                                _radioIndex = i;
                            }

                        }

                        // If motion vector is greater than zero, show message
                        int _motion = message.msgArray[ApplicationData.Instance.NumberOfRadios];
                        if (_motion > 10 && _radioIndex > -1)
                        {
                            if (_radioIndex != _oldIndex)
                            {

                                _selectToyToCharacter(_radioIndex);
                                _oldIndex = _radioIndex;
                            }
                        }

                    }

                    Thread.Sleep(1);
                } while (!mCancellationTokenSource.IsCancellationRequested);

                await _BLEDataReaderTask;

            });
        }

        private void _stopRadioScanning()
        {
            mCurrentDataProvider.StopReadingData();
            mCancellationTokenSource.Cancel();
        }

        private async void _selectToyToCharacter(int newVal)
        {
            var _oldChars = mCharacterDataProvider.GetAll().Where(c => c.RadioNum == newVal);
            if (_oldChars.Count() > 0)
            {
                var _oldChar = _oldChars.First();
                _oldChar = mCharacterDataProvider.GetAll().Where(c => c.RadioNum == newVal).First();
                if (_oldChar != null)
                {
                    // izbaci message box
                    MessageDialogResult result = await mMessageDialogService.ShowOKCancelDialogAsync(String.Format("The toy with index {0} is assigned to character {1}. Are You sure that you want to re-asign it?", newVal, _oldChar.CharacterName), "Check");
                    if (result == MessageDialogResult.OK)
                    {
                        // settuj na Unassigned ako je Yes
                        _oldChar.RadioNum = -1;
                        await mCharacterDataProvider.SaveAsync(_oldChar);
                        Character.Model.RadioNum = newVal;
                    }
                }
            }            
        }

        private void _onCharacterStructureChanged()
        {
            Load(string.IsNullOrEmpty(Character.CharacterPrefix) ? "" : Character.CharacterPrefix);
        }

        private bool _deletePrhase_CanExecute(PhraseEntry arg)
        {
            return !IsDialogStarted;
        }

        private bool _playDialogLine_CanExecute(string arg)
        {
            return !IsDialogStarted;
        }

        private async void _playDialogLine_Execute(string _fileName)
        {
            string _fullFileName = $"{Character.CharacterPrefix}_{_fileName}.mp3";
            string _fullPath = Path.Combine(ApplicationData.Instance.AudioDirectory, _fullFileName);
            MP3PlayerDialog dialog = new MP3PlayerDialog(_fullPath);

            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(dialog);            
        }

        private async void _deletePhrase_Execute(PhraseEntry phrase)
        {
            try
            {
                MessageDialogResult result = await mMessageDialogService.ShowOKCancelDialogAsync("Are you sure about deleting this line?", "Warning");
                if (result == MessageDialogResult.Cancel)
                    return;

                mCharacterDataProvider.RemovePhrase(Character.CharacterModel, phrase);
                await mCharacterDataProvider.SaveAsync(Character.CharacterModel);
                FilterText = "";
            }
            catch (Exception ex)
            {
                mLogger.Error($"_deletePhrase_Execute {ex.Message}");
            }
        }

        private void _copyToClipboard_Execute()
        {
            Clipboard.SetText($"{Character.Model.CharacterPrefix}_{UniqueIdentifier}");
        }

        private void _onCharacterSelectionActionChanged(bool _isDialogStarted)
        {
            IsDialogStarted = _isDialogStarted;
        }

        private bool _chooseImage_CanExecute()
        {
            if (string.IsNullOrEmpty(Character.CharacterPrefix) || Character.HasErrors)
            { 
               mMessageDialogService
                    .ShowMessage("Information", "You are not able to set image before all required fields are filled.");

                return false;
            }
            else
            {
                return true;
            }
        }

        private async void _chooseImage_Execute()
        {
            try
            {
                if (!_chooseImage_CanExecute())
                    return;

                System.Windows.Forms.OpenFileDialog _openFileDialog = new System.Windows.Forms.OpenFileDialog();
                _openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";

                if (_openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string _chCurrentImageFilePath = Character.CharacterImage;
                string _filePath = _openFileDialog.FileName;
                string _newFileName = $"{Character.CharacterPrefix}_{Path.GetFileName(_filePath)}";
                Character.CharacterImage = ApplicationData.Instance.DefaultImage;

                await Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "_chooseImage_Execute";

                    File.Copy(_filePath, Path.Combine(ApplicationData.Instance.ImagesDirectory, _newFileName), true);

                    if (!_chCurrentImageFilePath.Equals(ApplicationData.Instance.DefaultImage))
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        File.Delete(Path.Combine(ApplicationData.Instance.ImagesDirectory, _chCurrentImageFilePath));
                    }

                    Character.CharacterImage = _newFileName;
                });
            }
            catch (Exception ex)
            {
                mLogger.Error("_chooseImage_Execute " + ex.Message);
            }
        }

        private bool _exportCharacterCommand_CanExecute()
        {
            return IsEditing;
        }

        private async void _exportCharacterCommand_Execute()
        {
            try
            {
                System.Windows.Forms.SaveFileDialog _saveFileDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "T2lf file(*.t2lf)|*.t2lf",
                    FileName = Character.Model.CharacterName.Replace(" ", string.Empty)
                };
                System.Windows.Forms.DialogResult result = _saveFileDialog.ShowDialog();
                if (result != System.Windows.Forms.DialogResult.OK)
                    return;

                if (File.Exists(_saveFileDialog.FileName))
                {
                    File.Delete(_saveFileDialog.FileName);
                }

                mMessageDialogService.ShowBusyDialog();

                await Task.Run(() =>
                {
                    FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
                    mCharacterDataProvider.Export(Character.Model, ApplicationData.Instance.TempDirectory);

                    // zip content from temp directory
                    //ZipFile.CreateFromDirectory(ApplicationData.Instance.TempDirectory, _saveFileDialog.FileName);
                    FileHelper.ExportCharacter(ApplicationData.Instance.TempDirectory, _saveFileDialog.FileName);
                });

            }
            catch (Exception ex)
            {
                mLogger.Error("_exportCharacterCommand_Execute " + ex.Message);
                mMessageDialogService.CloseBusyDialog();
                await mMessageDialogService.ShowMessage("Error","An error occured during exporting character. Please try again.");                
            }
            finally
            {
                FileHelper.ClearDirectory(ApplicationData.Instance.TempDirectory);
                mMessageDialogService.CloseBusyDialog();
            }
        }

        private bool _editWithJSONEditorCommand_CanExecute()
        {
            return IsEditing;
        }

        private async void _editWithJSONEditorCommand_Execute()
        {
            try
            {
                if (ProcessHandler.Contains(Character.Model.FileName))
                {
                    await mMessageDialogService.ShowMessage("Info","Character already opened with JSON editor.");
                    return;
                }

                File.Copy(Path.Combine(ApplicationData.Instance.DataDirectory, Character.Model.FileName),
                    Path.Combine(ApplicationData.Instance.EditorTempDirectory, Character.Model.FileName));

                ProcessStartInfo _startInfo = new ProcessStartInfo();
                _startInfo.FileName = Path.Combine(ApplicationData.Instance.ToolsDirectory, ApplicationData.Instance.JSONEditorExeFileName);
                _startInfo.Arguments = Path.Combine(ApplicationData.Instance.EditorTempDirectory, Character.Model.FileName);

                var process = Process.Start(_startInfo);
                process.EnableRaisingEvents = true;

                ProcessHandler.Set(Character.Model.FileName, process);             
            }
            catch (Exception ex)
            {
                mLogger.Error("_editWithJSONEditorCommand_Execute " + ex.Message);
            }
        }

        private bool _deleteCommand_CanExecute()
        {
            return IsEditing;
        }

        private async void _deleteCommand_Execute()
        {
            try
            {
                var result = await mMessageDialogService
                    .ShowOKCancelDialogAsync("Are you sure about deleting this character?", "Delete character");
                if (result == MessageDialogResult.Cancel)
                    return;

                mMessageDialogService.ShowBusyDialog();

                string _imageFileName = Character.CharacterImage;
                Character.CharacterImage = ApplicationData.Instance.DefaultImage; // set to default image to avoid file in use exception

                int _characterIndex = mCharacterDataProvider.IndexOf(Character.Model);

                await mCharacterDataProvider.Remove(Character.Model, _imageFileName);

                // S.Ristic 02/03/2020
                // This should notify the gallery of arena view that the collection has changed.
                mEventAggregator.GetEvent<CharacterCollectionLoadedEvent>().Publish();

                // S.Ristic 10/12/2019
                // Find out if the deleted character has participated in the conversation and 
                // if it had, remove it's index from the forced character indices and decrease the 
                // count of forced characters for 1. 
                if (Character.Model.State == Model.Enum.CharacterState.On)
                {
                    int _forcedCharactersCount = Session.Get<int>(Constants.FORCED_CH_COUNT);
                    
                    if (Session.Get<int>(Constants.FORCED_CH_1) == _characterIndex)
                    {
                        Session.Set(Constants.FORCED_CH_1, -1);
                        Session.Set(Constants.FORCED_CH_COUNT, _forcedCharactersCount - 1);

                        if (Session.Get<int>(Constants.FORCED_CH_COUNT) == 1)
                        {
                            Session.Set(Constants.FORCED_CH_1, Session.Get<int>(Constants.FORCED_CH_2));
                            Session.Set(Constants.FORCED_CH_2, -1);
                        }
                    }

                    if (Session.Get<int>(Constants.FORCED_CH_2) == _characterIndex)
                    {
                        Session.Set(Constants.FORCED_CH_2, -1);
                        Session.Set(Constants.FORCED_CH_COUNT, _forcedCharactersCount - 1);
                    }
                }

                Load("");
            }
            catch (Exception ex)
            {
                mLogger.Error("_deleteCommand_Execute " + ex.Message);
                mMessageDialogService.CloseBusyDialog();
                await mMessageDialogService.ShowMessage("Error", "Error occured during deleting character.");
                Load(Character.CharacterPrefix);
            }
            finally
            {
                mMessageDialogService.CloseBusyDialog();
            }
        }

        private bool _saveCommand_CanExecute()
        {
           return Character != null && !Character.HasErrors;
        }

        private async void _saveCommand_Execute()
        {
            try
            {
                if (IsEditing)
                {
                    Character.CharacterName = Character.CharacterName.Trim();
                    await mCharacterDataProvider.SaveAsync(Character.Model);
                }
                else
                {                    
                    string _prefix  = $"{Character.CharacterPrefix}_{UniqueIdentifier}".Trim();
                    Character.CharacterName = Character.CharacterName.Trim();
                    Character.CharacterPrefix = _prefix;
                    await mCharacterDataProvider.AddAsync(Character.Model);
                }

                Load(Character.CharacterPrefix);
            }
            catch (Exception ex)
            {
                mLogger.Error("_saveCommand_Execute " + ex.Message);
                await mMessageDialogService.ShowMessage("Error", "Error occured during saving character. Please try again.");
            }

            await mMessageDialogService.ShowMessage("INFO", "The character data has been saved. The speech settings are not " +
                "transfered automatically to the existing dialog lines. They will be applied only in the moment when the new dialog lines will be recorded.");
        }

        private void _onOpenCharacterDetailView(string _characterPrefix)
        {
            //Load(_characterPrefix);
            string _prefix = _characterPrefix;
        }

        // Mason Zhwiti                -> MZ
        // mason lowercase zhwiti      -> MZ
        // Mason G Zhwiti              -> MZ
        // Mason G. Zhwiti             -> MZ
        // John Queue Public           -> JP
        // John Q. Public, Jr.         -> JP
        // John Q Public Jr.           -> JP
        // Thurston Howell III         -> TH
        // Thurston Howell, III        -> TH
        // Malcolm X                   -> MX
        // A Ron                       -> AR
        // A A Ron                     -> AR
        // Madonna                     -> M
        // Chris O'Donnell             -> CO
        // Malcolm McDowell            -> MM
        // Robert "Rocky" Balboa, Sr.  -> RB
        private string _extractInitialsFromName(string name)
        {
            // first remove all: punctuation, separator chars, control chars, and numbers (unicode style regexes)
            string initials = Regex.Replace(name, @"[\p{P}\p{S}\p{C}\p{N}]+", "");

            // Replacing all possible whitespace/separator characters (unicode style), with a single, regular ascii space.
            initials = Regex.Replace(initials, @"\p{Z}+", " ");

            // Remove all Sr, Jr, I, II, III, IV, V, VI, VII, VIII, IX at the end of names
            initials = Regex.Replace(initials.Trim(), @"\s+(?:[JS]R|I{1,3}|I[VX]|VI{0,3})$", "", RegexOptions.IgnoreCase);

            // Extract up to 2 initials from the remaining cleaned name.
            initials = Regex.Replace(initials, @"^(\p{L})[^\s]*(?:\s+(?:\p{L}+\s+(?=\p{L}))?(?:(\p{L})\p{L}*)?)?$", "$1$2").Trim();
            if (initials.Length > 2)
            {
                // Worst case scenario, everything failed, just grab the first two letters of what we have left.
                initials = initials.Substring(0, 2);
            }

            return initials.ToUpperInvariant();
        }

        #endregion

        #region - public functions -

        public void Load(string _characterInitials)
        {
            IsEditing = !string.IsNullOrEmpty(_characterInitials);

            var character = !string.IsNullOrEmpty(_characterInitials)
                ? mCharacterDataProvider.GetByInitials(_characterInitials)
                : new Character();

            if (character == null)
            {
                character = new Character();
                IsEditing = false;
            }

            Character = new CharacterWrapper(character,mCharacterDataProvider);
            Character.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(Character.HasErrors):
                        {
                            ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
                            break;
                        }
                    case nameof(Character.CharacterName):
                        {
                            if (!IsEditing)
                            {
                                Character.CharacterPrefix = _extractInitialsFromName(Character.CharacterName);
                            }
                            break;
                        }
                }
            };

            mPhrasesCollectionViewSource.Source = Character.CharacterModel.Phrases;
            ((DelegateCommand)DeleteCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)EditWithJSONEditorCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)ExportCharacterCommand).RaiseCanExecuteChanged();

            // used to force validation
            if (string.IsNullOrEmpty(_characterInitials))
            {
                Character.CharacterName = "";
                Character.CharacterPrefix = "";
                UniqueIdentifier = Guid.NewGuid().ToString().Substring(0, 4);
            }
            else
            {
                string[] _characterPrefixParts = Character.Model.CharacterPrefix.Split('_');
                UniqueIdentifier = _characterPrefixParts.Length > 1 ? _characterPrefixParts[1] : "";
            }

            RaisePropertyChanged(nameof(PhrasesViewSource));
        }


        #endregion

        #region - properties -
        public CharacterWrapper Character
        {
            get { return mCharacter; }
            set
            {
                mCharacter = value;
                RaisePropertyChanged();
            }
        }

        public List<string> VoicesCollection { get; set; } = new List<string>();

        public List<int> AgeValues { get; set; } = new List<int>(Enumerable.Range(2, 100));

        public List<string> GenderValues { get; set; } = new List<string>(new string[] { "Male", "Female" });

        public bool IsEditing
        {
            get { return mIsEditing; }
            set
            {
                mIsEditing = value;
                RaisePropertyChanged();
            }
        }

        public bool IsDialogStarted
        {
            get { return mIsDialogStarted; }
            set
            {
                mIsDialogStarted = value;
                RaisePropertyChanged();
            }
        }

        public string UniqueIdentifier
        {
            get { return mUniqueIdentifier; }
            set
            {
                mUniqueIdentifier = value;
                RaisePropertyChanged();
            }
        }

        public ICollectionView PhrasesViewSource
        {
            get { return mPhrasesCollectionViewSource.View; }
        }

        public string FilterText
        {
            get { return mFilterText; }
            set
            {
                mFilterText = value;
                mPhrasesCollectionViewSource.View?.Refresh();
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
