namespace FABBatchValidator.Models
{
    /// <summary>
    /// Represents a single bibliographic record from the input Excel file.
    /// Maps to a row in the input sheet with the schema defined in InputSchemaConfiguration.
    /// 
    /// Responsibility:
    /// - Hold bibliographic data extracted from Excel
    /// - Represent the input record in a strongly-typed manner
    /// - Support graceful handling of empty/missing fields
    /// 
    /// Design note:
    /// All fields are strings to match Excel cell data (which is always text).
    /// Conversion or validation happens in later steps.
    /// This class is intentionally simple: a data container, not a validator.
    /// </summary>
    public class BiblioRecord
    {
        /// <summary>PubMed Identifier (PMID). May be empty.</summary>
        public string PMID { get; set; } = string.Empty;

        /// <summary>Title of the publication.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Abstract or summary of the publication.</summary>
        public string Abstract { get; set; } = string.Empty;

        /// <summary>Medical Subject Headings (MeSH) terms.</summary>
        public string MeSHTerms { get; set; } = string.Empty;

        /// <summary>Chemical substances mentioned in the publication.</summary>
        public string Chemicals { get; set; } = string.Empty;

        /// <summary>Author names (may be formatted in various ways).</summary>
        public string Authors { get; set; } = string.Empty;

        /// <summary>Name of the journal where the publication appeared.</summary>
        public string JournalName { get; set; } = string.Empty;

        /// <summary>International Standard Serial Number (ISSN) for the journal.</summary>
        public string ISSN { get; set; } = string.Empty;

        /// <summary>Year of publication as a string (e.g., "2023").</summary>
        public string PublicationYear { get; set; } = string.Empty;

        /// <summary>Language of the publication.</summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>Country where the research was conducted or published.</summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>Returns a string representation of this record for debugging.</summary>
        public override string ToString()
        {
            return $"BiblioRecord({Title ?? "[empty]"})";
        }
    }
}
