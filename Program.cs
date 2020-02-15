using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace GenDash {
    class BoardData {
        public ulong Hash { get; set; }
    }
  
    class Program {

        static void Main(string[] args) {
            int seed = int.MaxValue;
            int maxNoMove = 10;
            int minMove = 15;
            int maxMove = 50;
            int maxSolutionSeconds = 600;
            string xmlDatabase = "GenDashDB.xml";
            try {
                for (int i = 0; i < args.Length; i++) {
                    if (args[i].Equals("-seed", StringComparison.OrdinalIgnoreCase)) {
                        seed = int.Parse(args[++i]);
                    } else
                    if (args[i].Equals("-maxempty", StringComparison.OrdinalIgnoreCase)) {
                        maxNoMove = int.Parse(args[++i]);
                    } else
                    if (args[i].Equals("-minmove", StringComparison.OrdinalIgnoreCase)) {
                        minMove = int.Parse(args[++i]);
                    } else
                    if (args[i].Equals("-maxmove", StringComparison.OrdinalIgnoreCase)) {
                        maxMove = int.Parse(args[++i]);
                    } else
                    if (args[i].Equals("-maxtime", StringComparison.OrdinalIgnoreCase))
                    {
                        maxSolutionSeconds = int.Parse(args[++i]);
                    } else
                    if (args[i].Equals("-xmldatabase", StringComparison.OrdinalIgnoreCase))
                    {
                        xmlDatabase = args[++i];
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return;
            }
            if (seed == int.MaxValue) seed = DateTime.Now.Millisecond;
            Random rnd = new Random(seed);

            var currentDirectory = Directory.GetCurrentDirectory();
            var filepath = Path.Combine(currentDirectory, xmlDatabase);
            XElement puzzledb;
            if (File.Exists(filepath)) {
                puzzledb = XElement.Load(filepath);
            } else {
                puzzledb = new XElement("GenDash");                
            }
            IEnumerable<XElement> boardsNode = puzzledb.Descendants("Boards"); 
            List<BoardData> records = (
                from puzzle in boardsNode.Descendants("Board")
                select new BoardData() {
                    Hash = (ulong)puzzle.Element("Hash"),
                }
            ).ToList<BoardData>();
            Console.WriteLine($"Boards loaded from {xmlDatabase} : {records.Count()}");

            IEnumerable<XElement> patternsNode = puzzledb.Descendants("Patterns");
            List<PatternData> patterns = (
                from p in patternsNode.Descendants("Pattern")
                select new PatternData() {
                    MinWidth = (int)p.Element("MinWidth"),
                    MinHeight = (int)p.Element("MinHeight"),
                    MaxWidth = (int)p.Element("MaxWidth"),
                    MaxHeight = (int)p.Element("MaxHeight"),
                    Start = new PointFloat() {
                        X = (float)p.Element("Start").Element("X"),
                        Y = (float)p.Element("Start").Element("Y"),
                    },
                    DNA = (string)p.Element("DNA"),
                    Commands = (
                        from c in p.Element("Commands").Elements("Command")                        
                        select new PatternCommmand {
                            From = new PointFloat() {
                                X = (float)c.Element("From").Element("X"),
                                Y = (float)c.Element("From").Element("Y"),
                            },
                            To = new PointFloat() {
                                X = (float)c.Element("To").Element("X"),
                                Y = (float)c.Element("To").Element("Y"),
                            },
                            Element = Element.CharToElementDetails(char.Parse(c.Element("Element").Value)),
                            Type = (string)c.Element("Type")
                        }       
                    ).ToList<PatternCommmand>()
                }
            ).ToList<PatternData>();
            Console.WriteLine($"Patterns loaded from {xmlDatabase} : {patterns.Count()}");
            do {
                while (!Console.KeyAvailable) {
                    PatternData pattern = patterns.ElementAt(rnd.Next(patterns.Count()));
                    List<ElementDetails> newdna = new List<ElementDetails>();
                    
                    char[] chrs = pattern.DNA.ToCharArray();
                    for (int i = 0; i < chrs.Length; i++) {
                        char c = chrs[i];
                        newdna.Add(Element.CharToElementDetails(c));
                    }
                    ElementDetails[] dna = newdna.ToArray();

                    Board board = new Board((byte)rnd.Next(pattern.MinWidth, pattern.MaxWidth), (byte)rnd.Next(pattern.MinWidth, pattern.MaxWidth));
                    board.Randomize(rnd, dna);
                    board.ApplyPattern(pattern);
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
