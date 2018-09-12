using System;

namespace DialogGenerator.DialogEngine.Model
{
    public class HistoricalDialog
    {
        public int Character1;
        public int Character2;
        public bool Completed;
        public int DialogIndex;
        public string DialogName = "";
        public DateTime StartedTime = DateTime.MinValue;
    }
}
