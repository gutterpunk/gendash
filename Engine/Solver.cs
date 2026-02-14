using GenDash.Models;
using System;
using System.Collections.Generic;

namespace GenDash.Engine {
    public class Solution {
        public List<Board> Path { get; set; } = [];
        public int Bound { get; set; }
    }
    public class Solver {
        private readonly List<Board> _successorBuffer = new(9);
        private readonly HashSet<ulong> _pathHashes = [];
        private readonly Point[] _diamondBuffer = new Point[32];
        private int _diamondBufferCount = 0;
        
        public bool IsCanceled { get; private set; }
        public const int FOUND = int.MaxValue;
        public const int NOT_FOUND = int.MaxValue - 1;
        public const int CANCELED = int.MaxValue - 2;
        public const int TIMEDOUT = int.MaxValue - 3;
        public int LastSearchResult { get; private set; }
        public DateTime LastSearch { get; private set; }
        public DateTime Timeout { get; private set; }
        
        public Solver() {
            for (int i = 0; i < _diamondBuffer.Length; i++) {
                _diamondBuffer[i] = new Point();
            }
        }
        
        public Solution Solve(int Id, Board root, TimeSpan delay, int maxcost = int.MaxValue, float ratio = 1f) {
            Solution solution = new()
            {
                Path = { root },
                Bound = Heuristic(root, ratio)
            };
            while (true) {
                LastSearch = DateTime.Now;
                Timeout = DateTime.Now.AddSeconds(delay.TotalSeconds);
                _pathHashes.Clear();
                
                int t = Search(solution, 0, Timeout, ratio);
                LastSearchResult = t;
                
                if (t == FOUND) {
                    return solution;
                }
                if (t == NOT_FOUND) {
                    return null;
                }
                if (t == CANCELED) {
                    return null;
                }
                if (t == TIMEDOUT)
                {
                    return null;
                }
                if (t >= maxcost) {
                    return null;
                }
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
            
            _diamondBufferCount = 0;
            
            int totalElements = node.RowCount * node.ColCount;
            for (int i = 0; i < totalElements; i++) {
                Element e = node.Data[i];
                if (e == null) continue;
                
                if (e.Details == Element.Diamond) {
                    if (_diamondBufferCount < _diamondBuffer.Length) {
                        _diamondBuffer[_diamondBufferCount].X = i % node.ColCount;
                        _diamondBuffer[_diamondBufferCount].Y = i / node.ColCount;
                        _diamondBufferCount++;
                    }
                } else if (e.Details == Element.Player) {
                    x = i % node.ColCount;
                    y = i / node.ColCount;
                }
            }
            
            // Greedy nearest-neighbor through diamonds
            uint visited = 0;
            int remaining = _diamondBufferCount;
            
            while (remaining > 0) {
                int minDist = int.MaxValue;
                int closestIdx = -1;
                
                for (int i = 0; i < _diamondBufferCount; i++) {
                    if ((visited & (1u << i)) != 0) continue;
                    
                    int dx = Math.Abs(x - _diamondBuffer[i].X);
                    int dy = Math.Abs(y - _diamondBuffer[i].Y);
                    int dist = dx + dy;
                    
                    if (dist < minDist) {
                        minDist = dist;
                        closestIdx = i;
                    }
                }
                
                if (closestIdx >= 0) {
                    d += minDist;
                    visited |= (1u << closestIdx);
                    x = _diamondBuffer[closestIdx].X;
                    y = _diamondBuffer[closestIdx].Y;
                    remaining--;
                } else {
                    break;
                }
            }
            
            int exitDx = Math.Abs(x - node.ExitX);
            int exitDy = Math.Abs(y - node.ExitY);
            d += exitDx + exitDy;
            
            return (int)Math.Floor(d * ratio);
        }
        
        private static int Cost(Board from, Board next) {
            int totalElements = next.Data.Length;
            for (int i = 0; i < totalElements; i++) {
                Element e = next.Data[i];
                if (e != null && e.Details == Element.Player)
                    return 1;
            }
            return 10000;
        }

        private int Search(Solution solution, int gcost, DateTime? tryUntil, float ratio = 1f) {
            if (tryUntil.HasValue && tryUntil.Value <= DateTime.Now) 
                return TIMEDOUT;
            
            if (IsCanceled) return CANCELED;
            
            Board node = solution.Path[^1];
            int fcost = gcost + Heuristic(node, ratio);
            
            if (fcost > solution.Bound) return fcost;
            if (IsGoal(node)) return ValidateSolution(solution) ? FOUND : NOT_FOUND;
            
            int min = NOT_FOUND;
            
            _successorBuffer.Clear();
            node.FoldSuccessors(_successorBuffer);
                        
            for (int i = 0; i < _successorBuffer.Count; i++) {
                Board board = _successorBuffer[i];
                ulong boardHash = board.FNV1aStateHash();
                
                if (!_pathHashes.Contains(boardHash)) {
                    solution.Path.Add(board);
                    _pathHashes.Add(boardHash);
                    
                    int t = Search(solution, gcost + Cost(node, board), tryUntil, ratio);
                    
                    if (t == FOUND) return FOUND;
                    if (t == TIMEDOUT) return TIMEDOUT;
                    if (t == CANCELED) return CANCELED;
                    if (t < min) min = t;
                    
                    solution.Path.RemoveAt(solution.Path.Count - 1);
                    _pathHashes.Remove(boardHash);
                }
            }
            
            return min;
        }

        private static bool ValidateSolution(Solution solution) {
            if (solution.Path.Count <= 1) return true;

            Board current = new(solution.Path[0]);
            for (int i = 1; i < solution.Path.Count; i++) {
                string move = solution.Path[i].NameMove();
                current.SetMove(move);
                if (!current.Fold()) {
                    return false;
                }
                if (current.FNV1aStateHash() != solution.Path[i].FNV1aStateHash()) {
                    return false;
                }
            }

            return true;
        }
        
        private static bool IsGoal(Board node) {
            int totalElements = node.Data.Length;
            bool hasDiamond = false;
            
            for (int i = 0; i < totalElements; i++) {
                Element e = node.Data[i];
                if (e != null && e.Details == Element.Diamond) {
                    hasDiamond = true;
                    break;
                }
            }
            
            if (!hasDiamond) {
                for (int i = 0; i < totalElements; i++) {
                    Element e = node.Data[i];
                    if (e != null && e.Details == Element.Player) {
                        int col = i % node.ColCount;
                        int row = i / node.ColCount;
                        return col == node.ExitX && row == node.ExitY;
                    }
                }
            }
            
            return false;
        }      
    }
}
