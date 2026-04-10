using System;
using System.Collections.Generic;
using System.IO;
using FABBatchValidator.Models;
using OfficeOpenXml;

namespace FABBatchValidator.Excel
{
    // Writes bibliographic records with validation results to an Excel output file.
    // Original BiblioRecord columns (11) + 5 validation columns = 16 total.
    public class ExcelOutputWriter
    {
        private readonly string _outputFilePath;

        // Path to the output Excel file to create/overwrite.
        public ExcelOutputWriter(string outputFilePath)
        {
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("Output file path cannot be empty.", nameof(outputFilePath));

            _outputFilePath = outputFilePath;
        }

        // Write records with their validation results to the output Excel file.
        // List of BiblioRecord + ValidationResult pairs.
        public void WriteResults(List<(BiblioRecord Record, ValidationResult Result)> records)
        {
            if (records == null || records.Count == 0)
                throw new ArgumentException("Records cannot be null or empty.", nameof(records));

            try
            {
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Validated Records");

                    WriteHeaderRow(ws);

                    for (int i = 0; i < records.Count; i++)
                    {
                        WriteDataRow(ws, i + 2, records[i]);
                    }

                    var file = new FileInfo(_outputFilePath);
                    package.SaveAs(file);
                }
            }
            catch (Exception ex)
            {
                throw new ExcelWriteException(
                    $"Failed to write validation results to {_outputFilePath}: {ex.Message}", ex);
            }
        }

        // Write header row to Excel sheet.
        private void WriteHeaderRow(ExcelWorksheet ws)
        {
            var headers = new[]
            {
                "PMID", "Title", "Abstract", "MeSHTerms", "Chemicals", "Authors",
                "JournalName", "ISSN", "PublicationYear", "Language", "Country",
                "ValidationCategory", "Confidence", "CandidatePmId", "Rationale", "IsMultipleCandidates"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
            }
        }

        // Write a single data row to Excel sheet.
        private void WriteDataRow(
            ExcelWorksheet ws,
            int rowIndex,
            (BiblioRecord Record, ValidationResult Result) recordPair)
        {
            var record = recordPair.Record;
            var result = recordPair.Result;

            ws.Cells[rowIndex, 1].Value  = record.PMID;
            ws.Cells[rowIndex, 2].Value  = record.Title;
            ws.Cells[rowIndex, 3].Value  = record.Abstract;
            ws.Cells[rowIndex, 4].Value  = record.MeSHTerms;
            ws.Cells[rowIndex, 5].Value  = record.Chemicals;
            ws.Cells[rowIndex, 6].Value  = record.Authors;
            ws.Cells[rowIndex, 7].Value  = record.JournalName;
            ws.Cells[rowIndex, 8].Value  = record.ISSN;
            ws.Cells[rowIndex, 9].Value  = record.PublicationYear;
            ws.Cells[rowIndex, 10].Value = record.Language;
            ws.Cells[rowIndex, 11].Value = record.Country;

            ws.Cells[rowIndex, 12].Value = result.Category.ToString();
            ws.Cells[rowIndex, 13].Value = result.Confidence;
            ws.Cells[rowIndex, 14].Value = result.CandidatePmId;
            ws.Cells[rowIndex, 15].Value = result.Rationale;
            ws.Cells[rowIndex, 16].Value = result.IsMultipleCandidates;
        }
    }

    // Exception thrown when Excel file writing fails.
    public class ExcelWriteException : Exception
    {
        public ExcelWriteException(string message) : base(message) { }
        public ExcelWriteException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}