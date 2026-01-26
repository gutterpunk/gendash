using System.Xml.Linq;

namespace GenDash.Engine
{
    public interface IFormatConverter
    {
        string FileExtension { get; }

        XElement Load(string filePath);
        void Save(XElement puzzleDb, string filePath);
    }
}