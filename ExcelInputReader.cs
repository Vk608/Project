using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FABBatchValidator.Configuration;
using FABBatchValidator.Models;
using OfficeOpenXml;

namespace FABBatchValidator.Excel
{
    /// Exception thrown when Excel input reading or validation fails.
    public class ExcelInputException : Exception
    {
        public ExcelInputException(string message) : base(message) { }
        public ExcelInputException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// Reads bibliographic records from an Excel file and validates the input schema.
    /// 
    /// Responsibility:
    /// - Open and read Excel file specified in configuration
    /// - Locate the correct worksheet
    /// - Validate that required columns exist
    /// - Extract row data into BiblioRecord objects
    /// - Provide meaningful error messages if reading/validation fails
    /// 
    /// Design:
    /// EPPlus library recommended for Excel I/O.
    /// BiblioRecord class and validation logic are library-agnostic.
    /// To use: dotnet add package EPPlus
    public class ExcelInputReader
    {
        private readonly FileHandlingConfiguration _fileConfig;
        private readonly InputSchemaConfiguration _schemaConfig;

        public ExcelInputReader(FileHandlingConfiguration fileConfig, InputSchemaConfiguration schemaConfig)
        {
            _fileConfig = fileConfig ?? throw new ArgumentNullException(nameof(fileConfig));
            _schemaConfig = schemaConfig ?? throw new ArgumentNullException(nameof(schemaConfig));
        }

        public List<BiblioRecord> ReadRecords(string filePath = null)
        {
            try
            {
                var resolvedFilePath = !string.IsNullOrWhiteSpace(filePath) 
                    ? filePath 
                    : _fileConfig.InputFilePath;

                if (!File.Exists(resolvedFilePath))
                    throw new ExcelInputException($"Input file not found: {resolvedFilePath}");

                using (var package = new ExcelPackage(new FileInfo(resolvedFilePath)))
                {
                    var worksheet = package.Workbook.Worksheets[_fileConfig.InputSheetName] 
                        ?? package.Workbook.Worksheets[0];
                    
                    if (worksheet == null)
                        throw new ExcelInputException($"Sheet '{_fileConfig.InputSheetName}' not found in workbook.");

                    var headerRow = ExtractHeaderRow(worksheet);
                    ValidateRequiredColumns(headerRow);
                    var records = ExtractDataRows(worksheet, headerRow);

                    return records;
                }
            }
            catch (ExcelInputException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExcelInputException(
                    $"Failed to read Excel file '{_fileConfig.InputFilePath}': {ex.Message}", ex);
            }
        }

        private Dictionary<string, int> ExtractHeaderRow(ExcelWorksheet worksheet)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int col = 1; col <= worksheet.Dimension?.Columns; col++)
            {
                var headerText = worksheet.Cells[1, col].Value?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(headerText))
                {
                    // Store both the original and normalized column name for matching
                    headerMap[headerText] = col - 1;
                    var normalized = NormalizeColumnName(headerText);
                    if (normalized != headerText && !headerMap.ContainsKey(normalized))
                        headerMap[normalized] = col - 1;
                }
            }
            return headerMap;
        }

        private string NormalizeColumnName(string columnName)
        {
            // Remove spaces and convert to consistent format for matching
            return System.Text.RegularExpressions.Regex.Replace(columnName, @"\s+", "");
        }

        private void ValidateRequiredColumns(Dictionary<string, int> headerMap)
        {
            var missingColumns = new List<string>();

            foreach (var requiredColumn in _schemaConfig.RequiredColumns)
            {
                // Try exact match first, then normalized match
                if (!headerMap.ContainsKey(requiredColumn) && !headerMap.ContainsKey(NormalizeColumnName(requiredColumn)))
                    missingColumns.Add(requiredColumn);
            }

            if (missingColumns.Count > 0)
            {
                var availableColumns = string.Join(", ", headerMap.Keys.Where(k => !k.Contains(" ")).OrderBy(x => x));
                var missing = string.Join(", ", missingColumns);
                throw new ExcelInputException(
                    $"Missing required columns: {missing}\nAvailable columns: {availableColumns}");
            }
        }

        private List<BiblioRecord> ExtractDataRows(ExcelWorksheet worksheet, Dictionary<string, int> headerMap)
        {
            var records = new List<BiblioRecord>();
            int lastRow = worksheet.Dimension?.Rows ?? 1;

            for (int excelRow = 2; excelRow <= lastRow; excelRow++)
            {
                var record = ExtractRowData(worksheet, excelRow, headerMap);
                records.Add(record);
            }
            return records;
        }

        private BiblioRecord ExtractRowData(ExcelWorksheet worksheet, int excelRow, Dictionary<string, int> headerMap)
        {
            Func<string, string> getCellValue = (columnName) =>
            {
                // Try exact match first, then normalized match
                if (!headerMap.TryGetValue(columnName, out var colIndex))
                {
                    var normalized = NormalizeColumnName(columnName);
                    if (!headerMap.TryGetValue(normalized, out colIndex))
                        return string.Empty;
                }
                return worksheet.Cells[excelRow, colIndex + 1].Value?.ToString() ?? string.Empty;
            };

            return new BiblioRecord
            {
                PMID = getCellValue("PMID"),
                Title = getCellValue("Title"),
                Abstract = getCellValue("Abstract"),
                MeSHTerms = getCellValue("MeSHTerms"),
                Chemicals = getCellValue("Chemicals"),
                Authors = getCellValue("Authors"),
                JournalName = getCellValue("JournalName"),
                ISSN = getCellValue("ISSN"),
                PublicationYear = getCellValue("PublicationYear"),
                Language = getCellValue("Language"),
                Country = getCellValue("Country")
            };
        }
    }

}
