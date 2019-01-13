using System.Threading.Tasks;
using System.Windows.Media.Converters;
using DialogGenerator.Core; 
using System;

namespace DialogGenerator.CharacterSelection.Data
{
    public class BLE_Message
    {
        // the +1 is for the sequence number at the end of the RSSI values for each radio
        public int[] msgArray = new int[ApplicationData.Instance.NumberOfRadios + 1];

        public BLE_Message DeepCopy()
        {
            BLE_Message other = (BLE_Message) this.MemberwiseClone();
            other.msgArray = new int[ApplicationData.Instance.NumberOfRadios + 1];
            Array.Copy(this.msgArray, other.msgArray, other.msgArray.Length );
            return other; 
        }
    }
    public interface IBLEDataProvider
    {
        BLE_Message GetMessage();
        Task StartReadingData();
        void StopReadingData();
    }
}
