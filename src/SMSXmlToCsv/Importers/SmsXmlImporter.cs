using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Importers;

/// <summary>
/// Imports messages from Android SMS Backup & Restore XML files.
/// Supports both SMS and MMS message types.
/// </summary>
public class SmsXmlImporter : IDataImporter
{
    public string SourceName => "Android SMS Backup & Restore";

    public async Task<IEnumerable<Message>> ImportAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();

        // Read and parse the XML file
        XDocument doc = await Task.Run(() => XDocument.Load(sourcePath));
        XElement? root = doc.Root;

        if (root == null)
        {
            throw new InvalidOperationException("XML file has no root element");
        }

        // Process SMS messages
        IEnumerable<XElement> smsElements = root.Elements("sms");
        foreach (XElement smsElement in smsElements)
        {
            Message? message = ParseSmsElement(smsElement);
            if (message != null)
            {
                messages.Add(message);
            }
        }

        // Process MMS messages
        IEnumerable<XElement> mmsElements = root.Elements("mms");
        foreach (XElement mmsElement in mmsElements)
        {
            Message? message = ParseMmsElement(mmsElement);
            if (message != null)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    private Message? ParseSmsElement(XElement smsElement)
    {
        try
        {
            string address = smsElement.Attribute("address")?.Value ?? string.Empty;
            string dateStr = smsElement.Attribute("date")?.Value ?? "0";
            string typeStr = smsElement.Attribute("type")?.Value ?? "0";
            string body = smsElement.Attribute("body")?.Value ?? string.Empty;
            string contactName = smsElement.Attribute("contact_name")?.Value ?? address;

            // Parse timestamp (milliseconds since Unix epoch)
            long dateMs = long.Parse(dateStr);
            DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(dateMs);

            // Parse direction (1 = received, 2 = sent)
            int type = int.Parse(typeStr);
            MessageDirection direction = type switch
            {
                1 => MessageDirection.Received,
                2 => MessageDirection.Sent,
                _ => MessageDirection.Unknown
            };

            // Create contacts
            Contact contact = Contact.FromPhoneNumber(contactName, address);
            Contact self = Contact.FromName("Me");

            Contact from = direction == MessageDirection.Sent ? self : contact;
            Contact to = direction == MessageDirection.Sent ? contact : self;

            return Message.CreateTextMessage(
                SourceName,
                from,
                to,
                timestamp,
                body,
                direction);
        }
        catch (Exception)
        {
            // Skip malformed messages
            return null;
        }
    }

    private Message? ParseMmsElement(XElement mmsElement)
    {
        try
        {
            string dateStr = mmsElement.Attribute("date")?.Value ?? "0";
            string msgBoxStr = mmsElement.Attribute("msg_box")?.Value ?? "0";
            string contactName = mmsElement.Attribute("contact_name")?.Value ?? "Unknown";

            // Parse timestamp (seconds since Unix epoch for MMS)
            long dateSec = long.Parse(dateStr);
            DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(dateSec);

            // Parse direction (1 = received, 2 = sent)
            int msgBox = int.Parse(msgBoxStr);
            MessageDirection direction = msgBox switch
            {
                1 => MessageDirection.Received,
                2 => MessageDirection.Sent,
                _ => MessageDirection.Unknown
            };

            // Extract address from addr elements
            IEnumerable<XElement> addrElements = mmsElement.Elements("addr");
            XElement? addressElement = addrElements.FirstOrDefault();
            string address = addressElement?.Attribute("address")?.Value ?? string.Empty;

            // Extract message body and attachments from part elements
            IEnumerable<XElement> partElements = mmsElement.Elements("part");
            string body = string.Empty;
            List<MediaAttachment> attachments = new List<MediaAttachment>();

            foreach (XElement part in partElements)
            {
                string ct = part.Attribute("ct")?.Value ?? string.Empty;
                string text = part.Attribute("text")?.Value ?? string.Empty;
                string data = part.Attribute("data")?.Value ?? string.Empty;
                string name = part.Attribute("name")?.Value ?? string.Empty;

                if (ct.StartsWith("text/") && !string.IsNullOrEmpty(text))
                {
                    body += text;
                }
                else if (!ct.StartsWith("text/") && !ct.StartsWith("application/smil"))
                {
                    // This is a media attachment
                    string sourcePath = !string.IsNullOrEmpty(name) ? name : $"attachment_{Guid.NewGuid():N}";
                    attachments.Add(new MediaAttachment(sourcePath, ct));
                }
            }

            // Create contacts
            Contact contact = !string.IsNullOrEmpty(address) 
                ? Contact.FromPhoneNumber(contactName, address)
                : Contact.FromName(contactName);
            Contact self = Contact.FromName("Me");

            Contact from = direction == MessageDirection.Sent ? self : contact;
            Contact to = direction == MessageDirection.Sent ? contact : self;

            return new Message(
                SourceName,
                from,
                to,
                timestamp,
                body,
                direction,
                attachments);
        }
        catch (Exception)
        {
            // Skip malformed messages
            return null;
        }
    }
}
