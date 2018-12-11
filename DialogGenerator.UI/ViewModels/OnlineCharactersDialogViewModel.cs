using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class OnlineCharactersDialogViewModel:BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IOnlineCharactersRepository mOnlineCharactersRepository;
        private IEnumerable<FileItem> mOnlineCharacters;

        #endregion

        #region - constructor -

        public OnlineCharactersDialogViewModel(ILogger logger,IOnlineCharactersRepository _onlineCharactersRepository)
        {
            mLogger = logger;
            mOnlineCharactersRepository = _onlineCharactersRepository;

            _bindCommands();
        }

        #endregion

        #region - commands -

        public ICommand DialogLoadedCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            DialogLoadedCommand = new DelegateCommand(_onDialogLoaded_Execute);
        }

        private void _onDialogLoaded_Execute()
        {
            try
            {
                OnlineCharacters = mOnlineCharactersRepository.GetAll();
            }
            catch (Exception)
            {

            }
        }

        #endregion

        #region - properties -

        public IEnumerable<FileItem> OnlineCharacters
        {
            get { return mOnlineCharacters; }
            set
            {
                mOnlineCharacters = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
