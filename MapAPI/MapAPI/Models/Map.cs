using System.Collections.Generic;

namespace MapAPI.Models
{
    public class Map
    {
        public int Id { get; set; }
        public double Ratio { get; set; }
        public List<Line> Lines { get; set; }
    }
}