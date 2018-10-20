using DialogGenerator.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace DialogGenerator.CharacterSelection.Data
{
    public class WinBLEWatcherDataProivder:IBLEDataProvider
    {
        #region - fields -
        
        private ILogger mLogger;
        private BluetoothLEAdvertisementWatcher mWatcher;
        private string mMessage;

        #endregion

        #region - constructor -

        public WinBLEWatcherDataProivder(ILogger logger)
        {
            mLogger = logger;

            mWatcher = new BluetoothLEAdvertisementWatcher();

            mWatcher.Received += _mWatcher_Received;
            mWatcher.ScanningMode = BluetoothLEScanningMode.Active;
        }

        #endregion

        #region - event handlers -

        private void _mWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                var sections = args.Advertisement.DataSections;

                if (sections.Count < 2)
                    return;

                using (var _dataReader = DataReader.FromBuffer(sections[1].Data))
                {
                    var input = new byte[_dataReader.UnconsumedBufferLength];
                    _dataReader.ReadBytes(input);

                    string message = BitConverter.ToString(input);

                    if (message.IndexOf("a5", StringComparison.OrdinalIgnoreCase) < 0)
                        return;

                    mMessage = message;
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_mWatcher_Received " + ex.Message);
            }
        }

        #endregion

        public string GetMessage()
        {
            return $"{CharacterSelectionConstants.WIN_BLE}:{mMessage}";
        }

        public async Task StartReadingData()
        {
            // fake task
            await Task.Run(() =>
            {
                Thread.CurrentThread.Name = "StartReadingData";
                mWatcher.Start();
            });
        }

        public void StopReadingData()
        {
            mWatcher.Stop();
        }
    }
}
