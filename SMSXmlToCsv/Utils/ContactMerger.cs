using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Utils
{
    /// <summary>
    /// Utility for merging duplicate contacts with fuzzy search support
    /// </summary>
    public class ContactMerger
    {
        /// <summary>
        /// Contact merge rule - defines how to merge two contacts
        /// </summary>
        public class MergeRule
        {
            public string SourcePhone { get; set; } = string.Empty;
            public string SourceName { get; set; } = string.Empty;
            public string TargetPhone { get; set; } = string.Empty;
            public string TargetName { get; set; } = string.Empty;
            public string ResultPhone { get; set; } = string.Empty;
            public string ResultName { get; set; } = string.Empty;
        }

        private readonly List<MergeRule> _mergeRules = new List<MergeRule>();

        /// <summary>
        /// Get the count of merge rules configured
        /// </summary>
        public int MergeRuleCount => _mergeRules.Count;

        /// <summary>
        /// Add a merge rule - FIX: Now supports multiple merge operations
        /// </summary>
        public void AddMergeRule(string sourcePhone, string sourceName, string targetPhone, string targetName,
                                string resultPhone, string resultName)
        {
            _mergeRules.Add(new MergeRule
            {
                SourcePhone = sourcePhone,
                SourceName = sourceName,
                TargetPhone = targetPhone,
                TargetName = targetName,
                ResultPhone = resultPhone,
                ResultName = resultName
            });

            AppLogger.Information($"Added merge rule {_mergeRules.Count}: [{sourceName}|{sourcePhone}] + [{targetName}|{targetPhone}] -> [{resultName}|{resultPhone}]");
        }

        /// <summary>
        /// FIX: Apply ALL merge rules to message list (supports multiple merges)
        /// </summary>
        public List<SmsMessage> ApplyMerges(List<SmsMessage> messages)
        {
            if (_mergeRules.Count == 0)
            {
                AppLogger.Information("No merge rules to apply");
                return messages;
            }

            AppLogger.Information($"Applying {_mergeRules.Count} merge rules to {messages.Count} messages");

            // Track which phones have been merged to avoid conflicts
            Dictionary<string, (string phone, string name)> phoneMapping = new Dictionary<string, (string phone, string name)>();

            // Build complete mapping from all rules
            foreach (MergeRule rule in _mergeRules)
            {
                // Map both source and target to the result
                if (!phoneMapping.ContainsKey(rule.SourcePhone))
                {
                    phoneMapping[rule.SourcePhone] = (rule.ResultPhone, rule.ResultName);
                }
                if (!phoneMapping.ContainsKey(rule.TargetPhone))
                {
                    phoneMapping[rule.TargetPhone] = (rule.ResultPhone, rule.ResultName);
                }

                // Also map the result phone to itself (in case it's used in another merge)
                if (!phoneMapping.ContainsKey(rule.ResultPhone))
                {
                    phoneMapping[rule.ResultPhone] = (rule.ResultPhone, rule.ResultName);
                }
            }

            int mergedCount = 0;
            foreach (SmsMessage msg in messages)
            {
                bool merged = false;

                // Check if FromPhone needs to be merged
                if (phoneMapping.ContainsKey(msg.FromPhone))
                {
                    (string phone, string name) result = phoneMapping[msg.FromPhone];
                    msg.FromPhone = result.phone;
                    msg.FromName = result.name;
                    merged = true;
                }

                // Check if ToPhone needs to be merged
                if (phoneMapping.ContainsKey(msg.ToPhone))
                {
                    (string phone, string name) result = phoneMapping[msg.ToPhone];
                    msg.ToPhone = result.phone;
                    msg.ToName = result.name;
                    merged = true;
                }

                if (merged)
                {
                    mergedCount++;
                }
            }

            AppLogger.Information($"Applied {_mergeRules.Count} merge rules, affected {mergedCount} messages");
            return messages;
        }

        /// <summary>
        /// FIX: Fuzzy search contacts by name or partial phone number
        /// </summary>
        public static List<(string Phone, string Name, int MessageCount)> SearchContacts(
            List<SmsMessage> messages,
            string userPhone,
            string searchTerm)
        {
            searchTerm = searchTerm.ToLower().Trim();

            // Build contact list with message counts
            Dictionary<string, (string name, int count)> contactCounts = new Dictionary<string, (string name, int count)>();

            foreach (SmsMessage msg in messages)
            {
                string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;
                string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;

                if (contactPhone == userPhone)
                {
                    continue;
                }

                if (!contactCounts.ContainsKey(contactPhone))
                {
                    contactCounts[contactPhone] = (contactName, 0);
                }
                contactCounts[contactPhone] = (contactCounts[contactPhone].name, contactCounts[contactPhone].count + 1);
            }

            // Search: match by name (fuzzy) or phone (partial)
            List<(string Phone, string Name, int MessageCount, int Score)> results = new List<(string Phone, string Name, int MessageCount, int Score)>();

            foreach (KeyValuePair<string, (string name, int count)> contact in contactCounts)
            {
                string phone = contact.Key;
                string name = contact.Value.name.ToLower();
                int count = contact.Value.count;

                int score = 0;

                // Exact match on name (highest score)
                if (name == searchTerm)
                {
                    score = 1000;
                }
                // Name starts with search term
                else if (name.StartsWith(searchTerm))
                {
                    score = 500;
                }
                // Name contains search term
                else if (name.Contains(searchTerm))
                {
                    score = 250;
                }
                // Fuzzy match: search term words in name
                else
                {
                    string[] searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string[] nameWords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    int matchedWords = searchWords.Count(sw => nameWords.Any(nw => nw.StartsWith(sw)));
                    if (matchedWords > 0)
                    {
                        score = 100 * matchedWords;
                    }
                }

                // Phone number match (partial)
                string phoneDigits = new string(phone.Where(char.IsDigit).ToArray());
                string searchDigits = new string(searchTerm.Where(char.IsDigit).ToArray());

                if (searchDigits.Length > 0 && phoneDigits.Contains(searchDigits))
                {
                    score = Math.Max(score, 150);
                }

                if (score > 0)
                {
                    results.Add((phone, contact.Value.name, count, score));
                }
            }

            // Return sorted by score (highest first), then by message count
            return results
                .OrderByDescending(r => r.Score)
                .ThenByDescending(r => r.MessageCount)
                .Select(r => (r.Phone, r.Name, r.MessageCount))
                .ToList();
        }

        /// <summary>
        /// Detect potential duplicate contacts based on phone number similarity or name similarity
        /// </summary>
        public static List<(string Phone1, string Name1, string Phone2, string Name2, string Reason)>
            DetectPotentialDuplicates(List<SmsMessage> messages, string userPhone)
        {
            List<(string, string, string, string, string)> potentialDuplicates = new List<(string, string, string, string, string)>();
            Dictionary<string, string> contactInfo = new Dictionary<string, string>(); // phone -> name

            // Build contact list
            foreach (SmsMessage msg in messages)
            {
                string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;
                string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;

                if (contactPhone == userPhone)
                {
                    continue;
                }

                if (!contactInfo.ContainsKey(contactPhone))
                {
                    contactInfo[contactPhone] = contactName;
                }
            }

            List<KeyValuePair<string, string>> contacts = contactInfo.ToList();

            // Check for duplicates
            for (int i = 0; i < contacts.Count; i++)
            {
                for (int j = i + 1; j < contacts.Count; j++)
                {
                    KeyValuePair<string, string> contact1 = contacts[i];
                    KeyValuePair<string, string> contact2 = contacts[j];

                    // Check if names are very similar (same person, different phones?)
                    if (!string.IsNullOrWhiteSpace(contact1.Value) &&
                        !string.IsNullOrWhiteSpace(contact2.Value) &&
                        contact1.Value != "(Unknown)" &&
                        contact2.Value != "(Unknown)")
                    {
                        string name1 = contact1.Value.ToLower().Trim();
                        string name2 = contact2.Value.ToLower().Trim();

                        // Exact match
                        if (name1 == name2)
                        {
                            potentialDuplicates.Add((
                                contact1.Key, contact1.Value,
                                contact2.Key, contact2.Value,
                                "Same name, different phone numbers"
                            ));
                            continue;
                        }

                        // Check if one is a substring of the other (e.g., "Person A" vs "Person A B")
                        if (name1.Contains(name2) || name2.Contains(name1))
                        {
                            potentialDuplicates.Add((
                                contact1.Key, contact1.Value,
                                contact2.Key, contact2.Value,
                                "Similar names (one contains the other)"
                            ));
                            continue;
                        }

                        // FIX: Check for common nickname patterns (e.g., "Fran" matches "Frances"/"Francis")
                        if (IsLikelyNickname(name1, name2))
                        {
                            potentialDuplicates.Add((
                                contact1.Key, contact1.Value,
                                contact2.Key, contact2.Value,
                                "Possible nickname/full name match"
                            ));
                            continue;
                        }
                    }

                    // Check if phone numbers are very similar (e.g., with/without country code)
                    string phone1Digits = new string(contact1.Key.Where(char.IsDigit).ToArray());
                    string phone2Digits = new string(contact2.Key.Where(char.IsDigit).ToArray());

                    if (phone1Digits.Length >= 10 && phone2Digits.Length >= 10)
                    {
                        // Check last 10 digits (US phone number)
                        string last10_1 = phone1Digits.Length > 10 ? phone1Digits.Substring(phone1Digits.Length - 10) : phone1Digits;
                        string last10_2 = phone2Digits.Length > 10 ? phone2Digits.Substring(phone2Digits.Length - 10) : phone2Digits;

                        if (last10_1 == last10_2 && phone1Digits != phone2Digits)
                        {
                            potentialDuplicates.Add((
                                contact1.Key, contact1.Value,
                                contact2.Key, contact2.Value,
                                "Same last 10 digits (different country codes?)"
                            ));
                        }
                    }
                }
            }

            return potentialDuplicates;
        }

        /// <summary>
        /// FIX: Check if one name is likely a nickname of another
        /// </summary>
        private static bool IsLikelyNickname(string name1, string name2)
        {
            // Simple heuristic: if one name starts with the other and is at least 3 chars
            if (name1.Length >= 3 && name2.Length >= 3)
            {
                if (name2.StartsWith(name1) || name1.StartsWith(name2))
                {
                    return true;
                }
            }

            // Check common patterns (this could be expanded with a dictionary)
            (string, string)[] commonPairs = new[]
            {
                ("fran", "frances"),
                ("fran", "francis"),
                ("mike", "michael"),
                ("beth", "elizabeth"),
                ("bob", "robert"),
                ("jim", "james"),
                ("bill", "william"),
                ("tony", "anthony"),
                ("chris", "christopher"),
                ("chris", "christina"),
                ("alex", "alexander"),
                ("alex", "alexandra"),
                ("sam", "samuel"),
                ("sam", "samantha")
            };

            foreach ((string? short1, string? long1) in commonPairs)
            {
                if ((name1 == short1 && name2.StartsWith(long1)) || (name2 == short1 && name1.StartsWith(long1)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get count of affected messages for a merge
        /// </summary>
        public static int GetMergeImpact(List<SmsMessage> messages, string phone1, string phone2)
        {
            return messages.Count(m =>
                m.FromPhone == phone1 || m.FromPhone == phone2 ||
                m.ToPhone == phone1 || m.ToPhone == phone2
            );
        }

        /// <summary>
        /// Get all current merge rules as MergeDecision objects for saving
        /// </summary>
        public List<MergeDecision> GetMergeDecisions()
        {
            return _mergeRules.Select(rule => new MergeDecision
            {
                SourceContacts = new List<string>
                {
                    $"{rule.SourceName}|{rule.SourcePhone}",
                    $"{rule.TargetName}|{rule.TargetPhone}"
                },
                TargetName = rule.ResultName,
                TargetPhone = rule.ResultPhone,
                MergedAt = DateTime.Now,
                Reason = $"Merged {rule.SourceName} and {rule.TargetName}"
            }).ToList();
        }
    }
}
