using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace SMSXmlToCsv.Services
{
    /// <summary>
    /// Global in-memory store of detected user identity attributes (names, emails) across imports.
    /// Used to improve direction inference and contact merging when multiple sources span long time ranges.
    /// </summary>
    public static class UserIdentityStore
    {
        private static readonly object _lock = new();
        private static readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _emails = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<string> Names { get { lock (_lock) { return _names.Count == 0 ? Array.Empty<string>() : new List<string>(_names); } } }
        public static IReadOnlyCollection<string> Emails { get { lock (_lock) { return _emails.Count == 0 ? Array.Empty<string>() : new List<string>(_emails); } } }

        public static void AddName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            lock (_lock)
            {
                _names.Add(name.Trim());
            }
        }

        public static void AddEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            email = email.Trim();
            if (!LooksLikeEmail(email)) return;
            lock (_lock)
            {
                _emails.Add(email);
            }
        }

        public static void AddNames(IEnumerable<string> names)
        {
            if (names == null) return;
            foreach (var n in names) AddName(n);
        }

        public static void AddEmails(IEnumerable<string> emails)
        {
            if (emails == null) return;
            foreach (var e in emails) AddEmail(e);
        }

        private static bool LooksLikeEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
