using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenDash.Engine;
using GenDash.Models;
using GenDash.Engine.Formats;
using GenDash.Utils;

namespace GenDash {
    class Program {
        static void Main(string[] args) {
 
            SystemPowerManager.PreventSystemSleep();
            
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
            
            // Load database
            var dbManager = new DatabaseManager(options.XmlDatabase, converter);
            XElement puzzledb = dbManager.LoadDatabase();

            // Handle conversion mode
            if (options.SkipGeneration)
            {
                dbManager.SaveDatabase(puzzledb);
                return;
            }

            HashSet<ulong> recordHashes = dbManager.LoadBoardHashes(puzzledb);

            // Handle playback mode
            if (options.Playback > 0) {
                var playbackEngine = new PlaybackEngine(puzzledb, options.Playback, options.PlaybackSpeed);
                playbackEngine.Run();
                return;
            }

            var rejectHashes = dbManager.LoadRejectHashes(puzzledb);
            XElement patternsdb = puzzledb;
            if (options.XmlDatabase != options.PatternDatabase) {
                var pfilepath = Path.Combine(Directory.GetCurrentDirectory(), options.PatternDatabase);
                if (!File.Exists(pfilepath)) {
                    throw new FileNotFoundException(pfilepath);
                }
                patternsdb = XElement.Load(pfilepath);
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
            int uiUpdateIntervalMs = 1000; 

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
                        int workerId = tasks.Count;
                        Task t = Task.Factory.StartNew(() => {
                            worker.Work(workerId, new Random(workerSeed), puzzledb, recordHashes, patterns, rejectHashes, saveQueue, options.MinMove, options.MaxMove, options.MinScore, options.IdleFold, options.MaxSolutionSeconds);
                        }, source.Token);
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
                            dbManager.SaveDatabase(puzzledb);
                        }
                        lastSave = DateTime.Now;
                    }

                    if ((DateTime.Now - lastUIUpdate).TotalMilliseconds >= uiUpdateIntervalMs)
                    {
                        ConsoleUIHelper.TrySetCursorPosition(cposx, cposy);
                        foreach (Task t in tasks.Keys)
                        {
                            ConsoleUIHelper.RenderWorkerStatus(t, tasks[t]);
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
                dbManager.SaveDatabase(puzzledb);
            }

            source.Cancel();
            
            SystemPowerManager.AllowSystemSleep();
        }
    }
}
