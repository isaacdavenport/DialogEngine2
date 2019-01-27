using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.UI.Data;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class ComputerSelectsViewModel:BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private ICharacterDataProvider mCharacterDataProvider;
        private IEventAggregator mEventAggregator;
        private CollectionViewSource mCharactersCollectionViewSource;
        private string mFilterText;

        #endregion

        #region - ctor -

        public ComputerSelectsViewModel(ILogger logger,IEventAggregator _eventAggregator,ICharacterDataProvider _characterDataProvider)
        {
            mLogger = logger;
            mCharacterDataProvider = _characterDataProvider;
            mEventAggregator = _eventAggregator;
            ViewLoadedCommand = new DelegateCommand(_viewLoaded_Execute);
            ChangeCharacterStatusCommand = new DelegateCommand<object>((p) => _changeCharacterStatusCommand_Execute(p));

            mCharactersCollectionViewSource = new CollectionViewSource();
            FilterText = "";

            mCharactersCollectionViewSource.Filter += _mCharacterViewSource_Filter;
        }

        #endregion

        #region - commands -

        public DelegateCommand ViewLoadedCommand { get; set; }
        public DelegateCommand<object> ChangeCharacterStatusCommand { get; set; }

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
            if (character.CharacterName.ToUpper().Contains(FilterText.ToUpper()))
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

        private void _viewLoaded_Execute()
        {
            var characters = mCharacterDataProvider.GetAll();
            mCharactersCollectionViewSource.Source = characters.Where(ch => !string.IsNullOrEmpty(ch.CharacterName));
            RaisePropertyChanged(nameof(CharactersViewSource));
        }

        private async void _changeCharacterStatusCommand_Execute(object obj)
        {
            try
            {
                var parameters = (object[])obj;
                var character = parameters[0] as Character;
                var _newState = (CharacterState)parameters[1];
                int index = int.Parse(parameters[2].ToString())+1; // add 1 bcs we have dammy character at first position
                int _forcedCharactersCount = Session.Get<int>(Constants.FORCED_CH_COUNT);

                if (_newState == character.State)
                    return;

                if (_newState == CharacterState.On)
                {
                    if (_forcedCharactersCount == 0)
                    {
                        Session.Set(Constants.FORCED_CH_1, index);
                        Session.Set(Constants.FORCED_CH_COUNT, 1);
                    }
                    else if (_forcedCharactersCount == 1)
                    {
                        Session.Set(Constants.FORCED_CH_2, index);
                        Session.Set(Constants.FORCED_CH_COUNT, 2);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (character.State == CharacterState.On)
                    {
                        if (Session.Get<int>(Constants.FORCED_CH_1) == index)
                        {
                            Session.Set(Constants.FORCED_CH_1, -1);
                            Session.Set(Constants.FORCED_CH_COUNT, _forcedCharactersCount - 1);

                            if (Session.Get<int>(Constants.FORCED_CH_COUNT) == 1)
                            {
                                Session.Set(Constants.FORCED_CH_1, Session.Get<int>(Constants.FORCED_CH_2));
                                Session.Set(Constants.FORCED_CH_2, -1);
                            }
                        }

                        if (Session.Get<int>(Constants.FORCED_CH_2) == index)
                        {
                            Session.Set(Constants.FORCED_CH_2, -1);
                            Session.Set(Constants.FORCED_CH_COUNT, _forcedCharactersCount - 1);
                        }
                    }
                }

                character.State = _newState;
                await mCharacterDataProvider.SaveAsync(character);

                mEventAggregator.GetEvent<ChangedCharacterStateEvent>().Publish();
                mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();
            }
            catch (Exception ex)
            {
                mLogger.Error("_changeCharacterStatusCommand_Execute " + ex.Message);
            }
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

        #endregion
    }
}
