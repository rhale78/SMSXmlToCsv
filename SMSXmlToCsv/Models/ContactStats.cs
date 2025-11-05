namespace SMSXmlToCsv.Models
{
    /// <summary>
    /// Contact statistics data model
    /// </summary>
    public class ContactStats
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int SentCount { get; set; }
        public int ReceivedCount { get; set; }
        public DateTime FirstMessage { get; set; }
        public DateTime LastMessage { get; set; }
    }
}
