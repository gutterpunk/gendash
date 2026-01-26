using System;

namespace GenDash.Engine
{
    /// <summary>
    /// Factory for creating format converters based on format name
    /// </summary>
    public static class FormatConverterFactory
    {
        /// <summary>
        /// Creates a format converter based on the format name
        /// </summary>
        /// <param name="format">Format name (xml, binary, bin, gdb)</param>
        public static IFormatConverter CreateConverter(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return new XmlFormatConverter();
            }

            switch (format.ToLowerInvariant())
            {
                case "xml":
                    return new XmlFormatConverter();
                
                case "binary":
                case "bin":
                case "gdb":
                    return new BinaryFormatConverter();
                
                default:
                    throw new ArgumentException($"Unknown format: {format}. Supported formats: xml, binary");
            }
        }

        /// <summary>
        /// Gets the default file extension for a format
        /// </summary>
        /// <param name="format">Format name</param>
        public static string GetExtension(string format)
        {
            var converter = CreateConverter(format);
            return converter.FileExtension;
        }
    }
}
