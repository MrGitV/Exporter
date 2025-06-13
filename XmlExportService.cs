using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Exporter
{
    public class XmlExportService
    {
        private const int FlushBatchSize = 10000;

        // Asynchronously exports data records to an XML file with progress reporting and cancellation support
        public async Task ExportAsync(IAsyncEnumerable<DataRecord> records, string filePath, IProgress<string> progress = null, CancellationToken cancellationToken = default)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                Async = true,
                CloseOutput = true
            };

            using var writer = XmlWriter.Create(filePath, settings);

            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(null, "TestProgram", null);

            int count = 0;

            await foreach (var record in records.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await writer.WriteStartElementAsync(null, "Record", null);
                await writer.WriteAttributeStringAsync(null, "id", null, record.Id.ToString());

                await writer.WriteElementStringAsync(null, "Date", null, record.Date.ToString("yyyy-MM-dd"));
                await writer.WriteElementStringAsync(null, "FirstName", null, record.FirstName);
                await writer.WriteElementStringAsync(null, "LastName", null, record.LastName);
                await writer.WriteElementStringAsync(null, "SurName", null, record.SurName);
                await writer.WriteElementStringAsync(null, "City", null, record.City);
                await writer.WriteElementStringAsync(null, "Country", null, record.Country);

                await writer.WriteEndElementAsync();

                count++;

                if (count % FlushBatchSize == 0)
                {
                    progress?.Report($"Exported {count} records...");
                    await writer.FlushAsync();
                }
            }

            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
            await writer.FlushAsync();

            progress?.Report($"Export completed: {count} records.");
        }
    }
}
