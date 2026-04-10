namespace FABBatchValidator.Models
{
    /// <summary>
    /// Represents a bibliographic record after validation by the AI Agent.
    /// This is the authoritative result model for storage and API responses.
    /// All fields come directly from the Agent API response.
    /// </summary>
    public class ValidatedRecord
    {
        /// <summary>The original PMID from the input Excel file.</summary>
        public string InputPMID { get; set; } = string.Empty;

        /// <summary>The PMID that the agent matched to this record (from top chunk).</summary>
        public string MatchedPMID { get; set; } = string.Empty;

        /// <summary>Confidence score (0.0-1.0) from the agent's top chunk.</summary>
        public double ConfidenceScore { get; set; } = 0.0;

        /// <summary>Extent of the match (e.g., "Exact", "Partial", "No Match").</summary>
        public string Match_Extent { get; set; } = string.Empty;

        /// <summary>Logical discrepancies identified by the agent.</summary>
        public string Discrepancies_Logical { get; set; } = string.Empty;

        /// <summary>Metadata discrepancies identified by the agent.</summary>
        public string Discrepancies_Metadata { get; set; } = string.Empty;

        /// <summary>Summary explanation from the agent.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>The original title from the input record for reference.</summary>
        public string OriginalTitle { get; set; } = string.Empty;
    }
}
