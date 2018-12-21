using AutoUpdaterDotNET;
using DialogGenerator.Core;
using DialogGenerator.Utilities;
using System;
using System.Windows;
using System.Windows.Threading;

namespace DialogGenerator.Handlers
{
    public class UpdatesHandler
    {
        private ILogger mLogger;
        private IMessageDialogService mMessageDialogService;
        private readonly DispatcherTimer mcTimer;

        public UpdatesHandler(ILogger logger,IMessageDialogService _messageDialogService)
        {
            mLogger = logger;
            mMessageDialogService = _messageDialogService;

            mcTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(ApplicationData.Instance.CheckForUpdateInterval) };
            mcTimer.Tick += _mcTimer_Tick;
            AutoUpdater.CheckForUpdateEvent += _autoUpdater_CheckForUpdateEvent;
            AutoUpdater.ReportErrors = true;

            //mcTimer.Start();
        }

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
                        _dialogResult = await mMessageDialogService.ShowMessage(
                            "Update Available",
                            $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.");
                    }
                    else
                    {
                        _dialogResult = await mMessageDialogService.ShowOKCancelDialogAsync(
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
                            await mMessageDialogService.ShowMessage(exception.GetType().ToString(), exception.Message);
                        }
                    }
                }
                else
                {
                    await mMessageDialogService.ShowMessage(@"No update available", @"There is no update available please try again later.");
                }
            }
            else
            {
                await mMessageDialogService.ShowMessage(
                    @"Update check failed"
                    , @"There is a problem reaching update server please check your internet connection and try again later.");
            }
        }

        public void CheckForUpdates()
        {
            //AutoUpdater.Start(ApplicationData.Instance.URLToUpdateFile);
        }
    }
}
