using System;
using System.Collections.Generic;

namespace FABBatchValidator.Models
{
    /// <summary>
    /// Represents the parsed, structured result of an agent response.
    /// Extracts semantic data (confidence, pm_id, explanation) from the raw AgentResponse.
    /// </summary>
    public class ParsedAgentResponse
    {
        /// <summary>
        /// Free-form explanation/reasoning from the agent.
        /// Describes why the agent chose a particular match (or why no match was found).
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score derived from the agent response.
        /// Range: 0.0 (no match) to 1.0 (perfect match).
        /// Derived from usedChunks[0].score if available; otherwise 0.0.
        /// </summary>
        public double Confidence { get; set; } = 0.0;

        /// <summary>
        /// Candidate PubMed ID extracted from the top chunk.
        /// Extracted from usedChunks[0].id if available; otherwise empty string.
        /// </summary>
        public string CandidatePmId { get; set; } = string.Empty;

        /// <summary>
        /// The top chunk used for confidence and pm_id extraction.
        /// Preserved for additional context or detailed analysis.
        /// </summary>
        public UsedChunk TopChunk { get; set; }

        /// <summary>
        /// All chunks returned by the agent.
        /// Top chunk is chunks[0] if present.
        /// </summary>
        public List<UsedChunk> AllChunks { get; set; } = new();

        /// <summary>
        /// Extent of the match extracted from agent response (e.g., "Exact", "Partial", "No Match").
        /// </summary>
        public string Match_Extent { get; set; } = string.Empty;

        /// <summary>
        /// Logical discrepancies extracted from agent response.
        /// </summary>
        public string Discrepancies_Logical { get; set; } = string.Empty;

        /// <summary>
        /// Metadata discrepancies extracted from agent response.
        /// </summary>
        public string Discrepancies_Metadata { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"ParsedAgentResponse(Confidence={Confidence:F3}, PM_ID={CandidatePmId})";
        }
    }

    /// <summary>Used chunk from agent response (part of evidence).</summary>
    public class UsedChunk
    {
        public string id { get; set; } = string.Empty;
        public string payload { get; set; } = string.Empty;
        public double score { get; set; }
    }
}
