using System;
using System.Collections.Generic;

namespace GenDash {
    class Point {
        public int X { get; set; }
        public int Y { get; set; }
    }
    public class Solution {
        public List<Board> Path { get; set; } = new List<Board>();
        public int Bound { get; set; }
    }
    public class Solver {
        public bool IsCanceled { get; private set; }
        private const int FOUND = int.MaxValue;
        private const int NOT_FOUND = int.MaxValue - 1;
        private const int CANCELED = int.MaxValue - 2;
        private const int TIMEDOUT = int.MaxValue - 3;
        public Solution Solve(Board root, int maxcost = int.MaxValue, DateTime? tryUntil = null, float ratio = 1f) {
            Solution solution = new Solution {
                Path = { root },
                Bound = Heuristic(root, ratio)
            };
            Console.Write($"Base Moves: {solution.Bound}");
            while (true) {
                int t = Search(solution, 0, tryUntil, ratio);
                if (t == FOUND) {
                    Console.WriteLine($". Solution found in {solution.Bound} moves.");
                    return solution;
                }
                if (t == NOT_FOUND) {
                    Console.WriteLine($". No solution found in {solution.Bound} moves.");
                    return null;
                }
                if (t == CANCELED) {
                    Console.WriteLine($". Solver canceled.");
                    return null;
                }
                if (t == TIMEDOUT)
                {
                    Console.WriteLine($". Timed out white looking for solution.");
                    return null;
                }
                if (t >= maxcost) {
                    Console.WriteLine($". Bailing due to maxcost ({maxcost}).");
                    return null;
                }
                Console.Write($" -> {t}");
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
            for (int i = 0; i < node.RowCount; i++) {
                for (int j = 0; j < node.ColCount; j++) {
                    Element e = node.Data[(i * node.ColCount) + j];
                    if (e == null) continue;
                    if (e.Details == Element.Diamond) diamonds.Add(new Point { X = j, Y = i });
                    if (e.Details == Element.Player) {
                        x = j;
                        y = i;
                    }
                }
            }
            while (diamonds.Count > 0) {
                int m = int.MaxValue;
                Point closest = null;
                foreach (Point p in diamonds) {
                    int dx = Math.Abs(x - p.X);
                    int dy = Math.Abs(y - p.Y);
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
            return (int)Math.Floor(d * ratio);
        }
        private int Cost(Board from, Board next) {
            if (Array.Find(next.Data, x => x != null && x.Details == Element.Player) != null)
                return 1;
            return 10000;
        }


        private int Search(Solution solution, int gcost, DateTime? tryUntil, float ratio = 1f) {
            if (tryUntil.HasValue)
                if (tryUntil.Value <= DateTime.Now) return TIMEDOUT;
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
                    if (t < min) min = t;
                    solution.Path.Remove(board);
                }
            }
            return min;
        }
        private bool IsGoal(Board node) {
            return Array.Find(node.Data, x => x != null && x.Details == Element.Diamond) == null;
        }
        private bool BoardOnPath(Board board, List<Board> path) {
            foreach (Board b in path) {
                if (b.Compare(board)) return true;
            }
            return false;
        }
    }
}
