using DialogGenerator.Core;
using System;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DialogGenerator.CharacterSelection.Data
{
    public class WinBLEWatcherDataProvider:IBLEDataProvider
    {
        #region - fields -
        
        private ILogger mLogger;
        private BluetoothLEAdvertisementWatcher mWatcher;
        //private BetterScanner bScanner;
        private string mMessage;
        private bool mNewDataAvailable;
        private static List<string> receivedBLE = new List<string>();
        
        #endregion

        #region - constructor -

        public WinBLEWatcherDataProvider(ILogger logger)
        {
            mLogger = logger;

            mWatcher = new BluetoothLEAdvertisementWatcher();

            mWatcher.Received += _mWatcher_Received;
            mWatcher.ScanningMode = BluetoothLEScanningMode.Passive;  // no need to send scan request packets, as our radios provide no scan response data
            //bScanner = new BetterScanner();
            
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

                    receivedBLE.Add(message);
                    if (receivedBLE.Count > 500)
                        receivedBLE.RemoveRange(0, 100);

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

        public Task StartReadingData()
        {
            BetterScanner.StartScanner(0, 29, 29);
            mWatcher.Start();

            return Task.CompletedTask;
        }

        public void StopReadingData()
        {
            mWatcher.Stop();
            // we should probably stop the BetterScanner here
        }

    }



class BetterScanner
    {
        /// <summary>
        /// The BLUETOOTH_FIND_RADIO_PARAMS structure facilitates enumerating installed Bluetooth radios.
        ///   This is used in parallel with the WinBLEWatcherDataProvider to force the scan parameters to more 
        ///   continuous scanning.  The WinBLEWatcherDataProvider only scans about 15% of the time, but windows uses 
        ///   the longer scan times if two objects are scanning ble
        ///   See https://stackoverflow.com/questions/37307301/ble-scan-interval-windows-10/37328965
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct BLUETOOTH_FIND_RADIO_PARAM
        {
            internal UInt32 dwSize;
            internal void Initialize()
            {
                this.dwSize = (UInt32)Marshal.SizeOf(typeof(BLUETOOTH_FIND_RADIO_PARAM));
            }
        }

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">[In] A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Finds the first bluetooth radio present in device manager
        /// </summary>
        /// <param name="pbtfrp">Pointer to a BLUETOOTH_FIND_RADIO_PARAMS structure</param>
        /// <param name="phRadio">Pointer to where the first enumerated radio handle will be returned. When no longer needed, this handle must be closed via CloseHandle.</param>
        /// <returns>In addition to the handle indicated by phRadio, calling this function will also create a HBLUETOOTH_RADIO_FIND handle for use with the BluetoothFindNextRadio function.
        /// When this handle is no longer needed, it must be closed via the BluetoothFindRadioClose.
        /// Returns NULL upon failure. Call the GetLastError function for more information on the error. The following table describe common errors:</returns>
        [DllImport("irprops.cpl", SetLastError = true)]
        static extern IntPtr BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAM pbtfrp, out IntPtr phRadio);

        [StructLayout(LayoutKind.Sequential)]
        private struct LE_SCAN_REQUEST
        {
            internal int scanType;
            internal ushort scanInterval;
            internal ushort scanWindow;
        }

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        ref LE_SCAN_REQUEST lpInBuffer, uint nInBufferSize,
        IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

        /// <summary>
        /// Starts scanning for LE devices.
        /// Example: BetterScanner.StartScanner(0, 29, 29)
        /// </summary>
        /// <param name="scanType">0 = Passive, 1 = Active</param>
        /// <param name="scanInterval">Interval in 0.625 ms units</param>
        /// <param name="scanWindow">Window in 0.625 ms units</param>
        public static void StartScanner(int scanType, ushort scanInterval, ushort scanWindow)
        {
            var thread = new Thread(() =>
            {
                BLUETOOTH_FIND_RADIO_PARAM param = new BLUETOOTH_FIND_RADIO_PARAM();
                param.Initialize();
                IntPtr handle;
                BluetoothFindFirstRadio(ref param, out handle);
                uint outsize;
                LE_SCAN_REQUEST req = new LE_SCAN_REQUEST { scanType = scanType, scanInterval = scanInterval, scanWindow = scanWindow };
                DeviceIoControl(handle, 0x41118c, ref req, 8, IntPtr.Zero, 0, out outsize, IntPtr.Zero);
            });
            thread.Start();
        }
    }

}
