using DialogGenerator.Core;
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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.ViewModels
{
    public class CharacterDetailViewModel : BindableBase
    {
        private ILogger mLogger;
        private IMessageDialogService mMessageDialogService;
        private ICharacterDataProvider mCharacterDataProvider;
        private Character mCharacter;

        public CharacterDetailViewModel(ILogger logger, ICharacterDataProvider _characterDataProvider, IMessageDialogService _messageDialogService)
        {
            mLogger = logger;
            mCharacterDataProvider = _characterDataProvider;
            mMessageDialogService = _messageDialogService;
        }

        public Character Character
        {
            get { return mCharacter; }
            set { SetProperty(ref mCharacter, value); }
        }

        // ... rest of ViewModel ...
    }
}
