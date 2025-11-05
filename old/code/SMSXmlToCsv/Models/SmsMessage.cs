namespace SMSXmlToCsv.Models
{
    /// <summary>
    /// SMS message data model
    /// </summary>
    public class SmsMessage
    {
        public string FromName { get; set; } = string.Empty;
        public string FromPhone { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string ToPhone { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public long UnixTimestamp { get; set; }
        public string MessageText { get; set; } = string.Empty;

        // Additional fields from XML (optional)
        public Dictionary<string, string> AdditionalFields { get; set; } = new Dictionary<string, string>();
    }
}
