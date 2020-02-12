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
                }

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

        public void Randomize(Random rnd, ElementDetails[] dna) {
            for (int i = 0; i < RowCount; i++)
                for (int j = 0; j < ColCount; j++) {
                    int pick = rnd.Next(dna.Length);
                    Element element = new Element(dna[pick]);
                    Data[(i * ColCount) + j] = element;
                    
                }
            Data[((RowCount / 2) * ColCount) + (ColCount / 2)] = new Element(Element.Player);
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
                }
            }
            
            return true;
        }



        public void Dump(bool singleLine = false) {
            for (int i = 0; i < RowCount; i++) {
                for (int j = 0; j < ColCount; j++) {
                    Element d = Data[(i * ColCount) + j];
                    if (d == null) {
                        Console.Write(Element.Space.Symbols[DirectionType.Undefined]);
                        continue;
                    }
                    Console.Write(d.Details.Symbols[d.Look]);
                }
                if (!singleLine) Console.WriteLine();
            }
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
