using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.UI.ViewModels
{
    public class CustomDialogCreatorViewModel : BindableBase
    {
        ICharacterDataProvider mCharacterDataProvider;
        IDialogModelRepository mDialogModelRepository;
        ILogger mLogger;
        IEventAggregator mEventAggregator;
        
        public CustomDialogCreatorViewModel(ICharacterDataProvider _CharacterDataProvider,
                                            IDialogModelRepository _DialogModelRepository,
                                            ILogger _Logger,
                                            IEventAggregator _EventAggregator)
        {
            mCharacterDataProvider = _CharacterDataProvider;
            mDialogModelRepository = _DialogModelRepository;
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            LeftCharacterModel = new CharacterSlotViewModel(_CharacterDataProvider, _EventAggregator);
            RightCharacterModel = new CharacterSlotViewModel(_CharacterDataProvider, _EventAggregator);
            DialogModel = new DialogSlotViewModel(mDialogModelRepository, mCharacterDataProvider);

            _bindCommands();
            _subscribeEvents();
        }
        
        public CharacterSlotViewModel RightCharacterModel { get; set; }

        public CharacterSlotViewModel LeftCharacterModel { get; set; }

        public DialogSlotViewModel DialogModel { get; set; }


        public DelegateCommand SaveCommand { get; set; }

        private void _subscribeEvents()
        {
            mEventAggregator.GetEvent<PhraseDefinitionSelectedEvent>().Subscribe(_phraseDefinitionSelected);
        }        

        private void _bindCommands()
        {
            SaveCommand = new DelegateCommand(_saveExecute, _saveCanExecute);
        }

        private bool _saveCanExecute()
        {
            throw new NotImplementedException();
        }

        private void _saveExecute()
        {
            string error;
            if(!DialogModel.SaveDialog(out error))
            {
                // Output error message
            }
        }

        private void _phraseDefinitionSelected(PhraseDefinitionModel _Model)
        {
            string _errorMessage;
            if(!DialogModel.AddPhraseDefinition(_Model, out _errorMessage))
            {
                // Output error
                mLogger.Error(_errorMessage);
            }
        }
    }
}
