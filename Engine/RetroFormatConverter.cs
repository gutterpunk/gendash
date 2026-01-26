using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GenDash.Engine
{
    /// <summary>
    /// Retro format converter for SGDK and older hardware compilers.
    /// Ultra-compact format using RLE compression and packed data.
    /// </summary>
    internal class RetroFormatConverter : IFormatConverter
    {
        private const string MAGIC = "GD1";
        
        public string FileExtension => ".gdr";

        public void Save(XElement puzzleDb, string filePath)
        {
            string fullPath = Path.ChangeExtension(filePath, FileExtension);
            
            var boards = puzzleDb.Descendants("Board")
                .Where(b => b.Element("Solution") != null)
                .ToList();

            using FileStream fs = new(fullPath, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(fs, Encoding.ASCII);
            writer.Write(Encoding.ASCII.GetBytes(MAGIC));
            writer.Write((ushort)boards.Count);

            foreach (var board in boards)
            {
                WriteBoardCompact(writer, board);
            }
        }

        public XElement Load(string filePath)
        {
            string fullPath = Path.ChangeExtension(filePath, FileExtension);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Retro format file not found: {fullPath}");
            }

            using FileStream fs = new(fullPath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fs, Encoding.ASCII);
            byte[] magicBytes = reader.ReadBytes(3);
            string magic = Encoding.ASCII.GetString(magicBytes);
            if (magic != MAGIC)
            {
                throw new InvalidDataException($"Invalid retro format: expected '{MAGIC}', got '{magic}'");
            }

            ushort boardCount = reader.ReadUInt16();

            XElement puzzleDb = new("GenDash");
            XElement boardsNode = new("Boards");


            for (int i = 0; i < boardCount; i++)
            {
                boardsNode.Add(ReadBoardCompact(reader));
            }

            puzzleDb.Add(boardsNode);
            puzzleDb.Add(new XElement("Rejects"));

            return puzzleDb;
        }

        private static void WriteBoardCompact(BinaryWriter writer, XElement board)
        {
            byte width = (byte)(int)board.Element("Width");
            byte height = (byte)(int)board.Element("Height");
            
            if (width <= 15 && height <= 15)
            {
                writer.Write((byte)((width << 4) | height));
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write(width);
                writer.Write(height);
            }

            byte startX = (byte)(int)board.Element("StartX");
            byte startY = (byte)(int)board.Element("StartY");
            byte exitX = (byte)(int)board.Element("ExitX");
            byte exitY = (byte)(int)board.Element("ExitY");

            if (startX < 16 && startY < 16)
            {
                writer.Write((byte)((startX << 4) | startY));
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write(startX);
                writer.Write(startY);
            }

            if (exitX < 16 && exitY < 16)
            {
                writer.Write((byte)((exitX << 4) | exitY));
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write(exitX);
                writer.Write(exitY);
            }

            int par = (int)board.Element("Par");
            int idle = (int)board.Element("Idle");
            
            if (par < 256)
            {
                writer.Write((byte)par);
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write((ushort)par);
            }
            
            writer.Write((byte)idle);

            string data = (string)board.Element("Data");
            WriteRLE(writer, data);

            var solution = board.Element("Solution");
            if (solution != null)
            {
                int score = (int)solution.Attribute("Score");
                if (score < 256)
                {
                    writer.Write((byte)score);
                }
                else
                {
                    writer.Write((byte)0xFF);
                    writer.Write((ushort)score);
                }

                var folds = solution.Elements("Fold").ToList();
                writer.Write((byte)folds.Count);

                foreach (var fold in folds)
                {
                    string move = (string)fold.Attribute("Move");
                    writer.Write((byte)(move.Length > 0 ? move[0] : '?'));
                    
                    WriteRLE(writer, fold.Value);
                }
            }
        }

        private static XElement ReadBoardCompact(BinaryReader reader)
        {
            byte widthHeight = reader.ReadByte();
            byte width, height;
            if (widthHeight == 0xFF)
            {
                width = reader.ReadByte();
                height = reader.ReadByte();
            }
            else
            {
                width = (byte)(widthHeight >> 4);
                height = (byte)(widthHeight & 0x0F);
            }

            byte startPacked = reader.ReadByte();
            byte startX, startY;
            if (startPacked == 0xFF)
            {
                startX = reader.ReadByte();
                startY = reader.ReadByte();
            }
            else
            {
                startX = (byte)(startPacked >> 4);
                startY = (byte)(startPacked & 0x0F);
            }

            byte exitPacked = reader.ReadByte();
            byte exitX, exitY;
            if (exitPacked == 0xFF)
            {
                exitX = reader.ReadByte();
                exitY = reader.ReadByte();
            }
            else
            {
                exitX = (byte)(exitPacked >> 4);
                exitY = (byte)(exitPacked & 0x0F);
            }

            byte parByte = reader.ReadByte();
            int par = parByte == 0xFF ? reader.ReadUInt16() : parByte;

            byte idle = reader.ReadByte();

            string data = ReadRLE(reader);

            ulong hash = HashUtility.ComputeFNV1aHash(width, height, startX, startY, exitX, exitY, idle, data);

            XElement board = new("Board",
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

            byte scoreByte = reader.ReadByte();
            int score = scoreByte == 0xFF ? reader.ReadUInt16() : scoreByte;

            XElement solution = new("Solution");
            solution.SetAttributeValue("Score", score);
            solution.SetAttributeValue("AvgDiff", 0);
            solution.SetAttributeValue("AvgGoals", 0);
            solution.SetAttributeValue("AvgBefore", 0);
            solution.SetAttributeValue("AvgAfter", 0);
            solution.SetAttributeValue("FallingDelta", 0);
            solution.SetAttributeValue("Proximity", 0);

            byte foldCount = reader.ReadByte();
            for (int i = 0; i < foldCount; i++)
            {
                char moveChar = (char)reader.ReadByte();
                string foldData = ReadRLE(reader);
                
                XElement fold = new("Fold", foldData);
                fold.SetAttributeValue("Move", moveChar.ToString());
                solution.Add(fold);
            }

            board.Add(solution);
            return board;
        }

        private static void WriteRLE(BinaryWriter writer, string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                writer.Write((ushort)0);
                return;
            }

            using MemoryStream ms = new();
            using BinaryWriter rleWriter = new(ms, Encoding.ASCII);
            int i = 0;
            while (i < input.Length)
            {
                char current = input[i];
                int count = 1;

                while (i + count < input.Length && input[i + count] == current && count < 255)
                {
                    count++;
                }

                rleWriter.Write((byte)count);
                rleWriter.Write((byte)current);

                i += count;
            }

            byte[] compressed = ms.ToArray();
            writer.Write((ushort)compressed.Length);
            writer.Write(compressed);
        }

        private static string ReadRLE(BinaryReader reader)
        {
            ushort compressedLength = reader.ReadUInt16();
            if (compressedLength == 0)
            {
                return string.Empty;
            }

            byte[] compressed = reader.ReadBytes(compressedLength);
            StringBuilder result = new();

            for (int i = 0; i < compressed.Length; i += 2)
            {
                byte count = compressed[i];
                char ch = (char)compressed[i + 1];
                result.Append(ch, count);
            }

            return result.ToString();
        }
    }
}
