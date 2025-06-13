using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Exporter
{
    public class CsvParserService
    {
        // Parses CSV file asynchronously and returns records as an async stream
        public async IAsyncEnumerable<DataRecord> ParseFileAsync(string filePath)
        {
            using var reader = new StreamReader(filePath);
            int lineNumber = 0;

            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                if (parts.Length != 6)
                    throw new FormatException($"Invalid CSV format at line {lineNumber}");

                if (!DateTime.TryParseExact(parts[0].Trim(), new[] { "dd.MM.yyyy", "d.M.yyyy" },
                    CultureInfo.CreateSpecificCulture("ru-RU"), DateTimeStyles.None, out var date))
                {
                    throw new FormatException($"Invalid date format at line {lineNumber}: {parts[0]}");
                }

                yield return new DataRecord
                {
                    Date = date,
                    FirstName = parts[1].Trim(),
                    LastName = parts[2].Trim(),
                    SurName = parts[3].Trim(),
                    City = parts[4].Trim(),
                    Country = parts[5].Trim()
                };
            }
        }
    }
}