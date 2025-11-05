using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SMSXmlToCsv.Models;
using Serilog;

namespace SMSXmlToCsv.Services.ML;

/// <summary>
/// Topic extracted from messages
/// </summary>
public class Topic
{
    public string Name { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public double Score { get; set; }
    public List<string> Keywords { get; set; } = new List<string>();
}

/// <summary>
/// Topic analyzer using keyword extraction and frequency analysis
/// Note: For ML-based topic modeling, consider integrating BERTopic via Python interop
/// </summary>
public class TopicAnalyzer
{
    private readonly int _minWordLength;
    private readonly int _maxTopicsPerContact;
    private readonly HashSet<string> _stopWords;

    public TopicAnalyzer(int minWordLength = 3, int maxTopicsPerContact = 250)
    {
        _minWordLength = minWordLength;
        _maxTopicsPerContact = maxTopicsPerContact;
        _stopWords = GetCommonStopWords();
    }

    /// <summary>
    /// Extract topics from messages
    /// </summary>
    public List<Topic> ExtractTopics(IEnumerable<Message> messages, int minMessageCount = 2)
    {
        Log.Information("Extracting topics from messages");

        // Extract keywords from each message
        Dictionary<string, List<string>> messageKeywords = new Dictionary<string, List<string>>();
        Dictionary<string, int> keywordFrequency = new Dictionary<string, int>();

        foreach (Message message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Body))
            {
                continue;
            }

            List<string> keywords = ExtractKeywords(message.Body);
            string messageId = $"{message.From.Name}_{message.TimestampUtc}";
            messageKeywords[messageId] = keywords;

            foreach (string keyword in keywords)
            {
                if (!keywordFrequency.ContainsKey(keyword))
                {
                    keywordFrequency[keyword] = 0;
                }
                keywordFrequency[keyword]++;
            }
        }

        // Create topics based on keyword co-occurrence and frequency
        List<Topic> topics = new List<Topic>();

        // Get most frequent keywords
        List<KeyValuePair<string, int>> sortedKeywords = keywordFrequency
            .Where(kvp => kvp.Value >= minMessageCount)
            .OrderByDescending(kvp => kvp.Value)
            .Take(_maxTopicsPerContact)
            .ToList();

        foreach (KeyValuePair<string, int> kvp in sortedKeywords)
        {
            Topic topic = new Topic
            {
                Name = kvp.Key,
                MessageCount = kvp.Value,
                Score = (double)kvp.Value / messageKeywords.Count,
                Keywords = new List<string> { kvp.Key }
            };

            // Find related keywords (co-occurring keywords)
            Dictionary<string, int> coOccurrence = new Dictionary<string, int>();

            foreach (KeyValuePair<string, List<string>> msgKw in messageKeywords)
            {
                if (msgKw.Value.Contains(kvp.Key))
                {
                    foreach (string otherKeyword in msgKw.Value)
                    {
                        if (otherKeyword != kvp.Key)
                        {
                            if (!coOccurrence.ContainsKey(otherKeyword))
                            {
                                coOccurrence[otherKeyword] = 0;
                            }
                            coOccurrence[otherKeyword]++;
                        }
                    }
                }
            }

            // Add top co-occurring keywords
            List<string> relatedKeywords = coOccurrence
                .OrderByDescending(c => c.Value)
                .Take(5)
                .Select(c => c.Key)
                .ToList();

            topic.Keywords.AddRange(relatedKeywords);

            topics.Add(topic);
        }

        Log.Information("Extracted {TopicCount} topics", topics.Count);

        return topics;
    }

    /// <summary>
    /// Extract topics per contact
    /// </summary>
    public Dictionary<string, List<Topic>> ExtractTopicsPerContact(IEnumerable<Message> messages, int minMessageCount = 2)
    {
        Log.Information("Extracting topics per contact");

        Dictionary<string, List<Topic>> contactTopics = new Dictionary<string, List<Topic>>();

        // Group messages by contact
        IEnumerable<IGrouping<string, Message>> contactGroups = messages
            .GroupBy(m => GetContactName(m));

        foreach (IGrouping<string, Message> group in contactGroups)
        {
            List<Topic> topics = ExtractTopics(group, minMessageCount);
            contactTopics[group.Key] = topics;
        }

        Log.Information("Extracted topics for {ContactCount} contacts", contactTopics.Count);

        return contactTopics;
    }

    private List<string> ExtractKeywords(string text)
    {
        // Convert to lowercase and remove punctuation
        string cleaned = Regex.Replace(text.ToLowerInvariant(), @"[^\w\s]", " ");

        // Split into words
        List<string> words = cleaned.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= _minWordLength && !_stopWords.Contains(w))
            .ToList();

        return words;
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

    private HashSet<string> GetCommonStopWords()
    {
        // Common English stop words
        return new HashSet<string>
        {
            "the", "be", "to", "of", "and", "a", "in", "that", "have", "i",
            "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
            "this", "but", "his", "by", "from", "they", "we", "say", "her", "she",
            "or", "an", "will", "my", "one", "all", "would", "there", "their",
            "what", "so", "up", "out", "if", "about", "who", "get", "which", "go",
            "me", "when", "make", "can", "like", "time", "no", "just", "him", "know",
            "take", "people", "into", "year", "your", "good", "some", "could", "them",
            "see", "other", "than", "then", "now", "look", "only", "come", "its", "over",
            "think", "also", "back", "after", "use", "two", "how", "our", "work", "first",
            "well", "way", "even", "new", "want", "because", "any", "these", "give", "day",
            "most", "us", "is", "was", "are", "been", "has", "had", "were", "said", "did",
            "im", "ive", "dont", "cant", "wont", "youre", "thats", "hes", "shes", "its",
            "yeah", "ok", "okay", "yes", "yep", "nope", "gonna", "wanna", "gotta"
        };
    }
}
