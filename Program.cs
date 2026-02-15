using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GenDash.Engine;
using GenDash.Models;
using GenDash.Engine.Formats;

namespace GenDash {
    class Fold {
        public string Move { get; set; }
        public string Data { get; set; }
        public string MoveOrder { get; set; }
    }
    class BoardSolution(byte width, byte height, string data) : Board(width, height, data) {
        public ulong Hash { get; set; }
        public List<Fold> Solution { get; set; }
        public int IdleFold { get; set; }
    }
    class BoardData {
        public ulong Hash { get; set; }
    }
    class RejectData {
        public ulong Hash { get; set; }
    }
  
    class Program {
        // Windows API to prevent sleep
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);
        
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;
        
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

        private static string FormatNumber(int number) {
            if (number >= 1000000)
                return $"{number / 1000000.0:F1}M";
            if (number >= 1000)
                return $"{number / 1000.0:F1}K";
            return number.ToString();
        }

        /// <summary>
        /// Merges hashes and rejects from XML database into the loaded puzzledb.
        /// </summary>
        private static void MergeHashesFromXml(XElement puzzledb, XElement xmlPuzzledb) {
            var xmlBoardsByKey = new Dictionary<string, ulong>();
            foreach (var xmlBoard in xmlPuzzledb.Descendants("Board")) {
                string key = BuildBoardKey(xmlBoard);
                ulong hash = (ulong)xmlBoard.Element("Hash");
                if (!xmlBoardsByKey.ContainsKey(key)) {
                    xmlBoardsByKey[key] = hash;
                }
            }
            
            int mergedCount = 0;
            foreach (var board in puzzledb.Descendants("Board")) {
                string key = BuildBoardKey(board);
                if (xmlBoardsByKey.TryGetValue(key, out ulong hash)) {
                    board.Element("Hash").Value = hash.ToString();
                    mergedCount++;
                }
            }
            
            if (mergedCount > 0) {
                Console.WriteLine($"Merged {mergedCount} hashes from XML");
            }

            var rejectsNode = puzzledb.Element("Rejects");
            var xmlRejectsNode = xmlPuzzledb.Element("Rejects");
            if (rejectsNode != null && xmlRejectsNode != null && !rejectsNode.HasElements && xmlRejectsNode.HasElements) {
                foreach (var reject in xmlRejectsNode.Elements("Reject")) {
                    rejectsNode.Add(new XElement(reject));
                }
                Console.WriteLine($"Merged {rejectsNode.Elements().Count()} rejects from XML");
            }
        }
        
        private static string BuildBoardKey(XElement board) {
            return $"{(int)board.Element("Width")}|{(int)board.Element("Height")}|" +
                   $"{(int)board.Element("StartX")}|{(int)board.Element("StartY")}|" +
                   $"{(int)board.Element("ExitX")}|{(int)board.Element("ExitY")}|" +
                   $"{(int)board.Element("Idle")}|{(string)board.Element("Data")}";
        }

