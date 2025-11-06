using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services.ML;
using Serilog;
using Spectre.Console;

namespace SMSXmlToCsv.Services.Visualization;

/// <summary>
/// Graph node for network visualization
/// </summary>
public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int Group { get; set; }  // 0 = user, 1 = contact, 2 = topic
    public List<string> TopTopics { get; set; } = new List<string>();
}

/// <summary>
/// Graph link for network visualization
/// </summary>
public class GraphLink
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int Value { get; set; }  // Message count
}

/// <summary>
/// Generates interactive D3.js network graphs showing contact and topic relationships
/// Adapted from working legacy code
/// </summary>
public class NetworkGraphGenerator
{
    private const bool UNLIMITED_TOPICS_MODE = true;  // From legacy - works well
    private const int MIN_TOPIC_MESSAGES = 2;  // From legacy
    private readonly OllamaSentimentAnalyzer _ollamaAnalyzer;

    public NetworkGraphGenerator(OllamaSentimentAnalyzer ollamaAnalyzer)
    {
        _ollamaAnalyzer = ollamaAnalyzer ?? throw new ArgumentNullException(nameof(ollamaAnalyzer));
    }

    /// <summary>
    /// Generate network graph for all contacts (from legacy code approach)
    /// </summary>
    public async Task GenerateGraphAsync(IEnumerable<Message> messages, string outputPath, string userName = "You")
    {
        Log.Information("Generating network graph with AI topic detection (legacy algorithm)");

        if (!await _ollamaAnalyzer.IsAvailableAsync())
        {
            throw new InvalidOperationException("Ollama is not available. Install from: https://ollama.ai and run: ollama pull llama3.2");
        }

        List<Message> messageList = messages.ToList();
        Log.Information("Processing {MessageCount} messages for network graph", messageList.Count);

        if (messageList.Count == 0)
        {
            throw new InvalidOperationException("No messages available to generate network graph");
        }

        // Group messages by contact (adapted from legacy)
        Dictionary<string, GraphNode> nodes = new Dictionary<string, GraphNode>();
        Dictionary<string, List<string>> contactMessages = new Dictionary<string, List<string>>();

        // Create user node
        nodes["user"] = new GraphNode
        {
            Id = "user",
            Name = userName,
            MessageCount = messageList.Count,
            Group = 0
        };

        // Extract contacts and their messages
        foreach (Message message in messageList)
        {
            string contactName = ExtractContactName(message, userName);
            string contactPhone = ExtractContactPhone(message, userName);

            if (string.IsNullOrEmpty(contactPhone) || contactPhone == "Unknown")
            {
                continue; // Skip invalid contacts
            }

            if (!nodes.ContainsKey(contactPhone))
            {
                nodes[contactPhone] = new GraphNode
                {
                    Id = contactPhone,
                    Name = contactName,
                    MessageCount = 0,
                    Group = 1
                };
                contactMessages[contactPhone] = new List<string>();

                Log.Debug("Contact: {ContactName} ({ContactPhone})", contactName, contactPhone);
            }

            nodes[contactPhone].MessageCount++;

            // Collect message text for topic extraction
            if (!string.IsNullOrWhiteSpace(message.Body))
            {
                contactMessages[contactPhone].Add(message.Body);
            }
        }

        Log.Information("Found {ContactCount} unique contacts", nodes.Count - 1);

        if (nodes.Count <= 1)
        {
            throw new InvalidOperationException("No contacts were extracted from messages!");
        }

        // Create user-to-contact links
        List<GraphLink> links = new List<GraphLink>();
        foreach (KeyValuePair<string, GraphNode> kvp in nodes.Where(n => n.Key != "user"))
        {
            links.Add(new GraphLink
            {
                Source = "user",
                Target = kvp.Key,
                Value = kvp.Value.MessageCount
            });
        }

        // Topic extraction using AI (from legacy algorithm)
        Dictionary<string, HashSet<string>> topicToContacts = new Dictionary<string, HashSet<string>>();
        Dictionary<string, int> globalTopicFrequency = new Dictionary<string, int>();
        Dictionary<string, Dictionary<string, int>> contactTopicMessageCounts = new Dictionary<string, Dictionary<string, int>>();

        int processed = 0;
        int totalContacts = contactMessages.Count;
        List<(string ContactName, int TopicCount)> topicResults = new List<(string, int)>();
        List<string> emptyResponseContacts = new List<string>();
        List<(string ContactName, string Error)> errorContacts = new List<(string, string)>();
        List<string> allContacts = new List<string>(); // Track all contacts for final status

        // Use Status for the overall operation - NO logging or console output inside this block
        await AnsiConsole.Status()
            .StartAsync("AI analyzing topics...", async ctx =>
            {
                foreach (KeyValuePair<string, List<string>> contactKvp in contactMessages)
                {
                    processed++;
                    string contactPhone = contactKvp.Key;
                    List<string> msgTexts = contactKvp.Value;

                    if (msgTexts.Count < MIN_TOPIC_MESSAGES)
                    {
                        continue;
                    }

                    // Update status - this is safe within Status context
                    ctx.Status($"AI analyzing {processed}/{totalContacts}: {nodes[contactPhone].Name}");

                    contactTopicMessageCounts[contactPhone] = new Dictionary<string, int>();

                    try
                    {
                        // Extract topics using AI (legacy approach)
                        List<string> contactTopics = await ExtractTopicsAsync(msgTexts, nodes[contactPhone].Name);

                        if (contactTopics.Count > 0)
                        {
                            // Store for logging later (outside Status context)
                            topicResults.Add((nodes[contactPhone].Name, contactTopics.Count));

                            nodes[contactPhone].TopTopics = UNLIMITED_TOPICS_MODE
                                ? contactTopics.ToList()
                                : contactTopics.Take(10).ToList();

                            foreach (string topic in contactTopics)
                            {
                                // Count messages containing this topic
                                int topicMessageCount = msgTexts.Count(m => m.IndexOf(topic, StringComparison.OrdinalIgnoreCase) >= 0);

                                if (topicMessageCount == 0)
                                {
                                    topicMessageCount = Math.Max(1, msgTexts.Count / Math.Max(contactTopics.Count, 1));
                                }

                                if (!topicToContacts.ContainsKey(topic))
                                {
                                    topicToContacts[topic] = new HashSet<string>();
                                    globalTopicFrequency[topic] = 0;
                                }

                                topicToContacts[topic].Add(contactPhone);
                                if (!contactTopicMessageCounts[contactPhone].ContainsKey(topic))
                                {
                                    contactTopicMessageCounts[contactPhone][topic] = 0;
                                }
                                contactTopicMessageCounts[contactPhone][topic] += topicMessageCount;
                                globalTopicFrequency[topic] += topicMessageCount;
                            }
                        }
                        else
                        {
                            emptyResponseContacts.Add(nodes[contactPhone].Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorContacts.Add((nodes[contactPhone].Name, ex.Message));
                    }

                    // Track all contacts
                    allContacts.Add(nodes[contactPhone].Name);
                }
            });

        // Now safe to log results outside Status context
        foreach ((string contactName, int topicCount) in topicResults)
        {
            Log.Information("Contact {ContactName}: Found {TopicCount} topics", contactName, topicCount);
        }

        // Log warnings for empty responses
        foreach (string contactName in emptyResponseContacts)
        {
            Log.Warning("Empty AI response for contact: {ContactName}", contactName);
        }

        // Log errors
        foreach ((string contactName, string error) in errorContacts)
        {
            Log.Error("Failed to extract topics for contact {ContactName}: {Error}", contactName, error);
        }

        // Final status summary
        int totalProcessed = processed;
        int totalSkipped = totalContacts - processed;
        AnsiConsole.MarkupLine($"[green]✓[/] Successfully processed {processed} contacts");
        if (totalSkipped > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]! Skipped {totalSkipped} contacts with insufficient messages[/]");
        }
        if (errorContacts.Count > 0)
        {
            AnsiConsole.MarkupLine($"[red]! Encountered errors for {errorContacts.Count} contacts see logs for details[/]");
        }

        // Filter and create topic nodes (from legacy)
        List<KeyValuePair<string, HashSet<string>>> validTopics = UNLIMITED_TOPICS_MODE
            ? topicToContacts
                .Where(kvp => globalTopicFrequency[kvp.Key] >= MIN_TOPIC_MESSAGES)
                .OrderByDescending(kvp => globalTopicFrequency[kvp.Key])
                .ToList()
            : topicToContacts
                .Where(kvp => globalTopicFrequency[kvp.Key] > 0)
                .OrderByDescending(kvp => globalTopicFrequency[kvp.Key])
                .ToList();

        Log.Information("Creating {TopicCount} topic nodes", validTopics.Count);

        int topicNodeId = 1;
        foreach (KeyValuePair<string, HashSet<string>> kvp in validTopics)
        {
            string topic = kvp.Key;
            HashSet<string> contactsDiscussingTopic = kvp.Value;

            string topicNodeIdStr = $"topic_{topicNodeId++}";
            nodes[topicNodeIdStr] = new GraphNode
            {
                Id = topicNodeIdStr,
                Name = topic,
                MessageCount = globalTopicFrequency[topic],
                Group = 2
            };

            // Create links from contacts to topics
            foreach (string contactPhone in contactsDiscussingTopic)
            {
                if (contactTopicMessageCounts.ContainsKey(contactPhone) &&
                    contactTopicMessageCounts[contactPhone].ContainsKey(topic))
                {
                    links.Add(new GraphLink
                    {
                        Source = contactPhone,
                        Target = topicNodeIdStr,
                        Value = contactTopicMessageCounts[contactPhone][topic]
                    });
                }
            }
        }

        // Safe to use AnsiConsole.MarkupLine outside of Status context
        AnsiConsole.MarkupLine($"[cyan]✓ Found {validTopics.Count} topics across {nodes.Count - 1} contacts[/]");

        // Generate HTML
        await GenerateHtmlAsync(nodes.Values.ToList(), links, outputPath);

        Log.Information("Network graph generated: {OutputPath}", outputPath);
        AnsiConsole.MarkupLine($"[green]✓[/] Network graph saved to: {outputPath}");
    }

    /// <summary>
    /// Extract contact name from Message (fixed version)
    /// </summary>
    private string ExtractContactName(Message message, string userName)
    {
        // Determine the contact (the person who is NOT the user)
        if (message.Direction == MessageDirection.Sent)
        {
            // Sent message - contact is the recipient
            return !string.IsNullOrEmpty(message.To?.Name) ? message.To.Name : "Unknown";
        }
        else
        {
            // Received message - contact is the sender
            return !string.IsNullOrEmpty(message.From?.Name) ? message.From.Name : "Unknown";
        }
    }

    /// <summary>
    /// Extract contact phone from Message (fixed version)
    /// </summary>
    private string ExtractContactPhone(Message message, string userName)
    {
        // Determine the contact phone (the phone that is NOT the user's)
        if (message.Direction == MessageDirection.Sent)
        {
            // Sent message - contact is the recipient
            return message.To?.PhoneNumbers?.FirstOrDefault() ?? "Unknown";
        }
        else
        {
            // Received message - contact is the sender
            return message.From?.PhoneNumbers?.FirstOrDefault() ?? "Unknown";
        }
    }

    /// <summary>
    /// Extract topics using AI (from legacy algorithm)
    /// </summary>
    private async Task<List<string>> ExtractTopicsAsync(List<string> messageTexts, string contactName)
    {
        if (messageTexts.Count < MIN_TOPIC_MESSAGES)
        {
            return new List<string>();
        }

        // Sample messages for analysis (legacy approach)
        int sampleSize = Math.Min(100, messageTexts.Count);
        List<string> sample = messageTexts
            .OrderBy(x => Guid.NewGuid())
            .Take(sampleSize)
            .ToList();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Messages from conversation with {contactName}:");
        foreach (string msg in sample)
        {
            sb.AppendLine($"- {msg}");
        }

        string prompt = $@"{sb}

Based on these messages, identify the main topics discussed.
Return ONLY a comma-separated list of 5-15 single-word or short-phrase topics (e.g., ""work, family, vacation, plans, hobbies"").
Do not include explanations, numbering, or extra formatting - just topics separated by commas.";

        try
        {
            string response = await _ollamaAnalyzer.CallOllamaAsync(prompt);

            if (string.IsNullOrWhiteSpace(response))
            {
                // Don't log here - we're inside Status context
                // Just return empty list silently
                return new List<string>();
            }

            // Parse response (legacy parsing logic)
            List<string> topics = response
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().Trim('.', '-', '*', '•', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', ')', '(', '[', ']', '"', '\''))
                .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 2 && t.Length < 50)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return topics;
        }
        catch (Exception)
        {
            // Don't log here - we're inside Status context
            // Just return empty list silently and let caller handle it
            return new List<string>();
        }
    }

    private async Task GenerateHtmlAsync(List<GraphNode> nodes, List<GraphLink> links, string outputPath)
    {
        string html = GenerateD3Html(nodes, links);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, html);
    }

