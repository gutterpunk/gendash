using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenDash.Engine;
using GenDash.Models;

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
        private static bool TrySetCursorPosition(int left, int top) {
            try {
                if (left >= 0 && left < Console.BufferWidth && top >= 0 && top < Console.BufferHeight) {
                    Console.SetCursorPosition(left, top);
                    return true;
                }
            } catch {
                // Silently handle any exceptions
            }
            return false;
        }

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
            
            HashSet<ulong> recordHashes = new HashSet<ulong>(
                from puzzle in boardsNode.Descendants("Board")
                select (ulong)puzzle.Element("Hash")
            );
            
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
                    TrySetCursorPosition(0, 0);                    
                    Console.Write("Engine");
                    TrySetCursorPosition(toPlayback.ColCount + 3, 0);                    
                    Console.Write("Stored");
                    for (int i = 0; i < toPlayback.IdleFold; i ++) {
                        toPlayback.Fold();
                        TrySetCursorPosition(0, 1);                    
                        toPlayback.Dump();
                        Console.WriteLine($"Idle {toPlayback.IdleFold - i}".PadRight(40));
                        Thread.Sleep(playbackSpeed);
                    }
                    toPlayback.Place(new Element(Element.Player), toPlayback.StartY, toPlayback.StartX);
                    toPlayback.Solution.Remove(toPlayback.Solution.First());
                    foreach (var s in toPlayback.Solution) {
                        toPlayback.SetMove(s.Move);
                        toPlayback.Fold();
                        TrySetCursorPosition(0, 1);                    
                        toPlayback.Dump();
                        Console.WriteLine();
                        for (int i = 0; i < s.Data.Length; i += toPlayback.ColCount) {
                            TrySetCursorPosition(toPlayback.ColCount + 3, (i / toPlayback.ColCount) + 1);                    
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

            HashSet<ulong> rejectHashes = new HashSet<ulong>(
                from puzzle in puzzledb.Descendants("Rejects").Descendants("Reject")
                select (ulong)puzzle.Element("Hash")
            );

            Console.WriteLine($"Boards loaded from {xmlDatabase} : {recordHashes.Count}");
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
            
            var saveQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();
            DateTime lastSave = DateTime.Now;
            int saveIntervalSeconds = 5; 
            
            using (CancellationTokenSource source = new CancellationTokenSource()) {
                do {
                    while (!Console.KeyAvailable) {
                        int count = Math.Max(0, cpu - tasks.Keys.Where(x => 
                            x.Status != TaskStatus.Canceled &&
                            x.Status != TaskStatus.Faulted &&
                            x.Status != TaskStatus.RanToCompletion).Count());
                        for (int i = 0; i < count; i ++) {
                            Worker worker = new Worker();
                            Task t = Task.Factory.StartNew(() => worker.Work(tasks.Count, rnd, puzzledb, filepath, recordHashes, patterns, rejectHashes, saveQueue, maxNoMove, minMove, maxMove, minScore, idleFold, maxSolutionSeconds),
                                source.Token);
                            tasks.Add(t, worker);
                        }
                        
                        // Process batched saves
                        if ((DateTime.Now - lastSave).TotalSeconds >= saveIntervalSeconds && saveQueue.Count > 0) {
                            lock(puzzledb) {
                                while (saveQueue.TryDequeue(out var action)) {
                                    action();
                                }
                                puzzledb.Save(filepath);
                            }
                            lastSave = DateTime.Now;
                        }
                        
                        TrySetCursorPosition(cposx, cposy);
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
                
                // Final save on exit
                lock(puzzledb) {
                    while (saveQueue.TryDequeue(out var action)) {
                        action();
                    }
                    puzzledb.Save(filepath);
                }
                
                source.Cancel();
            }
        }
    }
}
