 using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GenDash {
    [Serializable]
    public class Point {
        public int X { get; set; }
        public int Y { get; set; }
    }
    [Serializable]
    public class PointFloat {
        public float X { get; set; }
        public float Y { get; set; }
    }
}