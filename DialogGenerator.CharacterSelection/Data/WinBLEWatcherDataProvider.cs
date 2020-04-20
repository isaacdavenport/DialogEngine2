using DialogGenerator.Core;
using System;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DialogGenerator.CharacterSelection.Data
{
    public class WinBLEWatcherDataProvider:IBLEDataProvider
    {
        #region - fields -
        
        private ILogger mLogger;
        private BluetoothLEAdvertisementWatcher mWatcher;
        private BLE_Message mMessage = new BLE_Message();
        private int mBLETotalMessageCount = 0;
        private Queue<BLE_Message> mMessageQueue = new Queue<BLE_Message>();

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
            // We use the manufacturer ID for the first two bytes after the FF as per BLE spec, 
            // once we have packet format size check, look for FF Manf Type data field
            // current format example 0x0201040AFF00FF000000D5FF0036
            // first segment len 2, type 1, value of flags 0x04
            // second segment len 10 decimal, type 0xFF, value manf ID 00FF followed by six 
            // RSSI bytes (including an FF to mark the sender) and seq ID byte

            try
            {
                var sections = args.Advertisement.DataSections;

                if (ApplicationData.Instance.IgnoreRadioSignals)
                    return;

                if (sections.Count < 2)
                return;

                uint length = sections[1].Data.Length;
                var _numRadios = ApplicationData.Instance.NumberOfRadios;

                if (!(length == _numRadios + 4))
                    return;   // New format adds 2 byte 0x00FF manf ID and 1 byte filtered recent motion value

                using (var _dataReader = DataReader.FromBuffer(sections[1].Data))
                {
                    var input = new byte[_dataReader.UnconsumedBufferLength];
                    _dataReader.ReadBytes(input);

                    if (length == _numRadios + 4 && (input[0] != 0x00 || input[1] != 0xFF))  // manf ID no good
                        return;

                    //string message;
                    BLE_Message strippedInput = new BLE_Message();  // just RSSIs and seq number

                    // remove the manufacturer ID since it has been checked
                    Array.Copy(input, 2, strippedInput.msgArray, 0, strippedInput.msgArray.Length);                    

                    mMessage = strippedInput.DeepCopy();
                    mBLETotalMessageCount++;
                    mMessageQueue.Enqueue(mMessage);
                    if (mMessageQueue.Count > 1000)
                    {
                        mMessageQueue.Clear();
                    }

                    if (mBLETotalMessageCount % 100 == 0)
                    {
                        mLogger.Info("BLE Total Good Message Count: " + mBLETotalMessageCount + " Current queue size: "
                            + mMessageQueue.Count + "  " + DateTime.Now.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_mWatcher_Received " + ex.Message);
            }
        }

        #endregion

        public BLE_Message GetMessage()
        {
            if (mMessageQueue.Count > 0)
            {
                BLE_Message returnMessage = mMessageQueue.Dequeue().DeepCopy();
                return returnMessage;
            }
            return null;
        }

        public async Task StartReadingData()
        {
            mMessageQueue.Clear();
            mLogger.Info("StartReadingData BLE");
            mWatcher.Start();
            await BetterScanner.StartScanner(0, 29, 29);
        }

        public void StopReadingData()
        {
            mMessageQueue.Clear();
            mLogger.Info("StopReadingData BLE");
            mWatcher.Stop();
            BetterScanner.Cancel();
        }

    }



class BetterScanner
    {
        private static IntPtr handle;

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

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

        /// <summary>
        /// Starts scanning for LE devices.
        /// Example: BetterScanner.StartScanner(0, 29, 29)
        /// </summary>
        /// <param name="scanType">0 = Passive, 1 = Active</param>
        /// <param name="scanInterval">Interval in 0.625 ms units</param>
        /// <param name="scanWindow">Window in 0.625 ms units</param>
        public static Task StartScanner(int scanType, ushort scanInterval, ushort scanWindow)
        {
            return Task.Run(() =>
            {
                BLUETOOTH_FIND_RADIO_PARAM param = new BLUETOOTH_FIND_RADIO_PARAM();
                param.Initialize();
                BluetoothFindFirstRadio(ref param, out handle);
                uint outsize;
                LE_SCAN_REQUEST req = new LE_SCAN_REQUEST { scanType = scanType, scanInterval = scanInterval, scanWindow = scanWindow };
                bool value = DeviceIoControl(handle, 0x41118c, ref req, 8, IntPtr.Zero, 0, out outsize, IntPtr.Zero);
            });
        }

        public static void Cancel()
        {
            CancelIoEx(handle, IntPtr.Zero);
        }
    }

}
