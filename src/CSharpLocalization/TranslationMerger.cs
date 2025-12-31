using System.Collections.Generic;

namespace CSharpLocalization
{
    /// <summary>
    /// Provides deep merge functionality for translation dictionaries.
    /// </summary>
    public static class TranslationMerger
    {
        /// <summary>
        /// Deep merges two dictionaries. Values from overrideDict take precedence over baseDict.
        /// Nested dictionaries are merged recursively.
        /// </summary>
        /// <param name="baseDict">The base dictionary (lower priority).</param>
        /// <param name="overrideDict">The override dictionary (higher priority).</param>
        /// <returns>A new dictionary containing the merged values.</returns>
        public static Dictionary<string, object> DeepMerge(
            Dictionary<string, object> baseDict,
            Dictionary<string, object> overrideDict)
        {
            if (baseDict == null && overrideDict == null)
            {
                return new Dictionary<string, object>();
            }

            if (baseDict == null)
            {
                return new Dictionary<string, object>(overrideDict);
            }

            if (overrideDict == null)
            {
                return new Dictionary<string, object>(baseDict);
            }

            var result = new Dictionary<string, object>(baseDict);

            foreach (var kvp in overrideDict)
            {
                if (result.TryGetValue(kvp.Key, out object existingValue))
                {
                    // If both values are dictionaries, merge them recursively
                    if (existingValue is Dictionary<string, object> existingDict &&
                        kvp.Value is Dictionary<string, object> overrideNested)
                    {
                        result[kvp.Key] = DeepMerge(existingDict, overrideNested);
                    }
                    else
                    {
                        // Override the value
                        result[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // Add new key
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
    }
}
