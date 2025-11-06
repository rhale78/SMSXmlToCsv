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
/// Options for network graph generation
/// </summary>
public class NetworkGraphOptions
{
    /// <summary>
    /// Include topics from both sent and received messages
    /// </summary>
    public bool IncludeBothSides { get; set; } = false;
    
    /// <summary>
    /// Show links from user node to topics
    /// </summary>
    public bool IncludeUserLinks { get; set; } = true;
    
    /// <summary>
    /// Extract named entities (people, dates, events, promises)
    /// </summary>
    public bool ExtractNamedEntities { get; set; } = true;
    
    /// <summary>
    /// Use improved node spacing for better visualization
    /// </summary>
    public bool ImprovedSpacing { get; set; } = true;
    
    /// <summary>
    /// Skip contacts with "Unknown" as contact name
    /// </summary>
    public bool SkipUnknownContacts { get; set; } = true;
}

/// <summary>
/// Graph node for network visualization
/// </summary>
public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int Group { get; set; }  // 0 = user, 1 = contact, 2 = topic, 3 = person mentioned, 4 = date/event, 5 = promise
    public List<string> TopTopics { get; set; } = new List<string>();
    public string? EntityType { get; set; }  // For named entities: "person", "date", "event", "promise", "relationship"
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
    private readonly NetworkGraphOptions _options;

    public NetworkGraphGenerator(OllamaSentimentAnalyzer ollamaAnalyzer, NetworkGraphOptions? options = null)
    {
        _ollamaAnalyzer = ollamaAnalyzer ?? throw new ArgumentNullException(nameof(ollamaAnalyzer));
        _options = options ?? new NetworkGraphOptions();
    }

    /// <summary>
    /// Generate network graph for all contacts (from legacy code approach)
    /// </summary>
    public async Task GenerateGraphAsync(IEnumerable<Message> messages, string outputPath, string userName = "You")
    {
        Log.Information("Generating network graph with AI topic detection (legacy algorithm)");
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Starting generation - Output: {outputPath}, User: {userName}");

        if (!await _ollamaAnalyzer.IsAvailableAsync())
        {
            System.Diagnostics.Debug.WriteLine("[NET GRAPH] ERROR: Ollama not available");
            throw new InvalidOperationException("Ollama is not available. Install from: https://ollama.ai and run: ollama pull llama3.2");
        }

        List<Message> messageList = messages.ToList();
        Log.Information("Processing {MessageCount} messages for network graph", messageList.Count);
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Processing {messageList.Count} messages");

        if (messageList.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("[NET GRAPH] ERROR: No messages to process");
            throw new InvalidOperationException("No messages available to generate network graph");
        }

        // Group messages by contact (adapted from legacy)
        // NOTE: Initialize fresh dictionaries to prevent data corruption between calls
        Dictionary<string, GraphNode> nodes = new Dictionary<string, GraphNode>();
        Dictionary<string, List<string>> contactMessages = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> userMessages = new Dictionary<string, List<string>>();  // For two-sided processing
        
        // First pass: group by contact name to merge duplicates
        Dictionary<string, List<string>> contactPhonesByName = new Dictionary<string, List<string>>();

        // Create user node
        nodes["user"] = new GraphNode
        {
            Id = "user",
            Name = userName,
            MessageCount = messageList.Count,
            Group = 0
        };
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Created user node: {userName}");

        // Extract contacts and their messages (both sides if enabled)
        foreach (Message message in messageList)
        {
            string contactName = ExtractContactName(message, userName);
            string contactPhone = ExtractContactPhone(message, userName);

            if (string.IsNullOrEmpty(contactPhone) || contactPhone == "Unknown")
            {
                continue; // Skip invalid contacts
            }

            // Skip contacts with "Unknown" name if option is enabled
            if (_options.SkipUnknownContacts && contactName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Skipping unknown contact: {contactPhone}");
                continue;
            }

            // Track phone numbers for each contact name to merge duplicates
            if (!contactPhonesByName.ContainsKey(contactName))
            {
                contactPhonesByName[contactName] = new List<string>();
            }
            if (!contactPhonesByName[contactName].Contains(contactPhone))
            {
                contactPhonesByName[contactName].Add(contactPhone);
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
                if (_options.IncludeBothSides)
                {
                    userMessages[contactPhone] = new List<string>();
                }

                Log.Debug("Contact: {ContactName} ({ContactPhone})", contactName, contactPhone);
                System.Diagnostics.Debug.WriteLine($"[NET GRAPH] New contact: {contactName} ({contactPhone})");
            }

            nodes[contactPhone].MessageCount++;

            // Collect message text for topic extraction
            if (!string.IsNullOrWhiteSpace(message.Body))
            {
                if (message.Direction == MessageDirection.Received)
                {
                    // Messages from contact
                    contactMessages[contactPhone].Add(message.Body);
                }
                else if (_options.IncludeBothSides && message.Direction == MessageDirection.Sent)
                {
                    // Messages from user (only if both sides enabled)
                    userMessages[contactPhone].Add(message.Body);
                }
            }
        }

        // Merge contacts with duplicate names
        foreach (var nameGroup in contactPhonesByName.Where(kvp => kvp.Value.Count > 1))
        {
            string contactName = nameGroup.Key;
            List<string> phones = nameGroup.Value;
            string primaryPhone = phones[0];

            Log.Information("Merging {PhoneCount} phone numbers for contact: {ContactName}", phones.Count, contactName);
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Merging {phones.Count} phone numbers for contact: {contactName}");

            // Merge messages from all phones into the primary phone
            for (int i = 1; i < phones.Count; i++)
            {
                string otherPhone = phones[i];
                
                if (contactMessages.ContainsKey(otherPhone))
                {
                    contactMessages[primaryPhone].AddRange(contactMessages[otherPhone]);
                    contactMessages.Remove(otherPhone);
                }
                
                if (_options.IncludeBothSides && userMessages.ContainsKey(otherPhone))
                {
                    userMessages[primaryPhone].AddRange(userMessages[otherPhone]);
                    userMessages.Remove(otherPhone);
                }

                // Merge message counts
                if (nodes.ContainsKey(otherPhone))
                {
                    nodes[primaryPhone].MessageCount += nodes[otherPhone].MessageCount;
                    nodes.Remove(otherPhone);
                }
            }
        }

        Log.Information("Found {ContactCount} unique contacts", nodes.Count - 1);
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Found {nodes.Count - 1} unique contacts");
        if (_options.IncludeBothSides)
        {
            System.Diagnostics.Debug.WriteLine("[NET GRAPH] Two-sided processing enabled - analyzing sent and received messages");
        }

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
        List<(string ContactName, string TopicsPreview)> topicDetails = new List<(string, string)>();
        List<string> emptyResponseContacts = new List<string>();
        List<(string ContactName, string Error)> errorContacts = new List<(string, string)>();


        // Use Status for the overall operation - NO logging or console output inside this block
        await AnsiConsole.Status()
            .StartAsync("AI analyzing topics...", async ctx =>
            {
                // Process contact messages
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
                    ctx.Status($"AI analyzing {processed}/{totalContacts}: {nodes[contactPhone].Name} (contact messages)");

                    contactTopicMessageCounts[contactPhone] = new Dictionary<string, int>();

                    try
                    {
                        // Extract topics using AI with batching support
                        Action<string> statusCallback = (status) => ctx.Status(status);
                        List<string> contactTopics = await ExtractTopicsAsync(msgTexts, nodes[contactPhone].Name, statusCallback, _options.ExtractNamedEntities);

                        if (contactTopics.Count > 0)
                        {
                            // Store for logging later (outside Status context)
                            topicResults.Add((nodes[contactPhone].Name, contactTopics.Count));
                            
                            // Store topics summary for detailed logging
                            string topicsPreview = string.Join(", ", contactTopics.Take(5));
                            if (contactTopics.Count > 5)
                            {
                                topicsPreview += $" (and {contactTopics.Count - 5} more)";
                            }
                            topicDetails.Add((nodes[contactPhone].Name, topicsPreview));

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
                }

                // Process user messages if two-sided is enabled
                if (_options.IncludeBothSides)
                {
                    Dictionary<string, int> userTopicMessageCounts = new Dictionary<string, int>();
                    
                    foreach (KeyValuePair<string, List<string>> userKvp in userMessages)
                    {
                        string contactPhone = userKvp.Key;
                        List<string> msgTexts = userKvp.Value;

                        if (msgTexts.Count < MIN_TOPIC_MESSAGES)
                        {
                            continue;
                        }

                        ctx.Status($"AI analyzing user messages with {nodes[contactPhone].Name}");

                        try
                        {
                            Action<string> statusCallback = (status) => ctx.Status(status);
                            List<string> userTopics = await ExtractTopicsAsync(msgTexts, userName, statusCallback, _options.ExtractNamedEntities);

                            if (userTopics.Count > 0)
                            {
                                foreach (string topic in userTopics)
                                {
                                    int topicMessageCount = msgTexts.Count(m => m.IndexOf(topic, StringComparison.OrdinalIgnoreCase) >= 0);

                                    if (topicMessageCount == 0)
                                    {
                                        topicMessageCount = Math.Max(1, msgTexts.Count / Math.Max(userTopics.Count, 1));
                                    }

                                    if (!topicToContacts.ContainsKey(topic))
                                    {
                                        topicToContacts[topic] = new HashSet<string>();
                                        globalTopicFrequency[topic] = 0;
                                    }

                                    topicToContacts[topic].Add("user");
                                    if (!userTopicMessageCounts.ContainsKey(topic))
                                    {
                                        userTopicMessageCounts[topic] = 0;
                                    }
                                    userTopicMessageCounts[topic] += topicMessageCount;
                                    globalTopicFrequency[topic] += topicMessageCount;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug("Error extracting user topics for {Contact}: {Error}", nodes[contactPhone].Name, ex.Message);
                        }
                    }

                    // Store user topic counts for later use
                    if (userTopicMessageCounts.Count > 0)
                    {
                        contactTopicMessageCounts["user"] = userTopicMessageCounts;
                    }
                }
            });

        // Now safe to log results outside Status context
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Topic extraction completed - Success: {topicResults.Count}, Empty: {emptyResponseContacts.Count}, Errors: {errorContacts.Count}");
        
        foreach ((string contactName, int topicCount) in topicResults)
        {
            Log.Information("Contact {ContactName}: Found {TopicCount} topics", contactName, topicCount);
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Contact '{contactName}': Found {topicCount} topics");
        }
        
        // Log detailed topics for debugging
        foreach ((string contactName, string topicsPreview) in topicDetails)
        {
            Log.Debug("Topics for {ContactName}: {Topics}", contactName, topicsPreview);
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Topics for '{contactName}': {topicsPreview}");
        }

        // Log warnings for empty responses
        foreach (string contactName in emptyResponseContacts)
        {
            Log.Warning("Empty AI response for contact: {ContactName}", contactName);
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] WARNING: Empty AI response for '{contactName}'");
        }

        // Log errors
        foreach ((string contactName, string error) in errorContacts)
        {
            Log.Error("Failed to extract topics for contact {ContactName}: {Error}", contactName, error);
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] ERROR: Failed for '{contactName}': {error}");
        }

        // Final status summary
        int totalProcessed = processed;
        int totalSkipped = totalContacts - processed;
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Summary - Processed: {processed}, Skipped: {totalSkipped}, Errors: {errorContacts.Count}");
        
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

            // Parse entity type and name from prefixed topics
            (int groupType, string displayName) = ParseEntityType(topic);

            string topicNodeIdStr = $"topic_{topicNodeId++}";
            nodes[topicNodeIdStr] = new GraphNode
            {
                Id = topicNodeIdStr,
                Name = displayName,
                MessageCount = globalTopicFrequency[topic],
                Group = groupType
            };

            // Create links from contacts/user to topics
            foreach (string contactPhone in contactsDiscussingTopic)
            {
                // Skip user-to-topic links if option is disabled
                if (contactPhone == "user" && !_options.IncludeUserLinks)
                {
                    continue;
                }

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
        
        // Log final graph statistics
        int contactNodes = nodes.Values.Count(n => n.Group == 1);
        int topicNodes = nodes.Values.Count(n => n.Group == 2);
        Log.Information("Network graph stats - Total Nodes: {TotalNodes} (User: 1, Contacts: {Contacts}, Topics: {Topics}), Links: {Links}", 
            nodes.Count, contactNodes, topicNodes, links.Count);
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Final graph - Nodes: {nodes.Count} (User: 1, Contacts: {contactNodes}, Topics: {topicNodes}), Links: {links.Count}");

        // Generate HTML
        await GenerateHtmlAsync(nodes.Values.ToList(), links, outputPath);

        Log.Information("Network graph generated: {OutputPath}", outputPath);
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] HTML generated successfully: {outputPath}");
        AnsiConsole.MarkupLine($"[green]✓[/] Network graph saved to: {outputPath}");
    }

    /// <summary>
    /// Generate separate network graphs for each contact
    /// </summary>
    public async Task GeneratePerContactGraphsAsync(IEnumerable<Message> messages, string outputDirectory, string userName = "You")
    {
        Log.Information("Generating per-contact network graphs");

        List<Message> messageList = messages.ToList();
        
        if (messageList.Count == 0)
        {
            throw new InvalidOperationException("No messages available to generate network graphs");
        }

        // Create output directory
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Group messages by contact
        Dictionary<string, List<Message>> messagesByContact = new Dictionary<string, List<Message>>();
        
        foreach (Message message in messageList)
        {
            string contactName = ExtractContactName(message, userName);
            string contactPhone = ExtractContactPhone(message, userName);

            if (string.IsNullOrEmpty(contactPhone) || contactPhone == "Unknown")
            {
                continue;
            }

            // Skip contacts with "Unknown" name if option is enabled
            if (_options.SkipUnknownContacts && contactName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string contactKey = $"{contactName}_{contactPhone}";
            
            if (!messagesByContact.ContainsKey(contactKey))
            {
                messagesByContact[contactKey] = new List<Message>();
            }

            messagesByContact[contactKey].Add(message);
        }

        Log.Information("Found {ContactCount} contacts to process", messagesByContact.Count);

        int processedCount = 0;
        int skippedCount = 0;
        
        foreach (KeyValuePair<string, List<Message>> kvp in messagesByContact)
        {
            string contactKey = kvp.Key;
            List<Message> contactMessages = kvp.Value;
            
            // Skip contacts with too few messages
            if (contactMessages.Count < MIN_TOPIC_MESSAGES)
            {
                skippedCount++;
                continue;
            }

            // Extract contact name safely - use last underscore to split since phone is always at the end
            int lastUnderscoreIndex = contactKey.LastIndexOf('_');
            string contactName = lastUnderscoreIndex > 0 
                ? contactKey.Substring(0, lastUnderscoreIndex) 
                : contactKey;
            string safeFileName = string.Join("_", contactName.Split(Path.GetInvalidFileNameChars()));
            string outputPath = Path.Combine(outputDirectory, $"network-{safeFileName}.html");

            try
            {
                AnsiConsole.MarkupLine($"[cyan]Generating graph for {contactName}... ({contactMessages.Count} messages)[/]");
                
                // Generate graph for this contact only
                await GenerateGraphAsync(contactMessages, outputPath, userName);
                
                processedCount++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate graph for contact {ContactName}", contactName);
                AnsiConsole.MarkupLine($"[red]✗ Failed to generate graph for {contactName}: {ex.Message}[/]");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]✓ Generated {processedCount} network graphs[/]");
        if (skippedCount > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]! Skipped {skippedCount} contacts with insufficient messages (< {MIN_TOPIC_MESSAGES})[/]");
        }
        Log.Information("Per-contact network graphs completed: {ProcessedCount} generated, {SkippedCount} skipped", 
            processedCount, skippedCount);
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
    /// Extract topics using AI with batching for large message sets
    /// </summary>
    private async Task<List<string>> ExtractTopicsAsync(List<string> messageTexts, string contactName, Action<string>? statusUpdate = null, bool extractEntities = false)
    {
        if (messageTexts.Count < MIN_TOPIC_MESSAGES)
        {
            return new List<string>();
        }

        const int BATCH_SIZE = 75;  // Optimal batch size for Ollama

        // For smaller message sets, process in one batch
        if (messageTexts.Count <= 100)
        {
            return await ExtractTopicsBatchAsync(messageTexts, contactName, 1, 1, extractEntities);
        }

        // For larger sets, process in batches
        List<string> allTopics = new List<string>();
        Dictionary<string, int> topicFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        int totalBatches = (int)Math.Ceiling(messageTexts.Count / (double)BATCH_SIZE);
        
        Log.Information("Processing {MessageCount} messages for {ContactName} in {BatchCount} batches", 
            messageTexts.Count, contactName, totalBatches);
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] {contactName}: Processing {messageTexts.Count} messages in {totalBatches} batches");

        // Process batches concurrently with limited parallelism
        var batches = new List<Task<List<string>>>();
        for (int i = 0; i < totalBatches; i++)
        {
            int batchNumber = i + 1;
            int skip = i * BATCH_SIZE;
            int take = Math.Min(BATCH_SIZE, messageTexts.Count - skip);
            List<string> batchMessages = messageTexts.Skip(skip).Take(take).ToList();

            statusUpdate?.Invoke($"AI analyzing {contactName} (batch {batchNumber}/{totalBatches})");

            // Process batches with some parallelism (up to 3 concurrent)
            if (batches.Count >= 3)
            {
                await Task.WhenAny(batches);
                batches.RemoveAll(t => t.IsCompleted);
            }

            batches.Add(ExtractTopicsBatchAsync(batchMessages, contactName, batchNumber, totalBatches, extractEntities));
        }

        // Wait for all remaining batches to complete
        var batchResults = await Task.WhenAll(batches);

        // Aggregate topics from all batches
        foreach (var batchTopics in batchResults)
        {
            foreach (var topic in batchTopics)
            {
                if (topicFrequency.ContainsKey(topic))
                {
                    topicFrequency[topic]++;
                }
                else
                {
                    topicFrequency[topic] = 1;
                    allTopics.Add(topic);
                }
            }
        }

        // Sort by frequency and take top 250
        var sortedTopics = allTopics
            .OrderByDescending(t => topicFrequency[t])
            .Take(250)
            .ToList();

        Log.Information("{ContactName}: Aggregated {TopicCount} unique topics from {BatchCount} batches", 
            contactName, sortedTopics.Count, totalBatches);
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] {contactName}: Final {sortedTopics.Count} topics after aggregation");

        return sortedTopics;
    }

    /// <summary>
    /// Extract topics from a single batch of messages
    /// </summary>
    private async Task<List<string>> ExtractTopicsBatchAsync(List<string> batchMessages, string contactName, int batchNumber, int totalBatches, bool extractEntities = false)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Messages from conversation with {contactName} (batch {batchNumber}/{totalBatches}):");
        
        foreach (string msg in batchMessages)
        {
            sb.AppendLine($"- {msg}");
        }

        string prompt;
        if (extractEntities)
        {
            prompt = $@"{sb}

Analyze these messages and identify what was actually discussed in THIS conversation.

Extract ONLY items that appear in the messages above. Use these prefixes:
- For topics (general subjects): NO PREFIX - just the topic word
- For specific people mentioned BY NAME: person:ActualName
- For dates/events WITH CONTEXT: date:EventDescription
- For promises/commitments made: promise:ActionPromised  
- For relationships described: relationship:TypeWithContext

FORMAT RULES:
1. Topics: Use 1-3 word phrases describing subjects discussed
2. People: Use actual names from messages (person:NameFromMessage)
3. Dates: Include event context (date:WhatEvent WhenDate)
4. Promises: Specify what was promised (promise:SpecificAction)
5. Relationships: Include who or where (relationship:TypeAndContext)

ABSOLUTELY DO NOT INCLUDE:
- Category labels (PEOPLE, DATES, PROMISES, RELATIONSHIPS, TOPICS)
- Section numbers (1., 2., 3.)
- Generic placeholder examples (Dr. Smith, John Smith, Christmas dinner, etc.)
- The word ""Examples"" or explanatory text
- Anything not actually mentioned in the conversation above

OUTPUT FORMAT:
Return a comma-separated list with proper prefixes. Extract ONLY from the actual messages provided.
ONLY return items that were genuinely discussed in this specific conversation.";
        }
        else
        {
            prompt = $@"{sb}

Analyze the messages above and identify the main topics discussed in THIS specific conversation.

Extract ONLY topics that are actually mentioned or discussed in these messages.
Use 1-3 word phrases describing the subjects.

DO NOT include:
- Generic placeholder examples
- Category labels or section headers
- The word ""Examples""
- Any explanatory text

Return ONLY a comma-separated list of topics that were genuinely discussed in this conversation.";
        }

        try
        {
            string response = await _ollamaAnalyzer.CallOllamaAsync(prompt);

            if (string.IsNullOrWhiteSpace(response))
            {
                Log.Debug("{ContactName} batch {BatchNum}/{TotalBatches}: Empty response", 
                    contactName, batchNumber, totalBatches);
                return new List<string>();
            }

            // Parse response and filter out category labels
            List<string> topics = response
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().Trim('.', '-', '*', '•', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', ')', '(', '[', ']', '"', '\''))
                .Select(t => StripCategoryPrefix(t))  // Remove category prefixes like "dates & events: "
                .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 2 && t.Length < 100)  // Increased length for entities with context
                .Where(t => !IsCategoryLabel(t))  // Filter out category labels
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Log.Debug("{ContactName} batch {BatchNum}/{TotalBatches}: Found {TopicCount} topics: {Topics}", 
                contactName, batchNumber, totalBatches, topics.Count, string.Join(", ", topics.Take(10)));
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] {contactName} batch {batchNumber}/{totalBatches}: {topics.Count} topics - {string.Join(", ", topics.Take(5))}...");

            return topics;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ContactName} batch {BatchNum}/{TotalBatches}: Error extracting topics", 
                contactName, batchNumber, totalBatches);
            System.Diagnostics.Debug.WriteLine($"[NET GRAPH] {contactName} batch {batchNumber}/{totalBatches}: ERROR - {ex.Message}");
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
        // Use camelCase for JavaScript compatibility
        JsonSerializerOptions options = new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string nodesJson = JsonSerializer.Serialize(nodes, options);
        string linksJson = JsonSerializer.Serialize(links, options);

        // Log generated data for debugging
        Log.Debug("Generated {NodeCount} nodes and {LinkCount} links for D3 visualization", nodes.Count, links.Count);
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] D3 Data - Nodes: {nodes.Count}, Links: {links.Count}");
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Nodes JSON sample: {(nodesJson.Length > 200 ? nodesJson.Substring(0, 200) + "..." : nodesJson)}");
        System.Diagnostics.Debug.WriteLine($"[NET GRAPH] Links JSON sample: {(linksJson.Length > 200 ? linksJson.Substring(0, 200) + "..." : linksJson)}");

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
            transition: opacity 0.3s;
        }}
        .node.highlighted {{
            stroke: #ffff00;
            stroke-width: 3px;
        }}
        .node.dimmed {{
            opacity: 0.2;
        }}
        .link {{
            stroke: #999;
            stroke-opacity: 0.6;
            transition: opacity 0.3s, stroke-width 0.3s;
        }}
        .link.highlighted {{
            stroke: #ffff00;
            stroke-opacity: 1;
            stroke-width: 3px;
        }}
        .link.dimmed {{
            opacity: 0.1;
        }}
        .node-label {{
            font-size: 12px;
            pointer-events: none;
            fill: #ffffff;
            text-shadow: 0 1px 2px rgba(0,0,0,0.8);
        }}
        .node-count {{
            font-size: 10px;
            font-weight: bold;
            pointer-events: none;
            fill: #ffffff;
            text-anchor: middle;
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
        #controls {{
            position: absolute;
            bottom: 10px;
            left: 10px;
            background-color: rgba(0,0,0,0.8);
            padding: 10px;
            border-radius: 5px;
        }}
        .control-btn {{
            background-color: #4CAF50;
            border: none;
            color: white;
            padding: 8px 16px;
            text-align: center;
            text-decoration: none;
            display: inline-block;
            font-size: 12px;
            margin: 2px;
            cursor: pointer;
            border-radius: 4px;
        }}
        .control-btn:hover {{
            background-color: #45a049;
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
        <p>Click nodes to highlight connections</p>
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
            <span>Topics (with counts)</span>
        </div>
        <div class=""legend-item"">
            <div class=""legend-color"" style=""background-color: #F44336;""></div>
            <span>People Mentioned</span>
        </div>
        <div class=""legend-item"">
            <div class=""legend-color"" style=""background-color: #9C27B0;""></div>
            <span>Dates/Events</span>
        </div>
        <div class=""legend-item"">
            <div class=""legend-color"" style=""background-color: #00BCD4;""></div>
            <span>Promises</span>
        </div>
        <div style=""margin-top: 10px; font-size: 11px; color: #aaa;"">
            Click any node to highlight its connections
        </div>
    </div>
    <div id=""controls"">
        <button class=""control-btn"" onclick=""resetView()"">Reset View</button>
        <button class=""control-btn"" onclick=""zoomIn()"">Zoom In</button>
        <button class=""control-btn"" onclick=""zoomOut()"">Zoom Out</button>
        <button class=""control-btn"" onclick=""clearHighlight()"">Clear Highlight</button>
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

        // Create a group for zooming
        const g = svg.append('g');

        // Add zoom behavior
        const zoom = d3.zoom()
            .scaleExtent([0.1, 10])
            .on('zoom', (event) => {{
                g.attr('transform', event.transform);
            }});
        
        svg.call(zoom);

        // Zoom control functions
        window.resetView = function() {{
            svg.transition().duration(750).call(zoom.transform, d3.zoomIdentity);
            clearHighlight();
        }};
        
        window.zoomIn = function() {{
            svg.transition().duration(300).call(zoom.scaleBy, 1.3);
        }};
        
        window.zoomOut = function() {{
            svg.transition().duration(300).call(zoom.scaleBy, 0.7);
        }};

        window.clearHighlight = function() {{
            d3.selectAll('.node').classed('highlighted', false).classed('dimmed', false);
            d3.selectAll('.link').classed('highlighted', false).classed('dimmed', false);
        }};

        const simulation = d3.forceSimulation(data.nodes)
            .force('link', d3.forceLink(data.links).id(d => d.id).distance(d => Math.max(100, 300 - d.value)))
            .force('charge', d3.forceManyBody().strength(-800))
            .force('center', d3.forceCenter(width / 2, height / 2))
            .force('collision', d3.forceCollide().radius(d => Math.sqrt(d.messageCount) * 3 + 30));

        const link = g.append('g')
            .selectAll('line')
            .data(data.links)
            .join('line')
            .attr('class', 'link')
            .attr('stroke-width', d => Math.sqrt(d.value));

        const node = g.append('g')
            .selectAll('circle')
            .data(data.nodes)
            .join('circle')
            .attr('class', 'node')
            .attr('r', d => Math.max(5, Math.sqrt(d.messageCount) * 2))
            .attr('fill', d => {{
                if (d.group === 0) return '#4CAF50';  // User - green
                if (d.group === 1) return '#2196F3';  // Contact - blue
                if (d.group === 2) return '#FF9800';  // Topic - orange
                if (d.group === 3) return '#F44336';  // Person mentioned - red
                if (d.group === 4) return '#9C27B0';  // Date/event - purple
                if (d.group === 5) return '#00BCD4';  // Promise - cyan
                return '#999999';  // Unknown - gray
            }})
            .call(d3.drag()
                .on('start', dragstarted)
                .on('drag', dragged)
                .on('end', dragended))
            .on('click', (event, clickedNode) => {{
                event.stopPropagation();
                
                // Clear previous highlights
                clearHighlight();
                
                // Find all connected nodes and links
                const connectedNodeIds = new Set([clickedNode.id]);
                const connectedLinks = data.links.filter(l => {{
                    const sourceId = l.source.id || l.source;
                    const targetId = l.target.id || l.target;
                    if (sourceId === clickedNode.id) {{
                        connectedNodeIds.add(targetId);
                        return true;
                    }}
                    if (targetId === clickedNode.id) {{
                        connectedNodeIds.add(sourceId);
                        return true;
                    }}
                    return false;
                }});
                
                // Highlight connected nodes and dim others
                node.classed('highlighted', n => connectedNodeIds.has(n.id))
                    .classed('dimmed', n => !connectedNodeIds.has(n.id));
                
                // Highlight connected links and dim others
                link.classed('highlighted', l => {{
                    const sourceId = l.source.id || l.source;
                    const targetId = l.target.id || l.target;
                    return sourceId === clickedNode.id || targetId === clickedNode.id;
                }})
                .classed('dimmed', l => {{
                    const sourceId = l.source.id || l.source;
                    const targetId = l.target.id || l.target;
                    return sourceId !== clickedNode.id && targetId !== clickedNode.id;
                }});
                
                // No popup - just highlight the connections
            }});

        // Add message count labels on all nodes except contact and user nodes
        const nodeCount = g.append('g')
            .selectAll('text')
            .data(data.nodes.filter(d => d.group !== 0 && d.group !== 1))  // Show counts on topics, people, dates, promises
            .join('text')
            .attr('class', 'node-count')
            .text(d => d.messageCount)
            .attr('dy', 4);

        const label = g.append('g')
            .selectAll('text')
            .data(data.nodes)
            .join('text')
            .attr('class', 'node-label')
            .text(d => d.name)
            .attr('text-anchor', 'middle')
            .attr('dy', d => Math.max(5, Math.sqrt(d.messageCount) * 2) + 15);

        simulation.on('tick', () => {{
            link
                .attr('x1', d => d.source.x)
                .attr('y1', d => d.source.y)
                .attr('x2', d => d.target.x)
                .attr('y2', d => d.target.y);

            node
                .attr('cx', d => d.x)
                .attr('cy', d => d.y);

            nodeCount
                .attr('x', d => d.x)
                .attr('y', d => d.y);

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

    /// <summary>
    /// Check if a string is a category label that should be filtered out
    /// </summary>
    /// <summary>
    /// Strip category prefixes that LLMs sometimes add (e.g., "dates & events: date: Christmas")
    /// </summary>
    private string StripCategoryPrefix(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Common patterns: "category: prefix: value" or "Category: prefix: value"
        // We want to strip "category: " part but keep "prefix: value"
        
        // Check for patterns like "dates & events: date:" or "people: person:"
        string[] categoryPrefixes = new[]
        {
            "dates & events:",
            "dates and events:",
            "people:",
            "persons:",
            "promises:",
            "relationships:",
            "main topics:",
            "topics:",
            "regular topics:",
            "regular topic:",
            "events:"
        };

        string lowerText = text.ToLowerInvariant();
        foreach (string prefix in categoryPrefixes)
        {
            if (lowerText.StartsWith(prefix))
            {
                // Strip the category prefix, trim, and return the rest
                text = text.Substring(prefix.Length).Trim();
                break;
            }
        }

        return text;
    }

    private bool IsCategoryLabel(string text)
    {
        string upper = text.ToUpperInvariant();
        
        // Filter out common category labels that LLMs might return
        return upper == "PEOPLE" || 
               upper == "PERSON" ||
               upper == "DATES" || 
               upper == "DATES & EVENTS" ||
               upper == "DATES AND EVENTS" ||
               upper == "EVENTS" ||
               upper == "PROMISES" || 
               upper == "PROMISE" ||
               upper == "RELATIONSHIPS" || 
               upper == "RELATIONSHIP" ||
               upper == "MAIN TOPICS" ||
               upper == "MAIN TOPIC" ||
               upper == "TOPICS" ||
               upper == "TOPIC" ||
               upper == "REGULAR TOPICS" ||
               upper == "REGULAR TOPIC" ||
               upper == "EXAMPLES" ||
               upper == "EXAMPLE" ||
               upper.StartsWith("BASED ON") ||
               upper.StartsWith("HERE ARE") ||
               upper.StartsWith("THE FOLLOWING");
    }

    /// <summary>
    /// Parse entity type from prefixed topic and return group type and display name
    /// </summary>
    private (int groupType, string displayName) ParseEntityType(string topic)
    {
        if (topic.StartsWith("person:", StringComparison.OrdinalIgnoreCase))
        {
            return (3, topic.Substring(7).Trim());  // Group 3 = Person
        }
        else if (topic.StartsWith("date:", StringComparison.OrdinalIgnoreCase))
        {
            return (4, topic.Substring(5).Trim());  // Group 4 = Date/Event
        }
        else if (topic.StartsWith("promise:", StringComparison.OrdinalIgnoreCase))
        {
            return (5, topic.Substring(8).Trim());  // Group 5 = Promise
        }
        else if (topic.StartsWith("relationship:", StringComparison.OrdinalIgnoreCase))
        {
            return (3, topic.Substring(13).Trim());  // Group 3 = Person/Relationship
        }
        else if (topic.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
        {
            return (4, topic.Substring(6).Trim());  // Group 4 = Date/Event
        }
        else
        {
            return (2, topic);  // Group 2 = Regular topic
        }
    }
}
