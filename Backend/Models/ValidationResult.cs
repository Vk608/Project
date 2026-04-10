using System;

namespace FABBatchValidator.Models
{
    /// <summary>
    /// Represents the classification category for a validation result.
    /// </summary>
    public enum ValidationCategory
    {
        /// <summary>High confidence match; PM_ID confidence >= 0.65 threshold.</summary>
        Exact,

        /// <summary>Moderate match; confidence 0.45–0.64.</summary>
        Minor,

        /// <summary>Weak match; confidence 0.25–0.44; typically multiple candidates.</summary>
        Major,

        /// <summary>No match; confidence < 0.25.</summary>
        NotFound
    }

    /// <summary>
    /// Result of classifying a bibliographic record's validation response.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>The classification category (Exact, Minor, Major, NotFound).</summary>
        public ValidationCategory Category { get; set; }

        /// <summary>Confidence score from agent response (0.0–1.0).</summary>
        public double Confidence { get; set; }

        /// <summary>Candidate PM_ID extracted from top chunk, if any.</summary>
        public string CandidatePmId { get; set; } = string.Empty;

        /// <summary>Number of chunks used by agent.</summary>
        public int ChunkCount { get; set; }

        /// <summary>Human-readable explanation of classification decision.</summary>
        public string Rationale { get; set; } = string.Empty;

        /// <summary>True if multiple chunks suggest multiple candidate records (Major category).</summary>
        public bool IsMultipleCandidates { get; set; }

        /// <summary>True if no chunks available (NotFound category).</summary>
        public bool IsNoDataAvailable { get; set; }

        /// <summary>True if confidence exactly at threshold boundary (edge case for review).</summary>
        public bool IsAtThresholdBoundary { get; set; }

        /// <summary>True if the original PMID matches the candidate PMID from agent response.</summary>
        public bool IsPmIdMatched { get; set; }

        /// <summary>True if PMID mismatch detected (records are different).</summary>
        public bool IsPmIdMismatch { get; set; }
    }
}
