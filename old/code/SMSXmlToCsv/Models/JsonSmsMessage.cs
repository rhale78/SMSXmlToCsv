namespace SMSXmlToCsv.Models
{
    /// <summary>
    /// JSON SMS message data model
    /// </summary>
    public class JsonSmsMessage
    {
        public string FromName { get; set; } = string.Empty;
        public string FromPhone { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string ToPhone { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string DateTime { get; set; } = string.Empty;
        public long UnixTimestamp { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public Dictionary<string, string>? AdditionalFields { get; set; }
    }
}
