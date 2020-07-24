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
using System.Linq;
using System.Runtime.InteropServices;
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
        IMessageDialogService mMessageDialogService;
        IRegionManager mRegionManager;
        
        public CustomDialogCreatorViewModel(ICharacterDataProvider _CharacterDataProvider,
                                            IDialogModelRepository _DialogModelRepository,
                                            ILogger _Logger,
                                            IEventAggregator _EventAggregator,
                                            IMessageDialogService _MessageDialogService,
                                            IRegionManager _RegionManager)
        {
            mCharacterDataProvider = _CharacterDataProvider;
            mDialogModelRepository = _DialogModelRepository;
            mLogger = _Logger;
            mEventAggregator = _EventAggregator;
            mMessageDialogService = _MessageDialogService;
            mRegionManager = _RegionManager;
            LeftCharacterModel = new CharacterSlotViewModel(_CharacterDataProvider, _EventAggregator, mLogger, 1);
            RightCharacterModel = new CharacterSlotViewModel(_CharacterDataProvider, _EventAggregator, mLogger, 2);
            DialogModel = new DialogSlotViewModel(mDialogModelRepository, mCharacterDataProvider, _EventAggregator, _MessageDialogService);

            _bindCommands();
            _subscribeEvents();
        }        
        
        public CharacterSlotViewModel RightCharacterModel { get; set; }

        public CharacterSlotViewModel LeftCharacterModel { get; set; }

        public DialogSlotViewModel DialogModel { get; set; }


        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand CloseCommand { get; set; }

        private void _subscribeEvents()
        {
            
        }        

        private void _bindCommands()
        {
            SaveCommand = new DelegateCommand(_saveExecute, _saveCanExecute);
            CloseCommand = new DelegateCommand(_closeExecute);
        }

        private void _closeExecute()
        {
            mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("CreateView");
        }

        private bool _saveCanExecute()
        {
            return true;
        }

        private async void _saveExecute()
        {
            string error;
            if (!DialogModel.SaveDialog(out error))
            {
                // Output error message
                await mMessageDialogService.ShowMessage("Wrong Action", error);
            }
            else
            {
                await mMessageDialogService.ShowMessage("Success", string.Format("The dialog {0} successfully saved!", DialogModel.DialogName));
                CloseCommand.Execute();
            }
            
        }
    }
}
