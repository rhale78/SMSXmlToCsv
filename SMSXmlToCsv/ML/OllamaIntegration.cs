using System.Text;
using System.Text.Json;

using SMSXmlToCsv.Logging;

namespace SMSXmlToCsv.ML
{
    /// <summary>
    /// Ollama integration for ML-based analysis
    /// </summary>
    public class OllamaIntegration
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool? _isAvailable;

        public OllamaIntegration(string baseUrl = "http://localhost:11434")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        }

        /// <summary>
        /// Check if Ollama is installed and running
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
                    AppLogger.Information("Ollama detected and available");
                }
                else
                {
                    AppLogger.Warning("Ollama not responding");
                }

                return _isAvailable.Value;
            }
            catch
            {
                _isAvailable = false;
                AppLogger.Warning("Ollama not available");
                return false;
            }
        }

        /// <summary>
        /// Get available models
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
                JsonDocument doc = JsonDocument.Parse(json);

                List<string> models = new List<string>();
                if (doc.RootElement.TryGetProperty("models", out JsonElement modelsElement))
                {
                    foreach (JsonElement model in modelsElement.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out JsonElement nameElement))
                        {
                            models.Add(nameElement.GetString() ?? "");
                        }
                    }
                }

                return models.Where(m => !string.IsNullOrEmpty(m)).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Analyze sentiment of a message
        /// </summary>
        public async Task<SentimentResult> AnalyzeSentimentAsync(string text, string model = "llama3.2")
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new SentimentResult { Sentiment = "neutral", Confidence = 0.5, Error = "Empty text" };
            }

            // Enhanced prompt that explicitly requests extended sentiment types
            string prompt = $@"Analyze the sentiment and tone of this SMS message.

Classify using ONE or MORE of these categories (comma-separated):
- Basic: positive, negative, neutral
- Tone: professional, friendly, casual, formal, combative, argumentative

Examples:
""Thanks for your help!"" ? positive, friendly
""Per your request, attached."" ? neutral, professional, formal
""Are you kidding me?!"" ? negative, combative
""hey wanna hang?"" ? positive, casual, friendly
""I respectfully disagree."" ? negative, professional, formal

Message: {text}

Classification:";

            try
            {
                string response = await GenerateAsync(prompt, model);
                string sentiment = response.Trim().ToLower();

                // Primary sentiment extraction
                return sentiment.Contains("positive")
                    ? new SentimentResult { Sentiment = sentiment, Confidence = 0.8, Text = text }
                    : sentiment.Contains("negative")
                        ? new SentimentResult { Sentiment = sentiment, Confidence = 0.8, Text = text }
                        : new SentimentResult { Sentiment = sentiment.Contains("neutral") ? sentiment : "neutral", Confidence = 0.7, Text = text };
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Sentiment analysis failed: {ex.Message}");
                return new SentimentResult { Sentiment = "neutral", Confidence = 0.0, Error = ex.Message };
            }
        }

        /// <summary>
        /// Generate conversation cluster labels
        /// </summary>
        public async Task<string> GenerateClusterLabelAsync(List<string> sampleMessages, string model = "llama3.2")
        {
            string messagesText = string.Join("\n", sampleMessages.Take(10));
            string prompt = $"Based on these conversation samples, provide a short 2-3 word label that describes the topic:\n\n{messagesText}\n\nLabel:";

            try
            {
                string response = await GenerateAsync(prompt, model);
                return response.Trim().Split('\n')[0].Trim('"', ' ', '.', ',');
            }
            catch
            {
                return "Unknown Topic";
            }
        }

        /// <summary>
        /// Generate text using Ollama - now public for NetworkGraphGenerator
        /// </summary>
        public async Task<string> GenerateAsync(string prompt, string model = "llama3.2")
        {
            var request = new
            {
                model = model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3,
                    num_predict = 200  // Increased for topic lists
                }
            };

            StringContent content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
            response.EnsureSuccessStatusCode();

            string responseText = await response.Content.ReadAsStringAsync();
            JsonDocument doc = JsonDocument.Parse(responseText);

            return doc.RootElement.TryGetProperty("response", out JsonElement responseElement) ? responseElement.GetString() ?? "" : "";
        }
    }

    /// <summary>
    /// Sentiment analysis result
    /// </summary>
    public class SentimentResult
    {
        public string Text { get; set; } = string.Empty;
        public string Sentiment { get; set; } = "neutral";  // positive, negative, neutral
        public double Confidence { get; set; }
        public string? Error { get; set; }
    }
}
