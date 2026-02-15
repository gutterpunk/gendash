using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using GenDash.Engine;
using GenDash.Models;
using GenDash.Utils;

namespace GenDash.Engine {
    /// <summary>
    /// Handles playback visualization of solved puzzles.
    /// </summary>
    class PlaybackEngine {
        private readonly XElement _puzzledb;
        private readonly ulong _playbackHash;
        private readonly int _playbackSpeed;

        public PlaybackEngine(XElement puzzledb, ulong playbackHash, int playbackSpeed) {
            _puzzledb = puzzledb;
            _playbackHash = playbackHash;
            _playbackSpeed = playbackSpeed;
        }

        /// <summary>
        /// Runs the playback mode, showing solution animations.
        /// </summary>
        public void Run() {
            Console.CursorVisible = false;
            
            var allPlaybacks = LoadPlaybacks();
            
            if (allPlaybacks.Count == 0) {
                Console.WriteLine("No puzzles found for playback.");
                return;
            }

            for (int j = 0; j < allPlaybacks.Count; j++) {
                PlaybackSolution(allPlaybacks[j], j + 1, allPlaybacks.Count);
                Console.WriteLine("Playback complete. Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        private List<BoardSolution> LoadPlaybacks() {
            return (
                from puzzle in _puzzledb.Descendants("Board")
                where _playbackHash == ulong.MaxValue || (ulong)puzzle.Element("Hash") == _playbackHash
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
        }

        private void PlaybackSolution(BoardSolution board, int currentIndex, int totalCount) {
            Console.Clear();
            ConsoleUIHelper.TrySetCursorPosition(0, 0);
            Console.Write("Engine");
            ConsoleUIHelper.TrySetCursorPosition(board.ColCount + 3, 0);
            Console.Write("Stored");

            // Play idle folds
            for (int i = 0; i < board.IdleFold; i++) {
                board.Fold();
                ConsoleUIHelper.TrySetCursorPosition(0, 1);
                board.Dump();
                Console.WriteLine($"Idle {board.IdleFold - i}".PadRight(40));
                Console.WriteLine($"{currentIndex}/{totalCount}");
                Console.WriteLine(board.Hash);
                Thread.Sleep(_playbackSpeed);
            }

            // Place player and remove first fold from solution
            board.Place(new Element(Element.Player), board.StartY, board.StartX);
            board.Solution.Remove(board.Solution.First());

            // Play solution moves
            foreach (var step in board.Solution) {
                board.SetMove(step.Move);
                board.Fold();
                ConsoleUIHelper.TrySetCursorPosition(0, 1);
                board.Dump();
                Console.WriteLine();
                
                // Display stored board state
                for (int i = 0; i < step.Data.Length; i += board.ColCount) {
                    ConsoleUIHelper.TrySetCursorPosition(board.ColCount + 3, (i / board.ColCount) + 1);
                    Console.Write(step.Data.Substring(i, board.ColCount));
                }
                
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(step.Move.PadRight(40));
                Thread.Sleep(_playbackSpeed);
            }
        }
    }
}
