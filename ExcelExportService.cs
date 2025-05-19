using ClosedXML.Excel;
using System.Collections.Generic;

namespace Exporter
{
    // Service for exporting data to an Excel file using the ClosedXML library
    public class ExcelExportService
    {
        public void Export(List<DataRecord> records, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Data");

                worksheet.Cell(1, 1).Value = "Date";
                worksheet.Cell(1, 2).Value = "First Name";
                worksheet.Cell(1, 3).Value = "Last Name";
                worksheet.Cell(1, 4).Value = "SurName";
                worksheet.Cell(1, 5).Value = "City";
                worksheet.Cell(1, 6).Value = "Country";

                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    int row = i + 2;

                    worksheet.Cell(row, 1).Value = record.Date.ToString("dd.MM.yyyy");
                    worksheet.Cell(row, 2).Value = record.FirstName;
                    worksheet.Cell(row, 3).Value = record.LastName;
                    worksheet.Cell(row, 4).Value = record.SurName;
                    worksheet.Cell(row, 5).Value = record.City;
                    worksheet.Cell(row, 6).Value = record.Country;
                }

                workbook.SaveAs(filePath);
            }
        }
    }
}