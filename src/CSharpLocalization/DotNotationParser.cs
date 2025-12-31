using System;
using System.Collections.Generic;

namespace CSharpLocalization
{
    /// <summary>
    /// Parses dot-notation keys and retrieves nested values from dictionaries.
    /// </summary>
    public static class DotNotationParser
    {
        /// <summary>
        /// Gets a nested value from a dictionary using dot notation.
        /// Example: GetValue(dict, "messages.errors.not_found") returns dict["messages"]["errors"]["not_found"]
        /// </summary>
        /// <param name="data">The dictionary containing the translations.</param>
        /// <param name="key">The dot-notation key (e.g., "site.title").</param>
        /// <returns>The value at the specified key, or null if not found.</returns>
        public static object GetValue(Dictionary<string, object> data, string key)
        {
            if (data == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            string[] segments = ParseKey(key);
            object current = data;

            foreach (string segment in segments)
            {
                if (current is Dictionary<string, object> dict)
                {
                    // Try exact match first (case-insensitive due to dictionary comparer)
                    if (dict.TryGetValue(segment, out object value))
                    {
                        current = value;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// Splits a dot-notation key into segments.
        /// </summary>
        /// <param name="key">The dot-notation key.</param>
        /// <returns>An array of key segments.</returns>
        public static string[] ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Array.Empty<string>();
            }

            return key.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
