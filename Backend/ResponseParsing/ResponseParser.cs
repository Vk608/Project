using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FABBatchValidator.Agent;
using FABBatchValidator.Models;

namespace FABBatchValidator.ResponseParsing
{
    /// <summary>
    /// Exception thrown when agent response parsing fails.
    /// </summary>
    public class ResponseParsingException : Exception
    {
        public ResponseParsingException(string message) : base(message) { }
        public ResponseParsingException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Parses raw agent responses into structured, usable data.
    /// 
    /// Responsibility:
    /// - Extract output.content (agent explanation)
    /// - Extract usedChunks list (evidence/candidates)
    /// - Derive confidence from usedChunks[0].score
    /// - Extract pm_id from usedChunks[0].id
    /// - Extract structured fields (Match_Extent, Discrepancies_Logical, Discrepancies_Metadata)
    /// - Handle missing/empty chunks gracefully
    /// - Return structured ParsedAgentResponse
    /// </summary>
    public class ResponseParser
    {
        /// <summary>
        /// Parse a raw agent response into structured data.
        /// Throws ResponseParsingException if response is null or malformed.
        /// </summary>
        public ParsedAgentResponse Parse(AgentResponse agentResponse)
        {
            if (agentResponse == null)
                throw new ResponseParsingException("Agent response is null");

            try
            {
                string content = agentResponse.output?.content ?? string.Empty;
                var chunks = agentResponse.output?.usedChunks ?? new List<UsedChunk>();
                double confidence = ExtractConfidence(chunks);
                string pmId = ExtractPmIdFromContent(content);
                UsedChunk topChunk = chunks.Count > 0 ? chunks[0] : null;

                // Extract structured fields from content
                string matchExtent = ExtractMatchExtent(content);
                string discrepanciesLogical = ExtractDiscrepanciesLogical(content);
                string discrepanciesMetadata = ExtractDiscrepanciesMetadata(content);
                string summary = ExtractSummary(content);

                return new ParsedAgentResponse
                {
                    Content = summary,
                    Confidence = confidence,
                    CandidatePmId = pmId,
                    TopChunk = topChunk,
                    AllChunks = new List<UsedChunk>(chunks),
                    Match_Extent = matchExtent,
                    Discrepancies_Logical = discrepanciesLogical,
                    Discrepancies_Metadata = discrepanciesMetadata
                };
            }
            catch (Exception ex) when (!(ex is ResponseParsingException))
            {
                throw new ResponseParsingException(
                    $"Failed to parse agent response: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extract confidence score from chunks.
        /// Returns usedChunks[0].score if available, else 0.0.
        /// Clamps to [0.0, 1.0] range.
        /// </summary>
        private double ExtractConfidence(List<UsedChunk> chunks)
        {
            if (chunks == null || chunks.Count == 0)
                return 0.0;

            double score = chunks[0].score;
            return score < 0.0 ? 0.0 : (score > 1.0 ? 1.0 : score);
        }

        /// <summary>
        /// Extract PMID from content (e.g., "**PMID:** 1042").
        /// Returns the PMID number or empty string if not found.
        /// </summary>
        private string ExtractPmIdFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            var match = Regex.Match(content, @"\*\*PMID:\*\*\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            match = Regex.Match(content, @"PMID:\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return string.Empty;
        }

        /// <summary>
        /// Extract Match_Extent from content.
        /// Looks for patterns like "**Match_Extent:** EXACT_MATCH" or "Match_Extent: EXACT_MATCH"
        /// Returns the value or empty string if not found.
        /// </summary>
        private string ExtractMatchExtent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            // Try markdown bold pattern first: **Match_Extent:** VALUE
            var match = Regex.Match(content, @"\*\*Match_Extent:\*?\*?\s*([^\n]+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            // Try plain text pattern: Match_Extent: VALUE
            match = Regex.Match(content, @"Match_Extent:\s*([^\n]+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return string.Empty;
        }

        /// <summary>
        /// Extract Discrepancies_Logical from content.
        /// Looks for section like "## Discrepancies_Logical:" and extracts content until next section.
        /// Returns the content or empty string if not found.
        /// </summary>
        private string ExtractDiscrepanciesLogical(string content)
        {
            return ExtractSection(content, "Discrepancies_Logical");
        }

        /// <summary>
        /// Extract Discrepancies_Metadata from content.
        /// Looks for section like "## Discrepancies_Metadata:" and extracts content until next section.
        /// Returns the content or empty string if not found.
        /// </summary>
        private string ExtractDiscrepanciesMetadata(string content)
        {
            return ExtractSection(content, "Discrepancies_Metadata");
        }

        /// <summary>
        /// Extract Summary section from content.
        /// Looks for "## Summary:" or "**Summary:**" and extracts content until end or next section.
        /// If no explicit Summary section found, returns the original content.
        /// </summary>
        private string ExtractSummary(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            // Try to find ## Summary: section
            var summaryMatch = Regex.Match(content, @"## Summary:\s*(.*?)(?=##|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (summaryMatch.Success && !string.IsNullOrWhiteSpace(summaryMatch.Groups[1].Value))
            {
                return summaryMatch.Groups[1].Value.Trim();
            }

            // Try to find **Summary:** section
            summaryMatch = Regex.Match(content, @"\*\*Summary:\*\*\s*(.*?)(?=##|\*\*|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (summaryMatch.Success && !string.IsNullOrWhiteSpace(summaryMatch.Groups[1].Value))
            {
                return summaryMatch.Groups[1].Value.Trim();
            }

            // If no explicit summary section, return the whole content (fallback)
            return content.Trim();
        }

        /// <summary>
        /// Helper to extract a named section from markdown content.
        /// Looks for patterns like "## SectionName:", "**SectionName:**" and plain "SectionName:" and extracts until next section.
        /// </summary>
        private string ExtractSection(string content, string sectionName)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(sectionName))
                return string.Empty;

            // Try markdown section pattern: ## SectionName:
            var match = Regex.Match(content, $@"##\s*{Regex.Escape(sectionName)}:\s*(.*?)(?=##|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                return match.Groups[1].Value.Trim();
            }

            // Try bold pattern: **SectionName:**
            match = Regex.Match(content, $@"\*\*{Regex.Escape(sectionName)}:\*\*\s*(.*?)(?=##|\*\*|---)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                return match.Groups[1].Value.Trim();
            }

            // Try plain text pattern: SectionName:
            match = Regex.Match(content, $@"{Regex.Escape(sectionName)}:\s*(.*?)(?={Regex.Escape(sectionName)}:|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                return match.Groups[1].Value.Trim();
            }

            return string.Empty;
        }
    }
}
