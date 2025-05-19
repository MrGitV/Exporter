using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Exporter
{
    // Handles CSV file parsing and validation
    public class CsvParserService
    {
        // Parses CSV file with data validation
        public IEnumerable<DataRecord> ParseFile(string filePath)
        {
            var culture = CultureInfo.InvariantCulture;
            var dateStyles = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal;

            using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                string line;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    // Validate line format
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(';');
                    if (parts.Length != 6)
                        throw new FormatException($"Invalid CSV format at line {lineNumber}");

                    // Parse and validate date
                    if (!DateTime.TryParseExact(
                        parts[0].Trim(),
                        new[] { "dd.MM.yyyy", "yyyy-MM-dd" },
                        culture,
                        dateStyles,
                        out DateTime date))
                    {
                        throw new FormatException($"Invalid date format at line {lineNumber}: {parts[0]}");
                    }

                    yield return new DataRecord
                    {
                        Date = date.Date,
                        FirstName = Sanitize(parts[1]),
                        LastName = Sanitize(parts[2]),
                        SurName = Sanitize(parts[3]),
                        City = Sanitize(parts[4]),
                        Country = Sanitize(parts[5])
                    };
                }
            }
        }

        // Sanitizes input and enforces length limits
        private string Sanitize(string input) =>
            string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim();
    }
}