using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Exporter
{
    // Handles XML export functionality
    public class XmlExportService
    {
        // Exports data to XML with error handling
        public void Export(List<DataRecord> records, string filePath)
        {
            try
            {
                // Create XML structure
                var xmlDocument = new XDocument(
                    new XElement("TestProgram",
                        from record in records
                        select new XElement("Record",
                            new XAttribute("id", record.Id),
                            new XElement("Date", record.Date.ToString("yyyy-MM-dd")),
                            new XElement("FirstName", record.FirstName),
                            new XElement("LastName", record.LastName),
                            new XElement("SurName", record.SurName),
                            new XElement("City", record.City),
                            new XElement("Country", record.Country)
                        )
                    )
                );

                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                xmlDocument.Save(filePath, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("XML export failed", ex);
            }
        }
    }
}