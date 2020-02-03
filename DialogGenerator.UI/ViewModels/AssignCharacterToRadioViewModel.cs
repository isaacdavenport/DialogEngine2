using DialogGenerator.CharacterSelection;
using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DialogGenerator.UI.ViewModels
{
    public class AssignCharacterToRadioViewModel : BindableBase
    {
        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private ICharacterRepository mCharacterRepository;
        private Character mSelectedCharacter;
        private IBLEDataProviderFactory mBLEDataProviderFactory;
        private IBLEDataProvider mCurrentDataProvider;
        private CancellationTokenSource mCancellationTokenSource;
        private ICharacterDataProvider mCharacterDataProvider;
        private IMessageDialogService mMessageDialogService;
        private int mAssignedRadio = -1;

        public AssignCharacterToRadioViewModel(ILogger _Logger
            , IEventAggregator _EventAggregator
            , ICharacterRepository _CharacterRepository
            , IBLEDataProviderFactory _BLEDataProviderFactory
            , ICharacterDataProvider _CharacterDataProvider
            , IMessageDialogService _MessageDialogService)
        {
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mCharacterRepository = _CharacterRepository;
            mBLEDataProviderFactory = _BLEDataProviderFactory;
            mCharacterDataProvider = _CharacterDataProvider;
            mMessageDialogService = _MessageDialogService;

            _bindCommands();
        }

        #region Properties

        public ObservableCollection<Character> Characters { get; } = new ObservableCollection<Character>();

        public Character SelectedCharacter
        {
            get
            {
                return mSelectedCharacter;
            }

            set
            {
                mSelectedCharacter = value;
                RaisePropertyChanged();
            }
        }
    
        #endregion


        #region Commands

        public DelegateCommand ViewLoadedCommand { get; set; }
        public DelegateCommand ViewUnloadedCommand { get; set; }

        #endregion

        #region Private methods

        private void _bindCommands()
        {
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
            ViewUnloadedCommand = new DelegateCommand(_viewUnloaded_Execute);
        }        

        private async void _viewLoaded_Execute()
        {
            Characters.Clear();
            List<Character> _characters = mCharacterRepository.GetAll().Where(c => c.RadioNum == -1).ToList();
            foreach (Character _ch in _characters)
            {
                Characters.Add(_ch);
            }

            if(_characters.Count > 0)
            {
                SelectedCharacter = _characters[0];
            }

            await _startRadioScanning();
        }


        private void _viewUnloaded_Execute()
        {
            _stopRadioScanning();
            if(mAssignedRadio != -1)
            {
                mEventAggregator.GetEvent<RadioAssignedEvent>().Publish(mAssignedRadio);
                mAssignedRadio = -1;
            }
            
        }

        private async Task _startRadioScanning()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mCurrentDataProvider = mBLEDataProviderFactory.Create(BLEDataProviderType.WinBLEWatcher);
            await Task.Run(async () =>
            {
                Thread.CurrentThread.Name = "CharacterMovingDetection";
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
                                await _selectToyToCharacter(_radioIndex);
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

        private async Task _selectToyToCharacter(int newVal)
        {
            var _oldChars = mCharacterDataProvider.GetAll().Where(c => c.RadioNum == newVal);
            if (_oldChars.Count() > 0)
            {
                var _oldChar = _oldChars.First();
                _oldChar = mCharacterDataProvider.GetAll().Where(c => c.RadioNum == newVal).First();
                if (_oldChar != null)
                {
                    // Show message box.
                    MessageDialogResult result = await mMessageDialogService.ShowOKCancelDialogAsync(String.Format("The toy with index {0} is assigned to character {1}. Are You sure that you want to re-asign it?", newVal, _oldChar.CharacterName), "Check");
                    if (result == MessageDialogResult.OK)
                    {
                        // Set to Unassigned if Yes
                        _oldChar.RadioNum = -1;
                        await mCharacterDataProvider.SaveAsync(_oldChar);
                        SelectedCharacter.RadioNum = newVal;
                        await mCharacterDataProvider.SaveAsync(SelectedCharacter);
                        mAssignedRadio = newVal;
                    }
                }
            } else
            {
                SelectedCharacter.RadioNum = newVal;
                await mCharacterDataProvider.SaveAsync(SelectedCharacter);
                await mMessageDialogService.ShowMessage("Success", string.Format("The radio {0} was successfully attached to character '{1}'", newVal, SelectedCharacter.CharacterName));
                mAssignedRadio = newVal;
            }
        }

        #endregion
    }
}
