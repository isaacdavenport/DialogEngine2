using AutoUpdaterDotNET;
using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.UI.Views;
using DialogGenerator.Utilities;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Windows;
using System.Windows.Threading;

namespace DialogGenerator.ViewModels
{
    public class ShellViewModel:BindableBase
    {
        #region - fields -

        private readonly DispatcherTimer mcTimer;

        private IRegionManager mRegionManager;
        private IEventAggregator mEventAggregator;

        #endregion

        #region - constructor -

        public ShellViewModel(IRegionManager _regionManager,IEventAggregator _eventAggregator)
        {
            mRegionManager = _regionManager;
            mEventAggregator = _eventAggregator;
            mcTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(ApplicationData.Instance.CheckForUpdateInterval) };

            mcTimer.Tick += _mcTimer_Tick;
            AutoUpdater.CheckForUpdateEvent += _autoUpdater_CheckForUpdateEvent;
            AutoUpdater.ReportErrors = true;

            _subscribeForEvents();
            mcTimer.Start();
        }

        #endregion

        #region - event handlers -

        private void _mcTimer_Tick(object sender, EventArgs e)
        {
            AutoUpdater.Start(ApplicationData.Instance.URLToUpdateFile);
        }

        private async void _autoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {
                    MessageDialogResult _dialogResult;
                    if (args.Mandatory)
                    {
                        _dialogResult = await MessageDialogService.ShowMessage(
                            "Update Available",
                            $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.");
                    }
                    else
                    {
                        _dialogResult = await MessageDialogService.ShowOKCancelDialogAsync(
                                $@"There is new version {args.CurrentVersion} available. You are using version { args.InstalledVersion}. Do you want to update the application now?"
                                , @"Update Available");
                    }

                    if (_dialogResult == MessageDialogResult.OK)
                    {
                        try
                        {
                            if (AutoUpdater.DownloadUpdate())
                            {
                                Application.Current.Shutdown();
                            }
                        }
                        catch (Exception exception)
                        {
                            await MessageDialogService.ShowMessage(exception.GetType().ToString(),exception.Message);
                        }
                    }
                }
                else
                {
                    await MessageDialogService.ShowMessage(@"No update available",@"There is no update available please try again later.");
                }
            }
            else
            {
                await MessageDialogService.ShowMessage(
                    @"Update check failed"
                    ,@"There is a problem reaching update server please check your internet connection and try again later.");
            }
        }

        #endregion

        #region - private functions -

        private void _navigate(object _navigatePath)
        {
            var parameters = (object[])_navigatePath;

            if (_navigatePath != null)
                mRegionManager.RequestNavigate(parameters[0].ToString(), parameters[1].ToString());
        }

        private void _subscribeForEvents()
        {
            mEventAggregator.GetEvent<OpenCharacterDetailViewEvent>().Subscribe(_onOpenCharacterDetailView);
            mEventAggregator.GetEvent<OpenDialogModelDetailViewEvent>().Subscribe(_onOpenDialogModelDetailView);
        }

        private void _onOpenDialogModelDetailView(string obj)
        {
            var _activeView = mRegionManager.Regions[Constants.ContentRegion].GetView(typeof(DialogModelDetailView).FullName);

            if (_activeView == null)
            {
                mRegionManager.RequestNavigate(Constants.ContentRegion, typeof(DialogModelDetailView).FullName);
            }
        }

        private void _onOpenCharacterDetailView(string obj)
        {
            var _activeView = mRegionManager.Regions[Constants.ContentRegion].GetView(typeof(CharacterDetailView).FullName);

            if(_activeView == null)
            {
                mRegionManager.RequestNavigate(Constants.ContentRegion, typeof(CharacterDetailView).FullName);
            }
        }

        #endregion

        #region - properties -

        public IMessageDialogService MessageDialogService { get; set; }

        #endregion
    }
}
