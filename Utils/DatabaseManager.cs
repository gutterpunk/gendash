using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GenDash.Engine.Formats;

namespace GenDash.Utils {
    /// <summary>
    /// Manages database loading, saving, and merging operations.
    /// </summary>
    class DatabaseManager {
        private readonly string _databaseName;
        private readonly IFormatConverter _converter;
        private readonly string _currentDirectory;

        public string FilePath { get; private set; }
        public string FilePathWithoutExt { get; private set; }

        public DatabaseManager(string databaseName, IFormatConverter converter) {
            _databaseName = databaseName;
            _converter = converter;
            _currentDirectory = Directory.GetCurrentDirectory();
            FilePath = Path.Combine(_currentDirectory, databaseName);
            FilePathWithoutExt = Path.Combine(_currentDirectory, Path.GetFileNameWithoutExtension(databaseName));
        }

        /// <summary>
        /// Loads the puzzle database, handling multiple formats and fallbacks.
        /// </summary>
        public XElement LoadDatabase() {
            XElement puzzledb;
            XElement xmlPuzzledb = null;

            // Try to load XML version for hash preservation
            if (File.Exists(FilePath)) {
                try {
                    xmlPuzzledb = XElement.Load(FilePath);
                    Console.WriteLine($"Loaded hashes from XML: {FilePath}");
                } catch (Exception ex) {
                    Console.WriteLine($"Warning: Could not load XML for hash preservation: {ex.Message}");
                }
            }

            // Try to load database in the specified format
            string formatPath = Path.ChangeExtension(FilePathWithoutExt, _converter.FileExtension);
            if (File.Exists(formatPath)) {
                try {
                    puzzledb = _converter.Load(FilePathWithoutExt);
                    Console.WriteLine($"Loaded database from {Path.GetFileName(formatPath)}");

                    // Merge hashes from XML if available
                    if (xmlPuzzledb != null) {
                        MergeHashesFromXml(puzzledb, xmlPuzzledb);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error loading from format: {ex.Message}");
                    Console.WriteLine("Attempting to load from XML...");
                    puzzledb = xmlPuzzledb ?? (File.Exists(FilePath) ? XElement.Load(FilePath) : new XElement("GenDash"));
                }
            } else if (xmlPuzzledb != null) {
                puzzledb = xmlPuzzledb;
            } else if (File.Exists(FilePath)) {
                puzzledb = XElement.Load(FilePath);
            } else {
                puzzledb = new XElement("GenDash");
            }

            // Ensure required elements exist
            if (puzzledb.Element("Boards") == null) {
                puzzledb.Add(new XElement("Boards"));
            }
            if (puzzledb.Element("Rejects") == null) {
                puzzledb.Add(new XElement("Rejects"));
            }

            return puzzledb;
        }

        /// <summary>
        /// Saves the puzzle database in the specified format.
        /// </summary>
        public void SaveDatabase(XElement puzzledb) {
            _converter.Save(puzzledb, FilePathWithoutExt);
            Console.WriteLine($"Database saved to {Path.GetFileName(Path.ChangeExtension(FilePathWithoutExt, _converter.FileExtension))}");
        }

        /// <summary>
        /// Loads board hashes from the database.
        /// </summary>
        public HashSet<ulong> LoadBoardHashes(XElement puzzledb) {
            var hashes = new HashSet<ulong>(
                from puzzle in puzzledb.Descendants("Board")
                select (ulong)puzzle.Element("Hash")
            );
            Console.WriteLine($"Boards loaded from {_databaseName} : {hashes.Count}");
            return hashes;
        }

        /// <summary>
        /// Loads reject hashes from the database.
        /// </summary>
        public HashSet<ulong> LoadRejectHashes(XElement puzzledb) {
            return new HashSet<ulong>(
                from puzzle in puzzledb.Descendants("Rejects").Descendants("Reject")
                select (ulong)puzzle.Element("Hash")
            );
        }

        /// <summary>
        /// Merges hashes and rejects from XML database into the loaded puzzledb.
        /// </summary>
        private void MergeHashesFromXml(XElement puzzledb, XElement xmlPuzzledb) {
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
    }
}
