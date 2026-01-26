using System;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GenDash.Models;

namespace GenDash.Engine {
    internal class Worker {
        public Solver Solver { get; private set; }
        public Worker() {
            Solver = new Solver();
        }
        public void Work(int id, Random rnd,
            XElement puzzledb,
            string filepath,
            HashSet<ulong> recordHashes,
            List<PatternData> patterns,
            HashSet<ulong> rejectHashes,
            System.Collections.Concurrent.ConcurrentQueue<Action> saveQueue,
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
            
            // O(1) lookup instead of O(n)
            if (recordHashes.Contains(hash))
            {
                return;
            }
            if (rejectHashes.Contains(hash))
            {
                return;
            }

            for (int i = 0; i < idleFold; i ++) {
                board.Fold();
            }
            board.Place(new Element(Element.Player), board.StartY, board.StartX);

            Solution s = Solver.Solve(id, board, new TimeSpan(0, 0, maxSolutionSeconds), maxMove, 1f);
            if (s != null && s.Bound < minMove) {
                var rejectElement = new XElement("Reject",
                    new XElement("Hash", hash),
                    new XElement("Reason", "MinMove")
                );
                lock(rejectHashes) {
                    rejectHashes.Add(hash);
                }
                saveQueue.Enqueue(() => puzzledb.Element("Rejects").Add(rejectElement));
                s = null;
            } else 
            if (s == null && Solver.LastSearchResult == Solver.TIMEDOUT) {
                var rejectElement = new XElement("Reject",
                    new XElement("Hash", hash),
                    new XElement("Reason", "Timeout with no solution"),
                    new XElement("Data", original.ToString())
                );
                lock(rejectHashes) {
                    rejectHashes.Add(hash);
                }
                saveQueue.Enqueue(() => puzzledb.Element("Rejects").Add(rejectElement));
                s = null;
            } else
            if (s == null) {
                var rejectElement = new XElement("Reject",
                    new XElement("Hash", hash),
                    new XElement("Reason", "Unsolveable")
                );
                lock(rejectHashes) {
                    rejectHashes.Add(hash);
                }
                saveQueue.Enqueue(() => puzzledb.Element("Rejects").Add(rejectElement));
            }

            if (s != null) {
                float first = s.Bound;
                do
                {
                    Solver.Tries ++;
                    Solution better = Solver.Solve(id, board, new TimeSpan(0, 0, maxSolutionSeconds), s.Bound, (s.Bound - 1) / first);                    
                    if (better != null && better.Bound < s.Bound) {
                        s = better;
                        if (s.Bound < minMove) {
                            s = null;
                            break;
                        }
                    } else
                        if (better == null && Solver.LastSearchResult == Solver.TIMEDOUT) {
                            var rejectElement = new XElement("Reject",
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
                            );
                            lock(rejectHashes) {
                                rejectHashes.Add(hash);
                            }
                            saveQueue.Enqueue(() => puzzledb.Element("Rejects").Add(rejectElement));
                            s = null;
                            break;
                        } else {
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
                        goalTotal += goals;
                        var diffs = 0;
                        if (prev != null) {
                            for (int i = 0; i < len; i ++) {
                                if (prev.Data[i].Details != b.Data[i].Details) diffs++;
                            }
                            fallingDelta += prev.Data.Count(x => x.Falling) - b.Data.Count(x => x.Falling);
                        }
                        diffTotal += diffs;
                        var moveOrder = new StringBuilder();
                        bool before = true;
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

                        prev = b;
                        solution.Add(fold);
                        steps++;
                    }
                    fallingDelta = Math.Max(-10, fallingDelta * -1);                     
                    int score = (int)Math.Ceiling((double)diffTotal / (s.Path.Count - 1)) * 10 + mobBefore / s.Path.Count * 5 + mobAfter / s.Path.Count * 2 + fallingDelta * 5 + len + goalTotal / s.Path.Count * 3 + proximity * 12;
                    solution.SetAttributeValue("AvgDiff", (int)Math.Ceiling((double)diffTotal / (s.Path.Count - 1)));
                    solution.SetAttributeValue("AvgGoals", goalTotal / s.Path.Count);
                    solution.SetAttributeValue("AvgBefore", mobBefore / s.Path.Count);
                    solution.SetAttributeValue("AvgAfter", mobAfter / s.Path.Count);
                    solution.SetAttributeValue("FallingDelta", fallingDelta);
                    solution.SetAttributeValue("Proximity", proximity);
                    solution.SetAttributeValue("Score", score);
                    if (score >= minScore)
                    {
                        var boardElement = new XElement("Board",
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
                        );
                        lock (recordHashes)
                        {
                            recordHashes.Add(hash);
                        }
                        saveQueue.Enqueue(() => puzzledb.Element("Boards").Add(boardElement));
                    }
                    else
                    {
                        var rejectElement = new XElement("Reject",
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
                           );
                        lock (rejectHashes)
                        {
                            rejectHashes.Add(hash);
                        }
                        saveQueue.Enqueue(() => puzzledb.Element("Rejects").Add(rejectElement));
                    }
                }
            } 
        }

    }
}
