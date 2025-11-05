namespace SMSXmlToCsv.Models
{
    /// <summary>
    /// Represents a contact merge decision that can be saved and reused
    /// </summary>
    public class MergeDecision
    {
        /// <summary>
        /// Source contact keys (Name|Phone) being merged
        /// </summary>
        public List<string> SourceContacts { get; set; } = new();

        /// <summary>
        /// Target (merged) contact name
        /// </summary>
        public string TargetName { get; set; } = string.Empty;

        /// <summary>
        /// Target (merged) contact phone
        /// </summary>
        public string TargetPhone { get; set; } = string.Empty;

        /// <summary>
        /// When this merge was created
        /// </summary>
        public DateTime MergedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Optional description/reason for merge
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Track if this merge was skipped
        /// </summary>
        public bool IsSkipped { get; set; } = false;
    }
}
