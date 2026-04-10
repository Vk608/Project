using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FABBatchValidator.Agent;
using FABBatchValidator.Models;
using FABBatchValidator.QueryBuilder;
using FABBatchValidator.ResponseParsing;

namespace FABBatchValidator.Services
{
    /// <summary>
    /// Orchestrates the validation pipeline: read records → build query → call agent →
    /// parse response → create ValidatedRecord. No classification engine; Agent is source of truth.
    /// Handles resilience with single retry on failure.
    /// </summary>
    public class ValidationService
    {
        private readonly QueryTemplateBuilder _queryBuilder;
        private readonly AgentApiClient _agentClient;
        private readonly ResponseParser _responseParser;

        public ValidationService(
            QueryTemplateBuilder queryBuilder,
            AgentApiClient agentClient,
            ResponseParser responseParser)
        {
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
            _agentClient = agentClient ?? throw new ArgumentNullException(nameof(agentClient));
            _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
        }

        /// <summary>
        /// Validate a list of bibliographic records.
        /// Processes each record, retries on failure once, maintains error tracking.
        /// </summary>
        public async Task<ValidationServiceResult> ValidateRecordsAsync(List<BiblioRecord> records)
        {
            var result = new ValidationServiceResult();

            if (records == null || records.Count == 0)
                return result;

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                Console.WriteLine($"[ValidationService] Processing record {i + 1}/{records.Count} (PMID: {record.PMID ?? "N/A"})...");

                ValidatedRecord validatedRecord = await ProcessRecordWithRetryAsync(record);

                if (validatedRecord != null)
                {
                    result.ValidatedRecords.Add(validatedRecord);
                    result.SuccessfulRecords++;
                    Console.WriteLine($"[ValidationService]   SUCCESS. Confidence: {validatedRecord.ConfidenceScore:F2}");
                }
                else
                {
                    result.FailedRecords++;
                    Console.WriteLine($"[ValidationService]   FAILED after retry.");
                }
            }

            result.TotalRecords = records.Count;
            Console.WriteLine($"[ValidationService] Validation complete. Success: {result.SuccessfulRecords}, Failed: {result.FailedRecords}");

            return result;
        }

        /// <summary>
        /// Synchronous wrapper for backward compatibility.
        /// </summary>
        public ValidationServiceResult ValidateRecords(List<BiblioRecord> records)
            => ValidateRecordsAsync(records).GetAwaiter().GetResult();

        /// <summary>
        /// Process a single record with one automatic retry on failure.
        /// Returns null if both attempts fail.
        /// </summary>
        private async Task<ValidatedRecord> ProcessRecordWithRetryAsync(BiblioRecord record)
        {
            try
            {
                return await ProcessRecordAsync(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ValidationService]   First attempt failed: {ex.Message}. Retrying...");
                try
                {
                    await Task.Delay(1000); // Brief pause before retry
                    return await ProcessRecordAsync(record);
                }
                catch (Exception retryEx)
                {
                    Console.WriteLine($"[ValidationService]   Second attempt failed: {retryEx.Message}. Skipping record.");
                    return null;
                }
            }
        }

        /// <summary>
        /// Process a single record: build query → call agent → parse → convert to ValidatedRecord.
        /// </summary>
        private async Task<ValidatedRecord> ProcessRecordAsync(BiblioRecord record)
        {
            // Build query
            string query = _queryBuilder.BuildQuery(record);
            Console.WriteLine($"[ValidationService]   Query built ({query.Length} chars).");

            // Call agent API
            AgentResponse agentResponse = await _agentClient.SendQueryAsync(query);
            Console.WriteLine($"[ValidationService]   Agent response received.");

            // Parse response
            ParsedAgentResponse parsedResponse = _responseParser.Parse(agentResponse);
            Console.WriteLine($"[ValidationService]   Response parsed. Confidence: {parsedResponse.Confidence:F2}");

            // Convert to ValidatedRecord (Agent response is source of truth)
            var validatedRecord = new ValidatedRecord
            {
                InputPMID = record.PMID ?? string.Empty,
                MatchedPMID = parsedResponse.CandidatePmId,
                ConfidenceScore = parsedResponse.Confidence,
                Summary = parsedResponse.Content,
                OriginalTitle = record.Title ?? string.Empty,
                Match_Extent = parsedResponse.Match_Extent ?? string.Empty,
                Discrepancies_Logical = parsedResponse.Discrepancies_Logical ?? string.Empty,
                Discrepancies_Metadata = parsedResponse.Discrepancies_Metadata ?? string.Empty
            };

            return validatedRecord;
        }
    }

    /// <summary>
    /// Aggregated result from batch validation.
    /// </summary>
    public class ValidationServiceResult
    {
        public List<ValidatedRecord> ValidatedRecords { get; set; } = new();
        public int TotalRecords { get; set; } = 0;
        public int SuccessfulRecords { get; set; } = 0;
        public int FailedRecords { get; set; } = 0;

        public override string ToString()
        {
            return $"ValidationServiceResult [Total={TotalRecords}, Successful={SuccessfulRecords}, Failed={FailedRecords}]";
        }
    }
}
