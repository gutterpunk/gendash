using GenDash.Models;
using System;
using System.Collections.Generic;

namespace GenDash.Engine {
    public class Solution {
        public List<Board> Path { get; set; } = new List<Board>();
        public int Bound { get; set; }
    }
    public class Solver {
        public bool IsCanceled { get; private set; }
        public const int FOUND = int.MaxValue;
        public const int NOT_FOUND = int.MaxValue - 1;
        public const int CANCELED = int.MaxValue - 2;
        public const int TIMEDOUT = int.MaxValue - 3;
        public int LastSearchResult { get; private set; }
        public DateTime LastSearch { get; private set; }
        public DateTime Timeout { get; private set; }
        public int Tries { get; set; }
        public Solution Solve(int Id, Board root, TimeSpan delay, int maxcost = int.MaxValue, float ratio = 1f) {
            Solution solution = new Solution {
                Path = { root },
                Bound = Heuristic(root, ratio)
            };
            while (true) {
                LastSearch = DateTime.Now;
                Timeout =  DateTime.Now.AddSeconds(delay.TotalSeconds);
                //Console.WriteLine($"(Task {Id}) Searching bounds {solution.Bound} until {tryUntil.ToString("HH:mm:ss")}");
                int t = Search(solution, 0, Timeout, ratio);
                LastSearchResult = t;
                //Console.WriteLine($"(Task {Id}) Spent {(DateTime.Now - started).TotalSeconds.ToString("0.##")}s on last bounds.");
                if (t == FOUND) {
                    //Console.WriteLine($"    Solution found in {solution.Bound} moves.");
                    return solution;
                }
                if (t == NOT_FOUND) {
                    //Console.WriteLine($"    No solution found in {solution.Bound} moves.");
                    return null;
                }
                if (t == CANCELED) {
                    //Console.WriteLine($"    Solver canceled.");
                    return null;
                }
                if (t == TIMEDOUT)
                {
                    //Console.WriteLine($"    Timed out while looking for solution.");
                    return null;
                }
                if (t >= maxcost) {
                    //Console.WriteLine($"    Bailing due to maxcost ({maxcost}).");
                    return null;
                }
                //Console.WriteLine($"    Pushing bounds to {t} moves");
                solution.Bound = t;
            }
        }
        public void Cancel() {
            IsCanceled = true;
        }
        private int Heuristic(Board node, float ratio = 1f) {
            int d = 0;
            int x = -1;
            int y = -1;
            List<Point> diamonds = new List<Point>();
            
            int totalElements = node.RowCount * node.ColCount;
            for (int i = 0; i < totalElements; i++) {
                Element e = node.Data[i];
                if (e == null) continue;
                if (e.Details == Element.Diamond) {
                    diamonds.Add(new Point { X = i % node.ColCount, Y = i / node.ColCount });
                }
                if (e.Details == Element.Player) {
                    x = i % node.ColCount;
                    y = i / node.ColCount;
                }
            }
            
            int dx, dy;
            while (diamonds.Count > 0) {
                int m = int.MaxValue;
                Point closest = null;
                foreach (Point p in diamonds) {
                    dx = Math.Abs(x - p.X);
                    dy = Math.Abs(y - p.Y);
                    if (dx + dy < m) {
                        closest = p;
                        m = dx + dy;
                    }
                }
                if (m != int.MaxValue) {
                    d += m;
                    diamonds.Remove(closest);
                    x = closest.X;
                    y = closest.Y;
                }
            }
            
            dx = Math.Abs(x - node.ExitX);
            dy = Math.Abs(y - node.ExitY);
            d += dx + dy;
            
            return (int)Math.Floor(d * ratio);
        }
        private int Cost(Board from, Board next) {
            if (Array.Find(next.Data, x => x != null && x.Details == Element.Player) != null)
                return 1;
            return 10000;
        }

        private int Search(Solution solution, int gcost, DateTime? tryUntil, float ratio = 1f) {
            if (tryUntil.HasValue) {
                if (tryUntil.Value <= DateTime.Now) 
                    return TIMEDOUT;
            }
            if (IsCanceled) return CANCELED;            
            Board node = solution.Path[solution.Path.Count - 1];
            int fcost = gcost + Heuristic(node, ratio);
            if (fcost > solution.Bound) return fcost;
            if (IsGoal(node)) return FOUND;
            int min = NOT_FOUND;
            List<Board> successors = new List<Board>();
            node.FoldSuccessors(successors);
            foreach (Board board in successors) {
                if (!BoardOnPath(board, solution.Path)) {
                    //Console.Clear();
                    //board.Dump();
                    solution.Path.Add(board);
                    int t = Search(solution, gcost + Cost(node, board), tryUntil, ratio);
                    if (t == FOUND) return FOUND;
                    if (t == TIMEDOUT) 
                        return TIMEDOUT;
                    if (t == CANCELED) return CANCELED;
                    if (t < min) min = t;
                    solution.Path.Remove(board);
                }
            }
            return min;
        }
        private bool IsGoal(Board node) {
            // Early exit: check if any diamonds exist using Array.Exists (faster than Array.Find)
            if (!Array.Exists(node.Data, x => x != null && x.Details == Element.Diamond)) {
                bool onExit = false;
                int totalElements = node.RowCount * node.ColCount;
                for (int i = 0; i < totalElements; i++) {
                    Element e = node.Data[i];
                    if (e == null) continue;
                    if (e.Details == Element.Player) {
                        int col = i % node.ColCount;
                        int row = i / node.ColCount;
                        if (col == node.ExitX && row == node.ExitY) onExit = true;
                    }
                }
                return onExit;
            }
            return false;
        }
        private bool BoardOnPath(Board board, List<Board> path) {
            // Optimize: Use hash comparison first if available
            ulong boardHash = board.FNV1aHash();
            foreach (Board b in path) {
                ulong pathHash = b.FNV1aHash();
                if (boardHash == pathHash && b.Compare(board)) return true;
            }
            return false;
        }
    }
}
