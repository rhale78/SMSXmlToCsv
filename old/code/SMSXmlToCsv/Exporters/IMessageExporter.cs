using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// Interface for message exporters
    /// </summary>
    public interface IMessageExporter
    {
        /// <summary>
        /// Export messages to specified format
        /// </summary>
        Task ExportAsync(
            List<SmsMessage> messages,
            string outputPath,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null);
    }
}
