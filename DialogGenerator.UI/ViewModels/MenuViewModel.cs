using DialogGenerator.Core;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Views.Services;
using Prism.Commands;
using Prism.Mvvm;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class MenuViewModel:BindableBase
    {
        #region - fields -

        private ILogger mLogger;
        private IMessageDialogService mMessageDialogService;

        #endregion

        #region - constructor -

        public MenuViewModel(ILogger logger,IMessageDialogService _messageDialogService)
        {
            mLogger = logger;
            mMessageDialogService = _messageDialogService;
            _bindCommands();
        }

        #endregion

        #region - commands -

        public ICommand ReadTutorialCommand { get; set; }
        public ICommand AboutToys2LifeCommand { get; set; }
        public ICommand OpenSettingsDialogCommand { get; set; }

        #endregion

        #region - private functions -

        private void _bindCommands()
        {
            ReadTutorialCommand = new DelegateCommand(_onReadTutorial_Execute);
            AboutToys2LifeCommand = new DelegateCommand(_onAboutToys2Life_Execute);
            OpenSettingsDialogCommand = new DelegateCommand(_onOpenSettingsDialog_Execute);
        }

        private async void _onOpenSettingsDialog_Execute()
        {
            await mMessageDialogService.ShowDedicatedDialogAsync<int?>(new SettingsDialog());
        }

        private void _onAboutToys2Life_Execute()
        {
            Process.Start(ApplicationData.Instance.WebsiteUrl);
        }

        private void _onReadTutorial_Execute()
        {
            Process.Start(Path.Combine(ApplicationData.Instance.TutorialDirectory, ApplicationData.Instance.TutorialFileName));
        }

        #endregion
    }
}
