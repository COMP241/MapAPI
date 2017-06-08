namespace MapAPI.Models
{
    public class ConfigFile
    {
        public class WhiteDefinition
        {
            public float Saturation { get; set; }
            public float Brightness { get; set; }
        }

        public class Thresholds
        {
            public float Saturation { get; set; }
            public float Brightness { get; set; }
        }

        public class LineReduction
        {
            public double AngleLimit { get; set; }
            public int InitialCut { get; set; }
        }

        public class PixelCounts
        {
            public int InitialImage { get; set; }
            public int TransformedImage { get; set; }
        }

        public class Config
        {
            public float RectangleShift { get; set; }
            public WhiteDefinition WhiteDefinition { get; set; }
            public int ProcessRegionSize { get; set; }
            public Thresholds Thresholds { get; set; }
            public LineReduction LineReduction { get; set; }
            public int MinLoopSize { get; set; }
            public int MinLoopSizeLower { get; set; }
            public int MinLineLength { get; set; }
            public PixelCounts PixelCounts { get; set; }
        }
    }
}