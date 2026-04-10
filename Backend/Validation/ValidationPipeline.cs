using System;
using System.Collections.Generic;
using System.Linq;
using FABBatchValidator.Agent;
using FABBatchValidator.Configuration;
using FABBatchValidator.Excel;
using FABBatchValidator.Models;
using FABBatchValidator.QueryBuilder;
using FABBatchValidator.ResponseParsing;

namespace FABBatchValidator.Validation
{
    /// <summary>
    /// Orchestrates the full validation pipeline: Config → Excel Input → Query Building → 
    /// Agent Calling → Response Parsing → Classification → Excel Output.
    /// Sequential processing: one record at a time.
    /// </summary>
    public class ValidationPipeline
    {
        private readonly PipelineConfiguration _config;
        private readonly ExcelInputReader _inputReader;
        private readonly QueryTemplateBuilder _queryBuilder;
        private readonly AgentApiClient _agentClient;
        private readonly ResponseParser _responseParser;
        private readonly ClassificationEngine _classificationEngine;
        private readonly ExcelOutputWriter _outputWriter;

        public ValidationPipeline(
            PipelineConfiguration config,
            ExcelInputReader inputReader,
            QueryTemplateBuilder queryBuilder,
            AgentApiClient agentClient,
            ResponseParser responseParser,
            ClassificationEngine classificationEngine,
            ExcelOutputWriter outputWriter)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
            _agentClient = agentClient ?? throw new ArgumentNullException(nameof(agentClient));
            _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
            _classificationEngine = classificationEngine ?? throw new ArgumentNullException(nameof(classificationEngine));
            _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
        }

        /// <summary>
        /// Execute the full validation pipeline: read Excel → process each record → write output.
        /// </summary>
        /// <returns>PipelineResult with success status, record count, and error summary.</returns>
        public PipelineResult Execute()
        {
            var result = new PipelineResult();

            try
            {
                Console.WriteLine("[Pipeline] Starting validation pipeline...");

                // Step 1: Read input Excel file
                Console.WriteLine($"[Pipeline] Reading input file: {_config.DataProcessing.FileHandling.InputFilePath}");
                List<BiblioRecord> records = _inputReader.ReadRecords();
                result.TotalRecords = records.Count;
                Console.WriteLine($"[Pipeline] Loaded {records.Count} records from Excel.");

                // Step 2-6: Process each record sequentially
                var validatedRecords = new List<(BiblioRecord, ValidationResult)>();

                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    Console.WriteLine($"[Pipeline] Processing record {i + 1}/{records.Count} (PMID: {record.PMID ?? "N/A"})...");

                    try
                    {
                        // Step 3: Build query
                        string query = _queryBuilder.BuildQuery(record);
                        Console.WriteLine($"[Pipeline]   Query built ({query.Length} chars).");

                        // Step 4: Send to Agent
                        AgentResponse agentResponse = _agentClient.SendQuery(query);
                        Console.WriteLine($"[Pipeline]   Agent response received.");

                        // Step 5: Parse response
                        ParsedAgentResponse parsedResponse = _responseParser.Parse(agentResponse);
                        Console.WriteLine($"[Pipeline]   Confidence: {parsedResponse.Confidence:F2}, Chunks: {parsedResponse.AllChunks.Count}.");

                        // Step 6: Classify (with PMID validation)
                        ValidationResult validationResult = _classificationEngine.Classify(parsedResponse, record.PMID ?? string.Empty);
                        Console.WriteLine($"[Pipeline]   Classified as {validationResult.Category}. PMID Match: {validationResult.IsPmIdMatched}.");

                        validatedRecords.Add((record, validationResult));
                        result.SuccessfulRecords++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Pipeline]   ERROR: {ex.Message}");
                        result.FailedRecords++;
                        result.Errors.Add(new PipelineError
                        {
                            RecordIndex = i,
                            PMID = record.PMID,
                            ErrorMessage = ex.Message,
                            Exception = ex
                        });
                    }
                }

                // Step 7: Write output
                Console.WriteLine($"[Pipeline] Writing {validatedRecords.Count} records to output file...");
                _outputWriter.WriteResults(validatedRecords);
                Console.WriteLine($"[Pipeline] Output written to: {_config.DataProcessing.FileHandling.OutputFilePath}");

                result.Success = true;
                Console.WriteLine($"[Pipeline] Completed. Success: {result.SuccessfulRecords}, Failed: {result.FailedRecords}.");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.FatalError = ex.Message;
                Console.WriteLine($"[Pipeline] FATAL ERROR: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Aggregated result from pipeline execution.
    /// </summary>
    public class PipelineResult
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<PipelineError> Errors { get; set; } = new List<PipelineError>();
        public string FatalError { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"PipelineResult [Success={Success}, Total={TotalRecords}, Successful={SuccessfulRecords}, " +
                $"Failed={FailedRecords}, Errors={Errors.Count}]";
        }
    }

    /// <summary>
    /// Per-record error information.
    /// </summary>
    public class PipelineError
    {
        public int RecordIndex { get; set; }
        public string PMID { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception Exception { get; set; }

        public override string ToString()
        {
            return $"PipelineError [Index={RecordIndex}, PMID={PMID}, Message={ErrorMessage}]";
        }
    }
}