        static void Main(string[] args) {
 
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
            
            // Parse command line options
            CommandLineOptions options;
            try {
                options = CommandLineOptions.Parse(args);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return;
            }
            
            IFormatConverter converter;
            try {
                converter = FormatConverterFactory.CreateConverter(options.Format);
            } catch (ArgumentException ex) {
                Console.WriteLine(ex.Message);
                return;
            }
            
            var currentDirectory = Directory.GetCurrentDirectory();
            var filepath = Path.Combine(currentDirectory, options.XmlDatabase);
            var filepathWithoutExt = Path.Combine(currentDirectory, Path.GetFileNameWithoutExtension(options.XmlDatabase));

            XElement puzzledb;
            XElement xmlPuzzledb = null;
            
            if (File.Exists(filepath)) {
                try {
                    xmlPuzzledb = XElement.Load(filepath);
                    Console.WriteLine($"Loaded hashes from XML: {filepath}");
                } catch (Exception ex) {
                    Console.WriteLine($"Warning: Could not load XML for hash preservation: {ex.Message}");
                }
            }
            
            if (File.Exists(Path.ChangeExtension(filepathWithoutExt, converter.FileExtension))) {
                try {
                    puzzledb = converter.Load(filepathWithoutExt);
                    Console.WriteLine($"Loaded database from {Path.GetFileName(Path.ChangeExtension(filepathWithoutExt, converter.FileExtension))}");
                    
                    // Merge hashes from XML if available
                    if (xmlPuzzledb != null) {
                        MergeHashesFromXml(puzzledb, xmlPuzzledb);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error loading from format: {ex.Message}");
                    Console.WriteLine("Attempting to load from XML...");
                    if (xmlPuzzledb != null) {
                        puzzledb = xmlPuzzledb;
                    } else if (File.Exists(filepath)) {
                        puzzledb = XElement.Load(filepath);
                    } else {
                        puzzledb = new XElement("GenDash");
                    }
                }
            } else if (xmlPuzzledb != null) {
                puzzledb = xmlPuzzledb;
            } else if (File.Exists(filepath)) {
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
            if (options.SkipGeneration)
            {
                converter.Save(puzzledb, filepathWithoutExt);
                Console.WriteLine($"Database converted and saved to {Path.GetFileName(Path.ChangeExtension(filepathWithoutExt, converter.FileExtension))}");
                return;
            }

            HashSet<ulong> recordHashes = [.. from puzzle in puzzledb.Descendants("Board")
                select (ulong)puzzle.Element("Hash")];
            Console.WriteLine($"Boards loaded from {options.XmlDatabase} : {recordHashes.Count}");

            Console.CursorVisible = false;
            if (options.Playback > 0) {
                var allPlaybacks = (
                    from puzzle in puzzledb.Descendants("Board")  // Changed from boardsNode.Descendants("Board")
                    where options.Playback == ulong.MaxValue || (ulong)puzzle.Element("Hash") == options.Playback
                    select new BoardSolution((byte)(int)puzzle.Element("Width"), (byte)(int)puzzle.Element("Height"), (string)puzzle.Element("Data")) {
                        Hash = (ulong)puzzle.Element("Hash"),
                        StartX = (int)puzzle.Element("StartX"),
                        StartY = (int)puzzle.Element("StartY"),
                        ExitX = (int)puzzle.Element("ExitX"),
                        ExitY = (int)puzzle.Element("ExitX"),
                        IdleFold = (int)puzzle.Element("Idle"),
                        Solution = [.. puzzle.Element("Solution").Elements("Fold")
                            .Select(p => new Fold {
                                Move = (string)p.Attribute("Move"),
                                Data = p.Value,
                                MoveOrder = (string)p.Attribute("MoveOrder"),     
                            })]
                    }).ToList();
                if (allPlaybacks.Count > 0) {
                    for (int j = 0; j < allPlaybacks.Count; j++) {
                        var toPlayback = allPlaybacks[j];
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
                            Console.WriteLine($"{j + 1}/{allPlaybacks.Count}");
                            Console.WriteLine(toPlayback.Hash);                            
                            Thread.Sleep(options.PlaybackSpeed);
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
                            Thread.Sleep(options.PlaybackSpeed);
                        }
                        Console.WriteLine("Playback complete. Press any key to continue...");
                        Console.ReadKey(true);
                    }
                }
                return;
            }

            var rejectHashes = new HashSet<ulong>(
                from puzzle in puzzledb.Descendants("Rejects").Descendants("Reject")
                select (ulong)puzzle.Element("Hash")
            );
            XElement patternsdb = puzzledb;
            if (options.XmlDatabase != options.PatternDatabase) {
                var pfilepath = Path.Combine(currentDirectory, options.PatternDatabase);
                if (!File.Exists(pfilepath)) {
                    throw new FileNotFoundException(pfilepath);
                }
                patternsdb = XElement.Load(pfilepath);
            }
            if (options.SkipGeneration) {
                converter.Save(puzzledb, filepathWithoutExt);
                Console.WriteLine($"Database converted and saved to {Path.GetFileName(Path.ChangeExtension(filepathWithoutExt, converter.FileExtension))}");
                return;
            }
            List<PatternData> patterns = [.. (
                from p in patternsdb.Descendants("Pattern")  // Changed from patternsNode.Descendants("Pattern")
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
                    Commands = [.. (
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
                    )]
                }
            )];
            Console.WriteLine($"Patterns loaded from {options.PatternDatabase} : {patterns.Count}");
            Random rnd = new(options.Seed);
            Dictionary<Task, Worker> tasks = new();
            int cposx = 0;
            int cposy = Console.CursorTop + 1;
            
            var saveQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();
            DateTime lastSave = DateTime.Now;
            DateTime lastUIUpdate = DateTime.Now;
            int saveIntervalSeconds = 5;
            int uiUpdateIntervalMs = 1000; // Update UI every 1 second instead of 500ms

            using CancellationTokenSource source = new();
            do
            {
                while (!Console.KeyAvailable)
                {
                    int count = Math.Max(0, options.Cpu - tasks.Count);

                    for (int i = 0; i < count; i++)
                    {
                        Worker worker = new();
                        int workerSeed = rnd.Next();
                        Task t = Task.Factory.StartNew(() => worker.Work(tasks.Count, new Random(workerSeed), puzzledb, recordHashes, patterns, rejectHashes, saveQueue, options.MinMove, options.MaxMove, options.MinScore, options.IdleFold, options.MaxSolutionSeconds),
                            source.Token);
                        tasks.Add(t, worker);
                    }

                    if ((DateTime.Now - lastSave).TotalSeconds >= saveIntervalSeconds && !saveQueue.IsEmpty)
                    {
                        lock (puzzledb)
                        {
                            while (saveQueue.TryDequeue(out var action))
                            {
                                action();
                            }
                            converter.Save(puzzledb, filepathWithoutExt);
                        }
                        lastSave = DateTime.Now;
                    }

                    if ((DateTime.Now - lastUIUpdate).TotalMilliseconds >= uiUpdateIntervalMs)
                    {
                        TrySetCursorPosition(cposx, cposy);
                        foreach (Task t in tasks.Keys)
                        {
                            if (t.Status == TaskStatus.Running)
                            {
                                var worker = tasks[t];
                                var now = DateTime.Now;
                                var elapsed = (now - worker.Solver.LastSearch).TotalSeconds;
                                var timeout = (worker.Solver.Timeout - worker.Solver.LastSearch).TotalSeconds;
                                var timeRemaining = Math.Max(0, timeout - elapsed);
                                
                                int result = worker.Solver.LastSearchResult;
                                string resultStr = result switch
                                {
                                    Solver.NOT_FOUND => "NF",
                                    Solver.FOUND => "OK",
                                    Solver.TIMEDOUT => "TO",
                                    Solver.CANCELED => "CN",
                                    _ => result.ToString()
                                };
                                
                                string size = $"{worker.CurrentBoardWidth}x{worker.CurrentBoardHeight}";
                                string phase = worker.Phase.PadRight(10);

                                string metrics = $"D:{worker.Solver.CurrentDepth}/{worker.Solver.MaxDepth}".PadRight(10);
                                string nodes = $"N:{FormatNumber(worker.Solver.NodesExplored)}".PadRight(12);
                                string hash = $"H:{FormatNumber(worker.Solver.HashTableSize)}".PadRight(10);
                                string bound = $"B:{worker.Solver.CurrentBound}".PadRight(8);
                                string optimizing = worker.OptimizeBound > 0 ? $"O:{worker.OptimizeBound}".PadRight(8) : "".PadRight(8);
                                string time = $"T:{timeRemaining:F0}s".PadRight(8);
                                
                                Console.WriteLine(
                                    $"{t.Id,4} {resultStr,3} | {phase} | {size,6} | {metrics} {nodes} {hash} {bound} {optimizing} | {time}"
                                );
                            }
                        }
                        lastUIUpdate = DateTime.Now;

                        // Prune completed tasks
                        List<Task> completedTasks = null;
                        foreach (var kvp in tasks) {
                            if (kvp.Key.Status == TaskStatus.RanToCompletion || 
                                kvp.Key.Status == TaskStatus.Faulted || 
                                kvp.Key.Status == TaskStatus.Canceled) {
                                completedTasks ??= new List<Task>();
                                completedTasks.Add(kvp.Key);
                            }
                        }
                        if (completedTasks != null) {
                            foreach (var ct in completedTasks) tasks.Remove(ct);
                        }
                    }

                    Thread.Sleep(100);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            lock (puzzledb)
            {
                while (saveQueue.TryDequeue(out var action))
                {
                    action();
                }
                converter.Save(puzzledb, filepathWithoutExt);
            }

            source.Cancel();
            
            SetThreadExecutionState(ES_CONTINUOUS);
        }
    }
}
