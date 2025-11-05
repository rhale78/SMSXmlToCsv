using System.Text;

using SMSXmlToCsv.Logging;
using SMSXmlToCsv.ML;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Visualization
{
    public class NetworkGraphGenerator
    {
        private readonly bool _splitByContact;
        private readonly OllamaIntegration? _ollama;

        // FEATURE FLAG: Unlimited topics mode - filter only by message count (>=5)
        private const bool UNLIMITED_TOPICS_MODE = true;
        private const int MIN_TOPIC_MESSAGES = 5;  // Minimum messages to include a topic

        public NetworkGraphGenerator(bool splitByContact = true, OllamaIntegration? ollama = null)
        {
            _splitByContact = splitByContact;
            _ollama = ollama;
        }

        public async Task GenerateGraphAsync(List<SmsMessage> messages, string outputPath, string userPhone)
        {
            Console.WriteLine($"[DEBUG] Generating network graph with {messages.Count} messages");
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Starting generation with {messages.Count} messages, user: [MASKED]");

            AppLogger.Information($"Generating network graph with AI topic detection (split by contact: {_splitByContact})");
            AppLogger.Information($"Input: {messages.Count} messages, user phone: [MASKED]");

            bool useOllama = _ollama != null && await _ollama.IsAvailableAsync();

            Console.WriteLine($"[DEBUG] Ollama available: {useOllama}");
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Ollama available: {useOllama}");

            if (!useOllama)
            {
                Console.WriteLine("[DEBUG] Ollama not available - generating basic contact graph only");
                System.Diagnostics.Debug.WriteLine("[NET GRAPH] Ollama not available for topic detection");

                AppLogger.Warning("Ollama not available for topic detection. Generating basic contact graph only.");
                AppLogger.Warning("Please install Ollama from https://ollama.ai and ensure it's running for topic detection.");
            }

            Dictionary<string, GraphNode> nodes = new Dictionary<string, GraphNode>();
            List<GraphLink> links = new List<GraphLink>();
            Dictionary<string, List<string>> contactMessages = new Dictionary<string, List<string>>();
            Dictionary<string, HashSet<string>> topicToContacts = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> globalTopicFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Dictionary<string, int>> contactTopicMessageCounts = new Dictionary<string, Dictionary<string, int>>();

            nodes[userPhone] = new GraphNode { Id = userPhone, Name = "You", MessageCount = 0, Group = 0 };

            List<string> allMessages = new List<string>();

            foreach (SmsMessage msg in messages)
            {
                string otherPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;
                string otherName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
                if (otherPhone == userPhone)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(otherName) || otherName == "(Unknown)")
                {
                    otherName = otherPhone;
                }

                if (!nodes.ContainsKey(otherPhone))
                {
                    nodes[otherPhone] = new GraphNode { Id = otherPhone, Name = otherName, MessageCount = 0, Group = 1 };
                    contactMessages[otherPhone] = new List<string>();
                    contactTopicMessageCounts[otherPhone] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                }

                nodes[userPhone].MessageCount++;
                nodes[otherPhone].MessageCount++;

                // FIX: Collect ALL messages (both sent and received) for topic detection
                // This gives better topic analysis as it includes the full conversation context
                if (!string.IsNullOrWhiteSpace(msg.MessageText) && msg.MessageText.Length > 10)
                {
                    contactMessages[otherPhone].Add(msg.MessageText);

                    if (!_splitByContact)
                    {
                        allMessages.Add(msg.MessageText);
                    }
                }

                GraphLink? link = links.FirstOrDefault(l => l.Source == userPhone && l.Target == otherPhone);
                if (link == null)
                {
                    link = new GraphLink { Source = userPhone, Target = otherPhone, Value = 0 };
                    links.Add(link);
                }
                link.Value++;
            }

            AppLogger.Information($"Created {nodes.Count} contact nodes (including 'You' node)");
            AppLogger.Information($"Created {links.Count} contact-to-user links");
            AppLogger.Information($"Collected messages from {contactMessages.Count} contacts");

            Console.WriteLine($"[DEBUG] Created {nodes.Count} nodes, {links.Count} links, {contactMessages.Count} contacts");
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Nodes: {nodes.Count}, Links: {links.Count}, Contacts: {contactMessages.Count}");

            if (useOllama)
            {
                if (_splitByContact)
                {
                    AppLogger.Information("Using AI-powered per-contact topic detection");

                    int contactsProcessed = 0;
                    foreach (KeyValuePair<string, List<string>> kvp in contactMessages)
                    {
                        string contactPhone = kvp.Key;
                        List<string> msgTexts = kvp.Value;

                        if (msgTexts.Count > 0 && nodes.ContainsKey(contactPhone))
                        {
                            contactsProcessed++;
                            AppLogger.Information($"Analyzing topics for contact {contactsProcessed}/{contactMessages.Count}: {nodes[contactPhone].Name}");

                            // UNLIMITED_TOPICS_MODE: Request more topics, filter by message count later
                            int topicsToRequest = UNLIMITED_TOPICS_MODE ? 50 : 20;
                            List<string> contactTopics = await DetectTopicsWithAI(msgTexts, topicsToRequest);
                            AppLogger.Information($"  Detected {contactTopics.Count} topics for {nodes[contactPhone].Name}");

                            // Store all topics (unlimited) or limited (old behavior)
                            nodes[contactPhone].TopTopics = UNLIMITED_TOPICS_MODE
                                  ? contactTopics.ToList()  // Keep all detected topics
       : contactTopics.Take(10).ToList();  // OLD: Limit to 10

                            // FIX: Calculate actual message counts per topic using keyword matching
                            foreach (string topic in contactTopics)
                            {
                                // Count messages that actually contain this topic
                                int topicMessageCount = msgTexts.Count(m => m.IndexOf(topic, StringComparison.OrdinalIgnoreCase) >= 0);

                                // If no exact matches, distribute evenly (fallback)
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

                                AppLogger.Information($"    Topic '{topic}': {topicMessageCount} messages");
                            }
                        }
                    }

                    // ISSUE #6 FIX: Filter out zero-value topics
                    // UNLIMITED_TOPICS_MODE: Also filter topics with <5 total messages
                    List<KeyValuePair<string, HashSet<string>>> validTopics = UNLIMITED_TOPICS_MODE
? topicToContacts
      .Where(kvp => globalTopicFrequency[kvp.Key] >= MIN_TOPIC_MESSAGES)  // NEW: Min 5 messages
  .OrderByDescending(kvp => globalTopicFrequency[kvp.Key])
     .ToList()
  : topicToContacts  // OLD CODE: Just filter zero
     .Where(kvp => globalTopicFrequency[kvp.Key] > 0)
      .OrderByDescending(kvp => globalTopicFrequency[kvp.Key])
 .ToList();

                    int filteredCount = topicToContacts.Count - validTopics.Count;
                    if (filteredCount > 0)
                    {
                        string filterReason = UNLIMITED_TOPICS_MODE ? $"<{MIN_TOPIC_MESSAGES} messages" : "zero-value";
                        AppLogger.Information($"Filtered out {filteredCount} topics ({filterReason})");
                    }

                    AppLogger.Information($"Creating topic nodes from {validTopics.Count} valid topics" +
                     (UNLIMITED_TOPICS_MODE ? $" (min {MIN_TOPIC_MESSAGES} msgs)" : ""));

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
                            Group = 2,
                            TopTopics = new List<string>()
                        };

                        foreach (string contactPhone in contactsDiscussingTopic)
                        {
                            int linkValue = contactTopicMessageCounts.ContainsKey(contactPhone) &&
                                           contactTopicMessageCounts[contactPhone].ContainsKey(topic)
                                ? contactTopicMessageCounts[contactPhone][topic]
                                : globalTopicFrequency[topic] / contactsDiscussingTopic.Count;

                            links.Add(new GraphLink
                            {
                                Source = contactPhone,
                                Target = topicNodeIdStr,
                                Value = linkValue
                            });
                        }
                    }

                    AppLogger.Information($"Created {topicNodeId - 1} topic nodes");
                    AppLogger.Information($"Total links including topic links: {links.Count}");
                }
                else
                {
                    AppLogger.Information("Using AI-powered global topic detection for entire conversation");

                    if (allMessages.Count > 0)
                    {
                        // UNLIMITED_TOPICS_MODE: Request more global topics
                        int topicsToRequest = UNLIMITED_TOPICS_MODE ? 50 : 20;
                        List<string> globalTopics = await DetectTopicsWithAI(allMessages, topicsToRequest);
                        AppLogger.Information($"Detected {globalTopics.Count} global topics from {allMessages.Count} messages");

                        foreach (KeyValuePair<string, List<string>> kvp in contactMessages)
                        {
                            string contactPhone = kvp.Key;
                            List<string> msgTexts = kvp.Value;

                            if (msgTexts.Count > 0 && nodes.ContainsKey(contactPhone))
                            {
                                Dictionary<string, int> topicFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                                foreach (string topic in globalTopics)
                                {
                                    int count = msgTexts.Count(m => m.Contains(topic, StringComparison.OrdinalIgnoreCase));
                                    if (count > 0)
                                    {
                                        topicFrequency[topic] = count;

                                        if (!topicToContacts.ContainsKey(topic))
                                        {
                                            topicToContacts[topic] = new HashSet<string>();
                                            globalTopicFrequency[topic] = 0;
                                        }
                                        topicToContacts[topic].Add(contactPhone);
                                        globalTopicFrequency[topic] += count;

                                        if (!contactTopicMessageCounts[contactPhone].ContainsKey(topic))
                                        {
                                            contactTopicMessageCounts[contactPhone][topic] = 0;
                                        }
                                        contactTopicMessageCounts[contactPhone][topic] += count;
                                    }
                                }

                                nodes[contactPhone].TopTopics = topicFrequency
                                    .OrderByDescending(t => t.Value)
                                    .Take(10)
                                    .Select(t => t.Key)
                                    .ToList();
                            }
                        }

                        // ISSUE #6 FIX: Filter zero-value topics for global analysis
                        // UNLIMITED_TOPICS_MODE: Also filter <5 messages
                        List<string> validGlobalTopics = UNLIMITED_TOPICS_MODE
               ? globalTopics
                     .Where(topic => topicToContacts.ContainsKey(topic) && globalTopicFrequency[topic] >= MIN_TOPIC_MESSAGES)
                 .ToList()
              : globalTopics  // OLD CODE
                    .Where(topic => topicToContacts.ContainsKey(topic) && globalTopicFrequency[topic] > 0)
                  .ToList();

                        int filteredGlobalCount = globalTopics.Count - validGlobalTopics.Count;
                        if (filteredGlobalCount > 0)
                        {
                            string filterReason = UNLIMITED_TOPICS_MODE ? $"<{MIN_TOPIC_MESSAGES} messages" : "zero-value";
                            AppLogger.Information($"Filtered out {filteredGlobalCount} global topics ({filterReason})");
                        }

                        AppLogger.Information($"Creating topic nodes from {validGlobalTopics.Count} valid global topics" +
                         (UNLIMITED_TOPICS_MODE ? $" (min {MIN_TOPIC_MESSAGES} msgs)" : ""));

                        int topicNodeId = 1;
                        foreach (var topic in validGlobalTopics)
                        {
                            if (!topicToContacts.ContainsKey(topic))
                            {
                                continue;
                            }

                            HashSet<string> contactsDiscussingTopic = topicToContacts[topic];

                            string topicNodeIdStr = $"topic_{topicNodeId++}";
                            nodes[topicNodeIdStr] = new GraphNode
                            {
                                Id = topicNodeIdStr,
                                Name = topic,
                                MessageCount = globalTopicFrequency[topic],
                                Group = 2,
                                TopTopics = new List<string>()
                            };

                            foreach (string contactPhone in contactsDiscussingTopic)
                            {
                                int linkValue = contactTopicMessageCounts.ContainsKey(contactPhone) &&
                                               contactTopicMessageCounts[contactPhone].ContainsKey(topic)
                                    ? contactTopicMessageCounts[contactPhone][topic]
                                    : globalTopicFrequency[topic] / contactsDiscussingTopic.Count;

                                links.Add(new GraphLink
                                {
                                    Source = contactPhone,
                                    Target = topicNodeIdStr,
                                    Value = linkValue
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                AppLogger.Warning("Skipping topic detection - Ollama not available");
            }

            int contactNodeCount = nodes.Values.Count(n => n.Group <= 1);
            int topicNodeCount = nodes.Values.Count(n => n.Group == 2);
            AppLogger.Information($"EXPORT SUMMARY: {nodes.Count} total nodes ({contactNodeCount} contacts, {topicNodeCount} topics), {links.Count} links");

            Console.WriteLine($"[DEBUG] EXPORT SUMMARY: {nodes.Count} total nodes ({contactNodeCount} contacts, {topicNodeCount} topics), {links.Count} links");
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] EXPORT: {nodes.Count} nodes, {links.Count} links");

            Console.WriteLine($"[DEBUG] Exporting to: {outputPath}");
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Output path: {outputPath}");

            await ExportD3JsonAsync(nodes.Values.ToList(), links, outputPath);
            await GenerateHtmlViewerAsync(nodes.Values.ToList(), links, outputPath.Replace(".json", ".html"));

            Console.WriteLine($"[DEBUG] Export complete!");
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Export complete");

            AppLogger.Information($"Network graph files created: {outputPath} and {outputPath.Replace(".json", ".html")}");
            AppLogger.Information($"Network graph generated successfully: {nodes.Count} nodes, {links.Count} links");
        }

        /// <summary>
        /// ISSUE #5 FIX: Detect topics using AI with improved grouping
        /// </summary>
        private async Task<List<string>> DetectTopicsWithAI(List<string> messages, int topN = 20)
        {
            if (_ollama == null)
            {
                AppLogger.Warning("Ollama not available for topic detection");
                return new List<string>();
            }

            // UNLIMITED_TOPICS_MODE: Increase sampling for better coverage
            int sampleSize = UNLIMITED_TOPICS_MODE
             ? (messages.Count switch
             {
                 <= 500 => messages.Count,
                 <= 1000 => 750,    // More samples for better coverage
                 <= 3000 => 1500,
                 _ => 2500    // Larger sample for unlimited mode
             })
              : (messages.Count switch  // OLD CODE
              {
                  <= 500 => messages.Count,
                  <= 2000 => 500,
                  <= 5000 => 1000,
                  _ => 2000
              });
            List<string> samplesToAnalyze;
            if (messages.Count <= sampleSize)
            {
                samplesToAnalyze = messages;
            }
            else
            {
                int step = messages.Count / sampleSize;
                samplesToAnalyze = Enumerable.Range(0, sampleSize).Select(i => messages[i * step]).ToList();
            }

            AppLogger.Information($"Analyzing {samplesToAnalyze.Count} messages for topic detection using AI");

            // UNLIMITED_TOPICS_MODE: Analyze more message samples
            string conversationText = string.Join("\n---\n",
       samplesToAnalyze.Take(UNLIMITED_TOPICS_MODE ? 200 : 100));

            // Different prompt for unlimited vs limited mode
            string prompt = UNLIMITED_TOPICS_MODE
          ? $@"Analyze these conversation messages and identify ALL significant topics discussed (no limit).

IMPORTANT RULES:
1. Include EVERY meaningful topic that appears multiple times
2. Group similar topics together (e.g., 'COVID-19', 'covid', 'coronavirus' ? use 'COVID-19')
3. Use consistent naming (e.g., 'COVID vaccine' not 'covid shot', 'covid vaccination')
4. Be specific but not redundant (avoid 'work', 'project', 'work project' - pick one)
5. Use 2-3 words per topic maximum
6. Provide ONLY a comma-separated list, nothing else
7. NO LIMIT on number of topics - include everything meaningful

Examples of GOOD topics:
- 'work project' (not 'work', 'project', 'work stuff')
- 'COVID-19' (not 'covid', 'coronavirus', 'pandemic')
- 'weekend plans' (not 'weekend', 'plans', 'Saturday')
- 'health update' (not 'health', 'medical', 'doctor visit')

Conversation samples:
{conversationText}

ALL Topics (comma-separated, NO LIMIT):"
         : $@"Analyze these conversation messages and identify the top {topN} main topics discussed.

IMPORTANT RULES:
1. Group similar topics together (e.g., 'COVID-19', 'covid', 'coronavirus' ? use 'COVID-19')
2. Use consistent naming (e.g., 'COVID vaccine' not 'covid shot', 'covid vaccination')
3. Be specific but not redundant (avoid 'work', 'project', 'work project' - pick one)
4. Use 2-3 words per topic maximum
5. Provide ONLY a comma-separated list, nothing else

Examples of GOOD topics:
- 'work project' (not 'work', 'project', 'work stuff')
- 'COVID-19' (not 'covid', 'coronavirus', 'pandemic')
- 'weekend plans' (not 'weekend', 'plans', 'Saturday')
- 'health update' (not 'health', 'medical', 'doctor visit')

Conversation samples:
{conversationText}

Topics (comma-separated):";

            try
            {
                string response = await _ollama.GenerateAsync(prompt, "llama3.2");

                List<string> topics = response.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.Trim().Trim('"', '\'', '.', '-', '•', '*', '`'))
                    .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length >= 3 && t.Length <= 30)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                topics = DeduplicateTopics(topics);

                string modeLabel = UNLIMITED_TOPICS_MODE ? " - UNLIMITED MODE" : "";
                AppLogger.Information($"AI detected {topics.Count} topics (after deduplication){modeLabel}");

                // Return all topics (unlimited) or limited (old behavior)
                return UNLIMITED_TOPICS_MODE ? topics : topics.Take(topN).ToList();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"AI topic detection failed: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// ISSUE #5 FIX: Smart topic deduplication
        /// </summary>
        private List<string> DeduplicateTopics(List<string> topics)
        {
            List<string> deduplicated = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var topic in topics)
            {
                string normalized = topic.ToLowerInvariant()
                    .Replace("covid-19", "covid")
                    .Replace("coronavirus", "covid")
                    .Replace("covid 19", "covid")
                    .Replace(" shot", " vaccine")
                    .Replace(" vaccination", " vaccine")
                    .Replace(" jab", " vaccine")
                    .Trim();

                if (!seen.Contains(normalized))
                {
                    seen.Add(normalized);
                    deduplicated.Add(topic);
                }
            }

            return deduplicated;
        }

        private async Task ExportD3JsonAsync(List<GraphNode> nodes, List<GraphLink> links, string outputPath)
        {
            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                await writer.WriteLineAsync("{");
                await writer.WriteLineAsync("  \"nodes\": [");
                for (int i = 0; i < nodes.Count; i++)
                {
                    GraphNode node = nodes[i];
                    string comma = i < nodes.Count - 1 ? "," : "";
                    string topicsJson = node.TopTopics.Count > 0
                        ? $", \"topics\": [{string.Join(", ", node.TopTopics.Select(t => $"\"{EscapeJson(t)}\""))}]"
                        : "";
                    await writer.WriteLineAsync($"    {{\"id\": \"{EscapeJson(node.Id)}\", \"name\": \"{EscapeJson(node.Name)}\", \"group\": {node.Group}, \"value\": {node.MessageCount}{topicsJson}}}{comma}");
                }
                await writer.WriteLineAsync("  ],");
                await writer.WriteLineAsync("  \"links\": [");
                for (int i = 0; i < links.Count; i++)
                {
                    GraphLink link = links[i];
                    string comma = i < links.Count - 1 ? "," : "";
                    await writer.WriteLineAsync($"    {{\"source\": \"{EscapeJson(link.Source)}\", \"target\": \"{EscapeJson(link.Target)}\", \"value\": {link.Value}}}{comma}");
                }
                await writer.WriteLineAsync("  ]");
                await writer.WriteLineAsync("}");
            }
        }

        private string EscapeJson(string value)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private async Task GenerateHtmlViewerAsync(List<GraphNode> nodes, List<GraphLink> links, string outputPath)
        {
            var nodesJson = string.Join(",\n", nodes.Select(n =>
              $"{{\"id\":\"{EscapeJson(n.Id)}\",\"name\":\"{EscapeJson(n.Name)}\",\"group\":{n.Group},\"value\":{n.MessageCount}" +
     (n.TopTopics.Count > 0 ? $",\"topics\":[{string.Join(",", n.TopTopics.Select(t => $"\"{EscapeJson(t)}\""))}]" : "") +
          "}"));

            var linksJson = string.Join(",\n", links.Select(l =>
        $"{{\"source\":\"{EscapeJson(l.Source)}\",\"target\":\"{EscapeJson(l.Target)}\",\"value\":{l.Value}}}"));

            Console.WriteLine($"[DEBUG] Generating HTML with {nodes.Count} nodes and {links.Count} links");

            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"utf-8\">");
            html.AppendLine("<title>SMS Network Graph</title>");
            html.AppendLine("<script src=\"https://d3js.org/d3.v7.min.js\"></script>");
            html.AppendLine("<style>");
            html.AppendLine("body{margin:0;font-family:'Segoe UI',sans-serif;background:#1a1a2e;color:#eee;overflow:hidden}");
            html.AppendLine("#graph{width:100vw;height:100vh}");
            html.AppendLine(".node{stroke:#fff;stroke-width:2px;cursor:pointer;transition:all 0.3s}");
            html.AppendLine(".node:hover{stroke:#ffd700;stroke-width:3px}");
            html.AppendLine(".node.highlighted{stroke:#ffd700;stroke-width:3px;filter:brightness(1.3)}");
            html.AppendLine(".link{stroke:#999;stroke-opacity:0.4;transition:all 0.3s}");
            html.AppendLine(".link:hover{stroke:#ffd700;stroke-opacity:1;stroke-width:3px;cursor:pointer}");
            html.AppendLine(".link.highlighted{stroke:#ffd700;stroke-opacity:1;stroke-width:3px;animation:pulse 1.5s ease-in-out infinite}");
            html.AppendLine("@keyframes pulse{0%,100%{stroke-opacity:0.8}50%{stroke-opacity:1}}");
            html.AppendLine(".label{font-size:11px;fill:#eee;text-anchor:middle;pointer-events:none;text-shadow:0 0 3px #000;transition:opacity 0.3s}");
            html.AppendLine(".label.topic-label{font-size:13px;fill:#fff;font-weight:bold;text-shadow:0 0 4px #000}");
            html.AppendLine(".label.hidden{opacity:0}");
            html.AppendLine(".node-count{font-size:10px;fill:#fff;font-weight:bold;text-anchor:middle;pointer-events:none;text-shadow:0 0 2px #000}");
            html.AppendLine("#info{position:absolute;top:20px;left:20px;background:rgba(26,26,46,0.95);padding:20px;border-radius:10px;border:1px solid #16213e;min-width:280px;max-width:380px}");
            html.AppendLine("#info h3{margin:0 0 10px 0;color:#4ecca3;font-size:1.3em}");
            html.AppendLine("#info p{margin:5px 0;color:#aaa;font-size:0.9em}");
            html.AppendLine("#node-info{margin-top:15px;padding-top:15px;border-top:1px solid #16213e;min-height:60px}");
            html.AppendLine(".topics{margin-top:10px;padding:8px;background:rgba(78,204,163,0.1);border-radius:5px;border-left:3px solid #4ecca3}");
            html.AppendLine(".topic-tag{display:inline-block;padding:3px 8px;margin:3px;background:rgba(78,204,163,0.3);border-radius:12px;font-size:0.85em;color:#4ecca3}");
            html.AppendLine("#stats{position:absolute;bottom:20px;left:20px;background:rgba(26,26,46,0.95);padding:15px;border-radius:10px;border:1px solid #16213e;font-size:0.85em}");
            html.AppendLine(".stat-item{margin:5px 0}.stat-label{color:#4ecca3;font-weight:bold}");
            html.AppendLine(".legend{position:absolute;top:20px;right:20px;background:rgba(26,26,46,0.95);padding:15px;border-radius:10px;border:1px solid #16213e}");
            html.AppendLine(".legend-item{margin:8px 0;display:flex;align-items:center}");
            html.AppendLine(".legend-color{width:20px;height:20px;border-radius:50%;margin-right:10px;border:2px solid #fff}");
            html.AppendLine("#controls{position:absolute;bottom:20px;right:20px;background:rgba(26,26,46,0.95);padding:15px;border-radius:10px;border:1px solid #16213e}");
            html.AppendLine(".control-btn{background:#4ecca3;color:#1a1a2e;border:none;padding:8px 15px;margin:3px;border-radius:5px;cursor:pointer;font-weight:bold;transition:all 0.3s}");
            html.AppendLine(".control-btn:hover{background:#3dbb92;transform:scale(1.05)}");
            html.AppendLine("#zoom-level{position:absolute;bottom:90px;right:20px;background:rgba(26,26,46,0.95);padding:10px 15px;border-radius:10px;border:1px solid #16213e;color:#4ecca3;font-size:0.9em}");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<div id=\"info\">");
            html.AppendLine("<h3>SMS Network Graph</h3>");
            html.AppendLine("<p><strong>Drag</strong> nodes to rearrange</p>");
            html.AppendLine("<p><strong>Click</strong> nodes to see connections</p>");
            html.AppendLine("<p><strong>Click</strong> links for details</p>");
            html.AppendLine("<p><strong>Scroll</strong> to zoom</p>");
            html.AppendLine("<div id=\"node-info\"></div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class=\"legend\">");
            html.AppendLine("<h4 style=\"margin:0 0 10px 0;color:#4ecca3\">Legend</h4>");
            html.AppendLine("<div class=\"legend-item\"><div class=\"legend-color\" style=\"background:#ff4444\"></div><span>You</span></div>");
            html.AppendLine("<div class=\"legend-item\"><div class=\"legend-color\" style=\"background:#4444ff\"></div><span>Contacts</span></div>");
            html.AppendLine("<div class=\"legend-item\"><div class=\"legend-color\" style=\"background:#9b59b6\"></div><span>Topics</span></div>");
            html.AppendLine("<div style=\"margin-top:10px;padding-top:10px;border-top:1px solid #16213e;font-size:0.85em;color:#aaa\">");
            html.AppendLine("<strong>Node Size:</strong> Message volume<br>");
            html.AppendLine("<strong>Link Thickness:</strong> Discussion intensity<br>");
            html.AppendLine("<strong>Numbers in circles:</strong> Total messages");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div id=\"stats\">");
            html.AppendLine("<div class=\"stat-label\">Network Statistics</div>");
            html.AppendLine("<div class=\"stat-item\">People: <span id=\"stat-people\">-</span></div>");
            html.AppendLine("<div class=\"stat-item\">Topics: <span id=\"stat-topics\">-</span></div>");
            html.AppendLine("<div class=\"stat-item\">Connections: <span id=\"stat-links\">-</span></div>");
            html.AppendLine("<div class=\"stat-item\">Messages: <span id=\"stat-messages\">-</span></div>");
            html.AppendLine("</div>");

            html.AppendLine("<div id=\"zoom-level\">Zoom: 100%</div>");

            html.AppendLine("<div id=\"controls\">");
            html.AppendLine("<div style=\"color:#4ecca3;font-weight:bold;margin-bottom:8px\">View Controls</div>");
            html.AppendLine("<button class=\"control-btn\" onclick=\"resetZoom()\">Reset View</button>");
            html.AppendLine("<button class=\"control-btn\" onclick=\"zoomIn()\">Zoom In</button>");
            html.AppendLine("<button class=\"control-btn\" onclick=\"zoomOut()\">Zoom Out</button>");
            html.AppendLine("<button class=\"control-btn\" onclick=\"clearHighlights()\">Clear Highlights</button>");
            html.AppendLine("</div>");

            html.AppendLine("<svg id=\"graph\"></svg>");

            // COMPLETE JAVASCRIPT WITH ALL FIXES
            html.AppendLine("<script>");
            html.AppendLine("const graphData = {");
            html.AppendLine("  nodes: [");
            html.AppendLine(nodesJson);
            html.AppendLine("  ],");
            html.AppendLine("  links: [");
            html.AppendLine(linksJson);
            html.AppendLine("  ]");
            html.AppendLine("};");
            html.AppendLine("");
            html.AppendLine("const width = window.innerWidth;");
            html.AppendLine("const height = window.innerHeight;");
            html.AppendLine("");
            html.AppendLine("const svg = d3.select('#graph').attr('width', width).attr('height', height);");
            html.AppendLine("const g = svg.append('g');");
            html.AppendLine("");
            html.AppendLine("let currentZoom = 1;");
            html.AppendLine("const zoom = d3.zoom()");
            html.AppendLine("  .scaleExtent([0.1, 10])");
            html.AppendLine("  .on('zoom', (event) => {");
            html.AppendLine("    g.attr('transform', event.transform);");
            html.AppendLine("    currentZoom = event.transform.k;");
            html.AppendLine("    d3.select('#zoom-level').text('Zoom: ' + Math.round(currentZoom * 100) + '%');");
            html.AppendLine("    updateLabelVisibility(currentZoom);");
            html.AppendLine("  });");
            html.AppendLine("svg.call(zoom);");
            html.AppendLine("");
            html.AppendLine("window.resetZoom = () => { svg.transition().duration(750).call(zoom.transform, d3.zoomIdentity); clearHighlights(); };");
            html.AppendLine("window.zoomIn = () => { svg.transition().duration(300).call(zoom.scaleBy, 1.3); };");
            html.AppendLine("window.zoomOut = () => { svg.transition().duration(300).call(zoom.scaleBy, 0.7); };");
            html.AppendLine("");
            html.AppendLine("function clearHighlights() {");
            html.AppendLine("  d3.selectAll('.node').classed('highlighted', false);");
            html.AppendLine("  d3.selectAll('.link').classed('highlighted', false);");
            html.AppendLine("  d3.select('#node-info').html('');");
            html.AppendLine("}");
            html.AppendLine("window.clearHighlights = clearHighlights;");
            html.AppendLine("");
            html.AppendLine("const peopleCount = graphData.nodes.filter(n => n.group <= 1).length;");
            html.AppendLine("const topicCount = graphData.nodes.filter(n => n.group === 2).length;");
            html.AppendLine("const totalMessages = graphData.nodes.filter(n => n.group <= 1).reduce((sum, n) => sum + n.value, 0);");
            html.AppendLine("d3.select('#stat-people').text(peopleCount);");
            html.AppendLine("d3.select('#stat-topics').text(topicCount);");
            html.AppendLine("d3.select('#stat-links').text(graphData.links.length);");
            html.AppendLine("d3.select('#stat-messages').text(totalMessages.toLocaleString());");
            html.AppendLine("");
            html.AppendLine("const simulation = d3.forceSimulation(graphData.nodes)");
            html.AppendLine("  .force('link', d3.forceLink(graphData.links).id(d => d.id).distance(d => {");
            html.AppendLine("    const source = graphData.nodes.find(n => n.id === (d.source.id || d.source));");
            html.AppendLine("    const target = graphData.nodes.find(n => n.id === (d.target.id || d.target));");
            html.AppendLine("    return (source && source.group === 2) || (target && target.group === 2) ? 100 : 150;");
            html.AppendLine("  }))");
            html.AppendLine("  .force('charge', d3.forceManyBody().strength(d => d.group === 2 ? -600 : -400))");
            html.AppendLine("  .force('center', d3.forceCenter(width / 2, height / 2))");
            html.AppendLine("  .force('collision', d3.forceCollide().radius(d => Math.sqrt(d.value) + 25));");
            html.AppendLine("");

            // ISSUE #3 FIX: Link click handler
            html.AppendLine("const link = g.append('g').selectAll('line').data(graphData.links).enter().append('line')");
            html.AppendLine("  .attr('class', 'link')");
            html.AppendLine("  .attr('stroke-width', d => Math.max(1, Math.sqrt(d.value) / 5))");
            html.AppendLine("  .on('click', function(event, d) {");
            html.AppendLine("    event.stopPropagation();");
            html.AppendLine("    clearHighlights();");
            html.AppendLine("    d3.select(this).classed('highlighted', true);");
            html.AppendLine("    const sourceId = d.source.id || d.source;");
            html.AppendLine("    const targetId = d.target.id || d.target;");
            html.AppendLine("    const sourceNode = graphData.nodes.find(n => n.id === sourceId);");
            html.AppendLine("    const targetNode = graphData.nodes.find(n => n.id === targetId);");
            html.AppendLine("    node.classed('highlighted', n => n.id === sourceId || n.id === targetId);");
            html.AppendLine("    let type = 'Connection', desc = '';");
            html.AppendLine("    if (sourceNode.group <= 1 && targetNode.group <= 1) {");
            html.AppendLine("      type = 'Contact to User'; desc = sourceNode.name + ' ? ' + targetNode.name;");
            html.AppendLine("    } else if (sourceNode.group <= 1 && targetNode.group === 2) {");
            html.AppendLine("      type = 'Contact discusses Topic'; desc = '<strong>' + sourceNode.name + '</strong> discusses <strong>' + targetNode.name + '</strong>';");
            html.AppendLine("    } else if (sourceNode.group === 2 && targetNode.group <= 1) {");
            html.AppendLine("      type = 'Topic discussed by Contact'; desc = '<strong>' + targetNode.name + '</strong> discusses <strong>' + sourceNode.name + '</strong>';");
            html.AppendLine("    }");
            html.AppendLine("    d3.select('#node-info').html('<div style=\"color:#ffd700;font-weight:bold;margin-bottom:8px\">' + type + '</div><div style=\"color:#4ecca3;margin-bottom:4px\">' + desc + '</div><div style=\"color:#eee;margin-top:8px\"><strong>' + d.value.toLocaleString() + '</strong> messages</div>');");
            html.AppendLine("  })");
            html.AppendLine("  .on('mouseover', function() { if (!d3.select(this).classed('highlighted')) d3.select(this).style('stroke', '#ffd700').style('stroke-opacity', 1); })");
            html.AppendLine("  .on('mouseout', function() { if (!d3.select(this).classed('highlighted')) d3.select(this).style('stroke', '#999').style('stroke-opacity', 0.4); });");
            html.AppendLine("");

            // ISSUE #2 FIX: Node click handler with highlighting
            html.AppendLine("const node = g.append('g').selectAll('circle').data(graphData.nodes).enter().append('circle')");
            html.AppendLine("  .attr('class', 'node')");
            html.AppendLine("  .attr('r', d => d.group === 2 ? Math.max(12, Math.sqrt(d.value) + 5) : Math.max(10, Math.sqrt(d.value) + 5))");
            html.AppendLine("  .attr('fill', d => d.group === 0 ? '#ff4444' : (d.group === 1 ? '#4444ff' : '#9b59b6'))");
            html.AppendLine("  .call(d3.drag()");
            html.AppendLine("    .on('start', (e, d) => { if (!e.active) simulation.alphaTarget(0.3).restart(); d.fx = d.x; d.fy = d.y; })");
            html.AppendLine("    .on('drag', (e, d) => { d.fx = e.x; d.fy = e.y; })");
            html.AppendLine("    .on('end', (e, d) => { if (!e.active) simulation.alphaTarget(0); d.fx = null; d.fy = null; }))");
            html.AppendLine("  .on('click', function(event, d) {");
            html.AppendLine("    event.stopPropagation();");
            html.AppendLine("    clearHighlights();");
            html.AppendLine("if (d.group === 0) {");
            html.AppendLine("      d3.select('#node-info').html('<div style=\"color:#ff4444;font-size:1.1em;font-weight:bold\">You</div><div style=\"color:#eee\"><strong>' + d.value.toLocaleString() + '</strong> total messages</div>');");
            html.AppendLine("      return;");
            html.AppendLine("    }");
            html.AppendLine("  d3.select(this).classed('highlighted', true);");
            html.AppendLine("    const connectedLinks = graphData.links.filter(l => (l.source.id || l.source) === d.id || (l.target.id || l.target) === d.id);");
            html.AppendLine("    link.classed('highlighted', l => (l.source.id || l.source) === d.id || (l.target.id || l.target) === d.id);");
            html.AppendLine("    const connectedIds = new Set();");
            html.AppendLine("    connectedLinks.forEach(l => { connectedIds.add(l.source.id || l.source); connectedIds.add(l.target.id || l.target); });");
            html.AppendLine("    node.classed('highlighted', n => connectedIds.has(n.id));");
            html.AppendLine("    if (d.group === 2) {");
            html.AppendLine("const contacts = connectedLinks.map(l => {");
            html.AppendLine("        const otherId = (l.source.id || l.source) === d.id ? (l.target.id || l.target) : (l.source.id || l.source);");
            html.AppendLine("     return graphData.nodes.find(n => n.id === otherId && n.group <= 1);");
            html.AppendLine("    }).filter(n => n);");
            html.AppendLine("   const names = contacts.slice(0, 5).map(c => c.name).join(', ');");
            html.AppendLine("      const more = contacts.length > 5 ? ' (+' + (contacts.length - 5) + ' more)' : '';");
            html.AppendLine("      d3.select('#node-info').html('<div style=\"color:#9b59b6;font-size:1.1em;font-weight:bold\">' + d.name + '</div><div style=\"color:#eee\"><strong>' + contacts.length + '</strong> people discuss this</div><div style=\"color:#eee\"><strong>' + d.value.toLocaleString() + '</strong> messages</div><div style=\"color:#aaa;font-size:0.85em\">Including: ' + names + more + '</div>');");
            html.AppendLine("    } else {");
            html.AppendLine("      let topicsHtml = '';");
            html.AppendLine("      if (d.topics && d.topics.length > 0) {");
            html.AppendLine("        topicsHtml = '<div class=\"topics\"><strong>Top Topics:</strong> ' + d.topics.map(t => '<span class=\"topic-tag\">' + t + '</span>').join('') + '</div>';");
            html.AppendLine("      }");
            html.AppendLine("      d3.select('#node-info').html('<div style=\"color:#4ecca3;font-size:1.1em;font-weight:bold\">' + d.name + '</div><div style=\"color:#eee\"><strong>' + d.value.toLocaleString() + '</strong> messages</div>' + topicsHtml);");
            html.AppendLine("    }");
            html.AppendLine("  });");
            html.AppendLine("");
            html.AppendLine("const nodeCounts = g.append('g').selectAll('text').data(graphData.nodes).enter().append('text')");
            html.AppendLine("  .attr('class', 'node-count')");
            html.AppendLine("  .text(d => d.value > 999 ? Math.round(d.value / 1000) + 'k' : d.value)");
            html.AppendLine("  .style('opacity', d => d.group === 2 ? 0.8 : 0.9);");
            html.AppendLine("");
            html.AppendLine("const labels = g.append('g').selectAll('text').data(graphData.nodes).enter().append('text')");
            html.AppendLine("  .attr('class', d => 'label' + (d.group === 2 ? ' topic-label' : ''))");
            html.AppendLine("  .text(d => d.name)");
            html.AppendLine("  .attr('text-anchor', 'middle');");
            html.AppendLine("");
            html.AppendLine("function updateLabelVisibility(scale) {");
            html.AppendLine("  const topContacts = graphData.nodes.filter(n => n.group <= 1).sort((a, b) => b.value - a.value).slice(0, 10);");
            html.AppendLine("  labels.classed('hidden', function(d) {");
            html.AppendLine("    if (d.group === 2) return false;");
            html.AppendLine("    if (topContacts.includes(d) && scale >= 0.5) return false;");
            html.AppendLine("  if (scale >= 1.5) return false;");
            html.AppendLine(" if (d.value >= 500 && scale >= 0.8) return false;");
            html.AppendLine("    return true;");
            html.AppendLine("  });");
            html.AppendLine("}");
            html.AppendLine("updateLabelVisibility(1);");
            html.AppendLine("");
            html.AppendLine("simulation");
            html.AppendLine("  .nodes(graphData.nodes)");
            html.AppendLine("  .on('tick', () => {");
            html.AppendLine("    link.attr('x1', d => d.source.x)");
            html.AppendLine("        .attr('y1', d => d.source.y)");
            html.AppendLine("  .attr('x2', d => d.target.x)");
            html.AppendLine("        .attr('y2', d => d.target.y);");
            html.AppendLine("");
            html.AppendLine("    node.attr('cx', d => d.x).attr('cy', d => d.y);");
            html.AppendLine("    nodeCounts.attr('x', d => d.x).attr('y', d => d.y + 4);");
            html.AppendLine("    labels.attr('x', d => d.x).attr('y', d => d.y + (d.group === 2 ? 25 : Math.sqrt(d.value) + 20));");
            html.AppendLine("  });");
            html.AppendLine("");
            html.AppendLine("svg.on('click', clearHighlights);");
            html.AppendLine("");
            html.AppendLine("window.addEventListener('resize', () => {");
            html.AppendLine("  const newWidth = window.innerWidth;");
            html.AppendLine("  const newHeight = window.innerHeight;");
            html.AppendLine("  svg.attr('width', newWidth).attr('height', newHeight);");
            html.AppendLine("  simulation.force('center', d3.forceCenter(newWidth / 2, newHeight / 2));");
            html.AppendLine("});");
            html.AppendLine("");
            html.AppendLine("</script>");

            using (StreamWriter writer = new StreamWriter(outputPath.Replace(".json", ".html"), false, Encoding.UTF8))
            {
                await writer.WriteAsync(html.ToString());
            }
        }
    }

    public class GraphNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public int Group { get; set; }
        public List<string> TopTopics { get; set; } = new List<string>();
    }

    public class GraphLink
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}