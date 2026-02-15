using System;
using System.Threading.Tasks;
using GenDash.Engine;

namespace GenDash.Utils {
    class ConsoleUIHelper {
        /// <summary>
        /// Safely tries to set the cursor position, handling any exceptions.
        /// </summary>
        public static bool TrySetCursorPosition(int left, int top) {
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

        /// <summary>
        /// Formats a number with K/M suffixes for readability.
        /// </summary>
        public static string FormatNumber(int number) {
            if (number >= 1000000)
                return $"{number / 1000000.0:F1}M";
            if (number >= 1000)
                return $"{number / 1000.0:F1}K";
            return number.ToString();
        }

        /// <summary>
        /// Renders the status line for a worker task.
        /// </summary>
        public static void RenderWorkerStatus(Task task, Worker worker) {
            // Show all tasks with their final phase
            if (task.Status == TaskStatus.Running) {
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
                    $"{task.Id,4} {resultStr,3} | {phase} | {size,6} | {metrics} {nodes} {hash} {bound} {optimizing} | {time}"
                );
            } else {
                // Show completed/faulted tasks with their final phase
                string phase = worker.Phase.PadRight(12);
                string status = task.Status == TaskStatus.RanToCompletion ? "Done" : task.Status.ToString().Substring(0, 4);
                Console.WriteLine($"{task.Id,4} [{status}] {phase}");
            }
        }
    }
}
