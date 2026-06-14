using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class CreateViewModel : BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IMessageDialogService mMessageDialogService;
        private IDialogDataRepository mDialogDataRepository;
        private ICharacterDataProvider mCharacterDataProvider;
        private IDialogModelDataProvider mDialogModelDataProvider;
        private IWizardDataProvider mWizardDataProvider;
        private IEventAggregator mEventAggregator;
        private Character mSelectedCharacter;
        private CollectionViewSource mCharactersCollectionViewSource;
        private string mFilterText;
        private IRegionManager mRegionManager;

        #endregion

        #region - constructor -

        public CreateViewModel(ILogger logger, IEventAggregator _eventAggregator
            , IDialogDataRepository _dialogDataRepository
            , IWizardDataProvider _wizardDataProvider
            , IDialogModelDataProvider _dialogModelDataProvider
            , ICharacterDataProvider _characterDataProvider
            , IMessageDialogService _messageDialogService
            , IRegionManager _RegionManager)
        {
            mRegionManager = _RegionManager;
            mEventAggregator = _eventAggregator;
            mLogger = logger;
            mDialogDataRepository = _dialogDataRepository;
            mMessageDialogService = _messageDialogService;
            mCharacterDataProvider = _characterDataProvider;
            mDialogModelDataProvider = _dialogModelDataProvider;
            mWizardDataProvider = _wizardDataProvider;

            mCharactersCollectionViewSource = new CollectionViewSource();
            FilterText = "";

            mCharactersCollectionViewSource.Filter += _mCharacterViewSource_Filter;

            _bindCommands();
        }


        #endregion

        #region - commands -

        public DelegateCommand CreateNewCharacterCommand { get; set; }
        public DelegateCommand CreateCustomDialogCommand { get; set; }
        public DelegateCommand ViewLoadedCommand { get; set; }
        public DelegateCommand ViewUnloadedCommand { get; set; }

        #endregion

        #region - event handlers -

        private void _mCharacterViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }

            var character = e.Item as Character;
            if (character.CharacterName.ToUpper().Contains(FilterText.ToUpper()) || string.IsNullOrEmpty(character.CharacterName))
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
            CreateNewCharacterCommand = new DelegateCommand(_createNewCharacterCommand_Execute);
            CreateCustomDialogCommand = new DelegateCommand(_onCreateCustomDialog_Execute);
            ViewLoadedCommand = new DelegateCommand(_onViewLoaded_execute);
            ViewUnloadedCommand = new DelegateCommand(_onViewUnloaded_execute);
        }

        private void _onViewUnloaded_execute()
        {
            mLogger.Debug($"Expert View - Unloaded");
        }

        private void _onViewLoaded_execute()
        {
            mLogger.Debug($"Expert View - Loaded");
        }

        private void _onCreateCustomDialog_Execute()
        {
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("CustomDialogCreatorView");
        }

        private async Task _processImportedData(JSONObjectsTypesList _importedData, string path)
        {
            await Task.Run(() =>
            {
                var _supportedExtensions = new string[] { ".jpg", ".jpeg", ".jpe", ".png" };

                foreach (var character in _importedData.Characters.ToList() ?? Enumerable.Empty<Character>())
                {
                    var _dirInfo = new DirectoryInfo(ApplicationData.Instance.TempDirectory);
                    FileInfo[] _mp3Files = _dirInfo.GetFiles($"{character.CharacterPrefix}*.mp3");

                    foreach (FileInfo _mp3File in _mp3Files)
                    {
                        File.Copy(_mp3File.FullName, Path.Combine(ApplicationData.Instance.AudioDirectory, _mp3File.Name), true);
                    }

                    // S.Ristic - 10/20/2019. Fix of the bug DLGEN-405
                    // The image file name should not conform to character prefix anymore.
                    FileInfo _imageFile = _dirInfo.GetFiles()
                        .Where(f => _supportedExtensions.Contains(f.Extension.ToLower()))
                        .ToList().FirstOrDefault();

                    if (_imageFile != null)
                    {
                        File.Copy(_imageFile.FullName, Path.Combine(ApplicationData.Instance.ImagesDirectory, _imageFile.Name), true);
                    }
                }

                mDialogDataRepository.Save(_importedData, path);
            });
        }

        private void _createNewCharacterCommand_Execute()
        {
            mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>().Publish(null);
        }

        #endregion

        #region - public functions -

        public void Load()
        {
            /* S.Ristic - This way we show only the editable characters */
            var characters = new ObservableCollection<Character>(mCharacterDataProvider.GetAll().Where(c => c.Editable == true));
            mCharactersCollectionViewSource.Source = characters;
            RaisePropertyChanged(nameof(CharactersViewSource));
        }

        #endregion

        #region - properties -

        public ICollectionView CharactersViewSource
        {
            get { return mCharactersCollectionViewSource.View; }
        }


        public string FilterText
        {
            get { return mFilterText; }
            set
            {
                mFilterText = value;
                mCharactersCollectionViewSource.View?.Refresh();
                RaisePropertyChanged();
            }
        }

        public Character SelectedCharacter
        {
            get { return mSelectedCharacter; }
            set
            {
                mSelectedCharacter = value;
                RaisePropertyChanged();
                if (mSelectedCharacter != null)
                {
                    mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>().Publish(mSelectedCharacter.CharacterPrefix);
                }
            }
        }

        #endregion
    }
}
