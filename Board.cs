using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GenDash {
   
    public class FoldMovement {
        public FoldMovement(Element element, int fromRow, int fromCol, int toRow, int toCol) {

        }
    }
    public class Board {
        private static readonly ulong fnvOffset = 14695981039346656037;
        private static readonly ulong fnvPrime = 1099511628211;


        public Board Previous { get; set; }
        public Board Head { get; set; }
        public int InputX { get; set; }
        public int InputY { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int ExitX { get; set; }
        public int ExitY { get; set; }
        public bool Grabbing { get; set; }
        public int LastFoldImportant { get; set; }
        public byte RowCount { get; }
        public byte ColCount { get; }
        public Element[] Data { get; }

        private readonly Element borderElement = new Element(Element.Steel);
        private List<FoldMovement> movements = new List<FoldMovement>();        

        public Board(byte width, byte height) {
            RowCount = height;
            ColCount = width;
            Data = new Element[RowCount * ColCount];
        }
        public Board(byte width, byte height, string data) {
            RowCount = height;
            ColCount = width;
            Data = new Element[RowCount * ColCount];
            char[] carray = data.ToCharArray();
            for (int i = 0; i < RowCount; i++)
                for (int j = 0; j < ColCount; j++) {
                    ElementDetails details = Element.CharToElementDetails(carray[(i * ColCount) + j]);
                    Direction dir = Element.CharToFacing(carray[(i * ColCount) + j]);
                    Element e = new Element(details, dir);
                    if (e == null) continue;
                    Data[(i * ColCount) + j] = new Element(e.Details);
                    Data[(i * ColCount) + j].Look = e.Look;
                }            
        }
        public Board(Board clone) {
            RowCount = clone.RowCount;
            ColCount = clone.ColCount;
            Data = new Element[RowCount * ColCount];
            for (int i = 0; i < RowCount; i++)
                for (int j = 0; j < ColCount; j++) {
                    Element e = clone.Data[(i * ColCount) + j];
                    if (e == null) continue;
                    Data[(i * ColCount) + j] = new Element(e.Details);
                    Data[(i * ColCount) + j].Look = e.Look;
                    Data[(i * ColCount) + j].Falling = e.Falling;
                }
            ExitX = clone.ExitX;
            ExitY = clone.ExitY;
            StartX = clone.StartX;
            StartY = clone.StartY;
        }
        public string NameMove() {
            StringBuilder moveName = new StringBuilder();
            if (Grabbing) moveName.Append("Grab");
            if (InputX == -1 && InputY == 0) moveName.Append("Left");
            if (InputX == 1 && InputY == 0) moveName.Append("Right");
            if (InputX == 0 && InputY == -1) moveName.Append("Up");
            if (InputX == 0 && InputY == 1) moveName.Append("Down");
            if (InputX == 0 && InputY == 0) moveName.Append("Stationary");
            return moveName.ToString();
        }
        public void SetMove(string move) {
            Grabbing = false;
            if (move.StartsWith("grab", StringComparison.OrdinalIgnoreCase)) {
                Grabbing = true;
                move = move.Substring(4);
            }
            move = move.ToLowerInvariant();
            InputX = 0;
            InputY = 0;
            switch (move) {
                case "left": InputX = -1; break;
                case "right": InputX = 1; break;
                case "up": InputY = -1; break;
                case "down": InputY = 1; break;
            }
        }
        public void Randomize(Random rnd, PatternData pattern, ElementDetails[] dna) {
            ElementDetails[] nonMobs = Array.FindAll(dna, x => !x.Mob);
            for (int i = 0; i < RowCount; i++) {
                for (int j = 0; j < ColCount; j++) {
                    int pick = rnd.Next(nonMobs.Length);
                    Element element = new Element(nonMobs[pick]);
                    Data[(i * ColCount) + j] = element;                    
                }
            }
            ElementDetails[] mobs = Array.FindAll(dna, x => x.Mob);
            int mobCount = (int)Math.Round((RowCount * ColCount) * pattern.MobRatio);
            for (int i = 0; i < mobCount; i ++) {
                int mx, my;
                do {
                    mx = rnd.Next(ColCount);
                    my = rnd.Next(RowCount);
                    Element under = GetElementAt(my, mx);
                    if (under != null && !under.Details.Mob) break;
                } while (true);
                int pick = rnd.Next(mobs.Length);
                Element spawn = new Element(mobs[pick]);
                Place(spawn, my, mx);
            }
            foreach (PatternCommmand command in pattern.Commands) {
                int fx = (int)Math.Round(command.From.X * ColCount);
                int fy = (int)Math.Round(command.From.Y * RowCount);
                int tx = (int)Math.Round(command.To.X * ColCount);
                int ty = (int)Math.Round(command.To.Y * RowCount);

                if (command.Type.ToLower() == "line") {
                    if (ty - fy < tx - fx) {
                        float slopex = (ty - fy != 0) ? 1f / (float)(ty - fy) : 0f;
                        float row = (float)fy;
                        for (int i = fx; i < tx; i ++) {
                            Place(new Element(command.Element), (int)Math.Round(row), i);
                            row += slopex;
                        }
                    } else {
                        float slopey = (tx - fx != 0) ? 1f / (float)(tx - fx) : 0f;
                        float col = (float)fx;
                        for (int i = fy; i < ty; i ++) {
                            Place(new Element(command.Element), i, (int)Math.Round(col));
                            col += slopey;
                        }
                    }
                } else {
                    if (command.Type.ToLower() == "rectangle") {
                        for (int i = fx; i < tx; i ++) {
                            Place(new Element(command.Element), fy, i);
                            Place(new Element(command.Element), ty, i);
                        }
                        for (int i = fy + 1; i < ty - 1; i ++) {
                            Place(new Element(command.Element), i, fx);
                            Place(new Element(command.Element), i, tx);
                        }
                      
                    }
                }
            }

            int px = rnd.Next(ColCount);
            int py = rnd.Next(RowCount);
            if (pattern.Start != null) {
                px = (int)Math.Round(pattern.Start.X * ColCount);
                py = (int)Math.Round(pattern.Start.Y * RowCount);
            }
            StartX = px;
            StartY = py;
            Place(new Element(Element.Steel), py, px);
            
            do {
                px = rnd.Next(ColCount);
                py = rnd.Next(RowCount);
                if (pattern.Exit != null) {
                    px = (int)Math.Round(pattern.Exit.X * ColCount);
                    py = (int)Math.Round(pattern.Exit.Y * RowCount);
                }
                Element under = GetElementAt(py, px);
                if (under.Details != null && under.Details == Element.Player) {
                    continue;
                }
                ExitX = px;
                ExitY = py;
                Place(new Element(Element.Steel), py, px);
                break;
            } while (true);

        }

        public void FoldSuccessors(List<Board> successors) {
            Board cloned;
            for (int i = 0; i < 2; i++) {
                cloned = new Board(this) {
                    InputX = 0,
                    InputY = 1,
                    Grabbing = i == 1
                };
                if (cloned.Fold()) successors.Add(cloned);

                cloned = new Board(this) {
                    InputX = -1,
                    InputY = 0,
                    Grabbing = i == 1
                };
                if (cloned.Fold()) successors.Add(cloned);

                cloned = new Board(this) {
                    InputX = 0,
                    InputY = -1,
                    Grabbing = i == 1
                };
                if (cloned.Fold()) successors.Add(cloned);

                cloned = new Board(this) {
                    InputX = 1,
                    InputY = 0,
                    Grabbing = i == 1
                };
                if (cloned.Fold()) successors.Add(cloned);
            }
            //if (successors.Count == 0) {
                cloned = new Board(this) {
                    InputX = 0,
                    InputY = 0,
                    Grabbing = false
                };
                if (cloned.Fold()) successors.Add(cloned);

            //}
        }
        public bool Fold() {
            for (int i = 0; i < RowCount; i++) {
                for (int j = 0; j < ColCount; j++) {
                    Element e = Data[(i * ColCount) + j];
                    if (e == null) continue;
                    e.Scanned = false;
                }
            }
            bool nomove = true;
            LastFoldImportant = 0;
            for (int i = 0; i < RowCount; i++) {
                for (int j = 0; j < ColCount; j++) {
                    Element e = Data[(i * ColCount) + j];
                    if (e == null) continue;
                    if (e.Scanned) continue;

                    bool moved = e.Fold(this, i, j);
                    if (moved) {
                        if (e.Details.Important) LastFoldImportant++;
                        nomove = false;
                    } else {
                        if (e.Details == Element.Player) {
                            InputX = 0;
                            InputY = 0;
                            Grabbing = false;
                        }
                    }
                }
            }
            return !nomove;
        }
        public void Place(Element element, int row, int col) {
            Data[(row * ColCount) + col] = element;
        }
        public void AddMove(FoldMovement movement) {
            movements.Add(movement);
        }

        public Element GetElementAt(int row, int col) {
            if (row < 0 || col < 0) return borderElement;
            if (row >= RowCount || col >= ColCount) return borderElement;

            return Data[(row * ColCount) + col];
        }

        public bool Compare(Board other) {
            if (other.RowCount != RowCount) return false;
            if (other.ColCount != ColCount) return false;
            for (int i = 0; i < RowCount; i++) {
                for (int j = 0; j < ColCount; j++) {
                    Element e1 = Data[(i * ColCount) + j];
                    Element e2 = other.Data[(i * ColCount) + j];
                    if (e1 == null && e2 != null) return false;
                    if (e1 != null && e2 == null) return false;
                    if (e1 == null && e2 == null) continue;
                    if (e1.Details != e2.Details) return false;
                    if (e1.Falling != e2.Falling) return false;
                }
            }
            
            return true;
        }



        public void Dump(bool singleLine = false) {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < RowCount; i++) {
                for (int j = 0; j < ColCount; j++) {
                    Element d = Data[(i * ColCount) + j];
                    if (d == null) {
                        b.Append(Element.Space.Symbols[DirectionType.Undefined]);
                        continue;
                    }
                    //if (d.Details == Element.Diamond && d.Falling) { //DEBUG
                    //    b.Append('+');
                    //}
                    //else
                        b.Append(d.Details.Symbols[d.Look]);
                }
                if (!singleLine) b.Append(Environment.NewLine);
            }
            Console.WriteLine(b.ToString());
    
        }
        public override string ToString() {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < RowCount; i++) {
                for (int j = 0; j < ColCount; j++) {
                    Element d = Data[(i * ColCount) + j];
                    if (d == null) {
                        b.Append(Element.Space.Symbols[DirectionType.Undefined]);
                        continue;
                    }
                    b.Append(d.Details.Symbols[d.Look]);
                }
            }
            return b.ToString();
        }

        public ulong FNV1aHash()
        {
            ulong hash = fnvOffset;
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                writer.Write(ColCount);
                writer.Write(RowCount);
                writer.Write(Encoding.ASCII.GetBytes(ToString()));
                writer.BaseStream.Position = 0;

                for (int i = 0; i < writer.BaseStream.Length; i ++)
                {
                    hash ^= (byte)writer.BaseStream.ReadByte();
                    hash *= fnvPrime;
                }
            }
            return hash;
        }
    }
}
