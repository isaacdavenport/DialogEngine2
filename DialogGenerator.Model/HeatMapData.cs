using System;

namespace DialogGenerator.Model
{
    public class HeatMapData
    {
        public int[,] HeatMap { get; set; }
        public DateTime[] LastHeatMapUpdateTime { get; set; }
        public int Character1Index { get; set; }
        public int Character2Index { get; set; }

    }
}
