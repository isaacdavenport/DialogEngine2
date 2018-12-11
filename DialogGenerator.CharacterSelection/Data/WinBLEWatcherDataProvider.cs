using DialogGenerator.Core;
using System;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace DialogGenerator.CharacterSelection.Data
{
    public class WinBLEWatcherDataProvider:IBLEDataProvider
    {
        #region - fields -
        
        private ILogger mLogger;
        private BluetoothLEAdvertisementWatcher mWatcher;
        private string mMessage;
        private bool mNewDataAvailable;

        #endregion

        #region - constructor -

        public WinBLEWatcherDataProvider(ILogger logger)
        {
            mLogger = logger;

            mWatcher = new BluetoothLEAdvertisementWatcher();

            mWatcher.Received += _mWatcher_Received;
            mWatcher.ScanningMode = BluetoothLEScanningMode.Passive;  // no need to send scan request packets, as our radios provide no scan response data
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
                    mNewDataAvailable = true;
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
            if (mNewDataAvailable)
            {
                mNewDataAvailable = false;
                return mMessage;
            }
            return null;
        }

        public object StartReadingData()
        {
            mWatcher.Start();

            return null;
        }

        public void StopReadingData()
        {
            mWatcher.Stop();
        }
    }
}
