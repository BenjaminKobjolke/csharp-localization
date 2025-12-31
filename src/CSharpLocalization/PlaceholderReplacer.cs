using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CSharpLocalization
{
    /// <summary>
    /// Replaces placeholders in translation strings with provided values.
    /// Supports case-aware replacement: :name, :NAME, :Name
    /// </summary>
    public class PlaceholderReplacer
    {
        // Matches placeholders like :name, :NAME, :Name, :firstName
        private static readonly Regex PlaceholderPattern = new Regex(
            @":([a-zA-Z_][a-zA-Z0-9_]*)",
            RegexOptions.Compiled);

        /// <summary>
        /// Replaces all placeholders in the text with values from the replacements dictionary.
        /// The replacement is case-aware:
        /// - :name (all lowercase) -> replacement as lowercase
        /// - :NAME (all uppercase) -> replacement as uppercase
        /// - :Name (first letter uppercase) -> replacement with first letter uppercase
        /// </summary>
        /// <param name="text">The text containing placeholders.</param>
        /// <param name="replacements">Dictionary of placeholder names to values (including the colon prefix).</param>
        /// <returns>The text with all placeholders replaced.</returns>
        public string Replace(string text, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(text) || replacements == null || replacements.Count == 0)
            {
                return text ?? string.Empty;
            }

            return PlaceholderPattern.Replace(text, match =>
            {
                string placeholder = match.Value; // e.g., ":name"
                string placeholderName = match.Groups[1].Value; // e.g., "name"

                // Try to find a matching replacement (case-insensitive key lookup)
                string replacementValue = null;
                foreach (var kvp in replacements)
                {
                    // Remove the leading colon from the key if present for comparison
                    string keyWithoutColon = kvp.Key.StartsWith(":") ? kvp.Key.Substring(1) : kvp.Key;

                    if (string.Equals(keyWithoutColon, placeholderName, StringComparison.OrdinalIgnoreCase))
                    {
                        replacementValue = kvp.Value;
                        break;
                    }
                }

                if (replacementValue == null)
                {
                    return placeholder; // Return original if no replacement found
                }

                // Apply case transformation based on placeholder format
                PlaceholderCase caseType = DetectCase(placeholderName);
                return ApplyCase(replacementValue, caseType);
            });
        }

        /// <summary>
        /// Detects the case pattern of a placeholder name.
        /// </summary>
        private PlaceholderCase DetectCase(string placeholderName)
        {
            if (string.IsNullOrEmpty(placeholderName))
            {
                return PlaceholderCase.Lower;
            }

            bool hasLower = false;
            bool hasUpper = false;

            foreach (char c in placeholderName)
            {
                if (char.IsLetter(c))
                {
                    if (char.IsLower(c)) hasLower = true;
                    if (char.IsUpper(c)) hasUpper = true;
                }
            }

            if (hasUpper && !hasLower)
            {
                return PlaceholderCase.Upper;
            }
            else if (hasLower && !hasUpper)
            {
                return PlaceholderCase.Lower;
            }
            else if (hasUpper && hasLower && char.IsUpper(placeholderName[0]))
            {
                return PlaceholderCase.Pascal;
            }

            return PlaceholderCase.Lower;
        }

        /// <summary>
        /// Applies the specified case transformation to the value.
        /// </summary>
        private string ApplyCase(string value, PlaceholderCase caseType)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            switch (caseType)
            {
                case PlaceholderCase.Upper:
                    return value.ToUpperInvariant();

                case PlaceholderCase.Pascal:
                    if (value.Length == 1)
                    {
                        return value.ToUpperInvariant();
                    }
                    return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();

                case PlaceholderCase.Lower:
                default:
                    return value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Represents the case pattern of a placeholder.
        /// </summary>
        private enum PlaceholderCase
        {
            Lower,  // :name -> value as lowercase
            Upper,  // :NAME -> VALUE
            Pascal  // :Name -> Value
        }
    }
}