    private string GenerateD3Html(List<GraphNode> nodes, List<GraphLink> links)
    {
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false };

        string nodesJson = JsonSerializer.Serialize(nodes, options);
        string linksJson = JsonSerializer.Serialize(links, options);

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Message Network Graph (AI-Generated Topics)</title>
    <script src=""https://d3js.org/d3.v7.min.js""></script>
    <style>
        body {{
            margin: 0;
            font-family: Arial, sans-serif;
            background-color: #1a1a1a;
            color: #ffffff;
        }}
        #graph {{
            width: 100vw;
            height: 100vh;
        }}
        .node {{
            stroke: #fff;
            stroke-width: 1.5px;
            cursor: pointer;
        }}
        .link {{
            stroke: #999;
            stroke-opacity: 0.6;
        }}
        .node-label {{
            font-size: 12px;
            pointer-events: none;
            fill: #ffffff;
            text-shadow: 0 1px 2px rgba(0,0,0,0.8);
        }}
        #info {{
            position: absolute;
            top: 10px;
            left: 10px;
            background-color: rgba(0,0,0,0.8);
            padding: 15px;
            border-radius: 5px;
            max-width: 300px;
        }}
        #legend {{
            position: absolute;
            top: 10px;
            right: 10px;
            background-color: rgba(0,0,0,0.8);
            padding: 15px;
            border-radius: 5px;
        }}
        .legend-item {{
            display: flex;
            align-items: center;
            margin-bottom: 8px;
        }}
        .legend-color {{
            width: 20px;
            height: 20px;
            border-radius: 50%;
            margin-right: 10px;
        }}
    </style>
