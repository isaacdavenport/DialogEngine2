using DialogGenerator.Core;
using System;

namespace DialogGenerator.CharacterSelection.Model
{
    public class ReceivedMessage
    {
        public string CharacterPrefix = "XX";
        public DateTime ReceivedTime = DateTime.MinValue;
        public int[] Rssi = new int[ApplicationData.Instance.NumberOfRadios];
        public int SequenceNum = -1;
        public int Motion = -1;
    }
}
