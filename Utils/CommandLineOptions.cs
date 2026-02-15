using System;
using System.Collections.Generic;
using System.Linq;

namespace GenDash.Utils {
    class CommandLineOptions {
        public int Seed { get; set; } = int.MaxValue;
        public int MinMove { get; set; } = 15;
        public int MinScore { get; set; } = 100;
        public int MaxMove { get; set; } = 75;
        public int IdleFold { get; set; } = 5; // NEVER CHANGE THAT!
        public int MaxSolutionSeconds { get; set; } = 600;
        public int Cpu { get; set; } = Environment.ProcessorCount - 1;
        public string XmlDatabase { get; set; } = "GenDashDB.xml";
        public string PatternDatabase { get; set; }
        public string Format { get; set; } = "xml";
        public bool SkipGeneration { get; set; } = false;
        public ulong Playback { get; set; } = 0;
        public int PlaybackSpeed { get; set; } = 200;

        /// <summary>
        /// Parses command line arguments into CommandLineOptions.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Parsed options</returns>
        /// <exception cref="Exception">Thrown if parsing fails</exception>
        public static CommandLineOptions Parse(string[] args) {
            var options = new CommandLineOptions();
            
            try {
                for (int i = 0; i < args.Length; i++) {
                    if (args[i].Equals("-seed", StringComparison.OrdinalIgnoreCase)) {
                        options.Seed = int.Parse(args[++i]);
                    } else if (args[i].Equals("-minmove", StringComparison.OrdinalIgnoreCase)) {
                        options.MinMove = int.Parse(args[++i]);
                    } else if (args[i].Equals("-maxmove", StringComparison.OrdinalIgnoreCase)) {
                        options.MaxMove = int.Parse(args[++i]);
                    } else if (args[i].Equals("-maxtime", StringComparison.OrdinalIgnoreCase)) {
                        options.MaxSolutionSeconds = int.Parse(args[++i]);
                    } else if (args[i].Equals("-idle", StringComparison.OrdinalIgnoreCase)) {
                        options.IdleFold = int.Parse(args[++i]);
                    } else if (args[i].Equals("-database", StringComparison.OrdinalIgnoreCase)) {
                        options.XmlDatabase = args[++i];
                    } else if (args[i].Equals("-patterns", StringComparison.OrdinalIgnoreCase)) {
                        options.PatternDatabase = args[++i];
                    } else if (args[i].Equals("-tasks", StringComparison.OrdinalIgnoreCase)) {
                        options.Cpu = int.Parse(args[++i]);
                    } else if (args[i].Equals("-playback", StringComparison.OrdinalIgnoreCase)) {
                        if (ulong.TryParse(args[i + 1], out ulong playback)) {
                            options.Playback = playback;
                            i++;
                        } else {
                            options.Playback = ulong.MaxValue;
                        }
                    } else if (args[i].Equals("-playspeed", StringComparison.OrdinalIgnoreCase)) {
                        options.PlaybackSpeed = int.Parse(args[++i]);
                    } else if (args[i].Equals("-minscore", StringComparison.OrdinalIgnoreCase)) {
                        options.MinScore = int.Parse(args[++i]);
                    } else if (args[i].Equals("-format", StringComparison.OrdinalIgnoreCase)) {
                        options.Format = args[++i];
                    } else if (args[i].Equals("-convert", StringComparison.OrdinalIgnoreCase)) {
                        options.Format = args[++i];
                        options.SkipGeneration = true;
                    }
                }
            } catch (Exception e) {
                throw new Exception($"Error parsing command line arguments: {e.Message}", e);
            }

            // Set default pattern database if not specified
            options.PatternDatabase ??= options.XmlDatabase;
            
            // Set seed to current milliseconds if not specified
            if (options.Seed == int.MaxValue) {
                options.Seed = DateTime.Now.Millisecond;
            }

            return options;
        }
    }
}
