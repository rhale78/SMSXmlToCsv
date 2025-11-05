using System;
using System.Collections.Generic;
using System.Linq;
using SMSXmlToCsv.Models;
using Serilog;

namespace SMSXmlToCsv.Services.Filtering;

/// <summary>
/// Filters messages by date range
/// </summary>
public class DateRangeFilter
{
    private readonly DateTime? _startDate;
    private readonly DateTime? _endDate;
    private readonly bool _enabled;

    public DateRangeFilter(bool enabled, DateTime? startDate, DateTime? endDate)
    {
        _enabled = enabled;
        _startDate = startDate;
        _endDate = endDate;

        if (_enabled)
        {
            Log.Information("Date filtering enabled: {StartDate} to {EndDate}", 
                _startDate?.ToString("yyyy-MM-dd") ?? "no start",
                _endDate?.ToString("yyyy-MM-dd") ?? "no end");
        }
    }

    public bool IsEnabled => _enabled;
    public DateTime? StartDate => _startDate;
    public DateTime? EndDate => _endDate;

    /// <summary>
    /// Filter messages by date range
    /// </summary>
    public IEnumerable<Message> Filter(IEnumerable<Message> messages)
    {
        if (!_enabled)
        {
            return messages;
        }

        IEnumerable<Message> filtered = messages;

        if (_startDate.HasValue)
        {
            filtered = filtered.Where(m => m.TimestampUtc >= _startDate.Value);
        }

        if (_endDate.HasValue)
        {
            // Include the entire end date (up to 23:59:59)
            DateTime endOfDay = _endDate.Value.Date.AddDays(1).AddTicks(-1);
            filtered = filtered.Where(m => m.TimestampUtc <= endOfDay);
        }

        List<Message> result = filtered.ToList();

        if (result.Count < messages.Count())
        {
            int originalCount = messages.Count();
            Log.Information("Date filtering: {OriginalCount} messages -> {FilteredCount} messages", 
                originalCount, result.Count);
        }

        return result;
    }

    /// <summary>
    /// Check if a message is within the date range
    /// </summary>
    public bool IsInRange(Message message)
    {
        if (!_enabled)
        {
            return true;
        }

        if (_startDate.HasValue && message.TimestampUtc < _startDate.Value)
        {
            return false;
        }

        if (_endDate.HasValue)
        {
            DateTime endOfDay = _endDate.Value.Date.AddDays(1).AddTicks(-1);
            if (message.TimestampUtc > endOfDay)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Parse date string in multiple formats
    /// </summary>
    public static DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        string[] formats = new[]
        {
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ"
        };

        foreach (string format in formats)
        {
            if (DateTime.TryParseExact(dateString, format, null, 
                System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
        }

        // Try general parse as fallback
        if (DateTime.TryParse(dateString, out DateTime generalResult))
        {
            return generalResult;
        }

        return null;
    }
}