</head>
<body>
    <div id=""info"">
        <h3>Message Network</h3>
        <p>Nodes: <span id=""nodeCount"">0</span></p>
        <p>Links: <span id=""linkCount"">0</span></p>
        <p>Click nodes to see details</p>
    </div>
    <div id=""legend"">
        <h4>Legend</h4>
        <div class=""legend-item"">
            <div class=""legend-color"" style=""background-color: #4CAF50;""></div>
            <span>You</span>
        </div>
        <div class=""legend-item"">
            <div class=""legend-color"" style=""background-color: #2196F3;""></div>
            <span>Contacts</span>
        </div>
        <div class=""legend-item"">
            <div class=""legend-color"" style=""background-color: #FF9800;""></div>
            <span>Topics</span>
        </div>
    </div>
    <svg id=""graph""></svg>
    <script>
        const data = {{
            nodes: {nodesJson},
            links: {linksJson}
        }};

        document.getElementById('nodeCount').textContent = data.nodes.length;
        document.getElementById('linkCount').textContent = data.links.length;

        const width = window.innerWidth;
        const height = window.innerHeight;

        const svg = d3.select('#graph')
            .attr('width', width)
            .attr('height', height);

        const simulation = d3.forceSimulation(data.nodes)
            .force('link', d3.forceLink(data.links).id(d => d.Id).distance(d => Math.max(50, 200 - d.Value)))
            .force('charge', d3.forceManyBody().strength(-300))
            .force('center', d3.forceCenter(width / 2, height / 2))
            .force('collision', d3.forceCollide().radius(d => Math.sqrt(d.MessageCount) * 2 + 10));

        const link = svg.append('g')
            .selectAll('line')
            .data(data.links)
            .join('line')
            .attr('class', 'link')
            .attr('stroke-width', d => Math.sqrt(d.Value));

        const node = svg.append('g')
            .selectAll('circle')
            .data(data.nodes)
            .join('circle')
            .attr('class', 'node')
            .attr('r', d => Math.max(5, Math.sqrt(d.MessageCount) * 2))
            .attr('fill', d => d.Group === 0 ? '#4CAF50' : d.Group === 1 ? '#2196F3' : '#FF9800')
            .call(d3.drag()
                .on('start', dragstarted)
                .on('drag', dragged)
                .on('end', dragended))
            .on('click', (event, d) => {{
                alert(`${{d.Name}}\\nMessages: ${{d.MessageCount}}\\nTopics: ${{d.TopTopics.join(', ')}}`);
            }});

        const label = svg.append('g')
            .selectAll('text')
            .data(data.nodes)
            .join('text')
            .attr('class', 'node-label')
            .text(d => d.Name)
            .attr('text-anchor', 'middle')
            .attr('dy', d => Math.max(5, Math.sqrt(d.MessageCount) * 2) + 15);

        simulation.on('tick', () => {{
            link
                .attr('x1', d => d.source.x)
                .attr('y1', d => d.source.y)
                .attr('x2', d => d.target.x)
                .attr('y2', d => d.target.y);

            node
                .attr('cx', d => d.x)
                .attr('cy', d => d.y);

            label
                .attr('x', d => d.x)
                .attr('y', d => d.y);
        }});

        function dragstarted(event) {{
            if (!event.active) simulation.alphaTarget(0.3).restart();
            event.subject.fx = event.subject.x;
            event.subject.fy = event.subject.y;
        }}

        function dragged(event) {{
            event.subject.fx = event.x;
            event.subject.fy = event.y;
        }}

        function dragended(event) {{
            if (!event.active) simulation.alphaTarget(0);
            event.subject.fx = null;
            event.subject.fy = null;
        }}
    </script>
</body>
</html>";
    }
}
