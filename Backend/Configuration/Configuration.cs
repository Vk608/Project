using System;
using System.Collections.Generic;

namespace FABBatchValidator.Configuration
{
    /// <summary>
    /// Root configuration for the pipeline.
    /// </summary>
    public class PipelineConfiguration
    {
        public AgentApiConfiguration AgentApi { get; set; } = new();
        public DataProcessingConfiguration DataProcessing { get; set; } = new();
    }

    /// <summary>
    /// Simplified agent configuration with direct URL and API key.
    /// </summary>
    public class AgentApiConfiguration
    {
        /// <summary>
        /// Full executable URL: https://...lambda-url.../agent/{agentName}/execute
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// User identity sent in request headers.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// API key in form: keyId:keySecret
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// HTTP timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class DataProcessingConfiguration
    {
        public FileHandlingConfiguration FileHandling { get; set; } = new();
        public InputSchemaConfiguration InputSchema { get; set; } = new();
        public ClassificationConfiguration Classification { get; set; } = new();
    }

    public class FileHandlingConfiguration
    {
        public string InputFilePath { get; set; } = string.Empty;
        public string OutputFilePath { get; set; } = string.Empty;
        public string ResultsJsonPath { get; set; } = "results.json";
        public string InputSheetName { get; set; } = "Data";
    }

    public class InputSchemaConfiguration
    {
        public List<string> RequiredColumns { get; set; } = new()
        {
            "PMID", "Title", "Abstract", "MeSH Terms", "Chemicals",
            "Authors", "Journal Name", "ISSN", "Publication Year", "Language", "Country"
        };
    }

    public class ClassificationConfiguration
    {
        public double ConfidenceThreshold_Exact { get; set; } = 0.65;
        public double ConfidenceThreshold_Minor { get; set; } = 0.45;
        public double ConfidenceThreshold_Major { get; set; } = 0.25;
        public int MaxChunksForMajor { get; set; } = 3;
    }
}
