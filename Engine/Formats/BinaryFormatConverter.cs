using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GenDash.Engine.Formats
{
    /// <summary>
    /// Minimal binary format converter for puzzle database
    /// Format structure:
    /// - Header: "GENDASH\0" (8 bytes magic)
    /// - Version: uint (4 bytes) = 1
    /// - BoardCount: uint (4 bytes)
    /// - RejectCount: uint (4 bytes)
    /// - Boards data (variable length)
    /// - Rejects data (variable length)
    /// </summary>
    public class BinaryFormatConverter : IFormatConverter
    {
        private const string MAGIC = "GENDASH";
        private const uint VERSION = 1;

        public string FileExtension => ".gdb";

        public void Save(XElement puzzleDb, string filePath)
        {
            string fullPath = Path.ChangeExtension(filePath, FileExtension);

            using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs, Encoding.UTF8);
            // Write header
            writer.Write(Encoding.ASCII.GetBytes(MAGIC));
            writer.Write((byte)0); // null terminator
            writer.Write(VERSION);

            var boards = puzzleDb.Descendants("Board").ToList();
            // var rejects = puzzleDb.Descendants("Reject").ToList();

            writer.Write((uint)boards.Count);
            writer.Write((uint)0);// rejects.Count);

            // Write boards
            foreach (var board in boards)
            {
                WriteBoard(writer, board);
            }

            // Write rejects
            // foreach (var reject in rejects)
            // {
            //     WriteReject(writer, reject);
            // }
        }

        public XElement Load(string filePath)
        {
            string fullPath = Path.ChangeExtension(filePath, FileExtension);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Binary database file not found: {fullPath}");
            }

            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs, Encoding.UTF8);
            // Read and validate header
            byte[] magicBytes = reader.ReadBytes(8);
            string magic = Encoding.ASCII.GetString(magicBytes, 0, 7);
            if (magic != MAGIC)
            {
                throw new InvalidDataException($"Invalid binary format: magic bytes mismatch");
            }

            uint version = reader.ReadUInt32();
            if (version != VERSION)
            {
                throw new InvalidDataException($"Unsupported binary format version: {version}");
            }

            uint boardCount = reader.ReadUInt32();
            uint rejectCount = reader.ReadUInt32();

            var puzzleDb = new XElement("GenDash");
            var boardsNode = new XElement("Boards");
            var rejectsNode = new XElement("Rejects");

            // Read boards
            for (uint i = 0; i < boardCount; i++)
            {
                boardsNode.Add(ReadBoard(reader));
            }

            // Read rejects
            for (uint i = 0; i < rejectCount; i++)
            {
                rejectsNode.Add(ReadReject(reader));
            }

            puzzleDb.Add(boardsNode);
            puzzleDb.Add(rejectsNode);

            return puzzleDb;
        }

        private static void WriteBoard(BinaryWriter writer, XElement board)
        {
            writer.Write((ulong)board.Element("Hash"));
            writer.Write((byte)(int)board.Element("Width"));
            writer.Write((byte)(int)board.Element("Height"));
            writer.Write((int)board.Element("Par"));
            writer.Write((int)board.Element("StartX"));
            writer.Write((int)board.Element("StartY"));
            writer.Write((int)board.Element("ExitX"));
            writer.Write((int)board.Element("ExitY"));
            writer.Write((int)board.Element("Idle"));
            WriteString(writer, (string)board.Element("Data"));

            // Write solution
            var solution = board.Element("Solution");
            if (solution != null)
            {
                writer.Write(true); // has solution
                
                // Write solution attributes
                // writer.Write((int)solution.Attribute("AvgDiff"));
                // writer.Write((int)solution.Attribute("AvgGoals"));
                // writer.Write((int)solution.Attribute("AvgBefore"));
                // writer.Write((int)solution.Attribute("AvgAfter"));
                // writer.Write((int)solution.Attribute("FallingDelta"));
                // writer.Write((int)solution.Attribute("Proximity"));
                writer.Write((int)solution.Attribute("Score"));

                var folds = solution.Elements("Fold").ToList();
                writer.Write((ushort)folds.Count);

                foreach (var fold in folds)
                {
                    WriteString(writer, (string)fold.Attribute("Move"));
                    WriteString(writer, fold.Value);
                }
            }
            else
            {
                writer.Write(false); // no solution
            }
        }

        private static XElement ReadBoard(BinaryReader reader)
        {
            ulong hash = reader.ReadUInt64();
            byte width = reader.ReadByte();
            byte height = reader.ReadByte();
            int par = reader.ReadInt32();
            int startX = reader.ReadInt32();
            int startY = reader.ReadInt32();
            int exitX = reader.ReadInt32();
            int exitY = reader.ReadInt32();
            int idle = reader.ReadInt32();
            string data = ReadString(reader);

            var board = new XElement("Board",
                new XElement("Hash", hash),
                new XElement("Width", width),
                new XElement("Height", height),
                new XElement("Par", par),
                new XElement("StartX", startX),
                new XElement("StartY", startY),
                new XElement("ExitX", exitX),
                new XElement("ExitY", exitY),
                new XElement("Idle", idle),
                new XElement("Data", data)
            );

            bool hasSolution = reader.ReadBoolean();
            if (hasSolution)
            {
                int avgDiff = reader.ReadInt32();
                int avgGoals = reader.ReadInt32();
                int avgBefore = reader.ReadInt32();
                int avgAfter = reader.ReadInt32();
                int fallingDelta = reader.ReadInt32();
                int proximity = reader.ReadInt32();
                int score = reader.ReadInt32();

                var solution = new XElement("Solution");
                solution.SetAttributeValue("AvgDiff", avgDiff);
                solution.SetAttributeValue("AvgGoals", avgGoals);
                solution.SetAttributeValue("AvgBefore", avgBefore);
                solution.SetAttributeValue("AvgAfter", avgAfter);
                solution.SetAttributeValue("FallingDelta", fallingDelta);
                solution.SetAttributeValue("Proximity", proximity);
                solution.SetAttributeValue("Score", score);

                ushort foldCount = reader.ReadUInt16();
                for (int i = 0; i < foldCount; i++)
                {
                    string move = ReadString(reader);
                    string foldData = ReadString(reader);
                    var fold = new XElement("Fold", foldData);
                    fold.SetAttributeValue("Move", move);
                    solution.Add(fold);
                }

                board.Add(solution);
            }

            return board;
        }


        private static XElement ReadReject(BinaryReader reader)
        {
            ulong hash = reader.ReadUInt64();
            string reason = ReadString(reader);

            var reject = new XElement("Reject",
                new XElement("Hash", hash),
                new XElement("Reason", reason)
            );

            bool hasDetails = reader.ReadBoolean();
            if (hasDetails)
            {
                byte width = reader.ReadByte();
                byte height = reader.ReadByte();
                int startX = reader.ReadInt32();
                int startY = reader.ReadInt32();
                int exitX = reader.ReadInt32();
                int exitY = reader.ReadInt32();
                int idle = reader.ReadInt32();

                reject.Add(new XElement("Width", width));
                reject.Add(new XElement("Height", height));
                reject.Add(new XElement("StartX", startX));
                reject.Add(new XElement("StartY", startY));
                reject.Add(new XElement("ExitX", exitX));
                reject.Add(new XElement("ExitY", exitY));
                reject.Add(new XElement("Idle", idle));
            }

            bool hasData = reader.ReadBoolean();
            if (hasData)
            {
                string data = ReadString(reader);
                reject.Add(new XElement("Data", data));
            }

            bool hasSolution = reader.ReadBoolean();
            if (hasSolution)
            {
                int avgDiff = reader.ReadInt32();
                int avgGoals = reader.ReadInt32();
                int avgBefore = reader.ReadInt32();
                int avgAfter = reader.ReadInt32();
                int fallingDelta = reader.ReadInt32();
                int proximity = reader.ReadInt32();
                int score = reader.ReadInt32();

                var solution = new XElement("Solution");
                solution.SetAttributeValue("AvgDiff", avgDiff);
                solution.SetAttributeValue("AvgGoals", avgGoals);
                solution.SetAttributeValue("AvgBefore", avgBefore);
                solution.SetAttributeValue("AvgAfter", avgAfter);
                solution.SetAttributeValue("FallingDelta", fallingDelta);
                solution.SetAttributeValue("Proximity", proximity);
                solution.SetAttributeValue("Score", score);

                ushort foldCount = reader.ReadUInt16();
                for (int i = 0; i < foldCount; i++)
                {
                    string move = ReadString(reader);
                    string foldData = ReadString(reader);
                    var fold = new XElement("Fold", foldData);
                    fold.SetAttributeValue("Move", move);
                    solution.Add(fold);
                }

                reject.Add(solution);
            }

            return reject;
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write((ushort)0);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                writer.Write((ushort)bytes.Length);
                writer.Write(bytes);
            }
        }

        private static string ReadString(BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            if (length == 0)
            {
                return string.Empty;
            }
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
