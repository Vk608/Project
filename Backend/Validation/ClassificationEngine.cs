using System;
using FABBatchValidator.Configuration;
using FABBatchValidator.Models;

namespace FABBatchValidator.Validation
{
    /// <summary>
    /// Applies confidence thresholds from configuration to classify validation results.
    /// </summary>
    public class ClassificationEngine
    {
        private readonly ClassificationConfiguration _classificationConfig;

        /// <param name="classificationConfig">Threshold configuration (Exact, Minor, Major, MaxChunksForMajor).</param>
        public ClassificationEngine(ClassificationConfiguration classificationConfig)
        {
            _classificationConfig = classificationConfig
                ?? throw new ArgumentNullException(nameof(classificationConfig));
        }

        /// <summary>
        /// Classify a parsed agent response into a validation result.
        /// </summary>
        /// <param name="parsedResponse">The parsed agent response with confidence and PM_ID.</param>
        /// <param name="originalPmId">The original PMID read from Excel (for PMID validation).</param>
        /// <returns>ValidationResult with category, rationale, and flags.</returns>
        public ValidationResult Classify(ParsedAgentResponse parsedResponse, string originalPmId = "")
        {
            if (parsedResponse == null)
                throw new ArgumentNullException(nameof(parsedResponse));

            var result = new ValidationResult
            {
                Confidence = parsedResponse.Confidence,
                CandidatePmId = parsedResponse.CandidatePmId,
                ChunkCount = parsedResponse.AllChunks?.Count ?? 0
            };

            // PMID Validation: Compare original PMID with candidate PMID
            ValidatePmId(result, originalPmId);

            // Determine category based on confidence thresholds (hierarchical).
            if (parsedResponse.Confidence >= _classificationConfig.ConfidenceThreshold_Exact)
            {
                result.Category = ValidationCategory.Exact;
                result.Rationale = $"High confidence: {parsedResponse.Confidence:F2} >= {_classificationConfig.ConfidenceThreshold_Exact:F2}. " +
                    $"Candidate PM_ID: {parsedResponse.CandidatePmId}. PMID Match: {result.IsPmIdMatched}.";
                result.IsAtThresholdBoundary = IsAtBoundary(parsedResponse.Confidence, _classificationConfig.ConfidenceThreshold_Exact);
            }
            else if (parsedResponse.Confidence >= _classificationConfig.ConfidenceThreshold_Minor)
            {
                result.Category = ValidationCategory.Minor;
                result.Rationale = $"Moderate confidence: {parsedResponse.Confidence:F2} in range " +
                    $"[{_classificationConfig.ConfidenceThreshold_Minor:F2}, {_classificationConfig.ConfidenceThreshold_Exact:F2}). " +
                    $"Weak match; verify candidate PM_ID: {parsedResponse.CandidatePmId}. PMID Match: {result.IsPmIdMatched}.";
                result.IsAtThresholdBoundary = IsAtBoundary(parsedResponse.Confidence, _classificationConfig.ConfidenceThreshold_Minor);
            }
            else if (parsedResponse.Confidence >= _classificationConfig.ConfidenceThreshold_Major)
            {
                result.Category = ValidationCategory.Major;
                result.IsMultipleCandidates = result.ChunkCount > _classificationConfig.MaxChunksForMajor;
                string multipleFlag = result.IsMultipleCandidates 
                    ? $" (Multiple candidates: {result.ChunkCount} chunks > max {_classificationConfig.MaxChunksForMajor}.)"
                    : string.Empty;
                result.Rationale = $"Weak confidence: {parsedResponse.Confidence:F2} in range " +
                    $"[{_classificationConfig.ConfidenceThreshold_Major:F2}, {_classificationConfig.ConfidenceThreshold_Minor:F2}). " +
                    $"Ambiguous.{multipleFlag} PMID Match: {result.IsPmIdMatched}.";
                result.IsAtThresholdBoundary = IsAtBoundary(parsedResponse.Confidence, _classificationConfig.ConfidenceThreshold_Major);
            }
            else
            {
                result.Category = ValidationCategory.NotFound;
                result.IsNoDataAvailable = result.ChunkCount == 0;
                string noDataFlag = result.IsNoDataAvailable ? " No chunks available." : string.Empty;
                result.Rationale = $"No match: {parsedResponse.Confidence:F2} < {_classificationConfig.ConfidenceThreshold_Major:F2}.{noDataFlag} PMID Match: {result.IsPmIdMatched}.";
            }

            return result;
        }

        /// <summary>
        /// Validate PMID: Compare original PMID (read from Excel) with candidate PMID (from agent response).
        /// Sets IsPmIdMatched and IsPmIdMismatch flags on the result.
        /// </summary>
        private void ValidatePmId(ValidationResult result, string originalPmId)
        {
            // If no original PMID provided, cannot validate
            if (string.IsNullOrWhiteSpace(originalPmId))
            {
                result.IsPmIdMatched = false;
                result.IsPmIdMismatch = false;
                return;
            }

            // If no candidate PMID from agent, mismatch
            if (string.IsNullOrWhiteSpace(result.CandidatePmId))
            {
                result.IsPmIdMatched = false;
                result.IsPmIdMismatch = true;
                return;
            }

            // Compare PMIDs
            bool pmIdMatch = originalPmId.Trim().Equals(result.CandidatePmId.Trim(), StringComparison.OrdinalIgnoreCase);
            result.IsPmIdMatched = pmIdMatch;
            result.IsPmIdMismatch = !pmIdMatch;
        }

        private bool IsAtBoundary(double confidence, double threshold, double tolerance = 0.01)
        {
            return Math.Abs(confidence - threshold) < tolerance;
        }
    }
}
