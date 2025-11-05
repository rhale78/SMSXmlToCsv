using SMSXmlToCsv.Logging;

namespace SMSXmlToCsv.Utils
{
    /// <summary>
    /// File type detection using magic numbers (file signatures)
    /// </summary>
    public static class FileTypeDetector
    {
        // Magic number signatures for common file types
        private static readonly Dictionary<byte[], string> _magicNumbers = new()
        {
            // Images
            { new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg" },
            { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png" },
            { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, "image/gif" },
            { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, "image/gif" },
            { new byte[] { 0x42, 0x4D }, "image/bmp" },
            { new byte[] { 0x49, 0x49, 0x2A, 0x00 }, "image/tiff" },
            { new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, "image/tiff" },
            { new byte[] { 0x52, 0x49, 0x46, 0x46 }, "image/webp" }, // RIFF (also used by WAV)
            
            // Videos
            { new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }, "video/mp4" },
            { new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }, "video/mp4" },
            { new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }, "video/mp4" },
            { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, "video/webm" },
            { new byte[] { 0x66, 0x74, 0x79, 0x70, 0x33, 0x67, 0x70 }, "video/3gpp" },
            { new byte[] { 0x66, 0x74, 0x79, 0x70, 0x4D, 0x53, 0x4E, 0x56 }, "video/mp4" },
            
            // Audio
            { new byte[] { 0xFF, 0xFB }, "audio/mpeg" }, // MP3
            { new byte[] { 0xFF, 0xF3 }, "audio/mpeg" }, // MP3
            { new byte[] { 0xFF, 0xF2 }, "audio/mpeg" }, // MP3
            { new byte[] { 0x49, 0x44, 0x33 }, "audio/mpeg" }, // MP3 with ID3
            { new byte[] { 0x66, 0x74, 0x79, 0x70, 0x4D, 0x34, 0x41 }, "audio/mp4" },
            { new byte[] { 0x4F, 0x67, 0x67, 0x53 }, "audio/ogg" },
            { new byte[] { 0x66, 0x4C, 0x61, 0x43 }, "audio/flac" },
            
            // Documents
            { new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf" },
            { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "application/zip" }, // Also Office documents
            { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, "application/msword" },
            
            // Archives
            { new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 }, "application/x-rar-compressed" },
            { new byte[] { 0x1F, 0x8B }, "application/gzip" },
            { new byte[] { 0x42, 0x5A, 0x68 }, "application/x-bzip2" },
            
            // Text/XML
            { new byte[] { 0x3C, 0x3F, 0x78, 0x6D, 0x6C }, "text/xml" }, // <?xml
            { new byte[] { 0xEF, 0xBB, 0xBF }, "text/plain" }, // UTF-8 BOM
        };

        /// <summary>
        /// Detect MIME type from file data using magic numbers
        /// </summary>
        public static string DetectMimeType(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return "application/octet-stream";
            }

            // Check against all known magic numbers
            foreach (KeyValuePair<byte[], string> signature in _magicNumbers)
            {
                if (StartsWith(data, signature.Key))
                {
                    // Special handling for RIFF files (could be WebP or WAV)
                    if (signature.Value == "image/webp" && data.Length > 12)
                    {
                        // Check for WEBP signature at offset 8
                        byte[] webpSignature = { 0x57, 0x45, 0x42, 0x50 }; // "WEBP"
                        if (data.Length > 11 && StartsWith(data, webpSignature, 8))
                        {
                            return "image/webp";
                        }
                        // Check for WAVE signature at offset 8
                        byte[] waveSignature = { 0x57, 0x41, 0x56, 0x45 }; // "WAVE"
                        if (data.Length > 11 && StartsWith(data, waveSignature, 8))
                        {
                            return "audio/wav";
                        }
                    }

                    return signature.Value;
                }
            }

