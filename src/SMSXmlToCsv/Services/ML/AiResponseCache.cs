using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace SMSXmlToCsv.Services.ML;

/// <summary>
/// Cached AI response entry
/// </summary>
public class CachedAiResponse
{
    public string MessageHash { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new List<string>();
    public DateTime CachedAt { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public int BatchNumber { get; set; }
}

/// <summary>
/// File-based cache for AI responses to avoid re-processing the same messages
/// </summary>
public class AiResponseCache
{
    private readonly string _cacheDirectory;
    private readonly string _cacheFilePath;
    private Dictionary<string, CachedAiResponse> _cache;

    public AiResponseCache()
    {
        // Create cache directory in data folder off the application exe's path
        string appPath = AppContext.BaseDirectory;
        _cacheDirectory = Path.Combine(appPath, "data");
        _cacheFilePath = Path.Combine(_cacheDirectory, "ai_response_cache.json");
        _cache = new Dictionary<string, CachedAiResponse>();

        // Ensure directory exists
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
            Log.Information("Created AI cache directory: {CacheDirectory}", _cacheDirectory);
        }
    }

    /// <summary>
    /// Load cache from disk
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                string json = await File.ReadAllTextAsync(_cacheFilePath);
                List<CachedAiResponse>? cacheList = JsonSerializer.Deserialize<List<CachedAiResponse>>(json);
                
                if (cacheList != null)
                {
                    _cache = cacheList.ToDictionary(c => c.MessageHash, c => c);
                    Log.Information("Loaded {Count} cached AI responses from {CacheFile}", _cache.Count, _cacheFilePath);
                }
            }
            else
            {
                Log.Information("No existing AI cache found, starting fresh");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load AI cache, starting fresh");
            _cache = new Dictionary<string, CachedAiResponse>();
        }
    }

    /// <summary>
    /// Save cache to disk
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            List<CachedAiResponse> cacheList = _cache.Values.ToList();
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(cacheList, options);
            
            await File.WriteAllTextAsync(_cacheFilePath, json);
            Log.Information("Saved {Count} AI responses to cache file: {CacheFile}", _cache.Count, _cacheFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save AI cache");
        }
    }

    /// <summary>
    /// Compute hash for a batch of messages
    /// </summary>
    public string ComputeHash(List<string> messages, string contactName, int batchNumber)
    {
        // Create a stable hash from messages content + contact + batch
        StringBuilder sb = new StringBuilder();
        sb.Append(contactName);
        sb.Append("|");
        sb.Append(batchNumber);
        sb.Append("|");
        
        foreach (string message in messages.OrderBy(m => m))  // Sort for consistency
        {
            sb.Append(message);
            sb.Append("|");
        }

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Try to get cached topics for a message batch
    /// </summary>
    public bool TryGetCached(string messageHash, out List<string> topics)
    {
        if (_cache.TryGetValue(messageHash, out CachedAiResponse? cached))
        {
            topics = cached.Topics;
            Log.Debug("Cache HIT for hash: {Hash} (contact: {Contact}, batch: {Batch})", 
                messageHash.Substring(0, Math.Min(10, messageHash.Length)), cached.ContactName, cached.BatchNumber);
            return true;
        }

        topics = new List<string>();
        Log.Debug("Cache MISS for hash: {Hash}", messageHash.Substring(0, Math.Min(10, messageHash.Length)));
        return false;
    }

    /// <summary>
    /// Add topics to cache for a message batch
    /// </summary>
    public void AddToCache(string messageHash, List<string> topics, string contactName, int batchNumber)
    {
        CachedAiResponse cached = new CachedAiResponse
        {
            MessageHash = messageHash,
            Topics = topics,
            CachedAt = DateTime.UtcNow,
            ContactName = contactName,
            BatchNumber = batchNumber
        };

        _cache[messageHash] = cached;
        Log.Debug("Added to cache: hash={Hash}, topics={TopicCount}, contact={Contact}, batch={Batch}",
            messageHash.Substring(0, Math.Min(10, messageHash.Length)), topics.Count, contactName, batchNumber);
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int totalEntries, DateTime? oldestEntry, DateTime? newestEntry) GetStatistics()
    {
        if (_cache.Count == 0)
        {
            return (0, null, null);
        }

        DateTime? oldest = _cache.Values.Min(c => c.CachedAt);
        DateTime? newest = _cache.Values.Max(c => c.CachedAt);
        
        return (_cache.Count, oldest, newest);
    }

    /// <summary>
    /// Clear cache (for testing or reset)
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        Log.Information("AI cache cleared");
    }

    /// <summary>
    /// Remove old cache entries (older than specified days)
    /// </summary>
    public int RemoveOldEntries(int olderThanDays = 30)
    {
        DateTime cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        int removedCount = 0;

        List<string> toRemove = new List<string>();
        foreach (KeyValuePair<string, CachedAiResponse> kvp in _cache)
        {
            if (kvp.Value.CachedAt < cutoff)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (string key in toRemove)
        {
            _cache.Remove(key);
            removedCount++;
        }

        if (removedCount > 0)
        {
            Log.Information("Removed {Count} old cache entries older than {Days} days", removedCount, olderThanDays);
        }

        return removedCount;
    }
}
