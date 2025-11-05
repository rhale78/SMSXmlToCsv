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
/// </summary>
public class NetworkGraphGenerator
{
    private readonly int _minTopicMessages;
    private readonly int _maxTopicsPerContact;
    private readonly TopicAnalyzer _topicAnalyzer;
    private readonly OllamaSentimentAnalyzer? _ollamaAnalyzer;

    public NetworkGraphGenerator(
        int minTopicMessages = 5,
        int maxTopicsPerContact = 250,
        OllamaSentimentAnalyzer? ollamaAnalyzer = null)
    {
        _minTopicMessages = minTopicMessages;
        _maxTopicsPerContact = maxTopicsPerContact;
        _topicAnalyzer = new TopicAnalyzer(maxTopicsPerContact: maxTopicsPerContact);
        _ollamaAnalyzer = ollamaAnalyzer;
    }

    /// <summary>
    /// Generate network graph for all contacts
    /// </summary>
    public async Task GenerateGraphAsync(IEnumerable<Message> messages, string outputPath, string userName = "You")
    {
        Log.Information("Generating network graph with {MaxTopics} topics per contact", _maxTopicsPerContact);

        List<Message> messageList = messages.ToList();
        Dictionary<string, GraphNode> nodes = new Dictionary<string, GraphNode>();
        List<GraphLink> links = new List<GraphLink>();

        // Create user node
        string userId = "user";
        nodes[userId] = new GraphNode
        {
            Id = userId,
            Name = userName,
            MessageCount = messageList.Count,
            Group = 0
        };

        // Create contact nodes and links
        Dictionary<string, List<Message>> contactMessages = new Dictionary<string, List<Message>>();

        foreach (Message message in messageList)
        {
            string contactName = GetContactName(message);

            if (!nodes.ContainsKey(contactName))
            {
                nodes[contactName] = new GraphNode
                {
                    Id = contactName,
                    Name = contactName,
                    MessageCount = 0,
                    Group = 1
                };
                contactMessages[contactName] = new List<Message>();
            }

            nodes[contactName].MessageCount++;
            contactMessages[contactName].Add(message);

            // Create or update link
            GraphLink? link = links.FirstOrDefault(l => l.Source == userId && l.Target == contactName);
            if (link == null)
            {
                link = new GraphLink { Source = userId, Target = contactName, Value = 0 };
                links.Add(link);
            }
            link.Value++;
        }

        Log.Information("Created {NodeCount} contact nodes and {LinkCount} links", nodes.Count - 1, links.Count);

        // Extract topics per contact
        await AnsiConsole.Status()
            .StartAsync("Analyzing topics...", async ctx =>
            {
                int processed = 0;
                foreach (KeyValuePair<string, List<Message>> kvp in contactMessages)
                {
                    processed++;
                    ctx.Status($"Analyzing topics for contact {processed}/{contactMessages.Count}: {kvp.Key}");

                    List<Topic> topics = _topicAnalyzer.ExtractTopics(kvp.Value, _minTopicMessages);

                    // Take up to max topics per contact
                    nodes[kvp.Key].TopTopics = topics
                        .Take(_maxTopicsPerContact)
                        .Select(t => t.Name)
                        .ToList();

                    // Create topic nodes and links
                    foreach (Topic topic in topics.Take(_maxTopicsPerContact))
                    {
                        string topicId = $"topic_{topic.Name}";

                        if (!nodes.ContainsKey(topicId))
                        {
                            nodes[topicId] = new GraphNode
                            {
                                Id = topicId,
                                Name = topic.Name,
                                MessageCount = topic.MessageCount,
                                Group = 2  // Topic group
                            };
                        }
                        else
                        {
                            // Update message count if topic already exists
                            nodes[topicId].MessageCount += topic.MessageCount;
                        }

                        // Create link from contact to topic
                        GraphLink topicLink = new GraphLink
                        {
                            Source = kvp.Key,
                            Target = topicId,
                            Value = topic.MessageCount
                        };
                        links.Add(topicLink);
                    }

                    await Task.Delay(1);  // Allow UI updates
                }
            });

        Log.Information("Created {TopicCount} topic nodes", nodes.Values.Count(n => n.Group == 2));

        // Generate HTML
        await GenerateHtmlAsync(nodes.Values.ToList(), links, outputPath);

        Log.Information("Network graph generated: {OutputPath}", outputPath);
        AnsiConsole.MarkupLine($"[green]✓[/] Network graph saved to: {outputPath}");
    }

