using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace SMSXmlToCsv.Services.ML;

/// <summary>
/// Extended sentiment categories for message analysis
/// </summary>
public enum ExtendedSentiment
{
    Positive,
    Negative,
    Neutral,
    Flirty,
    Professional,
    Caring,
    Friendly,
    Excited,
    Sad,
    Angry,
    Humorous,
    Supportive
}

/// <summary>
/// Result of sentiment analysis
/// </summary>
public class SentimentResult
{
    public ExtendedSentiment PrimarySentiment { get; set; }
    public Dictionary<ExtendedSentiment, double> SentimentScores { get; set; } = new Dictionary<ExtendedSentiment, double>();
    public double Confidence { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Ollama integration for sentiment analysis with extended categories
/// </summary>
public class OllamaSentimentAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;
    private bool? _isAvailable;

    // Recommended models for sentiment analysis
    public static readonly List<string> RecommendedModels = new List<string>
    {
        "llama3.2:latest",      // Good balance of speed and accuracy
        "llama3.1:latest",      // Larger, more accurate
        "mistral:latest",       // Fast and efficient
        "phi3:latest",          // Lightweight, good for sentiment
        "gemma2:latest"         // Google's efficient model
    };

    public OllamaSentimentAnalyzer(string model = "llama3.2:latest", string baseUrl = "http://localhost:11434")
    {
        _model = model;
        _baseUrl = baseUrl;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    }

    /// <summary>
    /// Check if Ollama is available
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        if (_isAvailable.HasValue)
        {
            return _isAvailable.Value;
        }

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            _isAvailable = response.IsSuccessStatusCode;

            if (_isAvailable.Value)
            {
                Log.Information("Ollama detected and available at {BaseUrl}", _baseUrl);
            }
            else
            {
                Log.Warning("Ollama not responding at {BaseUrl}", _baseUrl);
            }

            return _isAvailable.Value;
        }
        catch (Exception ex)
        {
            _isAvailable = false;
            Log.Warning(ex, "Ollama not available at {BaseUrl}", _baseUrl);
            return false;
        }
    }

    /// <summary>
    /// Get available models from Ollama
    /// </summary>
    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            if (!response.IsSuccessStatusCode)
            {
                return new List<string>();
            }

            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);

            List<string> models = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out JsonElement modelsElement))
            {
                foreach (JsonElement model in modelsElement.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out JsonElement nameElement))
                    {
                        string? modelName = nameElement.GetString();
                        if (!string.IsNullOrEmpty(modelName))
                        {
                            models.Add(modelName);
                        }
                    }
                }
            }

            Log.Information("Found {ModelCount} available Ollama models", models.Count);
            return models;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting available Ollama models");
            return new List<string>();
        }
    }

    /// <summary>
    /// Analyze sentiment of a message with extended categories
    /// </summary>
    public async Task<SentimentResult> AnalyzeSentimentAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SentimentResult
            {
                PrimarySentiment = ExtendedSentiment.Neutral,
                Confidence = 0.0,
                Error = "Empty text"
            };
        }

        try
        {
            string prompt = BuildSentimentPrompt(text);

            object requestBody = new
            {
                model = _model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.1  // Low temperature for more consistent results
                }
            };

            string jsonRequest = JsonSerializer.Serialize(requestBody);
            StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                return new SentimentResult
                {
                    PrimarySentiment = ExtendedSentiment.Neutral,
                    Confidence = 0.0,
                    Error = $"API error: {response.StatusCode}"
                };
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);

            if (doc.RootElement.TryGetProperty("response", out JsonElement responseElement))
            {
                string aiResponse = responseElement.GetString() ?? "";
                return ParseSentimentResponse(aiResponse);
            }

            return new SentimentResult
            {
                PrimarySentiment = ExtendedSentiment.Neutral,
                Confidence = 0.0,
                Error = "No response from model"
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error analyzing sentiment for text: {Text}", text.Substring(0, Math.Min(50, text.Length)));
            return new SentimentResult
            {
                PrimarySentiment = ExtendedSentiment.Neutral,
                Confidence = 0.0,
                Error = ex.Message
            };
        }
    }

    private string BuildSentimentPrompt(string text)
    {
        return $@"Analyze the sentiment of this message and classify it into one or more of these categories:
- Positive: Generally positive, happy, or optimistic
- Negative: Generally negative, sad, or pessimistic
- Neutral: Factual, informational, or lacking emotion
- Flirty: Romantic, playful, or suggestive
- Professional: Business-like, formal, or work-related
- Caring: Compassionate, nurturing, or concerned
- Friendly: Warm, amiable, or sociable
- Excited: Enthusiastic, energetic, or eager
- Sad: Sorrowful, melancholic, or disappointed
- Angry: Upset, frustrated, or irritated
- Humorous: Funny, joking, or lighthearted
- Supportive: Encouraging, helpful, or reassuring

Message: ""{text}""

Respond ONLY with JSON in this exact format (no other text):
{{
  ""primary"": ""<category>"",
  ""scores"": {{
    ""positive"": <0.0-1.0>,
    ""negative"": <0.0-1.0>,
    ""neutral"": <0.0-1.0>,
    ""flirty"": <0.0-1.0>,
    ""professional"": <0.0-1.0>,
    ""caring"": <0.0-1.0>,
    ""friendly"": <0.0-1.0>,
    ""excited"": <0.0-1.0>,
    ""sad"": <0.0-1.0>,
    ""angry"": <0.0-1.0>,
    ""humorous"": <0.0-1.0>,
    ""supportive"": <0.0-1.0>
  }},
  ""confidence"": <0.0-1.0>
}}";
    }

    private SentimentResult ParseSentimentResponse(string response)
    {
        try
        {
            // Try to extract JSON from response (model might add extra text)
            int jsonStart = response.IndexOf('{');
            int jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                string jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using JsonDocument doc = JsonDocument.Parse(jsonStr);

                SentimentResult result = new SentimentResult();

                // Parse primary sentiment
                if (doc.RootElement.TryGetProperty("primary", out JsonElement primaryElement))
                {
                    string primaryStr = primaryElement.GetString() ?? "neutral";
                    result.PrimarySentiment = ParseSentimentString(primaryStr);
                }

                // Parse scores
                if (doc.RootElement.TryGetProperty("scores", out JsonElement scoresElement))
                {
                    foreach (ExtendedSentiment sentiment in Enum.GetValues<ExtendedSentiment>())
                    {
                        string key = sentiment.ToString().ToLowerInvariant();
                        if (scoresElement.TryGetProperty(key, out JsonElement scoreElement))
                        {
                            result.SentimentScores[sentiment] = scoreElement.GetDouble();
                        }
                    }
                }

                // Parse confidence
                if (doc.RootElement.TryGetProperty("confidence", out JsonElement confidenceElement))
                {
                    result.Confidence = confidenceElement.GetDouble();
                }

                return result;
            }

            // Fallback: parse simple text response
            return FallbackParsing(response);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error parsing sentiment response, using fallback");
            return FallbackParsing(response);
        }
    }

    private SentimentResult FallbackParsing(string response)
    {
        string lowerResponse = response.ToLowerInvariant();
        SentimentResult result = new SentimentResult { Confidence = 0.5 };

        // Simple keyword-based fallback
        if (lowerResponse.Contains("flirty") || lowerResponse.Contains("romantic"))
        {
            result.PrimarySentiment = ExtendedSentiment.Flirty;
        }
        else if (lowerResponse.Contains("professional") || lowerResponse.Contains("business"))
        {
            result.PrimarySentiment = ExtendedSentiment.Professional;
        }
        else if (lowerResponse.Contains("caring") || lowerResponse.Contains("compassionate"))
        {
            result.PrimarySentiment = ExtendedSentiment.Caring;
        }
        else if (lowerResponse.Contains("friendly") || lowerResponse.Contains("warm"))
        {
            result.PrimarySentiment = ExtendedSentiment.Friendly;
        }
        else if (lowerResponse.Contains("excited") || lowerResponse.Contains("enthusiastic"))
        {
            result.PrimarySentiment = ExtendedSentiment.Excited;
        }
        else if (lowerResponse.Contains("sad") || lowerResponse.Contains("melancholic"))
        {
            result.PrimarySentiment = ExtendedSentiment.Sad;
        }
        else if (lowerResponse.Contains("angry") || lowerResponse.Contains("frustrated"))
        {
            result.PrimarySentiment = ExtendedSentiment.Angry;
        }
        else if (lowerResponse.Contains("humorous") || lowerResponse.Contains("funny"))
        {
            result.PrimarySentiment = ExtendedSentiment.Humorous;
        }
        else if (lowerResponse.Contains("supportive") || lowerResponse.Contains("encouraging"))
        {
            result.PrimarySentiment = ExtendedSentiment.Supportive;
        }
        else if (lowerResponse.Contains("positive"))
        {
            result.PrimarySentiment = ExtendedSentiment.Positive;
        }
        else if (lowerResponse.Contains("negative"))
        {
            result.PrimarySentiment = ExtendedSentiment.Negative;
        }
        else
        {
            result.PrimarySentiment = ExtendedSentiment.Neutral;
        }

        result.SentimentScores[result.PrimarySentiment] = 0.7;

        return result;
    }

    private ExtendedSentiment ParseSentimentString(string sentiment)
    {
        string lower = sentiment.ToLowerInvariant();

        return lower switch
        {
            "positive" => ExtendedSentiment.Positive,
            "negative" => ExtendedSentiment.Negative,
            "neutral" => ExtendedSentiment.Neutral,
            "flirty" => ExtendedSentiment.Flirty,
            "professional" => ExtendedSentiment.Professional,
            "caring" => ExtendedSentiment.Caring,
            "friendly" => ExtendedSentiment.Friendly,
            "excited" => ExtendedSentiment.Excited,
            "sad" => ExtendedSentiment.Sad,
            "angry" => ExtendedSentiment.Angry,
            "humorous" => ExtendedSentiment.Humorous,
            "supportive" => ExtendedSentiment.Supportive,
            _ => ExtendedSentiment.Neutral
        };
    }
}
