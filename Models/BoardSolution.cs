using System.Collections.Generic;
using GenDash.Engine;

namespace GenDash.Models {
    class BoardSolution(byte width, byte height, string data) : Board(width, height, data) {
        public ulong Hash { get; set; }
        public List<Fold> Solution { get; set; }
        public int IdleFold { get; set; }
    }
}
