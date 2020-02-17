using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GenDash {
   
    [Serializable]
    public class PatternData {
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public float MobRatio { get; set; }
        public PointFloat Start { get; set; }
        public PointFloat Exit { get; set; }
        public String DNA { get; set; }
        public List<PatternCommmand> Commands { get; set; }
    }  
    [Serializable]
    public class PatternCommmand {
        public virtual ElementDetails Element { get; set; } 
        public PointFloat From { get; set; }
        public PointFloat To { get; set; }
        public string Type { get; set; } 
    }
}