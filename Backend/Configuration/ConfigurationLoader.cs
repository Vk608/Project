using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FABBatchValidator.Configuration
{
    public class ConfigurationLoader
    {
        public static PipelineConfiguration LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Config file not found: {path}");

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<PipelineConfiguration>(json, options)
                         ?? throw new InvalidOperationException("Invalid config JSON");

            OverrideFromEnvironment(config);
            Validate(config);
            return config;
        }

        public static PipelineConfiguration LoadFromEnvironment()
        {
            var config = new PipelineConfiguration();
            OverrideFromEnvironment(config);
            Validate(config);
            return config;
        }

        private static void OverrideFromEnvironment(PipelineConfiguration config)
        {
            string p = "FAB_AgentApi__";

            config.AgentApi.Url =
                Environment.GetEnvironmentVariable(p + "Url") ?? config.AgentApi.Url;

            config.AgentApi.UserId =
                Environment.GetEnvironmentVariable(p + "UserId") ?? config.AgentApi.UserId;

            config.AgentApi.ApiKey =
                Environment.GetEnvironmentVariable(p + "ApiKey") ?? config.AgentApi.ApiKey;

            if (int.TryParse(
                    Environment.GetEnvironmentVariable(p + "TimeoutSeconds"),
                    out var timeout))
            {
                config.AgentApi.TimeoutSeconds = timeout;
            }

            var inputFile = Environment.GetEnvironmentVariable(
                "FAB_DataProcessing__FileHandling__InputFilePath");
            if (!string.IsNullOrEmpty(inputFile))
                config.DataProcessing.FileHandling.InputFilePath = inputFile;

            var outputFile = Environment.GetEnvironmentVariable(
                "FAB_DataProcessing__FileHandling__OutputFilePath");
            if (!string.IsNullOrEmpty(outputFile))
                config.DataProcessing.FileHandling.OutputFilePath = outputFile;
        }

        private static void Validate(PipelineConfiguration config)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.AgentApi.Url))
                errors.Add("AgentApi.Url is required");

            if (string.IsNullOrWhiteSpace(config.AgentApi.UserId))
                errors.Add("AgentApi.UserId is required");

            if (string.IsNullOrWhiteSpace(config.AgentApi.ApiKey))
                errors.Add("AgentApi.ApiKey is required");

            if (config.AgentApi.TimeoutSeconds <= 0)
                errors.Add("AgentApi.TimeoutSeconds must be > 0");

            if (!File.Exists(config.DataProcessing.FileHandling.InputFilePath))
                errors.Add($"Input file not found: {config.DataProcessing.FileHandling.InputFilePath}");

            var c = config.DataProcessing.Classification;
            if (!(c.ConfidenceThreshold_Exact > c.ConfidenceThreshold_Minor &&
                  c.ConfidenceThreshold_Minor > c.ConfidenceThreshold_Major))
            {
                errors.Add("Confidence thresholds must be: Exact > Minor > Major");
            }

            if (errors.Count > 0)
                throw new InvalidOperationException(
                    "Configuration validation failed:\n" + string.Join("\n", errors));
        }
    }
}
