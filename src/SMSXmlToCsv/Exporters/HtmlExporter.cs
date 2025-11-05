using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters;

/// <summary>
/// Exports messages to a styled HTML file resembling a chat interface.
/// </summary>
public class HtmlExporter : IDataExporter
{
    public string FileExtension => "html";

    public async Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName)
    {
        string filePath = Path.Combine(outputDirectory, $"{baseFileName}.{FileExtension}");
        StringBuilder sb = new StringBuilder();

        // HTML header with embedded CSS
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("    <title>Message Export</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body {");
        sb.AppendLine("            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;");
        sb.AppendLine("            background-color: #f5f5f5;");
        sb.AppendLine("            margin: 0;");
        sb.AppendLine("            padding: 20px;");
        sb.AppendLine("        }");
        sb.AppendLine("        .chat-container {");
        sb.AppendLine("            max-width: 800px;");
        sb.AppendLine("            margin: 0 auto;");
        sb.AppendLine("            background-color: white;");
        sb.AppendLine("            border-radius: 10px;");
        sb.AppendLine("            padding: 20px;");
        sb.AppendLine("            box-shadow: 0 2px 10px rgba(0,0,0,0.1);");
        sb.AppendLine("        }");
        sb.AppendLine("        h1 {");
        sb.AppendLine("            text-align: center;");
        sb.AppendLine("            color: #333;");
        sb.AppendLine("        }");
        sb.AppendLine("        .message {");
        sb.AppendLine("            border-radius: 10px;");
        sb.AppendLine("            padding: 10px 15px;");
        sb.AppendLine("            margin: 10px 0;");
        sb.AppendLine("            max-width: 70%;");
        sb.AppendLine("            display: inline-block;");
        sb.AppendLine("            clear: both;");
        sb.AppendLine("            word-wrap: break-word;");
        sb.AppendLine("        }");
        sb.AppendLine("        .sent {");
        sb.AppendLine("            background-color: #dcf8c6;");
        sb.AppendLine("            float: right;");
        sb.AppendLine("            margin-left: 30%;");
        sb.AppendLine("        }");
        sb.AppendLine("        .received {");
        sb.AppendLine("            background-color: #ffffff;");
        sb.AppendLine("            border: 1px solid #e0e0e0;");
        sb.AppendLine("            float: left;");
        sb.AppendLine("            margin-right: 30%;");
        sb.AppendLine("        }");
        sb.AppendLine("        .sender {");
        sb.AppendLine("            font-weight: bold;");
        sb.AppendLine("            font-size: 0.9em;");
        sb.AppendLine("            color: #555;");
        sb.AppendLine("            margin-bottom: 5px;");
        sb.AppendLine("        }");
        sb.AppendLine("        .body {");
        sb.AppendLine("            margin: 5px 0;");
        sb.AppendLine("            white-space: pre-wrap;");
        sb.AppendLine("        }");
        sb.AppendLine("        .timestamp {");
        sb.AppendLine("            font-size: 0.8em;");
        sb.AppendLine("            color: #888;");
        sb.AppendLine("            text-align: right;");
        sb.AppendLine("            margin-top: 5px;");
        sb.AppendLine("        }");
        sb.AppendLine("        .attachments {");
        sb.AppendLine("            font-size: 0.85em;");
        sb.AppendLine("            color: #666;");
        sb.AppendLine("            margin-top: 5px;");
        sb.AppendLine("            font-style: italic;");
        sb.AppendLine("        }");
        sb.AppendLine("        .clearfix::after {");
        sb.AppendLine("            content: \"\";");
        sb.AppendLine("            display: table;");
        sb.AppendLine("            clear: both;");
        sb.AppendLine("        }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"chat-container\">");
        sb.AppendLine("        <h1>Message History</h1>");

        // Sort messages by timestamp
        IEnumerable<Message> sortedMessages = messages.OrderBy(m => m.TimestampUtc);

        foreach (Message message in sortedMessages)
        {
            string directionClass = message.Direction == MessageDirection.Sent ? "sent" : "received";
            
            sb.AppendLine("        <div class=\"clearfix\">");
            sb.AppendLine($"            <div class=\"message {directionClass}\">");
            sb.AppendLine($"                <div class=\"sender\">{HtmlEncode(message.From.Name)}</div>");
            sb.AppendLine($"                <div class=\"body\">{HtmlEncode(message.Body)}</div>");
            
            if (message.Attachments.Count > 0)
            {
                string attachmentList = string.Join(", ", message.Attachments.Select(a => a.FileName));
                sb.AppendLine($"                <div class=\"attachments\">📎 {HtmlEncode(attachmentList)}</div>");
            }
            
            sb.AppendLine($"                <div class=\"timestamp\">{message.TimestampUtc.ToLocalTime():g}</div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");
        }

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    private string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return HttpUtility.HtmlEncode(text);
    }
}
