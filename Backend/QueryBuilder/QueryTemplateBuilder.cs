using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FABBatchValidator.Models;

namespace FABBatchValidator.QueryBuilder
{
    /// Builds natural language queries for the FAB Agent from bibliographic records.
    /// 
    /// Responsibility:
    /// - Format a BiblioRecord into a query string that the FAB Agent can understand
    /// - Apply a consistent template across all records
    /// - Handle missing/empty fields gracefully
    /// - Construct queries that help the Agent find matching PubMed records
    /// 
    /// Design:
    /// Single-line query template with bibliographic details for agent reasoning.
    /// This is natural language, not a keyword search.
    public class QueryTemplateBuilder
    {
        /// Build a query string from a bibliographic record.
        /// Returns a formatted, multi-line string ready for submission to the FAB Agent.
        public string BuildQuery(BiblioRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var query = new StringBuilder();

            // Opening: Include PMID if available for agent reference
            query.Append("The record details: ");

            // PMID
            if (!string.IsNullOrWhiteSpace(record.PMID))
            {
                query.Append($"\nPMID: {record.PMID.Trim()}");
            }
            // Primary identifier: Title
            if (!string.IsNullOrWhiteSpace(record.Title))
            {
                query.Append($"\nTitle: {record.Title.Trim()}");
            }

            // Secondary context: Journal, Year, Authors
            var contextParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(record.JournalName))
                contextParts.Add($"Published in: {record.JournalName.Trim()}");

            if (!string.IsNullOrWhiteSpace(record.PublicationYear))
                contextParts.Add($"Year: {record.PublicationYear.Trim()}");

            if (!string.IsNullOrWhiteSpace(record.Authors))
                contextParts.Add($"Authors: {record.Authors.Trim()}");

            if (contextParts.Count > 0)
            {
                query.Append("\n");
                query.Append(string.Join("\n", contextParts));
            }

            // Tertiary context: Abstract (for semantic understanding)
            if (!string.IsNullOrWhiteSpace(record.Abstract))
            {
                query.Append($"\n\nAbstract:\n{record.Abstract.Trim()}");
            }

            // Optional context: MeSH terms, Chemicals
            if (!string.IsNullOrWhiteSpace(record.MeSHTerms))
            {
                query.Append($"\n\nMeSH Terms: {record.MeSHTerms.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(record.Chemicals))
            {
                query.Append($"\nChemicals: {record.Chemicals.Trim()}");
            }

            // Optional context: Language, Country
            var additionalNotes = new List<string>();

            if (!string.IsNullOrWhiteSpace(record.Language))
                additionalNotes.Add($"Language: {record.Language.Trim()}");

            if (!string.IsNullOrWhiteSpace(record.Country))
                additionalNotes.Add($"Country: {record.Country.Trim()}");

            if (additionalNotes.Count > 0)
            {
                query.Append("\n\nAdditional Information:\n");
                query.Append(string.Join("\n", additionalNotes));
            }

            return query.ToString();
        }

        /// Build queries for multiple records.
        /// Returns a list of query strings in order.
        public List<string> BuildQueries(IEnumerable<BiblioRecord> records)
        {
            var queries = new List<string>();

            foreach (var record in records)
            {
                if (record != null)
                {
                    queries.Add(BuildQuery(record));
                }
            }

            return queries;
        }
    }
}
