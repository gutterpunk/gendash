using System.IO;
using System.Xml.Linq;

namespace GenDash.Engine
{
    /// <summary>
    /// Default XML format converter (no conversion, just saves as XML)
    /// </summary>
    public class XmlFormatConverter : IFormatConverter
    {
        public string FileExtension => ".xml";

        public void Save(XElement puzzleDb, string filePath)
        {
            string fullPath = Path.ChangeExtension(filePath, FileExtension);
            puzzleDb.Save(fullPath);
        }

        public XElement Load(string filePath)
        {
            string fullPath = Path.ChangeExtension(filePath, FileExtension);
            return XElement.Load(fullPath);
        }
    }
}
