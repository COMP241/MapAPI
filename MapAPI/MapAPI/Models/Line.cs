using System.Collections.Generic;
using System.Drawing;

namespace MapAPI.Models
{
    public class Line
    {
        public int Color { get; set; }
        public bool Loop { get; set; }
        public List<PointF> Points { get; set; }

        public static class Colors
        {
            public static int Black = 0;
            public static int Red = 1;
            public static int Green = 2;
            public static int Blue = 3;
            public static int Cyan = 4;
            public static int Magenta = 5;
            public static int Yellow = 6;
        }
    }
}