    /// <summary>
    /// Generate network graph per contact
    /// </summary>
    public async Task GeneratePerContactGraphsAsync(IEnumerable<Message> messages, string outputDirectory, string userName = "You")
    {
        Log.Information("Generating per-contact network graphs");

        // Group messages by contact
        Dictionary<string, List<Message>> contactMessages = messages
            .GroupBy(m => GetContactName(m))
            .ToDictionary(g => g.Key, g => g.ToList());

        Directory.CreateDirectory(outputDirectory);

        int processed = 0;
        int total = contactMessages.Count;

        foreach (KeyValuePair<string, List<Message>> kvp in contactMessages)
        {
            processed++;
            AnsiConsole.MarkupLine($"[grey]Processing {processed}/{total}: {kvp.Key}[/]");

            string safeFileName = string.Join("_", kvp.Key.Split(Path.GetInvalidFileNameChars()));
            string outputPath = Path.Combine(outputDirectory, $"network_{safeFileName}.html");

            await GenerateSingleContactGraphAsync(kvp.Value, outputPath, userName, kvp.Key);
        }

        Log.Information("Generated {Count} per-contact network graphs", contactMessages.Count);
        AnsiConsole.MarkupLine($"[green]✓[/] Generated {contactMessages.Count} network graphs in: {outputDirectory}");
    }

    private async Task GenerateSingleContactGraphAsync(List<Message> messages, string outputPath, string userName, string contactName)
    {
        Dictionary<string, GraphNode> nodes = new Dictionary<string, GraphNode>();
        List<GraphLink> links = new List<GraphLink>();

        // User node
        nodes["user"] = new GraphNode
        {
            Id = "user",
            Name = userName,
            MessageCount = messages.Count,
            Group = 0
        };

        // Contact node
        nodes[contactName] = new GraphNode
        {
            Id = contactName,
            Name = contactName,
            MessageCount = messages.Count,
            Group = 1
        };

        // Link between user and contact
        links.Add(new GraphLink
        {
            Source = "user",
            Target = contactName,
            Value = messages.Count
        });

        // Extract topics
        List<Topic> topics = _topicAnalyzer.ExtractTopics(messages, _minTopicMessages);

        foreach (Topic topic in topics.Take(_maxTopicsPerContact))
        {
            string topicId = $"topic_{topic.Name}";

            nodes[topicId] = new GraphNode
            {
                Id = topicId,
                Name = topic.Name,
                MessageCount = topic.MessageCount,
                Group = 2
            };

            // Link from contact to topic
            links.Add(new GraphLink
            {
                Source = contactName,
                Target = topicId,
                Value = topic.MessageCount
            });
        }

        await GenerateHtmlAsync(nodes.Values.ToList(), links, outputPath);
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
        // Serialize data to JSON
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        string nodesJson = JsonSerializer.Serialize(nodes, options);
        string linksJson = JsonSerializer.Serialize(links, options);

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Message Network Graph</title>
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
            if (!event.active) simulation.alphaTarget(0.3);
            event.subject.fx = null;
            event.subject.fy = null;
        }}
    </script>
</body>
</html>";
    }

    private string GetContactName(Message message)
    {
        if (!string.IsNullOrEmpty(message.From?.Name))
        {
            return message.From.Name;
        }

        if (!string.IsNullOrEmpty(message.To?.Name))
        {
            return message.To.Name;
        }

        return "Unknown";
    }
}
