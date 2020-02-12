using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace GenDash {
    class BoardData {
        public ulong Hash { get; set; }
        public string Data { get; set; }
        public string Solution { get; set; }
    }
    class Program {

        private static string dnastr = "........********##dd00<";
        private static int seed = int.MaxValue;
        private static int minCol = 5;
        private static int maxCol = 11;
        private static int minRow = 5;
        private static int maxRow = 11;
        private static int maxEmpty = 10;
        private static int minMove = 15;
        private static int maxMove = 50;
        private static int maxSolutionSeconds = 600;
        static void Main(string[] args) {

            try {
                for (int i = 0; i < args.Length; i++) {
                    if (args[i].Equals("-dna", StringComparison.OrdinalIgnoreCase)) {
                        dnastr = args[i + 1];
                        i++;
                    } else
                    if (args[i].Equals("-seed", StringComparison.OrdinalIgnoreCase)) {
                        seed = int.Parse(args[i + 1]);
                        i++;
                    } else
                    if (args[i].Equals("-mincol", StringComparison.OrdinalIgnoreCase)) {
                        minCol = int.Parse(args[i + 1]);
                        i++;
                    } else
                    if (args[i].Equals("-maxcol", StringComparison.OrdinalIgnoreCase)) {
                        maxCol = int.Parse(args[i + 1]);
                        i++;
                    } else
                    if (args[i].Equals("-minrow", StringComparison.OrdinalIgnoreCase)) {
                        minRow = int.Parse(args[i + 1]);
                        i++;
                    } else
                    if (args[i].Equals("-maxrow", StringComparison.OrdinalIgnoreCase)) {
                        maxRow = int.Parse(args[i + 1]);
                        i++;
                    } else
                    if (args[i].Equals("-maxempty", StringComparison.OrdinalIgnoreCase)) {
                        maxEmpty = int.Parse(args[i + 1]);
                        i++;
                    } else
                    if (args[i].Equals("-minmove", StringComparison.OrdinalIgnoreCase)) {
                        minMove = int.Parse(args[i + 1]);
                        i++;
                    } else
                    if (args[i].Equals("-maxmove", StringComparison.OrdinalIgnoreCase)) {
                        maxMove = int.Parse(args[i + 1]);
                        i++;
                    } else
                    if (args[i].Equals("-maxtime", StringComparison.OrdinalIgnoreCase))
                    {
                        maxSolutionSeconds = int.Parse(args[i + 1]);
                        i++;
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return;
            }
            List<ElementDetails> newdna = new List<ElementDetails>();
            char[] chrs = dnastr.ToCharArray();
            for (int i = 0; i < chrs.Length; i++) {
                char c = chrs[i];
                switch (c)
                {
                    case '.': newdna.Add(Element.Space); break;
                    case '*': newdna.Add(Element.Dirt); break;
                    case '#': newdna.Add(Element.Bricks); break;
                    case '0': newdna.Add(Element.Boulder); break;
                    case 'd': newdna.Add(Element.Diamond); break;
                    case '%': newdna.Add(Element.Steel); break;
                    case '^': 
                    case '<': 
                    case 'v': 
                    case '>': newdna.Add(Element.Firefly); break;
                    case 'M':
                    case 'E':
                    case 'W':
                    case '3': newdna.Add(Element.Butterfly); break;
                }
            }
            ElementDetails[] dna = newdna.ToArray();
                //{ Element.Space, Element.Space, Element.Dirt, Element.Dirt, Element.Space, Element.Space, Element.Dirt, Element.Dirt,
                //Element.Bricks, Element.Diamond, Element.Boulder };
            if (seed == int.MaxValue) seed = DateTime.Now.Millisecond;
            Random rnd = new Random(seed);

            var filename = "GenDashDB.xml";
            var currentDirectory = Directory.GetCurrentDirectory();
            var filepath = Path.Combine(currentDirectory, filename);
            XElement puzzledb = XElement.Load(filepath);
            IEnumerable<BoardData> records =
                from puzzle in puzzledb.Descendants("Board")
                select new BoardData();
            Console.WriteLine(records.Count());

            do {
                while (!Console.KeyAvailable) {
                    Board board = new Board((byte)rnd.Next(minRow, maxRow), (byte)rnd.Next(minCol, maxCol));
                    board.Randomize(rnd, dna);
                    ulong hash = board.FNV1aHash();
                    BoardData existing = records.Where(x => x.Hash == hash).FirstOrDefault();
                    if (existing != null)
                    {
                        Console.WriteLine($"Board already exists (hash: {hash}).");
                        continue;
                    }
                    board.Dump();
                    DateTime until = DateTime.Now + new TimeSpan(0, 0, maxSolutionSeconds);
                    Solver solver = new Solver();
                    Solution s = solver.Solve(board, maxMove, until, 1f);
                    if (s != null && s.Bound < minMove) {
                        Console.WriteLine($"Move under the minimum ({minMove}), discarding.");
                        s = null;
                    } else 
                    if (s == null && until <= DateTime.Now) {
                        Console.WriteLine($"Timeout, couldn't verify board. Discarding.");
                        s = null;
                    }

                    if (s != null) {
                        Console.WriteLine("Trying to find better solutions.");
                        float first = s.Bound;
                        do
                        {
                            until = DateTime.Now + new TimeSpan(0, 0, maxSolutionSeconds);
                            Solution better = solver.Solve(board, s.Bound, until, (s.Bound - 1) / first);
                            if (better != null && better.Bound < s.Bound) {
                                Console.WriteLine($"Better solution found: {better.Bound}");
                                s = better;
                                if (s.Bound < minMove) {
                                    Console.WriteLine($"Move under the minimum ({minMove}), discarding.");
                                    s = null;
                                    break;
                                }
                            } else
                                if (better == null && until <= DateTime.Now) {
                                    Console.WriteLine($"Timeout, couldn't verify board. Discarding.");
                                    s = null;
                                    break;
                                } else {
                                    Console.WriteLine("No better solution, bailing.");
                                    break;
                                }

                        } while (true);
                        if (s != null) {
                            XElement solution = new XElement("Solution");
                            int steps = 0;
                            foreach (Board b in s.Path)
                            {
                                solution.Add(new XElement("Move", b.NameMove()));
                                steps++;
                            }
                            puzzledb.Add(new XElement("Board",
                                new XElement("Width", s.Path[0].ColCount),
                                new XElement("Height", s.Path[0].RowCount),
                                new XElement("ParSteps", steps),
                                new XElement("Hash", s.Path[0].FNV1aHash()),
                                new XElement("Data", s.Path[0].ToString()),
                                solution
                            ));

                            puzzledb.Save(filepath);
                            Console.WriteLine("Puzzle added to DB");

                            //foreach (Board b in s.Path) {
                            //    Console.WriteLine("------------");
                            //    b.Dump();
                            //    Console.WriteLine(b.NameMove());
                            //    Thread.Sleep(200);
                            //}
                        }
                    } else
                        Console.WriteLine($"No solutions found");
                    //int noImportant = maxEmptyMove;
                    //while (board.Fold() && noImportant > 0) {
                    //    if (board.LastFoldImportant == 0) {
                    //        noImportant--;
                    //    } else {
                    //        noImportant = maxEmptyMove;
                    //    }
                    //    Console.Clear();
                    //    board.Dump();
                    //    Thread.Sleep(100);
                    //}
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

    }
}