            // Fallback: try to detect by content analysis
            return AnalyzeContent(data);
        }

        /// <summary>
        /// Validate that file data matches its declared MIME type
        /// </summary>
        public static ValidationResult ValidateFileType(byte[] data, string declaredMimeType)
        {
            string detectedMimeType = DetectMimeType(data);

            // Normalize MIME types for comparison
            string normalizedDeclared = NormalizeMimeType(declaredMimeType);
            string normalizedDetected = NormalizeMimeType(detectedMimeType);

            if (normalizedDeclared == normalizedDetected)
            {
                return new ValidationResult
                {
                    IsValid = true,
                    DetectedMimeType = detectedMimeType,
                    Message = "File type matches declaration"
                };
            }

            // Check if they're compatible (e.g., video/mp4 and video/3gpp)
            if (AreCompatibleTypes(normalizedDeclared, normalizedDetected))
            {
                return new ValidationResult
                {
                    IsValid = true,
                    DetectedMimeType = detectedMimeType,
                    Message = $"Compatible types: {declaredMimeType} ? {detectedMimeType}"
                };
            }

            AppLogger.Warning($"MIME type mismatch: declared={declaredMimeType}, detected={detectedMimeType}");

            return new ValidationResult
            {
                IsValid = false,
                DetectedMimeType = detectedMimeType,
                Message = $"Type mismatch: declared {declaredMimeType}, detected {detectedMimeType}"
            };
        }

        /// <summary>
        /// Get file extension from MIME type
        /// </summary>
        public static string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/tiff" => ".tiff",
                "image/webp" => ".webp",
                "video/mp4" => ".mp4",
                "video/3gpp" => ".3gp",
                "video/webm" => ".webm",
                "audio/mpeg" => ".mp3",
                "audio/mp4" => ".m4a",
                "audio/ogg" => ".ogg",
                "audio/wav" => ".wav",
                "audio/flac" => ".flac",
                "application/pdf" => ".pdf",
                "text/plain" => ".txt",
                "text/xml" => ".xml",
                "text/vcard" => ".vcf",
                "text/x-vcard" => ".vcf",
                _ => ".bin"
            };
        }

        /// <summary>
        /// Check if file appears to be corrupted
        /// </summary>
        public static bool IsLikelyCorrupted(byte[] data, string mimeType)
        {
            if (data == null || data.Length < 10)
            {
                return true; // Too small to be valid
            }

            // Check for common corruption patterns
            if (mimeType.StartsWith("image/"))
            {
                // Images should not be all zeros or all 0xFF
                bool allZeros = data.Take(Math.Min(100, data.Length)).All(b => b == 0x00);
                bool allOnes = data.Take(Math.Min(100, data.Length)).All(b => b == 0xFF);

                if (allZeros || allOnes)
                {
                    AppLogger.Warning($"Suspicious data pattern detected for {mimeType}");
                    return true;
                }
            }

            return false;
        }

        private static bool StartsWith(byte[] data, byte[] signature, int offset = 0)
        {
            if (data.Length < signature.Length + offset)
            {
                return false;
            }

            for (int i = 0; i < signature.Length; i++)
            {
                if (data[i + offset] != signature[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string AnalyzeContent(byte[] data)
        {
            // Text detection: check if mostly printable ASCII/UTF-8
            if (data.Length > 100)
            {
                int printableCount = data.Take(100).Count(b =>
                    (b >= 0x20 && b <= 0x7E) || b == 0x09 || b == 0x0A || b == 0x0D);

                if (printableCount > 90) // >90% printable
                {
                    return "text/plain";
                }
            }

            return "application/octet-stream";
        }

        private static string NormalizeMimeType(string mimeType)
        {
            // Remove parameters (e.g., "text/plain; charset=utf-8" -> "text/plain")
            int semicolonIndex = mimeType.IndexOf(';');
            if (semicolonIndex > 0)
            {
                mimeType = mimeType.Substring(0, semicolonIndex);
            }

            return mimeType.Trim().ToLowerInvariant();
        }

        private static bool AreCompatibleTypes(string type1, string type2)
        {
            // Same main type (e.g., both video/*)
            string[] parts1 = type1.Split('/');
            string[] parts2 = type2.Split('/');

            if (parts1.Length == 2 && parts2.Length == 2)
            {
                // Compatible video formats
                if (parts1[0] == "video" && parts2[0] == "video")
                {
                    return true;
                }

                // Compatible audio formats
                if (parts1[0] == "audio" && parts2[0] == "audio")
                {
                    return true;
                }

                // JPEG variants
                if ((type1.Contains("jpeg") || type1.Contains("jpg")) &&
                    (type2.Contains("jpeg") || type2.Contains("jpg")))
                {
                    return true;
                }
            }

            return false;
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string DetectedMimeType { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }
}
