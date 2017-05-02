using System.Collections.Generic;
using System.Drawing;

namespace MapAPI.Models
{
    public class Line
    {
        public string Color { get; set; }
        public bool Loop { get; set; }
        public List<PointF> Points { get; set; }
    }
}
