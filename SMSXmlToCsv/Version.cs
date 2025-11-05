namespace SMSXmlToCsv
{
    /// <summary>
    /// Centralized version management for SMS Backup XML Converter
    /// This file is automatically updated during build/release by Visual Studio
    /// </summary>
    public static class AppVersion
    {
      /// <summary>
        /// Current application version
      /// Format: Major.Minor (e.g., 0.7)
   /// </summary>
    public const string Version = "0.7";

        /// <summary>
        /// Full version string with build info
 /// </summary>
      public const string FullVersion = "0.7.0";

        /// <summary>
        /// Build date (updated during compilation)
        /// </summary>
        public const string BuildDate = "2025-10-28";

  /// <summary>
        /// Project codename
        /// </summary>
        public const string Codename = "VibeCoded MVP";

 /// <summary>
        /// Development info
   /// </summary>
        public const string DevelopmentInfo = "Created with Claude Sonnet 4.5 in VS 2026";

        /// <summary>
   /// Get formatted version string for display
        /// </summary>
 public static string GetVersionString()
  {
            return $"SMS Backup XML Converter v{Version} ({Codename})";
        }

   /// <summary>
   /// Get full version info including build date
        /// </summary>
        public static string GetFullVersionInfo()
        {
  return $"{GetVersionString()} - Built: {BuildDate}";
        }

     /// <summary>
        /// Get development attribution
        /// </summary>
        public static string GetDevelopmentAttribution()
        {
    return DevelopmentInfo;
        }
    }
}
