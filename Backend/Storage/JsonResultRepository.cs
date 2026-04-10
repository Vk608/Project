using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FABBatchValidator.Models;

namespace FABBatchValidator.Storage
{
    /// <summary>
    /// Persists and retrieves validated bibliographic records in JSON format.
    /// Each batch run overwrites the previous results.
    /// </summary>
    public class JsonResultRepository
    {
        private readonly string _resultsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonResultRepository(string resultsFilePath)
        {
            if (string.IsNullOrWhiteSpace(resultsFilePath))
                throw new ArgumentException("Results file path cannot be empty", nameof(resultsFilePath));

            _resultsFilePath = resultsFilePath;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Save validated records to JSON file.
        /// Overwrites any existing file.
        /// </summary>
        public async Task SaveAsync(List<ValidatedRecord> records)
        {
            try
            {
                var directory = Path.GetDirectoryName(_resultsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(records, _jsonOptions);
                await File.WriteAllTextAsync(_resultsFilePath, json);
            }
            catch (Exception ex)
            {
                throw new JsonResultRepositoryException(
                    $"Failed to save results to {_resultsFilePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load validated records from JSON file.
        /// Returns empty list if file doesn't exist.
        /// </summary>
        public async Task<List<ValidatedRecord>> LoadAsync()
        {
            try
            {
                if (!File.Exists(_resultsFilePath))
                    return new List<ValidatedRecord>();

                var json = await File.ReadAllTextAsync(_resultsFilePath);
                return JsonSerializer.Deserialize<List<ValidatedRecord>>(json, _jsonOptions)
                    ?? new List<ValidatedRecord>();
            }
            catch (Exception ex)
            {
                throw new JsonResultRepositoryException(
                    $"Failed to load results from {_resultsFilePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Synchronous wrapper around SaveAsync for compatibility.
        /// </summary>
        public void Save(List<ValidatedRecord> records)
            => SaveAsync(records).GetAwaiter().GetResult();

        /// <summary>
        /// Synchronous wrapper around LoadAsync for compatibility.
        /// </summary>
        public List<ValidatedRecord> Load()
            => LoadAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Get the path to the results JSON file.
        /// </summary>
        public string GetResultsFilePath()
        {
            return _resultsFilePath;
        }
    }

    /// <summary>Exception thrown when JSON repository operations fail.</summary>
    public class JsonResultRepositoryException : Exception
    {
        public JsonResultRepositoryException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
