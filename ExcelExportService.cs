using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Exporter
{
    public class ExcelExportService
    {
        private const int MaxExcelRows = 1048576;
        private const int BatchSize = 10000;

        // Asynchronously exports data records to an Excel file with batch processing, progress reporting, and cancellation support
        public async Task ExportAsync(IAsyncEnumerable<DataRecord> records, string filePath, IProgress<string> progress = null, CancellationToken cancellationToken = default)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");

            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "First Name";
            worksheet.Cell(1, 3).Value = "Last Name";
            worksheet.Cell(1, 4).Value = "SurName";
            worksheet.Cell(1, 5).Value = "City";
            worksheet.Cell(1, 6).Value = "Country";

            int currentRow = 2;
            int count = 0;

            var buffer = new List<DataRecord>(BatchSize);

            await foreach (var record in records.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                buffer.Add(record);
                count++;

                if (buffer.Count >= BatchSize)
                {
                    WriteBuffer(worksheet, buffer, ref currentRow);
                    buffer.Clear();

                    progress?.Report($"Exported {count} records...");
                    await Task.Run(() => workbook.SaveAs(filePath), cancellationToken).ConfigureAwait(false);
                }

                if (currentRow > MaxExcelRows)
                    throw new InvalidOperationException($"Excel row limit reached ({MaxExcelRows} rows). Export aborted.");
            }

            if (buffer.Count > 0)
            {
                WriteBuffer(worksheet, buffer, ref currentRow);
            }

            await Task.Run(() => workbook.SaveAs(filePath), cancellationToken).ConfigureAwait(false);
            progress?.Report($"Export completed: {count} records.");
        }

        // Helper method to write a batch of records to the worksheet starting at a given row
        private void WriteBuffer(IXLWorksheet worksheet, List<DataRecord> buffer, ref int startRow)
        {
            foreach (var record in buffer)
            {
                worksheet.Cell(startRow, 1).Value = record.Date.ToString("dd.MM.yyyy");
                worksheet.Cell(startRow, 2).Value = record.FirstName;
                worksheet.Cell(startRow, 3).Value = record.LastName;
                worksheet.Cell(startRow, 4).Value = record.SurName;
                worksheet.Cell(startRow, 5).Value = record.City;
                worksheet.Cell(startRow, 6).Value = record.Country;
                startRow++;
            }
        }
    }
}
