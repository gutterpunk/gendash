using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GenDash {
    class BoardData {
        public ulong Hash { get; set; }
    }
    class RejectData {
        public ulong Hash { get; set; }
    }
  
    class Program {

        static void Main(string[] args) {
            int seed = int.MaxValue;
            int maxNoMove = 10;
            int minMove = 15;
            int maxMove = 75;
            int idleFold = 5;
            int maxSolutionSeconds = 600;
            int cpu = Environment.ProcessorCount - 1;
            string xmlDatabase = "GenDashDB.xml";
            string patternDatabase = null;
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
                    if (args[i].Equals("-idle", StringComparison.OrdinalIgnoreCase))
                    {
                        idleFold = int.Parse(args[++i]);
                    } else
                    if (args[i].Equals("-xmldatabase", StringComparison.OrdinalIgnoreCase))
                    {
                        xmlDatabase = args[++i];
                    } else
                    if (args[i].Equals("-patterns", StringComparison.OrdinalIgnoreCase))
                    {
                        patternDatabase = args[++i];
                    } else
                    if (args[i].Equals("-cpu", StringComparison.OrdinalIgnoreCase))
                    {
                        cpu = int.Parse(args[++i]);
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return;
            }            
            if (patternDatabase == null) patternDatabase = xmlDatabase;
            if (seed == int.MaxValue) seed = DateTime.Now.Millisecond;
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

            List<RejectData> rejects = (
                from puzzle in puzzledb.Descendants("Rejects").Descendants("Reject")
                select new RejectData() {
                    Hash = (ulong)puzzle.Element("Hash"),
                }
            ).ToList<RejectData>();

            Console.WriteLine($"Boards loaded from {xmlDatabase} : {records.Count()}");
            XElement patternsdb = puzzledb;
            if (xmlDatabase != patternDatabase) {
                var pfilepath = Path.Combine(currentDirectory, patternDatabase);
                if (!File.Exists(pfilepath)) {
                    throw new FileNotFoundException(pfilepath);
                }
                patternsdb = XElement.Load(pfilepath);
            }
            IEnumerable<XElement> patternsNode = patternsdb.Descendants("Patterns");
            List<PatternData> patterns = (
                from p in patternsNode.Descendants("Pattern")
                select new PatternData() {
                    MinWidth = (int)p.Element("MinWidth"),
                    MinHeight = (int)p.Element("MinHeight"),
                    MaxWidth = (int)p.Element("MaxWidth"),
                    MaxHeight = (int)p.Element("MaxHeight"),
                    MobRatio = (p.Element("MobRatio") != null)? (float)p.Element("MobRatio"): 0,
                    Start = (p.Element("Start") != null)? new PointFloat() {
                        X = (float)p.Element("Start").Element("X"),
                        Y = (float)p.Element("Start").Element("Y"),
                    } : null,
                    Exit =  (p.Element("Exit") != null)? new PointFloat() {
                        X = (float)p.Element("Exit").Element("X"),
                        Y = (float)p.Element("Exit").Element("Y"),
                    } : null,
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
            Console.WriteLine($"Patterns loaded from {patternDatabase} : {patterns.Count()}");
            Random rnd = new Random(seed);
            int id = 1;
            Task[] tasks = new Task[cpu];
            using (CancellationTokenSource source = new CancellationTokenSource()) {
                do {
                    while (!Console.KeyAvailable) {

                        for (int i = 0; i < cpu; i ++) {
                            if (tasks[i] == null) {
                                Console.WriteLine($"New thread, Id {id}");
                                Worker worker = new Worker();
                                Task t = Task.Factory.StartNew(() => worker.Work(id, rnd, puzzledb, filepath, records, patterns, rejects, maxNoMove, minMove, maxMove, idleFold, maxSolutionSeconds),
                                    source.Token);
                                tasks[i] = t;
                                id++;
                            }
                        }
                        try {
                            Task.WaitAny(tasks);
                            foreach (Task t in tasks) {
                                if (t.IsCompleted) {
                                    for (int i = 0; i < cpu; i ++) {
                                        if (tasks[i] == t) {
                                            tasks[i] = null;
                                        }
                                    }
                                }
                            }

                        } catch (AggregateException ae) {
                            Console.WriteLine("One or more exceptions occurred: ");
                            foreach (var ex in ae.Flatten().InnerExceptions)
                                Console.WriteLine("   {0}", ex.Message);
                        }           
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                source.Cancel();
            }
        }
    }
    class Worker {
        public void Work(int id, Random rnd, 
            XElement puzzledb,
            string filepath,
            List<BoardData> records,
            List<PatternData> patterns,
            List<RejectData> rejects,
            int maxNoMove,
            int minMove,
            int maxMove,
            int idleFold,
            int maxSolutionSeconds) {

            List<ElementDetails> newdna = new List<ElementDetails>();
            PatternData pattern = patterns.ElementAt(rnd.Next(patterns.Count()));
            char[] chrs = pattern.DNA.ToCharArray();
            for (int i = 0; i < chrs.Length; i++) {
                char c = chrs[i];
                newdna.Add(Element.CharToElementDetails(c));
            }
            ElementDetails[] dna = newdna.ToArray();

            Board board = new Board((byte)rnd.Next(pattern.MinWidth, pattern.MaxWidth), (byte)rnd.Next(pattern.MinHeight, pattern.MaxHeight));
            board.Randomize(rnd, pattern, dna);
            Board original = new Board(board);
            ulong hash = original.FNV1aHash();
            BoardData existing = records.Where(x => x.Hash == hash).FirstOrDefault();
            if (existing != null)
            {
                Console.WriteLine($"(Task {id}) Board already exists (hash: {hash}).");
                return;
            }
            RejectData rejected = rejects.Where(x => x.Hash == hash).FirstOrDefault();
            if (rejected != null)
            {
                Console.WriteLine($"(Task {id}) Board already rejected (hash: {hash}).");
                return;
            }

            for (int i = 0; i < idleFold; i ++) {
                board.Fold();
            }
            board.Place(new Element(Element.Player), board.StartY, board.StartX);

            Console.WriteLine($"(Task {id}) Origin:");
            original.Dump();
            Console.WriteLine($"\n(Task {id}) Idle folded:");
            board.Dump();
            Solver solver = new Solver();
            Solution s = solver.Solve(id, board, new TimeSpan(0, 0, maxSolutionSeconds), maxMove, 1f);
            if (s != null && s.Bound < minMove) {
                Console.WriteLine($"(Task {id}) Move under the minimum ({minMove}), discarding.");
                puzzledb.Element("Rejects").Add(
                    new XElement("Reject",
                    new XElement("Hash", hash),
                    new XElement("Reason", "MinMove")
                ));
                lock(puzzledb) {
                    puzzledb.Save(filepath);
                }
                lock(rejects) {
                    rejects.Add(new RejectData{ 
                        Hash = hash
                    });
                }
                s = null;
            } else 
            if (s == null && solver.LastSearchResult == Solver.TIMEDOUT) {
                Console.WriteLine($"(Task {id}) Timeout, couldn't verify board. Discarding.");
                puzzledb.Element("Rejects").Add(
                    new XElement("Reject",
                    new XElement("Hash", hash),
                    new XElement("Reason", "Timeout with no solution"),
                    new XElement("Data", original.ToString())
                ));
                lock(puzzledb) {
                    puzzledb.Save(filepath);
                }
                lock(rejects) {
                    rejects.Add(new RejectData{ 
                        Hash = hash
                    });
                }
                s = null;
            } else
            if (s == null) {
                Console.WriteLine($"(Task {id}) No Solution found. Discarding.");
                puzzledb.Element("Rejects").Add(
                    new XElement("Reject",
                    new XElement("Hash", hash),
                    new XElement("Reason", "Unsolveable")
                ));
                lock(puzzledb) {
                    puzzledb.Save(filepath);
                }
                lock(rejects) {
                    rejects.Add(new RejectData{ 
                        Hash = hash
                    });
                }
            }

            if (s != null) {
                Console.WriteLine($"(Task {id}) Trying to find better solutions.");
                float first = s.Bound;
                do
                {
                    Solution better = solver.Solve(id, board, new TimeSpan(0, 0, maxSolutionSeconds), s.Bound, (s.Bound - 1) / first);
                    if (better != null && better.Bound < s.Bound) {
                        Console.WriteLine($"(Task {id}) Better solution found: {better.Bound}");
                        s = better;
                        if (s.Bound < minMove) {
                            Console.WriteLine($"(Task {id}) Move under the minimum ({minMove}), discarding.");
                            s = null;
                            break;
                        }
                    } else
                        if (better == null && solver.LastSearchResult == Solver.TIMEDOUT) {
                            Console.WriteLine($"(Task {id}) Timeout, couldn't verify board. Discarding.");
                            puzzledb.Element("Rejects").Add(
                                new XElement("Reject",
                                new XElement("Hash", hash),
                                new XElement("Width", original.ColCount),
                                new XElement("Height", original.RowCount),
                                new XElement("StartX", original.StartX),
                                new XElement("StartY", original.StartY),
                                new XElement("ExitX", original.ExitX),
                                new XElement("ExitY", original.ExitY),
                                new XElement("Reason", "Timeout while looking for a better solution"),
                                new XElement("Data", original.ToString())
                            ));
                            lock(puzzledb) {
                                puzzledb.Save(filepath);
                            }
                            lock(rejects) {
                                rejects.Add(new RejectData{ 
                                    Hash = hash
                                });
                            }                            
                            s = null;
                            break;
                        } else {
                            Console.WriteLine($"(Task {id}) No better solution, bailing.");
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
                    puzzledb.Element("Boards").Add(new XElement("Board",
                        new XElement("Hash", original.FNV1aHash()),
                        new XElement("Width", original.ColCount),
                        new XElement("Height", original.RowCount),
                        new XElement("Par", steps),
                        new XElement("StartX", original.StartX),
                        new XElement("StartY", original.StartY),
                        new XElement("ExitX", original.ExitX),
                        new XElement("ExitY", original.ExitY),
                        new XElement("Data", s.Path[0].ToString()),
                        solution
                    ));
                    lock(puzzledb) {
                        puzzledb.Save(filepath);
                    }
                    lock(records) {
                        records.Add(new BoardData {
                            Hash = hash
                        });
                    }
                    Console.WriteLine($"(Task {id}) Puzzle added to DB");
                }
            } 
        }

    }
}
