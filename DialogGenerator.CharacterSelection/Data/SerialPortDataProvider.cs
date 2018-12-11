using DialogGenerator.CharacterSelection.Dialogs;
using DialogGenerator.CharacterSelection.Model.Exceptions;
using DialogGenerator.CharacterSelection.SerialPortDataProviderWorkflow;
using DialogGenerator.Core;
using DialogGenerator.Utilities;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.CharacterSelection.Data
{
    public class SerialPortDataProvider:IBLEDataProvider
    {
        #region - fields -

        private ILogger mLogger;
        private IMessageDialogService mMessageDialogService;
        private string mMessage;
        private SerialPort mSerialPort;
        private SerialPortDataReadingWorkflow mWorkflow;
        private CancellationTokenSource mCancellationTokenSource; 

        #endregion

        #region - constructor -

        public SerialPortDataProvider(ILogger logger,IMessageDialogService _messageDialogService)
        {
            mLogger = logger;
            mMessageDialogService = _messageDialogService;
            mWorkflow = new SerialPortDataReadingWorkflow(() => { });
        }

        #endregion

        #region - private functions -

        private void _configureWorkflow()
        {
            mWorkflow.Configure(States.Waiting)
                .Permit(Triggers.Initialize, States.Init);

            mWorkflow.Configure(States.Init)
                .Permit(Triggers.ProcessSerialPortNameError, States.SerialCOMPortNameError)
                .Permit(Triggers.ProcessUSBDiconnectedError, States.SerialCOMPortNameError)
                .Permit(Triggers.ReadMessage, States.ReadingMessage);

            mWorkflow.Configure(States.SerialCOMPortNameError)
                .Permit(Triggers.Initialize, States.Init)
                .Permit(Triggers.Finish, States.Finished);

            mWorkflow.Configure(States.USBDisconnected)
                .Permit(Triggers.Initialize, States.Init)
                .Permit(Triggers.Finish, States.Finished);

            mWorkflow.Configure(States.ReadingMessage)
                .PermitReentry(Triggers.ReadMessage)
                .Permit(Triggers.Initialize, States.Init);

            mWorkflow.Configure(States.Finished)
                .OnEntry(() =>_finish())
                .Permit(Triggers.Wait, States.Waiting);
        }

        private  void _finish()
        {
            try
            {
                mSerialPort.Close();  // Close() method calls Dispose() se we don't need to call Dispose()
            }
            catch (IOException ex)
            {
                mLogger.Error("_finish " + ex.Message);
            }

            mWorkflow.Fire(Triggers.Wait);
        }
        

        private Triggers _readMessage()
        {
            try
            {
                if (mSerialPort.IsOpen)
                {
                    if (mSerialPort.BytesToRead > 18)
                    {
                        mMessage = mSerialPort.ReadLine();

                        if (mSerialPort.BytesToRead > 1000)
                        {
                            // got behind for some reason
                            mSerialPort.DiscardInBuffer();

                            mLogger.Debug("serial buffer over run.");
                        }
                    }
                }
                else
                {
                    return Triggers.Initialize;
                }
            }
            catch (TimeoutException ex)
            {
                mLogger.Error(ex.Message);
                return Triggers.ReadMessage;
            }
            catch (InvalidOperationException ex)  // port is not open
            {
                mLogger.Error(ex.Message);
                return Triggers.Initialize;
            }

            return Triggers.ReadMessage;
        }

        private async Task<Triggers> _processUSBDisconnectedError()
        {
            var result = await mMessageDialogService.ShowOKCancelDialogAsync("USB disconected.Please check connection and try again.",
                "Error", "Try again", "Finish dialog");

            if (result == MessageDialogResult.OK)
            {
                return Triggers.Initialize;
            }
            else
            {
                StopReadingData();

                return Triggers.Finish;
            }
        }

        private async Task<Triggers> _processSerialCOMPortNameError()
        {
            var dialog = new COMPortErrorDialog();
            var result = await mMessageDialogService.ShowDedicatedDialogAsync<MessageDialogResult>(dialog);

            if (result == MessageDialogResult.OK)
            {
                return Triggers.Initialize;
            }
            else
            {
                return Triggers.Wait;
            }
        }

        private Triggers _initSerial()
        {
            try
            {
                mSerialPort = new SerialPort
                {
                    PortName = ApplicationData.Instance.ComPortName,
                    BaudRate = 460800,
                    Handshake = Handshake.None,
                    ReadTimeout = 500
                };
                mSerialPort.Open();
                mSerialPort.DiscardInBuffer();

                return Triggers.ReadMessage;
            }
            catch (InvalidOperationException ex)  // Instance of SerialPort is already open and wi will redirect for reading messages
            {
                mLogger.Error("InvalidOperationException _initSerial  " + ex.Message);
                return Triggers.ReadMessage;
            }
            catch (ArgumentException ex) // invalid port name (name is not formed as COM + digit)
            {
                mLogger.Error("ArgumentException _initSerial  " + ex.Message);
            }
            catch (IOException ex) // com port doesn't exists (usb is disconnected or not valid COM port name)
            {
                mLogger.Error("IOException _initSerial " + ex.Message);
            }
    
            return Triggers.ProcessSerialPortNameError;        
        }

        #endregion

        #region - public functions -

        public string GetMessage()
        {
            return mMessage;
        }

        public Task StartReadingData()
        {
            return Task.Run(async() =>
            {
                mWorkflow.Fire(Triggers.Initialize);
                mCancellationTokenSource = new CancellationTokenSource();
                Thread.CurrentThread.Name = "SerialPortDataProviderThread";

                do
                {
                    Triggers next;

                    switch (mWorkflow.State)
                    {
                        case States.Init:
                            {
                                next = _initSerial();
                                break;
                            }
                        case States.SerialCOMPortNameError:
                            {
                                next = await _processSerialCOMPortNameError();
                                break;
                            }
                        case States.USBDisconnected:
                            {
                                next = await _processUSBDisconnectedError();
                                break;
                            }
                        case States.ReadingMessage:
                            {
                                next = _readMessage();
                                break;
                            }
                        default:
                            {
                                next = Triggers.Finish;
                                break;
                            }
                    }

                    mWorkflow.Fire(next);
                }
                while (!mCancellationTokenSource.Token.IsCancellationRequested);

                mWorkflow.Fire(Triggers.Finish);
            });
        }

        public void StopReadingData()
        {
            mCancellationTokenSource.Cancel();
        }

        #endregion
    }
}
