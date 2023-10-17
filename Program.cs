using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace GenDash {
    class Fold {
        public string Move { get; set; }
        public string Data { get; set; }
        public string MoveOrder { get; set; }
    }
    class BoardSolution : Board {
        public List<Fold> Solution { get; set; }
        public int IdleFold { get; set; }
        public BoardSolution(byte width, byte height, string data) 
            :base(width, height, data) {
        }        
    }
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
            int minScore = 100;
            int maxMove = 75;
            int idleFold = 5; //NEVER CHANGE THAT!
            int maxSolutionSeconds = 600;
            int cpu = Environment.ProcessorCount - 1;
            string xmlDatabase = "GenDashDB.xml";
            string patternDatabase = null;
            ulong playback = 0;
            int playbackSpeed = 200;
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
                    if (args[i].Equals("-database", StringComparison.OrdinalIgnoreCase))
                    {
                        xmlDatabase = args[++i];
                    } else
                    if (args[i].Equals("-patterns", StringComparison.OrdinalIgnoreCase))
                    {
                        patternDatabase = args[++i];
                    } else
                    if (args[i].Equals("-tasks", StringComparison.OrdinalIgnoreCase))
                    {
                        cpu = int.Parse(args[++i]);
                    } else
                    if (args[i].Equals("-playback", StringComparison.OrdinalIgnoreCase))
                    {
                        playback = ulong.Parse(args[++i]);
                    } else
                    if (args[i].Equals("-playspeed", StringComparison.OrdinalIgnoreCase))
                    {
                        playbackSpeed = int.Parse(args[++i]);
                    }
                    else
                    if (args[i].Equals("-minscore", StringComparison.OrdinalIgnoreCase))
                    {
                        minScore = int.Parse(args[++i]);
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
            if (puzzledb.Element("Boards") == null)
            {
                puzzledb.Add(new XElement("Boards"));
            }
            if (puzzledb.Element("Rejects") == null)
            {
                puzzledb.Add(new XElement("Rejects"));
            }
            IEnumerable<XElement> boardsNode = puzzledb.Descendants("Boards"); 
            List<BoardData> records = (
                from puzzle in boardsNode.Descendants("Board")
                select new BoardData() {
                    Hash = (ulong)puzzle.Element("Hash"),
                }
            ).ToList<BoardData>();
            Console.CursorVisible = false;
            if (playback > 0) {
                var toPlayback = (
                    from puzzle in boardsNode.Descendants("Board")
                    where (ulong)puzzle.Element("Hash") == playback
                    select new BoardSolution((byte)(int)puzzle.Element("Width"), (byte)(int)puzzle.Element("Height"), (string)puzzle.Element("Data")) {
                        StartX = (int)puzzle.Element("StartX"),
                        StartY = (int)puzzle.Element("StartY"),
                        ExitX = (int)puzzle.Element("ExitX"),
                        ExitY = (int)puzzle.Element("ExitX"),
                        IdleFold = (int)puzzle.Element("Idle"),
                        Solution = puzzle.Element("Solution").Elements("Fold")
                            .Select(p => new Fold {
                                Move = (string)p.Attribute("Move"),
                                Data = p.Value,
                                MoveOrder = (string)p.Attribute("MoveOrder"),     
                            })
                            .ToList()
                    }).FirstOrDefault();
                if (toPlayback != null) {
                    Console.Clear();
                    Console.SetCursorPosition(0, 0);                    
                    Console.Write("Engine");
                    Console.SetCursorPosition(toPlayback.ColCount + 3, 0);                    
                    Console.Write("Stored");
                    for (int i = 0; i < toPlayback.IdleFold; i ++) {
                        toPlayback.Fold();
                        Console.SetCursorPosition(0, 1);                    
                        toPlayback.Dump();
                        Console.WriteLine($"Idle {toPlayback.IdleFold - i}".PadRight(40));
                        Thread.Sleep(playbackSpeed);
                    }
                    toPlayback.Place(new Element(Element.Player), toPlayback.StartY, toPlayback.StartX);
                    toPlayback.Solution.Remove(toPlayback.Solution.First());
                    foreach (var s in toPlayback.Solution) {
                        toPlayback.SetMove(s.Move);
                        toPlayback.Fold();
                        Console.SetCursorPosition(0, 1);                    
                        toPlayback.Dump();
                        Console.WriteLine();
                        for (int i = 0; i < s.Data.Length; i += toPlayback.ColCount) {
                            Console.SetCursorPosition(toPlayback.ColCount + 3, (i / toPlayback.ColCount) + 1);                    
                            Console.Write(s.Data.Substring(i, toPlayback.ColCount));
                        }
                        Console.WriteLine();
                        Console.WriteLine();
                        //Console.WriteLine(s.MoveOrder.PadRight(80));
                        Console.WriteLine(s.Move.PadRight(40));
                        Thread.Sleep(playbackSpeed);
                    }
                }
                return;
            }

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
            Dictionary<Task, Worker> tasks = new Dictionary<Task, Worker>();
            int cposx = 0;//Console.CursorLeft;
            int cposy = Console.CursorTop + 1;
            using (CancellationTokenSource source = new CancellationTokenSource()) {
                do {
                    while (!Console.KeyAvailable) {
                        int count = Math.Max(0, cpu - tasks.Keys.Where(x => 
                            x.Status != TaskStatus.Canceled &&
                            x.Status != TaskStatus.Faulted &&
                            x.Status != TaskStatus.RanToCompletion).Count());
                        for (int i = 0; i < count; i ++) {
                            Worker worker = new Worker();
                            Task t = Task.Factory.StartNew(() => worker.Work(tasks.Count, rnd, puzzledb, filepath, records, patterns, rejects, maxNoMove, minMove, maxMove, minScore, idleFold, maxSolutionSeconds),
                                source.Token);
                            tasks.Add(t, worker);
                        }
                        Console.SetCursorPosition(cposx, cposy);
                        foreach (Task t in tasks.Keys) {
                            int result;
                            string resultStr;
                            switch (t.Status) {
                                case TaskStatus.Running: 
                                    var now = DateTime.Now;
                                    var timeout = (tasks[t].Solver.Timeout - tasks[t].Solver.LastSearch).TotalSeconds;
                                    var progress = 0;
                                    if (timeout > 0) {
                                        progress = (int)Math.Ceiling(((now - tasks[t].Solver.LastSearch).TotalSeconds * 60) / timeout);
                                    }
                                    result = tasks[t].Solver.LastSearchResult;
                                    resultStr = result.ToString();
                                    switch (result) {
                                        case Solver.NOT_FOUND :
                                        resultStr = "NF";
                                        break;
                                        case Solver.FOUND :
                                        resultStr = "??";
                                        break;
                                    }
                                    Console.WriteLine($"{t.Id,4} [{"".PadRight(progress, '█').PadRight(60)}] {resultStr} / {tasks[t].Solver.Tries}".PadRight(80));
                                    break;
                            } 
                        }
                        Thread.Sleep(500);       
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                source.Cancel();
            }
        }
    }
    class Worker {
        public Solver Solver { get; private set; }
        public Worker() {
            Solver = new Solver();
        }
        public void Work(int id, Random rnd,
            XElement puzzledb,
            string filepath,
            List<BoardData> records,
            List<PatternData> patterns,
            List<RejectData> rejects,
            int maxNoMove,
            int minMove,
            int maxMove,
            int minScore,
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
                //Console.WriteLine($"(Task {id}) Board already exists (hash: {hash}).");
                return;
            }
            RejectData rejected = rejects.Where(x => x.Hash == hash).FirstOrDefault();
            if (rejected != null)
            {
                //Console.WriteLine($"(Task {id}) Board already rejected (hash: {hash}).");
                return;
            }

            for (int i = 0; i < idleFold; i ++) {
                board.Fold();
            }
            board.Place(new Element(Element.Player), board.StartY, board.StartX);

            //Console.WriteLine($"(Task {id}) Origin:");
            //original.Dump();
            //Console.WriteLine($"\n(Task {id}) Idle folded:");
            //board.Dump();
            Solution s = Solver.Solve(id, board, new TimeSpan(0, 0, maxSolutionSeconds), maxMove, 1f);
            if (s != null && s.Bound < minMove) {
                //Console.WriteLine($"(Task {id}) Move under the minimum ({minMove}), discarding.");
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
            if (s == null && Solver.LastSearchResult == Solver.TIMEDOUT) {
                //Console.WriteLine($"(Task {id}) Timeout, couldn't verify board. Discarding.");
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
                //Console.WriteLine($"(Task {id}) No Solution found. Discarding.");
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
                //Console.WriteLine($"(Task {id}) Trying to find better solutions.");
                float first = s.Bound;
                do
                {
                    Solver.Tries ++;
                    Solution better = Solver.Solve(id, board, new TimeSpan(0, 0, maxSolutionSeconds), s.Bound, (s.Bound - 1) / first);                    
                    if (better != null && better.Bound < s.Bound) {
                        //Console.WriteLine($"(Task {id}) Better solution found: {better.Bound}");
                        s = better;
                        if (s.Bound < minMove) {
                            //Console.WriteLine($"(Task {id}) Move under the minimum ({minMove}), discarding.");
                            s = null;
                            break;
                        }
                    } else
                        if (better == null && Solver.LastSearchResult == Solver.TIMEDOUT) {
                            //Console.WriteLine($"(Task {id}) Timeout, couldn't verify board. Discarding.");
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
                                new XElement("Idle", idleFold),
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
                            //Console.WriteLine($"(Task {id}) No better solution, bailing.");
                            break;
                        }

                } while (true);
                if (s != null) {
                    XElement solution = new XElement("Solution");
                    int steps = 0;
                    Board prev = null;
                    int len = s.Path[0].Data.Length;                    
                    int diffTotal = 0;
                    int goalTotal = 0;
                    int mobBefore = 0;
                    int mobAfter = 0;
                    int fallingDelta = 0;
                    int proximity = 0;
                    foreach (Board b in s.Path)
                    {
                        var foldStr = b.ToString();
                        var fold = new XElement("Fold", foldStr);
                        fold.SetAttributeValue("Move", b.NameMove());
                        int goals = b.Data.Where(x => x != null && x.Details == Element.Diamond).Count();
                        //fold.SetAttributeValue("Goals", goals);
                        goalTotal += goals;
                        var diffs = 0;
                        if (prev != null) {
                            for (int i = 0; i < len; i ++) {
                                if (prev.Data[i].Details != b.Data[i].Details) diffs++;
                            }
                            fallingDelta += prev.Data.Count(x => x.Falling) - b.Data.Count(x => x.Falling);
                        }
                        diffTotal += diffs;
                        //fold.SetAttributeValue("Diffs", diffs);
                        //var ff = b.Data.Count(x => x.Falling);
                        //if (ff > 0)
                        //    fold.SetAttributeValue("Falling", ff);
                        //fold.SetAttributeValue("Space", b.Data.Count(x => x == null || x.Details == Element.Space));
                        var moveOrder = new StringBuilder();
                        Boolean before = true;
                        for (int i = 0; i < len; i ++) {
                            var details = b.Data[i].Details;
                            if (details.Mob) {
                                if (i > 0 && b.Data[i - 1].Details == Element.Player) proximity ++;
                                if (i < len - 1 && b.Data[i + 1].Details == Element.Player) proximity ++;
                                if (i > b.ColCount && b.Data[i - b.ColCount].Details == Element.Player) proximity ++;
                                if (i < b.ColCount - 1 && b.Data[i + b.ColCount].Details == Element.Player) proximity ++;
                            }
                            if (details.Mob || details == Element.Boulder || details == Element.Diamond || details == Element.Player) {
                                moveOrder.Append(details.Symbols[details.StartFacing]);                                
                                if (details != Element.Player) {
                                    if (details.Mob) {
                                        if (before)
                                            mobBefore++;
                                        else 
                                            mobAfter++;
                                    }
                                } else
                                    before = false;
                            }
                        }
                        //if (proximity > 0)
                        //    fold.SetAttributeValue("Proximity", proximity);
                        //fold.SetAttributeValue("MoveOrder", moveOrder.ToString());

                        prev = b;
                        solution.Add(fold);
                        steps++;
                    }
                    fallingDelta = Math.Max(-10, fallingDelta * -1);                     
                    int score = ((int)Math.Ceiling((double)diffTotal / (s.Path.Count - 1)) * 10) + ((mobBefore / s.Path.Count) * 5) + ((mobAfter / s.Path.Count) * 2) + (fallingDelta * 5) + len + ((goalTotal / s.Path.Count) * 3) + (proximity * 12);
                    solution.SetAttributeValue("AvgDiff", (int)Math.Ceiling((double)diffTotal / (s.Path.Count - 1)));
                    solution.SetAttributeValue("AvgGoals", goalTotal / s.Path.Count);
                    solution.SetAttributeValue("AvgBefore", (mobBefore / s.Path.Count));
                    solution.SetAttributeValue("AvgAfter", (mobAfter / s.Path.Count));
                    solution.SetAttributeValue("FallingDelta", fallingDelta);
                    solution.SetAttributeValue("Proximity", proximity);
                    solution.SetAttributeValue("Score", score);
                    if (score >= minScore)
                    {
                        puzzledb.Element("Boards").Add(new XElement("Board",
                            new XElement("Hash", original.FNV1aHash()),
                            new XElement("Width", original.ColCount),
                            new XElement("Height", original.RowCount),
                            new XElement("Par", steps),
                            new XElement("StartX", original.StartX),
                            new XElement("StartY", original.StartY),
                            new XElement("ExitX", original.ExitX),
                            new XElement("ExitY", original.ExitY),
                            new XElement("Idle", idleFold),
                            new XElement("Data", original.ToString()),
                            solution
                        ));
                        lock (puzzledb)
                        {
                            puzzledb.Save(filepath);
                        }
                        lock (records)
                        {
                            records.Add(new BoardData
                            {
                                Hash = hash
                            });
                        }
                    }
                    else
                    {
                        puzzledb.Element("Rejects").Add(
                               new XElement("Reject",
                               new XElement("Hash", hash),
                               new XElement("Width", original.ColCount),
                               new XElement("Height", original.RowCount),
                               new XElement("StartX", original.StartX),
                               new XElement("StartY", original.StartY),
                               new XElement("ExitX", original.ExitX),
                               new XElement("ExitY", original.ExitY),
                               new XElement("Reason", "Min score"),
                               new XElement("Idle", idleFold),
                               new XElement("Data", original.ToString()),
                               solution
                           ));
                        lock (puzzledb)
                        {
                            puzzledb.Save(filepath);
                        }
                        lock (rejects)
                        {
                            rejects.Add(new RejectData
                            {
                                Hash = hash
                            });
                        }
                    }
                   // Console.WriteLine($"(Task {id}) Puzzle added to DB");
                }
            } 
        }

    }
}
